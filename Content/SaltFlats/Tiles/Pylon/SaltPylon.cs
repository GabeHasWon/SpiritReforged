using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.SaltFlats.Biome;

namespace SpiritReforged.Content.SaltFlats.Tiles.Pylon;

public class SaltPylon : PylonTile
{
	public override void SetStaticDefaults(LocalizedText mapEntry) => AddMapEntry(new Color(190, 150, 205), mapEntry);
	public override bool ValidTeleportCheck_BiomeRequirements(TeleportPylonInfo pylonInfo, SceneMetrics sceneData) => SceneTileCounter.GetSurvey<SaltBiome>().Success;
	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) => (r, g, b) = (.72f, .5f, .8f);
	public override NPCShop.Entry GetNPCShopEntry() => new(ModItem.Type, Condition.HappyEnoughToSellPylons, SpiritConditions.InSaltFlats);
}