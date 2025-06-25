using SpiritReforged.Common.TileCommon.Conversion;
using SpiritReforged.Common.TileCommon.Tree;
using SpiritReforged.Content.Savanna.Tiles;
using SpiritReforged.Content.Savanna.Tiles.AcaciaTree;
using SpiritReforged.Content.Savanna.Walls;
using Terraria.DataStructures;

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

	private static readonly Dictionary<int, int> Plants = new()
	{
		{ TileID.Plants, ModContent.TileType<SavannaFoliage>() },
		{ TileID.Plants2, ModContent.TileType<SavannaFoliage>() },
		{ TileID.CorruptPlants, ModContent.TileType<SavannaFoliageCorrupt>() },
		{ TileID.CrimsonPlants, ModContent.TileType<SavannaFoliageCrimson>() },
		{ TileID.HallowedPlants, ModContent.TileType<SavannaFoliageHallow>() },
		{ TileID.HallowedPlants2, ModContent.TileType<SavannaFoliageHallow>() }
	};

	public override void SetStaticDefaults()
	{
		ConversionType = Type;

		TileLoader.RegisterConversion(TileID.Dirt, ConversionType, static (i, j, type, conversionType) =>
		{
			WorldGen.ConvertTile(i, j, ModContent.TileType<SavannaDirt>());
			return true;
		});

		WallLoader.RegisterConversion(WallID.DirtUnsafe, ConversionType, ConvertWalls);
		WallLoader.RegisterConversion(WallID.Dirt, ConversionType, ConvertWalls);

		TileLoader.RegisterConversion(TileID.Grass, ConversionType, ConvertGrass);
		TileLoader.RegisterConversion(TileID.CorruptGrass, ConversionType, ConvertGrass);
		TileLoader.RegisterConversion(TileID.CrimsonGrass, ConversionType, ConvertGrass);
		TileLoader.RegisterConversion(TileID.HallowedGrass, ConversionType, ConvertGrass);
	}

	private static bool ConvertWalls(int i, int j, int type, int conversionType)
	{
		int newType = type switch
		{
			WallID.DirtUnsafe => SavannaDirtWall.UnsafeType,
			WallID.Dirt => ModContent.WallType<SavannaDirtWall>(),
			_ => -1
		};

		if (newType != -1)
		{
			WorldGen.ConvertWall(i, j, newType);
			return true;
		}

		return false;
	}

	private static bool ConvertGrass(int i, int j, int type, int conversionType)
	{
		int newType = type switch
		{
			TileID.CorruptGrass => ModContent.TileType<SavannaGrassCorrupt>(),
			TileID.CrimsonGrass => ModContent.TileType<SavannaGrassCrimson>(),
			TileID.HallowedGrass => ModContent.TileType<SavannaGrassHallow>(),
			_ => ModContent.TileType<SavannaGrass>()
		};

		if (ScanUpTree(i, j - 1, out var top))
		{
			int height = j - top.Y;
			var area = new Rectangle(top.X - 1, top.Y, 3, height);

			ClearArea(area, Main.tile[i, j - 1].TileType);

			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendTileSquare(-1, area.X, area.Y, area.Width, area.Height);

			WorldGen.ConvertTile(i, j, newType);

			if (newType == ModContent.TileType<SavannaGrass>())
				CustomTree.GrowTree<AcaciaTree>(i, j - 1);
			else if (newType == ModContent.TileType<SavannaGrassCorrupt>())
				CustomTree.GrowTree<AcaciaTreeCorrupt>(i, j - 1);
			else if (newType == ModContent.TileType<SavannaGrassCrimson>())
				CustomTree.GrowTree<AcaciaTreeCrimson>(i, j - 1);
			else if (newType == ModContent.TileType<SavannaGrassHallow>())
				CustomTree.GrowTree<AcaciaTreeHallow>(i, j - 1);
		}
		else
		{
			var above = Framing.GetTileSafely(i, j - 1);

			if (Plants.TryGetValue(above.TileType, out int value))
			{
				above.TileType = (ushort)value;
				WorldGen.Reframe(i, j - 1, true);
			}

			WorldGen.ConvertTile(i, j, newType);
		}

		return true;

		static void ClearArea(Rectangle area, int type)
		{
			for (int x = area.X; x < area.X + area.Width; x++)
			{
				for (int y = area.Y; y < area.Y + area.Height; y++)
				{
					var tile = Framing.GetTileSafely(x, y);

					if (tile.TileType == type)
						Framing.GetTileSafely(x, y).ClearTile();
				}
			}
		}
	}

	private static bool ScanUpTree(int x, int y, out Point16 topCoordinates)
	{
		Point16 start = new(x, y);
		int height = 0;

		while (IsTile(x, y - height))
			height++;

		topCoordinates = new(x, y - height);
		return height > 2;

		bool IsTile(int i, int j)
		{
			if (!WorldGen.InWorld(i, j))
				return false;

			int tileType = Main.tile[i, j].TileType;
			return TileID.Sets.IsATreeTrunk[tileType] && Framing.GetTileSafely(start).TileType == tileType;
		}
	}
}