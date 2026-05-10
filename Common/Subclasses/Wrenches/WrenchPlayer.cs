using SpiritReforged.Common.Misc;
using System.Runtime.CompilerServices;
using Terraria.Audio;
using Terraria.ModLoader.IO;
using Terraria.UI.Chat;

namespace SpiritReforged.Common.Subclasses.Wrenches;

/// <summary>
/// Controls wrench (sentry onhit) functionality and the scrap UI element on the mouse.
/// </summary>
internal class WrenchPlayer : ModPlayer
{
	const int MeleeHitTime = 60000;

	/// <summary>
	/// Sparse array where projectiles set their own flag in <see cref="ISentryHitEntity.SentryHitProjectile.PostAI(Projectile)"/> for checking in <see cref="PostUpdateEquips"/>.
	/// </summary>
	internal readonly bool[] IsSentryHitProjectile = new bool[Main.maxProjectiles];

	/// <summary>
	/// Internal sparse array for per-projectile immune frames.
	/// </summary>
	private readonly int[] _sentryImmune = new int[Main.maxProjectiles];

	public int StoredScrap { get; private set; }

	private int _scrapGrabTimer = 0;
	private int _lastScrapAmount = 0;

	public override void Load() => CustomCursor.DrawCustomCursor += AddScrapIcon;

	private static void AddScrapIcon(bool thick)
	{
		if (thick || Main.LocalPlayer.HeldItem.ModItem is not IScrapDropEntity and not ISentryHitEntity || Main.LocalPlayer.mouseInterface)
			return;

		const int Offset = 30;

		int type = ModContent.ItemType<ScrapItem>();
		Main.instance.LoadItem(type);
		Texture2D tex = TextureAssets.Item[type].Value;
		Vector2 pos = Main.MouseScreen;
		float opacity = 1f;

		if (pos.X < Main.screenWidth - Offset * 2)
			pos.X += Offset;
		else
			pos.X -= Offset;

		WrenchPlayer plr = Main.LocalPlayer.GetModPlayer<WrenchPlayer>();
		string text = "x" + plr.StoredScrap.ToString();
		Color color = Main.MouseTextColorReal;
		float adjustedTimer = MathF.Max(0, plr._scrapGrabTimer * 0.02f);
		float textScale = 1f + adjustedTimer;

		if (text == "x0")
		{
			text = "x";
			opacity = 0.6f;
			color = new Color(255, 140, 140);
		}

		color = Color.Lerp(color, plr._lastScrapAmount >= 0 ? Color.Yellow : Color.Red, plr._scrapGrabTimer / 15f);
		Main.spriteBatch.Draw(tex, pos, Color.White * opacity);

		pos += new Vector2(16, 14);
		ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value, text, pos, color * opacity, adjustedTimer, Vector2.Zero, new(textScale));
	}

	/// <summary>
	/// Allows the amount of scrap to be modified. This is done to make sure <see cref="_scrapGrabTimer"/> can be set to animate the UI properly.
	/// </summary>
	/// <param name="amount"></param>
	public void ModifyScrap(int amount)
	{
		StoredScrap += amount;
		_lastScrapAmount = amount;
		_scrapGrabTimer = 15;
	}

	public override void PostUpdateEquips() 
	{
		_scrapGrabTimer--;

		bool hasItem = Player.itemAnimation > 0 && !Player.ItemAnimationJustStarted;
		Rectangle drawHitbox = Item.GetDrawHitbox(Player.HeldItem.type, Player);
		GetItemHitbox(Player, Player.HeldItem, drawHitbox, out _, out Rectangle hitbox);

		for (int i = 0; i < _sentryImmune.Length; ++i)
		{
			ref int timer = ref _sentryImmune[i];

			if (timer <= 0)
				continue;

			if (timer == MeleeHitTime) // Melee hits are hardcoded to only reset when the item being used "resets"
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

			if (hasItem && hitbox.Intersects(proj.Hitbox) && Player.HeldItem.ModItem is ISentryHitEntity wrench && wrench.CanHitSentry(Player, proj))
				OnHitSentry(wrench, proj, true);

			for (int i = 0; i < IsSentryHitProjectile.Length; ++i)
			{
				if (IsSentryHitProjectile[i] && Main.projectile[i].ModProjectile is ISentryHitEntity wrenchProj && wrenchProj.CanHitSentry(Player, proj))
					OnHitSentry(wrenchProj, proj, false);

				IsSentryHitProjectile[i] = false;
			}
		}
	}

	private void OnHitSentry(ISentryHitEntity wrench, Projectile proj, bool isMelee)
	{
		wrench.OnHitSentry(Player, proj);
		_sentryImmune[proj.whoAmI] = 15; // Immune time defaults to 15 frames (1/4th of a second)...

		wrench.ModifySentryImmuneTime(proj, ref _sentryImmune[proj.whoAmI], ref isMelee);

		if (isMelee) // ...unless the item or self-marked projectile counts as "melee", where it will last for as long as the item is being used
			_sentryImmune[proj.whoAmI] = MeleeHitTime;

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
