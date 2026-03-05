using SpiritReforged.Common.WallCommon;

namespace SpiritReforged.Content.Savanna.Walls;

public class SavannaDirtWall : ModWall, IAutoloadUnsafeWall, IAutoloadWallItem
{
	public static int UnsafeType { get; private set; }

	public void AddItemRecipes(ModItem item)
	{
		var mod = SpiritReforgedMod.Instance; //Mod is null here, so get the instance manually

		item.CreateRecipe(4)
			.AddIngredient(mod.Find<ModItem>("SavannaDirtItem").Type)
			.AddTile(TileID.WorkBenches)
			.Register();

		//Allow wall items to be crafted back into base materials
		Recipe.Create(mod.Find<ModItem>("SavannaDirtItem").Type)
			.AddIngredient(item.Type, 4)
			.AddTile(TileID.WorkBenches)
			.Register();
	}

	public override void SetStaticDefaults()
	{
		Main.wallHouse[Type] = true;

		var entryColor = new Color(98, 39, 5);
		AddMapEntry(entryColor);

		var wall = Mod.Find<ModWall>(Name + "Unsafe");
		UnsafeType = wall.Type;
		wall.AddMapEntry(entryColor); //Set the unsafe wall's map entry
	}

	public override bool WallFrame(int i, int j, bool randomizeFrame, ref int style, ref int frameNumber)
	{
		//Inner part of the wall
		if (style == 15 && WorldGen.genRand.NextBool(70))
		{
			Tile t = Main.tile[i, j];
			t.WallFrameNumber = frameNumber;
			t.WallFrameX = (10 + frameNumber) * 36;
			t.WallFrameY = 4 * 36;
			return false;
		}

		return true;
	}
}