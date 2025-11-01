namespace SpiritReforged.Content.Desert.Biome;

/// <summary> Solely used to override the underground desert's background with <see cref="UndergroundDesertBackground"/>. </summary>
public class UndergroundDesertScene : ModSceneEffect
{
	public class UndergroundDesertBackground : ModUndergroundBackgroundStyle
	{
		public override void FillTextureArray(int[] textureSlots)
		{
			textureSlots[0] = BackgroundTextureLoader.GetBackgroundSlot(Mod, "Assets/Textures/Backgrounds/SubDesertBackground0");
			textureSlots[1] = BackgroundTextureLoader.GetBackgroundSlot(Mod, "Assets/Textures/Backgrounds/SubDesertBackground1");
			textureSlots[2] = BackgroundTextureLoader.GetBackgroundSlot(Mod, "Assets/Textures/Backgrounds/SubDesertBackground2");
			textureSlots[3] = BackgroundTextureLoader.GetBackgroundSlot(Mod, "Assets/Textures/Backgrounds/SubDesertBackground1");
		}
	}

	public override SceneEffectPriority Priority => SceneEffectPriority.BiomeLow;
	public override ModUndergroundBackgroundStyle UndergroundBackgroundStyle => ModContent.GetInstance<UndergroundDesertBackground>();
	public override bool IsSceneEffectActive(Player player) => player.ZoneUndergroundDesert || player.Center.Y / 16 > Main.worldSurface && player.ZoneDesert;
}