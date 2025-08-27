using Terraria.DataStructures;

namespace SpiritReforged.Common.TileCommon.PresetTiles;

public abstract class PianoTile : FurnitureTile
{
	public override void SetItemDefaults(ModItem item) => item.Item.value = Item.sellPrice(copper: 60);

	public override void AddItemRecipes(ModItem item)
	{
		if (Info.Material != ItemID.None)
			item.CreateRecipe().AddIngredient(ItemID.Bone, 4).AddIngredient(Info.Material, 15).AddIngredient(ItemID.Book).AddTile(TileID.Sawmill).Register();
	}

	public override void StaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
		TileObjectData.newTile.Origin = new Point16(2, 1);
		TileObjectData.newTile.CoordinateHeights = [16, 16];
		TileObjectData.addTile(Type);

		AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTable);
		AddMapEntry(new Color(100, 100, 60), Language.GetText("ItemName.Piano"));
		AdjTiles = [TileID.Pianos];
		DustType = -1;
	}
}
