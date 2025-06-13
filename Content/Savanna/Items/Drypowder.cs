using SpiritReforged.Common.TileCommon.Tree;
using SpiritReforged.Content.Forest.Stargrass.Tiles;
using SpiritReforged.Content.Savanna.Tiles;
using SpiritReforged.Content.Savanna.Tiles.AcaciaTree;
using SpiritReforged.Content.Savanna.Walls;

namespace SpiritReforged.Content.Savanna.Items;

public class Drypowder : ModItem
{
	public override void SetStaticDefaults()
	{
		Item.ResearchUnlockCount = 99;
		ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.PurificationPowder;
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 14;
		Item.useAnimation = 15;
		Item.useTime = 10;
		Item.maxStack = Item.CommonMaxStack;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.UseSound = SoundID.Item1;
		Item.useTurn = true;
		Item.autoReuse = true;
		Item.consumable = true;
		Item.shoot = ModContent.ProjectileType<DrypowderSpray>();
		Item.shootSpeed = 5;
		Item.value = Item.sellPrice(copper: 20);
	}

	public override void AddRecipes()
	{
		CreateRecipe(5).AddIngredient(ItemID.PurificationPowder, 5).AddIngredient(ItemID.JungleGrassSeeds).Register();
		CreateRecipe(5).AddIngredient(ItemID.PurificationPowder, 5).AddIngredient<SavannaGrassSeeds>().Register();
	}
}

internal class DrypowderSpray : ModProjectile
{
	public override string Texture => AssetLoader.EmptyTexture;

	public override void SetDefaults() => Projectile.CloneDefaults(ProjectileID.PurificationPowder);
	public override void AI()
	{
		if (Projectile.velocity.Length() > 1.5f)
		{
			for (int i = 0; i < 2; i++)
			{
				var d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Sand, Projectile.velocity.X, Projectile.velocity.Y, Main.rand.Next(50, 150), Scale: Main.rand.NextFloat() + .75f);
				d.noGravity = true;
				d.fadeIn = 1.2f;
			}
		}

		Point pt = Projectile.Center.ToTileCoordinates();
		WorldGen.Convert(pt.X, pt.Y, SavannaConversion.ConversionType, 3);
	}

	public override bool? CanCutTiles() => false;
	public override bool? CanDamage() => false;
}

public class SavannaConversion : ModBiomeConversion
{
	public static int ConversionType { get; private set; }
	private static readonly Dictionary<int, int> Conversions = new()
	{
		{ TileID.Dirt, ModContent.TileType<SavannaDirt>() },
		{ TileID.Grass, ModContent.TileType<SavannaGrass>() },
		{ TileID.CorruptGrass, ModContent.TileType<SavannaGrassCorrupt>() },
		{ TileID.CrimsonGrass, ModContent.TileType<SavannaGrassCrimson>() },
		{ TileID.HallowedGrass, ModContent.TileType<SavannaGrassHallow>() },
		{ ModContent.TileType<StargrassTile>(), ModContent.TileType<SavannaGrass>() },
		{ TileID.Plants, ModContent.TileType<SavannaFoliage>() },
		{ TileID.CorruptPlants, ModContent.TileType<SavannaFoliageCorrupt>() },
		{ TileID.CrimsonPlants, ModContent.TileType<SavannaFoliageCrimson>() },
		{ TileID.HallowedPlants, ModContent.TileType<SavannaFoliageHallow>() },
		{ ModContent.TileType<StargrassFlowers>(), ModContent.TileType<SavannaFoliage>() }
	};

	public override void SetStaticDefaults()
	{
		ConversionType = Type;

		foreach (int key in Conversions.Keys)
			TileLoader.RegisterConversion(key, ConversionType, ConvertTiles);

		WallLoader.RegisterConversion(WallID.DirtUnsafe, ConversionType, ConvertWalls);
	}

	public static bool ConvertTiles(int i, int j, int type, int conversionType)
	{
		if (Conversions.TryGetValue(type, out int newType) && !ScanUpTree(i, j, newType))
			WorldGen.ConvertTile(i, j, newType);

		return false;
	}

	public static bool ConvertWalls(int i, int j, int type, int conversionType)
	{
		WorldGen.ConvertWall(i, j, ModContent.WallType<SavannaDirtWall>());
		return false;
	}

	/// <summary> Converts vanilla trees using special logic. </summary>
	private static bool ScanUpTree(int x, int y, int type)
	{
		if (!TileID.Sets.IsATreeTrunk[Framing.GetTileSafely(x, --y).TileType])
			return false;

		int startY = y;
		int startType = Main.tile[x, y].TileType;

		while (WorldGen.InWorld(x, y) && Main.tile[x, y].TileType == startType)
		{
			Main.tile[x, y].ClearTile();
			y--;
		}

		WorldGen.RangeFrame(x, y, x, startY); //Frame everything to avoid floating branches
		if (startY - y < 3)
			return false;

		WorldGen.ConvertTile(x, startY + 1, type);

		if (type == ModContent.TileType<SavannaGrass>())
			CustomTree.GrowTree<AcaciaTree>(x, startY);
		else if (type == ModContent.TileType<SavannaGrassCorrupt>())
			CustomTree.GrowTree<AcaciaTreeCorrupt>(x, startY);
		else if (type == ModContent.TileType<SavannaGrassCrimson>())
			CustomTree.GrowTree<AcaciaTreeCrimson>(x, startY);
		else if (type == ModContent.TileType<SavannaGrassHallow>())
			CustomTree.GrowTree<AcaciaTreeHallow>(x, startY);

		return true;
	}
}