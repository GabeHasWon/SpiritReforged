using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon.PresetTiles;

namespace SpiritReforged.Content.Space;

public class SkyPylon : PylonTile
{
	public override void SetStaticDefaults(LocalizedText mapEntry) => AddMapEntry(new Color(241, 196, 37), mapEntry);
	public override bool ValidTeleportCheck_BiomeRequirements(TeleportPylonInfo pylonInfo, SceneMetrics sceneData) => pylonInfo.PositionInTiles.Y < Main.worldSurface * 0.35f;
	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) => (r, g, b) = (.77f, .77f, .55f);
	public override NPCShop.Entry GetNPCShopEntry() => new(ModItem.Type, Condition.HappyEnoughToSellPylons, SpiritConditions.InSpace);
}