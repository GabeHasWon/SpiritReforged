using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.SaltFlats.Biome;
using SpiritReforged.Content.Savanna.Biome;

namespace SpiritReforged.Content.Savanna.Tiles;

public class SavannaGravestones : ILoadable
{
	public static int DesertGravesetIndex;
	public static int JungleGravesetIndex;
	public static int SavannaGravesetIndex;

	public void Load(Mod mod)
	{
		if (CrossMod.Fables.Enabled && false)
		{
			DesertGravesetIndex = (int)CrossMod.Fables.Instance.Call("gravestones.getGravesetIndex", "desert");
			JungleGravesetIndex = (int)CrossMod.Fables.Instance.Call("gravestones.getGravesetIndex", "jungle");

			CrossMod.Fables.Instance.Call("gravestones.registerSelectorSheet", mod, "SpiritReforged/Assets/Textures/FablesCrossmodGravestoneUI");

			//Savanna
			List<int> normalGraveProjs = (List<int>)CrossMod.Fables.Instance.Call("gravestones.autoloadGraveSet", mod, "SpiritReforged/Content/Savanna/Tiles/Misc/", "SavannaGrave", false, 
				new string[] { "DrywoodCrossGraveMarker", "DrywoodGraveMarker", "SavannaGravestone", "DrywoodTombstone" },
				new Color(142, 125, 106), 
				DustID.t_PearlWood);

			List<int> gildedGraveProjs = (List<int>)CrossMod.Fables.Instance.Call("gravestones.autoloadGraveSet", mod, "SpiritReforged/Content/Savanna/Tiles/Misc/", "TerracottaGrave", true,
				new string[] { "TerracottaObelisk", "TerracottaGravestone", "TerracottaGraveMarker", "TerracottaTombstone" },
				new Color(202, 85, 20),
				DustID.Clay);

			LocalizedText localizationKey = mod.GetLocalization("Tiles.FablesGraves.SetNames.Savanna");
			SavannaGravesetIndex = (int)CrossMod.Fables.Instance.Call("gravestones.registerGraveset", mod, "Savanna", 0, localizationKey, normalGraveProjs, gildedGraveProjs,
				(Player p) => PlayerInSavanna(p), 1.3f, (Player p, bool[] unlocks) => HasPlayerUnlockedDesertOrJungleGravestones(p, unlocks));
		}
	}

	public static bool PlayerInSavanna(Player player)
	{
		return player.InModBiome<SavannaBiome>();
	}

	public static bool HasPlayerUnlockedDesertOrJungleGravestones(Player player, bool[] unlockedGravestoneIndices)
	{
		return unlockedGravestoneIndices[DesertGravesetIndex] || unlockedGravestoneIndices[JungleGravesetIndex];
	}

	public void Unload() { }
}