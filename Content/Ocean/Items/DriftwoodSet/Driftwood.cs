using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Content.Ocean.Tiles;

namespace SpiritReforged.Content.Ocean.Items.DriftwoodSet;

[FromClassic("Driftwood1Item")]
public class SmallDriftwoodItem : ModItem
{
	public override string Texture => base.Texture.Replace(nameof(SmallDriftwoodItem), "Driftwood0");

	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<SmallDriftwoodTile>());
		Item.width = 30;
		Item.height = 18;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(AutoContent.ItemType<Driftwood>(), 10).Register();
}

public class SmallDriftwoodTile : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileTable[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Height = 2;
		TileObjectData.newTile.Width = 3;
		TileObjectData.newTile.CoordinateHeights = [16, 16];
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
		TileObjectData.newTile.StyleWrapLimit = 2;
		TileObjectData.newTile.StyleMultiplier = 2;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
		TileObjectData.addAlternate(1); //facing right will use the second texture style
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(69, 54, 43));
		DustType = DustID.BorealWood;
	}

	public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY) => offsetY = 2;
}

[FromClassic("Driftwood2Item")]
public class MediumDriftwoodItem : ModItem
{
	public override string Texture => base.Texture.Replace(nameof(MediumDriftwoodItem), "Driftwood1");

	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<MediumDriftwoodTile>());
		Item.width = 30;
		Item.height = 18;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(AutoContent.ItemType<Driftwood>(), 20).Register();
}

public class MediumDriftwoodTile : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileTable[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Height = 2;
		TileObjectData.newTile.Width = 4;
		TileObjectData.newTile.CoordinateHeights = [16, 16];
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
		TileObjectData.newTile.StyleWrapLimit = 2;
		TileObjectData.newTile.StyleMultiplier = 2;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
		TileObjectData.addAlternate(1); //facing right will use the second texture style
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(69, 54, 43));
		DustType = DustID.BorealWood;
	}

	public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY) => offsetY = 2;
}

[FromClassic("Driftwood3Item")]
public class LargeDriftwoodItem : ModItem
{
	public override string Texture => base.Texture.Replace(nameof(LargeDriftwoodItem), "Driftwood2");

	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<LargeDriftwoodTile>());
		Item.width = 30;
		Item.height = 18;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(AutoContent.ItemType<Driftwood>(), 25).Register();
}

public class LargeDriftwoodTile : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileTable[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Height = 2;
		TileObjectData.newTile.Width = 4;
		TileObjectData.newTile.CoordinateHeights = [16, 16];
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
		TileObjectData.newTile.StyleWrapLimit = 2;
		TileObjectData.newTile.StyleMultiplier = 2;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
		TileObjectData.addAlternate(1); //facing right will use the second texture style
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(69, 54, 43));
		DustType = DustID.BorealWood;
	}

	public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY) => offsetY = 2;
}