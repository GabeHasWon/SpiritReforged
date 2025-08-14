using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes;
using Terraria.Graphics.Effects;

namespace SpiritReforged.Content.Desert.Oasis;

public class UndergroundOasisScene : ModSceneEffect
{
	private float _effectIntensity;

	public override SceneEffectPriority Priority => SceneEffectPriority.BiomeLow;
	public override int Music => MusicID.Desert;
	public override ModUndergroundBackgroundStyle UndergroundBackgroundStyle => ModContent.GetInstance<UndergroundOasisBackground>();

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
				Filters.Scene["Solar"].GetShader().UseIntensity(1.2f);
			}
		}
		else if (Filters.Scene["Solar"].IsActive())
		{
			Filters.Scene.Deactivate("Solar");
		}
	}
}

public class UndergroundOasisBackground : ModUndergroundBackgroundStyle, IWaterStyle
{
	public static readonly Asset<Texture2D> OpenBackground1 = ModContent.Request<Texture2D>("SpiritReforged/Assets/Textures/Backgrounds/OasisBackground1");
	public static readonly Asset<Texture2D> OpenBackground2 = ModContent.Request<Texture2D>("SpiritReforged/Assets/Textures/Backgrounds/OasisBackground2");

	public override void Load() => On_Main.DrawBackground += DrawOpenBackground;
	private static void DrawOpenBackground(On_Main.orig_DrawBackground orig, Main self)
	{
		//const int roundWidth = 128; //160 - 32
		//const int roundHeight = 96;
		const float scrollSpeed = 0.137f;

		orig(self);

		if (Main.LocalPlayer.Center.Y / 16 > Main.worldSurface && Main.LocalPlayer.ZoneDesert)
		{
			var sb = Main.spriteBatch;
			var background = OpenBackground1.Value;
			var subBackground = OpenBackground2.Value;

			foreach (var area in UndergroundOasisBiome.OasisAreas)
			{
				var worldCoords = area.Center.ToWorldCoordinates();
				var roundedPos = worldCoords; //new Vector2((int)(worldCoords.X / roundWidth) * roundWidth, (int)(worldCoords.Y / roundHeight) * roundHeight);

				Vector2 scroll = new((Main.screenPosition.X - roundedPos.X) * Main.caveParallax * scrollSpeed, 0);
				Vector2 instantParallax = new(MathHelper.Clamp((Main.LocalPlayer.Center.X - roundedPos.X) * 0.03f, -16, 16), 0);

				var center = roundedPos + scroll + new Vector2(120, -100);

				sb.Draw(TextureAssets.MagicPixel.Value, center + instantParallax - Main.screenPosition + TileExtensions.TileOffset, subBackground.Bounds, new Color(0.02f, 0.025f, 0.025f), 0, subBackground.Size() / 2, 1, default, 0);

				DrawBackgroundSliced(sb, subBackground, center + instantParallax, Color.White * 0.7f);
				DrawBackgroundSliced(sb, background, center);
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

	public override void FillTextureArray(int[] textureSlots) => ModContent.GetInstance<UndergroundDesertBackground>().FillTextureArray(textureSlots);

	public void ForceWaterStyle(ref int style)
	{
		if (style == WaterStyleID.UndergroundDesert && UndergroundOasisBiome.InUndergroundOasis(Main.LocalPlayer))
			style = WaterStyleID.Desert;
	}
}