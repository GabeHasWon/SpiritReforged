using SpiritReforged.Common.TileCommon;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Misc;

public class HerosMemorialStatue : ModTile, IAutoloadTileItem
{
	void IAutoloadTileItem.SetItemDefaults(ModItem item)
	{
		item.Item.rare = ItemRarityID.Orange;
		item.Item.Size = new Vector2(30, 48);
	}

	void IAutoloadTileItem.AddItemRecipes(ModItem item)
	{
		item.CreateRecipe()
			.AddIngredient(ItemID.StoneBlock, 50)
			.AddTile(TileID.HeavyWorkBench)
			.Register();
	}

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
		TileObjectData.newTile.Width = 3;
		TileObjectData.newTile.Height = 5;
		TileObjectData.newTile.Origin = new Point16(2, 3);
		TileObjectData.newTile.CoordinateHeights = [16, 16, 16, 16, 18];
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
		TileObjectData.newTile.StyleWrapLimit = 2;
		TileObjectData.newTile.StyleMultiplier = 2;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
		TileObjectData.addAlternate(1);
		TileObjectData.addTile(Type);

		LocalizedText name = CreateMapEntryName();
		AddMapEntry(new Color(216, 216, 216), name);

		DustType = DustID.Stone;
	}
}