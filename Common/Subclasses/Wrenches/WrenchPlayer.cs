using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Common.Subclasses.Wrenches;

/// <summary> Controls wrench (sentry onhit) functionality and the scrap UI element on the mouse. </summary>
public sealed class WrenchPlayer : ModPlayer
{
	public const int SCRAP_GRAB_MAX = 15;

	/// <summary> Internal sparse array for per-projectile immune frames. </summary>
	public readonly int[] sentryImmune = new int[Main.maxProjectiles];

	public int StoredScrap
	{
		get => _storedScrap;
		set
		{
			scrapGrabTimer = SCRAP_GRAB_MAX;
			_lastScrapAmount = _storedScrap;
			_uiDisplayTime = 0;

			_storedScrap = value;
		}
	}

	public static bool HoldingWrench
	{
		get
		{
			Item heldItem = Main.LocalPlayer.HeldItem;
			return heldItem.ModItem is IHitSentry || ProjectileLoader.GetProjectile(heldItem.shoot) is IHitSentry;
		}
	}

	public int scrapGrabTimer;

	private int _storedScrap;
	private int _lastScrapAmount;
	private int _uiDisplayTime;

	public override void Load() => CustomCursor.DrawCustomCursor += DrawScrapIcon;

	private static void DrawScrapIcon(bool thick)
	{
		const int inactive_fade_time = 120;

		if (thick && !Main.LocalPlayer.mouseInterface && HoldingWrench)
		{
			SpriteBatch spriteBatch = Main.spriteBatch;
			Texture2D texture = ScrapPickup.WorldIcon.Value;
			Texture2D gridTexture = AssetLoader.LoadedTextures["GridPattern"].Value;

			Rectangle source = texture.Frame(3, 1, 0, 0, -2, 0);
			Vector2 position = Main.MouseScreen + new Vector2(30);

			if (Main.LocalPlayer.TryGetModPlayer(out WrenchPlayer wrenchPlayer))
			{
				float opacity = 0.2f + EaseFunction.EaseCircularOut.Ease(1f - Math.Min(wrenchPlayer._uiDisplayTime / (float)inactive_fade_time, 1)) * 0.8f;
				float lerp = wrenchPlayer.scrapGrabTimer / (float)SCRAP_GRAB_MAX;
				float rotation = EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 60f) * 0.1f;
				bool increase = wrenchPlayer._lastScrapAmount < wrenchPlayer.StoredScrap;

				Color lerpColor = Color.Lerp(Color.Yellow, increase ? Color.White : Color.PaleVioletRed, lerp) * opacity;
				Vector2 iconOffset = new Vector2(0, lerp * (increase ? 5 : -5)) + Vector2.UnitY * EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 50f);

				DrawHelpers.DrawOutline(default, default, default, default, (offset) =>
				{
					Texture2D solid = TextureColorCache.ColorSolid(texture, Color.White);
					float outlineOpacity = EaseFunction.EaseCubicOut.Ease(1f - Math.Min(wrenchPlayer._uiDisplayTime / (float)inactive_fade_time, 1));

					spriteBatch.Draw(solid, position + offset - iconOffset, source, Color.Yellow.Additive() * outlineOpacity, rotation, source.Size() / 2, 1, 0, 0);
				});

				spriteBatch.Draw(texture, position - iconOffset, source, Color.White * opacity, rotation, source.Size() / 2, 1, 0, 0);
				Utils.DrawBorderString(spriteBatch, wrenchPlayer.StoredScrap.ToString(), position + new Vector2(4, 0), lerpColor, 1 + lerp * 0.2f, 0, 0.3f);
			}
		}
	}

	public override void PostUpdateEquips() 
	{
		if (scrapGrabTimer > 0)
			scrapGrabTimer--;

		if (HoldingWrench)
			_uiDisplayTime++;
		else
			_uiDisplayTime = 0;

		for (int type = 0; type < sentryImmune.Length; type++)
		{
			if (sentryImmune[type] > 0)
				sentryImmune[type]--; //Decrease cooldowns
		}
	}

	public override void SaveData(TagCompound tag) => tag.Add("scrap", StoredScrap);
	public override void LoadData(TagCompound tag) => StoredScrap = tag.GetInt("scrap");
}