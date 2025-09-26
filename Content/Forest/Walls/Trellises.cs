using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.WallCommon;

namespace SpiritReforged.Content.Forest.Walls;

public class Trellis : ModWall, IAutoloadWallItem
{
	public virtual void AddItemRecipes(ModItem item)
	{
		item.CreateRecipe(4).AddIngredient(ItemID.Wood).AddTile(TileID.Sawmill).Register();
		item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<TrellisTwo>(), 4).AddTile(TileID.Sawmill).Register();
	}

	public override void SetStaticDefaults()
	{
		Main.wallHouse[Type] = true;
		Main.wallLight[Type] = true;
		DustType = DustID.WoodFurniture;

		var entryColor = new Color(72, 50, 35);
		AddMapEntry(entryColor);
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
}

public class TrellisBoreal : ModWall, IAutoloadWallItem
{
	public virtual void AddItemRecipes(ModItem item)
	{
		item.CreateRecipe(4).AddIngredient(ItemID.BorealWood).AddTile(TileID.Sawmill).Register();
		item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<TrellisTwoBoreal>(), 4).AddTile(TileID.Sawmill).Register();
	}

	public override void SetStaticDefaults()
	{
		Main.wallHouse[Type] = true;
		Main.wallLight[Type] = true;
		DustType = DustID.BorealWood;

		var entryColor = new Color(72, 60, 50);
		AddMapEntry(entryColor);
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
}

public class TrellisEbonwood : ModWall, IAutoloadWallItem
{
	public virtual void AddItemRecipes(ModItem item)
	{
		item.CreateRecipe(4).AddIngredient(ItemID.Ebonwood).AddTile(TileID.Sawmill).Register();
		item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<TrellisTwoEbonwood>(), 4).AddTile(TileID.Sawmill).Register();
	}

	public override void SetStaticDefaults()
	{
		Main.wallHouse[Type] = true;
		Main.wallLight[Type] = true;
		DustType = DustID.Ebonwood;

		var entryColor = new Color(52, 50, 62);
		AddMapEntry(entryColor);
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
}

public class TrellisMahogany : ModWall, IAutoloadWallItem
{
	public virtual void AddItemRecipes(ModItem item)
	{
		item.CreateRecipe(4).AddIngredient(ItemID.RichMahogany).AddTile(TileID.Sawmill).Register();
		item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<TrellisTwoMahogany>(), 4).AddTile(TileID.Sawmill).Register();
	}

	public override void SetStaticDefaults()
	{
		Main.wallHouse[Type] = true;
		Main.wallLight[Type] = true;
		DustType = DustID.RichMahogany;

		var entryColor = new Color(70, 42, 44);
		AddMapEntry(entryColor);
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
}

public class TrellisPalm : ModWall, IAutoloadWallItem
{
	public virtual void AddItemRecipes(ModItem item)
	{
		item.CreateRecipe(4).AddIngredient(ItemID.PalmWood).AddTile(TileID.Sawmill).Register();
		item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<TrellisTwoPalm>(), 4).AddTile(TileID.Sawmill).Register();
	}

	public override void SetStaticDefaults()
	{
		Main.wallHouse[Type] = true;
		Main.wallLight[Type] = true;
		DustType = DustID.PalmWood;

		var entryColor = new Color(102, 75, 34);
		AddMapEntry(entryColor);
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
}

public class TrellisPearlwood : ModWall, IAutoloadWallItem
{
	public virtual void AddItemRecipes(ModItem item)
	{
		item.CreateRecipe(4).AddIngredient(ItemID.Pearlwood).AddTile(TileID.Sawmill).Register();
		item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<TrellisTwoPearlwood>(), 4).AddTile(TileID.Sawmill).Register();
	}

	public override void SetStaticDefaults()
	{
		Main.wallHouse[Type] = true;
		Main.wallLight[Type] = true;
		DustType = DustID.Pearlwood;

		var entryColor = new Color(84, 80, 75);
		AddMapEntry(entryColor);
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
}

public class TrellisShadewood : ModWall, IAutoloadWallItem
{
	public virtual void AddItemRecipes(ModItem item)
	{
		item.CreateRecipe(4).AddIngredient(ItemID.Shadewood).AddTile(TileID.Sawmill).Register();
		item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<TrellisTwoShadewood>(), 4).AddTile(TileID.Sawmill).Register();
	}

	public override void SetStaticDefaults()
	{
		Main.wallHouse[Type] = true;
		Main.wallLight[Type] = true;
		DustType = DustID.Shadewood;

		var entryColor = new Color(54, 62, 70);
		AddMapEntry(entryColor);
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
}