using SpiritReforged.Common;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;

namespace SpiritReforged.Content.Ocean.Tiles.Furniture;

public class DriftwoodSet : FurnitureSet
{
	public override string Name => "Driftwood";
	public override FurnitureTile.IFurnitureData GetInfo(FurnitureTile tile) => new FurnitureTile.LightedInfo(tile.AutoModItem(), AutoContent.ItemType<SaltBlockDull>(), Color.Orange.ToVector3() / 255f, DustID.t_BorealWood, true);
	public override bool Autoload(FurnitureTile tile) => tile is not SofaTile and not ChestTile;

	public override void OnPostSetupContent()
	{
		var mod = SpiritReforgedMod.Instance;
		int workbenchType = mod.Find<ModItem>("DriftwoodWorkBenchItem").Type;

		SpiritClassic.AddItemReplacement("DriftwoodWorkbenchItem", workbenchType);
		SpiritSets.Workbench[workbenchType] = true;
	}
}

public class DriftwoodBarrel : BarrelTile
{
	public override IFurnitureData Info => new BasicInfo(this.AutoModItem(), AutoContent.ItemType<Driftwood>());
}

public class DriftwoodBench : SofaTile
{
	public override IFurnitureData Info => new BasicInfo(this.AutoModItem(), AutoContent.ItemType<Driftwood>());
}