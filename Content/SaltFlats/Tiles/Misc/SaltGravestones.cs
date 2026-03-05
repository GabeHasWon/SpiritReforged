using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.SaltFlats.Biome;
using SpiritReforged.Content.Savanna.Biome;

namespace SpiritReforged.Content.Savanna.Tiles;

public class SaltGravestones : ILoadable
{
	public static int DesertGravesetIndex;
	public static int IceGravesetIndex;
	public static int SaltGravesetIndex;

	public void Load(Mod mod)
	{
		if (CrossMod.Fables.Enabled && false)
		{
			DesertGravesetIndex = (int)CrossMod.Fables.Instance.Call("gravestones.getGravesetIndex", "desert");
			IceGravesetIndex = (int)CrossMod.Fables.Instance.Call("gravestones.getGravesetIndex", "ice");

			//Technically not necessary as we do that for the savanna ones but there's no harm in doing it multiple times
			CrossMod.Fables.Instance.Call("gravestones.registerSelectorSheet", mod, "SpiritReforged/Assets/Textures/FablesCrossmodGravestoneUI");

			//Salt
			List<int> normalGraveProjs = (List<int>)CrossMod.Fables.Instance.Call("gravestones.autoloadGraveSet", mod, "SpiritReforged/Content/SaltFlats/Tiles/Misc/", "SaltGrave", false,
				new string[] { "SaltGraveMarker", "SaltGravestone", "SaltTombstone", "SaltHeadstone" },
				new Color(128, 128, 128),
				DustID.Stone);

			List<int> gildedGraveProjs = (List<int>)CrossMod.Fables.Instance.Call("gravestones.autoloadGraveSet", mod, "SpiritReforged/Content/SaltFlats/Tiles/Misc/", "ModernGrave", true,
				new string[] { "ModernHeadstone", "ModernGravestone", "ModernTombscreen", "ModernObelisk" },
				new Color(251, 230, 237),
				DustID.BubbleBurst_White);

			LocalizedText localizationKey = mod.GetLocalization("Tiles.FablesGraves.SetNames.SaltFlats");
			SaltGravesetIndex = (int)CrossMod.Fables.Instance.Call("gravestones.registerGraveset", mod, "Salt", 1, localizationKey, normalGraveProjs, gildedGraveProjs,
				(Player p) => PlayerInFlats(p), 1.3f, (Player p, bool[] unlocks) => HasPlayerUnlockedDesertOrIceGravestones(p, unlocks));
		}
	}

	public static bool PlayerInFlats(Player player)
	{
		return player.InModBiome<SaltBiome>();
	}

	public static bool HasPlayerUnlockedDesertOrIceGravestones(Player player, bool[] unlockedGravestoneIndices)
	{
		return unlockedGravestoneIndices[DesertGravesetIndex] || unlockedGravestoneIndices[IceGravesetIndex];
	}

	public void Unload() { }
}