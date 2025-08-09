namespace SpiritReforged.Content.Desert.Oasis;

public class UndergroundOasisScene : ModSceneEffect
{
	public override SceneEffectPriority Priority => SceneEffectPriority.BiomeLow;
	public override int Music => MusicID.Desert;
	public override bool IsSceneEffectActive(Player player) => UndergroundOasisBiome.InUndergroundOasis(player);

	public override void SpecialVisuals(Player player, bool isActive)
	{
	}
}