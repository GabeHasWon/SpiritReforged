using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WallCommon;

namespace SpiritReforged.Content.Forest.Walls;

public class Trellis : ModWall, IAutoloadWallItem, ICheckItemUse
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
		AddEntry();
	}

	public virtual void AddEntry() => AddMapEntry(new Color(72, 50, 35));

	public override void RandomUpdate(int i, int j)
	{
		if (Main.rand.NextBool(9) && WorldGen.CountNearBlocksTypes(i, j, 1, 1, ModContent.TileType<TrellisVine>()) == 1)
			Placer.PlaceTile(i, j, Type).Send();
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
	public bool? CheckItemUse(int type, int i, int j)
	{
		if (type == ItemID.GrassSeeds)
		{
			Placer.PlaceTile<TrellisVine>(i, j).Send();
			return true;
		}

		return null;
	}
}

public class TrellisBoreal : Trellis
{
	public override void AddItemRecipes(ModItem item)
	{
		item.CreateRecipe(4).AddIngredient(ItemID.BorealWood).AddTile(TileID.Sawmill).Register();
		item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<TrellisTwoBoreal>(), 4).AddTile(TileID.Sawmill).Register();
	}

	public override void AddEntry()
	{
		DustType = DustID.BorealWood;
		AddMapEntry(new Color(72, 60, 50));
	}
}

public class TrellisEbonwood : Trellis
{
	public override void AddItemRecipes(ModItem item)
	{
		item.CreateRecipe(4).AddIngredient(ItemID.Ebonwood).AddTile(TileID.Sawmill).Register();
		item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<TrellisTwoEbonwood>(), 4).AddTile(TileID.Sawmill).Register();
	}

	public override void AddEntry()
	{
		DustType = DustID.Ebonwood;
		AddMapEntry(new Color(52, 50, 62));
	}
}

public class TrellisMahogany : Trellis
{
	public override void AddItemRecipes(ModItem item)
	{
		item.CreateRecipe(4).AddIngredient(ItemID.RichMahogany).AddTile(TileID.Sawmill).Register();
		item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<TrellisTwoMahogany>(), 4).AddTile(TileID.Sawmill).Register();
	}

	public override void AddEntry()
	{
		DustType = DustID.RichMahogany;
		AddMapEntry(new Color(70, 42, 44));
	}
}

public class TrellisPalm : Trellis
{
	public override void AddItemRecipes(ModItem item)
	{
		item.CreateRecipe(4).AddIngredient(ItemID.PalmWood).AddTile(TileID.Sawmill).Register();
		item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<TrellisTwoPalm>(), 4).AddTile(TileID.Sawmill).Register();
	}

	public override void AddEntry()
	{
		DustType = DustID.PalmWood;
		AddMapEntry(new Color(102, 75, 34));
	}
}

public class TrellisPearlwood : Trellis
{
	public override void AddItemRecipes(ModItem item)
	{
		item.CreateRecipe(4).AddIngredient(ItemID.Pearlwood).AddTile(TileID.Sawmill).Register();
		item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<TrellisTwoPearlwood>(), 4).AddTile(TileID.Sawmill).Register();
	}

	public override void AddEntry()
	{
		DustType = DustID.Pearlwood;
		AddMapEntry(new Color(84, 80, 75));
	}
}

public class TrellisShadewood : Trellis
{
	public override void AddItemRecipes(ModItem item)
	{
		item.CreateRecipe(4).AddIngredient(ItemID.Shadewood).AddTile(TileID.Sawmill).Register();
		item.CreateRecipe(4).AddIngredient(AutoContent.ItemType<TrellisTwoShadewood>(), 4).AddTile(TileID.Sawmill).Register();
	}

	public override void AddEntry()
	{
		DustType = DustID.Shadewood;
		AddMapEntry(new Color(54, 62, 70));
	}
}