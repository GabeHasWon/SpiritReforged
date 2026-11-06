using SpiritReforged.Content.Desert.NPCs.TownBeetle;
using SpiritReforged.Content.Glyphs;
using SpiritReforged.Content.Underground.NPCs;

namespace SpiritReforged.Common.ModCompat;

internal class CensusCompat : ModSystem
{
	public override bool IsLoadingEnabled(Mod mod) => CrossMod.Census.Enabled;

	public override void PostSetupContent()
	{
		var census = CrossMod.Census.Instance;

		RegisterEntry<PotterySlime>(census);
		RegisterEntry<Enchanter>(census);
		RegisterEntry<BeetleTownPet>(census);
	}

	public static void RegisterEntry<T>(Mod census) where T : ModNPC => census.Call("TownNPCCondition", ModContent.NPCType<T>(), ModContent.GetInstance<T>().GetLocalization("Census.SpawnCondition"));
}