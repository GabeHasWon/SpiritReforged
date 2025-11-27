using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using System.Linq;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.Tiles;

public class ScarabTablet : ModTile
{
	public class ScarabTabletOneItem : ModItem
	{
		public override void AddRecipes() => CreateRecipe().AddIngredient(AutoContent.ItemType<RedSandstoneBrick>(), 12).AddTile(TileID.WorkBenches).Register();
		public override void SetDefaults() => Item.DefaultToPlaceableTile(ModContent.TileType<ScarabTablet>(), 0);
	}

	public class ScarabTabletTwoItem : ModItem
	{
		public override void AddRecipes() => CreateRecipe().AddIngredient(AutoContent.ItemType<RedSandstoneBrick>(), 12).AddTile(TileID.WorkBenches).Register();
		public override void SetDefaults() => Item.DefaultToPlaceableTile(ModContent.TileType<ScarabTablet>(), 1);
	}

	public override void SetStaticDefaults()
	{
		const int width = 6;
		const int height = 4;

		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileID.Sets.FramesOnKillWall[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Width = width;
		TileObjectData.newTile.Height = height;
		TileObjectData.newTile.CoordinateHeights = [.. Enumerable.Repeat(16, height)];
		TileObjectData.newTile.Origin = new(width / 2, height / 2);

		TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
		TileObjectData.newTile.AnchorTop = AnchorData.Empty;
		TileObjectData.newTile.AnchorWall = true;
		TileObjectData.addTile(Type);

		DustType = DustID.DynastyShingle_Red;
		AddMapEntry(FurnitureTile.CommonColor, Language.GetText("MapObject.Painting"));
	}
}