using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Savanna.Biome;

namespace SpiritReforged.Content.Savanna.Tiles.Pylon;

public class SavannaPylon : PylonTile
{
	public override void SetStaticDefaults(LocalizedText mapEntry) => AddMapEntry(new Color(220, 185, 32), mapEntry);
	public override bool ValidTeleportCheck_BiomeRequirements(TeleportPylonInfo pylonInfo, SceneMetrics sceneData) => SceneTileCounter.GetSurvey<SavannaBiome>().Success;
	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) => (r, g, b) = (.8f, .72f, .4f);
	public override NPCShop.Entry GetNPCShopEntry() => new(ModItem.Type, Condition.HappyEnoughToSellPylons, SpiritConditions.InSavanna);
}