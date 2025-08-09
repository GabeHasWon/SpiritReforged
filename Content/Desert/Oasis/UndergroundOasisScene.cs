using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes;
using Terraria.Graphics.Effects;

namespace SpiritReforged.Content.Desert.Oasis;

public class UndergroundOasisScene : ModSceneEffect
{
	private float _effectIntensity;

	public override SceneEffectPriority Priority => SceneEffectPriority.BiomeLow;
	public override int Music => MusicID.Desert;

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
	public override void FillTextureArray(int[] textureSlots)
	{
		/*textureSlots[0] = 0;
		textureSlots[1] = 1;
		textureSlots[2] = 2;
		textureSlots[3] = 3;*/
	}

	public void ForceWaterStyle(ref int style)
	{
		if (style == WaterStyleID.UndergroundDesert && UndergroundOasisBiome.InUndergroundOasis(Main.LocalPlayer))
			style = WaterStyleID.Desert;
	}
}