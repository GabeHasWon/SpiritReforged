using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.Conversion;
using SpiritReforged.Content.Crossmod.Spooky.SpookyForest;
using SpiritReforged.Content.Crossmod.Spooky.SpookyForest.Plants;
using SpiritReforged.Content.Forest.Stargrass.Tiles;
using Terraria.DataStructures;
using TileHelper.Common;

namespace SpiritReforged.Content.Forest.Stargrass.Items;

public class StarPowder : ModItem
{
	public override void SetStaticDefaults()
	{
		ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.PurificationPowder;
		Item.ResearchUnlockCount = 99;
	}

	public override void SetDefaults()
	{
		Item.width = 26;
		Item.height = 28;
		Item.rare = ItemRarityID.White;
		Item.maxStack = Item.CommonMaxStack;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.useTime = 15;
		Item.useAnimation = 15;
		Item.noMelee = true;
		Item.consumable = true;
		Item.UseSound = SoundID.Item1;
		Item.shoot = ModContent.ProjectileType<StarPowderProj>();
		Item.shootSpeed = 6f;
	}

	public override void AddRecipes() => CreateRecipe(5).AddIngredient(ItemID.FallenStar, 1).Register();
}

internal class StarPowderProj : ModProjectile
{
	public override string Texture => base.Texture[..^"Proj".Length];

	private bool _justSpawned;

	public override void SetDefaults() => Projectile.CloneDefaults(ProjectileID.PurificationPowder);
	public override void AI()
	{
		if (!_justSpawned)
		{
			for (int i = 0; i < 20; i++)
			{
				var rectDims = new Vector2(50, 50);
				Vector2 position = new Vector2(Projectile.Center.X - rectDims.X / 2, Projectile.Center.Y - rectDims.Y / 2) + Projectile.velocity * 2;
				Vector2 velocity = (new Vector2(Projectile.velocity.X, Projectile.velocity.Y) * Main.rand.NextFloat(0.8f, 1.2f)).RotatedByRandom(1f);
				var dust = Dust.NewDustDirect(position, (int)rectDims.X, (int)rectDims.Y, Main.rand.NextBool(2) ? DustID.BlueTorch : DustID.PurificationPowder,
					velocity.X, velocity.Y, 0, default, Main.rand.NextFloat(0.7f, 1.1f));
				dust.noGravity = true;
				dust.fadeIn = 1.1f;
				if (dust.type == DustID.PurificationPowder && Main.rand.NextBool(2))
					dust.color = Color.Goldenrod;
			}

			_justSpawned = true;
		}

		Point16 pt = Projectile.Center.ToTileCoordinates16();
		WorldGen.Convert(pt.X, pt.Y, StarConversion.ConversionType, 3, true, true);
	}

	public override bool? CanCutTiles() => false;
	public override bool? CanDamage() => false;
}

public class StarConversion : ModBiomeConversion
{
	public static int ConversionType => ModContent.GetInstance<StarConversion>().Type;

	public override void SetStaticDefaults()
	{
		TileLoader.RegisterConversion(TileID.Sunflower, ConversionType, static (i, j, type, conversionType) =>
		{
			if (Framing.GetTileSafely(i, j + 1).TileType == type)
				return false; //Return if this is not the base of the flower

			(i, j) = Helpers.GetTopLeft(i, j);
			return ConversionHelper.ConvertTiles(i, j, 2, 4, ModContent.TileType<Starflower>());
		});

		TileLoader.RegisterSimpleConversion(TileID.Grass, Type, ModContent.TileType<StargrassTile>());
		TileLoader.RegisterSimpleConversion(TileID.GolfGrass, Type, ModContent.TileType<StargrassMowed>());

		//	{ BiomeConversionID.Corruption, TileID.CorruptGrass },
		//	{ BiomeConversionID.Crimson, TileID.CrimsonGrass },
		//	{ BiomeConversionID.Hallow, TileID.GolfGrassHallowed },
		//	{ BiomeConversionID.PurificationPowder, TileID.GolfGrass },
		//	{ SavannaConversion.ConversionType, ModContent.TileType<SavannaGrass>() }

		if (CrossMod.Spooky.Enabled)
		{
			if (CrossMod.Spooky.CheckFind("SpookyGrassGreen", out ModTile green))
				TileLoader.RegisterSimpleConversion(green.Type, Type, ModContent.TileType<GreenSpookyStargrass>());

			if (CrossMod.Spooky.CheckFind("SpookyGrass", out ModTile orange))
				TileLoader.RegisterSimpleConversion(orange.Type, Type, ModContent.TileType<OrangeSpookyStargrass>());

			ConvertGourd<StarGourdGreen>("GourdGreen");
			ConvertCarvedGourd(Mod.Find<ModTile>("StarGourdGreenCarved").Type, "GourdGreenCarved");
			ConvertGourd<StarGourdLime>("GourdLime");
			ConvertCarvedGourd(Mod.Find<ModTile>("StarGourdLimeCarved").Type, "GourdLimeCarved");
			ConvertGourd<StarGourdOrangeLime>("GourdLimeOrangeLime");
			ConvertCarvedGourd(Mod.Find<ModTile>("StarGourdOrangeLimeCarved").Type, "GourdLimeOrangeCarved");
			ConvertGourd<StarGourdOrange>("GourdLimeOrange");
			ConvertCarvedGourd(Mod.Find<ModTile>("StarGourdOrangeCarved").Type, "GourdOrangeCarved");
			ConvertGourd<StarGourdRed>("GourdRed");
			ConvertCarvedGourd(Mod.Find<ModTile>("StarGourdRedCarved").Type, "GourdRedCarved");
			ConvertGourd<StarGourdRotten>("GourdRotten");
			ConvertCarvedGourd(Mod.Find<ModTile>("StarGourdRottenCarved").Type, "GourdRottenCarved");
			ConvertGourd<StarGourdOrange>("GourdOrange");
			ConvertCarvedGourd(Mod.Find<ModTile>("StarGourdOrangeCarved").Type, "GourdOrangeCarved");
			ConvertGourd<StarGourdWhite>("GourdWhite");
			ConvertCarvedGourd(Mod.Find<ModTile>("StarGourdWhiteCarved").Type, "GourdWhiteCarved");
			ConvertGourd<StarGourdYellow>("GourdYellow");
			ConvertCarvedGourd(Mod.Find<ModTile>("StarGourdYellowCarved").Type, "GourdYellowCarved");
			ConvertGourd<StarGourdYellowGreen>("GourdYellowGreen");
			ConvertCarvedGourd(Mod.Find<ModTile>("StarGourdYellowGreenCarved").Type, "GourdYellowGreenCarved");
		}
	}

	static void ConvertCarvedGourd(int gourd, string name)
	{
		ConvertLitGourd(gourd, name + "Lit");
		ConvertGourd(gourd, name);
	}

	static void ConvertGourd<T>(string name) where T : StarGourd => ConvertGourd(ModContent.TileType<T>(), name);

	static void ConvertGourd(int gourd, string name)
	{
		if (!CrossMod.Spooky.CheckFind(name, out ModTile tile))
			return;

		TileLoader.RegisterConversion(tile.Type, ConversionType, (i, j, type, conversionType) =>
		{
			if (Framing.GetTileSafely(i, j + 1).TileType == type)
				return false; //Return if this is not the base of the flower

			TileObjectData data = TileObjectData.GetTileData(type, 0);
			TileExtensions.GetTopLeft(ref i, ref j);
			return ConversionHelper.ConvertTiles(i, j, data.Width, data.Height, gourd);
		});
	}

	static void ConvertLitGourd(int gourd, string name)
	{
		if (!CrossMod.Spooky.CheckFind(name, out ModTile tile))
			return;

		TileLoader.RegisterConversion(tile.Type, ConversionType, (i, j, type, conversionType) =>
		{
			if (Framing.GetTileSafely(i, j + 1).TileType == type)
				return false; //Return if this is not the base of the flower

			TileObjectData data = TileObjectData.GetTileData(type, 0);
			TileExtensions.GetTopLeft(ref i, ref j);
			bool val = ConversionHelper.ConvertTiles(i, j, data.Width, data.Height, gourd);

			if (val)
			{
				for (int k = 0; k < data.Width; ++k)
				{
					for (int v = 0; v < data.Height; ++v)
					{
						Tile tile = Framing.GetTileSafely(i + k, j + v);
						tile.TileFrameY += (short)(18 * data.Height);
					}
				}

				NetMessage.SendTileSquare(-1, i, j, data.Width, data.Height);
			}

			return val;
		});
	}
}