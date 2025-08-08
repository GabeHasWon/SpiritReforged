namespace SpiritReforged.Content.Desert.Oasis;

public class UndergroundOasisScene : ModSceneEffect
{
	public override SceneEffectPriority Priority => SceneEffectPriority.BiomeLow;
	public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Music/Duststorm" + (SpiritReforgedMod.SwapMusic ? "Otherworld" : ""));
	public override bool IsSceneEffectActive(Player player) => UndergroundOasisBiome.InUndergroundOasis(player);

	public override void SpecialVisuals(Player player, bool isActive)
	{
	}
}