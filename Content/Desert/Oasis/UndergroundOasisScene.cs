using SpiritReforged.Common.Easing;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes;
using Terraria.Graphics.Effects;

namespace SpiritReforged.Content.Desert.Oasis;

public class UndergroundOasisScene : ModSceneEffect
{
	private float _effectIntensity;

	public override SceneEffectPriority Priority => SceneEffectPriority.BiomeLow;
	public override int Music => MusicID.Desert;

	public static readonly Asset<Texture2D> OpenBackground1 = ModContent.Request<Texture2D>("SpiritReforged/Assets/Textures/Backgrounds/OasisBackground1");
	public static readonly Asset<Texture2D> OpenBackground2 = ModContent.Request<Texture2D>("SpiritReforged/Assets/Textures/Backgrounds/OasisBackground2");

	public override void Load() => On_Main.DrawBackground += DrawOpenBackground;
	private static void DrawOpenBackground(On_Main.orig_DrawBackground orig, Main self)
	{
		const int roundWidth = 40; //160 - 32
		const int roundHeight = 24;

		orig(self);

		Player player = Main.LocalPlayer;
		if (player.Center.Y / 16 > Main.worldSurface && Main.undergroundBackground == ModContent.GetInstance<UndergroundDesertBackground>().Slot)
		{
			var sb = Main.spriteBatch;
			var background = OpenBackground1.Value;
			var subBackground = OpenBackground2.Value;

			float parallax = 1f - Main.caveParallax;
			float transition = EaseFunction.EaseQuadOut.Ease(1f - Main.ugBackTransition);

			foreach (var area in UndergroundOasisBiome.OasisAreas)
			{
				var worldCoords = area.Center.ToWorldCoordinates();
				var roundedPos = new Vector2((int)(worldCoords.X / roundWidth) * roundWidth, (int)(worldCoords.Y / roundHeight) * roundHeight);

				Vector2 scroll = new((Main.screenPosition.X - roundedPos.X) * parallax, 0);
				Vector2 instantParallax = new(MathHelper.Clamp((player.Center.X - roundedPos.X) * 0.03f, -16, 16), 0);
				Vector2 center = roundedPos + scroll + new Vector2(16 + 110, 32 - 130);

				sb.Draw(TextureAssets.MagicPixel.Value, center + instantParallax - Main.screenPosition + TileExtensions.TileOffset, subBackground.Bounds, Color.Black * transition, 0, subBackground.Size() / 2, 1, default, 0);

				DrawBackgroundSliced(sb, subBackground, center + instantParallax, Color.White * 0.7f * transition);
				DrawBackgroundSliced(sb, background, center, Color.White * transition);
			}
		}

		static void DrawBackgroundSliced(SpriteBatch sb, Texture2D texture, Vector2 topLeft, Color? tint = null)
		{
			const int sliceScale = 8;
			topLeft -= texture.Size() / 2;

			for (int x = 0; x < texture.Width / sliceScale; x++)
			{
				for (int y = 0; y < texture.Height / sliceScale; y++)
				{
					var offset = new Vector2(x, y) * sliceScale;
					Vector3 light = Lighting.GetSubLight(topLeft + offset) * 0.9f;
					Rectangle source = new((int)offset.X, (int)offset.Y, sliceScale, sliceScale);

					sb.Draw(texture, topLeft + offset - Main.screenPosition + TileExtensions.TileOffset, source, (tint is Color finalTint) ? new Color(light).MultiplyRGB(finalTint) : new Color(light));
				}
			}
		}
	}

	public override bool IsSceneEffectActive(Player player) => UndergroundOasisBiome.InUndergroundOasis(player);
	public override void SpecialVisuals(Player player, bool isActive)
	{
		_effectIntensity = isActive ? Math.Min(_effectIntensity + 0.05f, 1) : Math.Max(_effectIntensity - 0.05f, 0);

		if (_effectIntensity > 0f) //Give the screen a warm tint
		{
			if (!Filters.Scene["Solar"].IsActive())
			{
				Filters.Scene.Activate("Solar");
			}
			else
			{
				Filters.Scene["Solar"].GetShader().UseTargetPosition(player.Center);
				float progress = MathHelper.Lerp(0f, 1f, _effectIntensity);
				Filters.Scene["Solar"].GetShader().UseProgress(progress);
				Filters.Scene["Solar"].GetShader().UseIntensity(1f);
			}
		}
		else if (Filters.Scene["Solar"].IsActive())
		{
			Filters.Scene.Deactivate("Solar");
		}

		if (isActive)
		{
			Main.LocalPlayer.GetModPlayer<FountainPlayer>().SetFountain(WaterStyleID.Desert);
		}
	}
}