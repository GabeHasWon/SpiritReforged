using SpiritReforged.Content.Underground.NPCs;
using static AssGen.Assets;

namespace SpiritReforged.Common.ModCompat;

internal class CensusCompat : ModSystem
{
	public override bool IsLoadingEnabled(Mod mod) => CrossMod.Census.Enabled;
	public override void PostSetupContent()
	{
		var census = CrossMod.Census.Instance;
		census.Call("TownNPCCondition", ModContent.NPCType<PotterySlime>(), ModContent.GetInstance<PotterySlime>().GetLocalization("Census.SpawnCondition"));
	}
}