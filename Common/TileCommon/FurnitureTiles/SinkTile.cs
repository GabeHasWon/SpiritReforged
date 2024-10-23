using Terraria.DataStructures;

namespace SpiritReforged.Common.TileCommon.FurnitureTiles;

public abstract class SinkTile : FurnitureTile
{
	public override void SetItemDefaults(ModItem item) => item.Item.value = Item.sellPrice(copper: 60);

	public override void AddItemRecipes(ModItem item)
	{
		if (CoreMaterial != ItemID.None)
			item.CreateRecipe()
			.AddIngredient(CoreMaterial, 6)
			.AddIngredient(ItemID.WaterBucket)
			.AddTile(TileID.WorkBenches)
			.Register();
	}

	public override void StaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new Point16(1, 1);
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(100, 100, 60), Language.GetText("MapObject.Sink"));
		AdjTiles = [TileID.Sinks];
		DustType = -1;
	}
}
