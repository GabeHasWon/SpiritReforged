using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using System.Runtime.CompilerServices;
using Terraria.Audio;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Common.Subclasses.Wrenches;

/// <summary> Controls wrench (sentry onhit) functionality and the scrap UI element on the mouse. </summary>
public sealed class WrenchPlayer : ModPlayer
{
	public const int SCRAP_GRAB_MAX = 15;
	private const int MELEE_HIT_TIME = 60000;

	/// <summary> Sparse array where projectiles set their own flag in <see cref="IHitSentry.SentryHitProjectile.PostAI(Projectile)"/> for checking in <see cref="PostUpdateEquips"/>. </summary>
	internal readonly bool[] IsSentryHitProjectile = new bool[Main.maxProjectiles];

	/// <summary> Internal sparse array for per-projectile immune frames. </summary>
	private readonly int[] _sentryImmune = new int[Main.maxProjectiles];

	public static bool DisplayUI => !Main.LocalPlayer.mouseInterface && Main.LocalPlayer.HeldItem.ModItem is IHitSentry; //REMOVE PROPERTY

	public int StoredScrap
	{
		get => _storedScrap;
		set
		{
			_scrapGrabTimer = SCRAP_GRAB_MAX;
			_lastScrapAmount = _storedScrap;
			_uiDisplayTime = 0;

			_storedScrap = value;
		}
	}

	private int _storedScrap;
	private int _scrapGrabTimer;
	private int _lastScrapAmount;
	private int _uiDisplayTime;

	public override void Load() => CustomCursor.DrawCustomCursor += DrawScrapIcon;

	private static void DrawScrapIcon(bool thick)
	{
		const int inactive_fade_time = 120;

		if (thick && !Main.LocalPlayer.mouseInterface && Main.LocalPlayer.HeldItem.ModItem is IHitSentry)
		{
			SpriteBatch spriteBatch = Main.spriteBatch;
			Texture2D texture = ScrapPickup.WorldIcon.Value;
			Texture2D gridTexture = AssetLoader.LoadedTextures["GridPattern"].Value;

			Rectangle source = texture.Frame(3, 1, 0, 0, -2, 0);
			Vector2 position = Main.MouseScreen + new Vector2(30);

			if (Main.LocalPlayer.TryGetModPlayer(out WrenchPlayer wrenchPlayer))
			{
				float opacity = 0.2f + EaseFunction.EaseCircularOut.Ease(1f - Math.Min(wrenchPlayer._uiDisplayTime / (float)inactive_fade_time, 1)) * 0.8f;
				float lerp = wrenchPlayer._scrapGrabTimer / (float)SCRAP_GRAB_MAX;
				float rotation = EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 60f) * 0.1f;
				bool increase = wrenchPlayer._lastScrapAmount < wrenchPlayer.StoredScrap;

				Color lerpColor = Color.Lerp(Color.Yellow, increase ? Color.White : Color.PaleVioletRed, lerp) * opacity;
				Vector2 iconOffset = new Vector2(0, lerp * (increase ? 5 : -5)) + Vector2.UnitY * EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 50f);

				DrawHelpers.DrawOutline(default, default, default, default, (offset) =>
				{
					Texture2D solid = TextureColorCache.ColorSolid(texture, Color.White);
					spriteBatch.Draw(solid, position + offset - iconOffset, source, Color.Yellow.Additive() * (opacity - 0.2f), rotation, source.Size() / 2, 1, 0, 0);
				});

				spriteBatch.Draw(texture, position - iconOffset, source, Color.White * opacity, rotation, source.Size() / 2, 1, 0, 0);

				Utils.DrawBorderString(spriteBatch, wrenchPlayer.StoredScrap.ToString(), position + new Vector2(4, 0), lerpColor, 1 + lerp * 0.2f, 0, 0.3f);
			}
		}
	}

	public override void PostUpdateEquips() 
	{
		if (_scrapGrabTimer > 0)
			_scrapGrabTimer--;

		if (Main.LocalPlayer.HeldItem.ModItem is IHitSentry)
			_uiDisplayTime++;
		else
			_uiDisplayTime = 0;

		bool hasItem = Player.itemAnimation > 0 && !Player.ItemAnimationJustStarted;
		Rectangle drawHitbox = Item.GetDrawHitbox(Player.HeldItem.type, Player);
		GetItemHitbox(Player, Player.HeldItem, drawHitbox, out _, out Rectangle hitbox);

		for (int i = 0; i < _sentryImmune.Length; ++i)
		{
			ref int timer = ref _sentryImmune[i];

			if (timer <= 0)
				continue;

			if (timer == MELEE_HIT_TIME) // Melee hits are hardcoded to only reset when the item being used "resets"
			{
				if (!hasItem) // Only decrement when the item is no longer in use or has been reused
					timer = 0;
			}
			else
				timer = Math.Max(timer - 1, 0);
		}
		
		foreach (Projectile proj in Main.ActiveProjectiles)
		{
			if (proj.owner != Player.whoAmI || !proj.sentry || _sentryImmune[proj.whoAmI] > 0)
				continue;

			if (hasItem && hitbox.Intersects(proj.Hitbox) && Player.HeldItem.ModItem is IHitSentry wrench && wrench.CanHitSentry(Player, proj))
				OnHitSentry(wrench, proj, true);

			for (int i = 0; i < IsSentryHitProjectile.Length; ++i)
			{
				if (IsSentryHitProjectile[i] && Main.projectile[i].ModProjectile is IHitSentry wrenchProj && wrenchProj.CanHitSentry(Player, proj))
					OnHitSentry(wrenchProj, proj, false);

				IsSentryHitProjectile[i] = false;
			}
		}
	}

	private void OnHitSentry(IHitSentry wrench, Projectile proj, bool isMelee)
	{
		wrench.OnHitSentry(Player, proj);
		_sentryImmune[proj.whoAmI] = 15; // Immune time defaults to 15 frames (1/4th of a second)...

		wrench.ModifySentryImmuneTime(proj, ref _sentryImmune[proj.whoAmI], ref isMelee);

		if (isMelee) // ...unless the item or self-marked projectile counts as "melee", where it will last for as long as the item is being used
			_sentryImmune[proj.whoAmI] = MELEE_HIT_TIME;

		SoundStyle sound = Main.rand.NextBool() ? SoundID.Item53 : SoundID.Item52;
		int dustType = DustID.MinecartSpark;
		int dustCount = 4;

		if (wrench.PreHitEffects(ref sound, ref dustType, ref dustCount))
		{
			SoundEngine.PlaySound(sound with { PitchRange = (-0.2f, 0.2f) });

			for (int i = 0; i < dustCount; ++i)
			{
				Dust dust = Main.dust[Dust.NewDust(proj.position, proj.width, proj.height, dustType)];
				dust.fadeIn = 2;
				dust.scale = 0.2f;
			}
		}
	}

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ItemCheck_GetMeleeHitbox")]
	public static extern void GetItemHitbox(Player player, Item sItem, Rectangle heldItemFrame, out bool dontAttack, out Rectangle itemRectangle);

	public override void SaveData(TagCompound tag) => tag.Add("scrap", StoredScrap);
	public override void LoadData(TagCompound tag) => StoredScrap = tag.GetInt("scrap");
}
