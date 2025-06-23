using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.Conversion;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Content.Forest.Stargrass.Tiles;
using SpiritReforged.Content.Savanna.Tiles;

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

		Point pt = Projectile.Center.ToTileCoordinates();
		WorldGen.Convert(pt.X, pt.Y, StarConversion.ConversionType, 3);
	}

	public override bool? CanCutTiles() => false;
	public override bool? CanDamage() => false;
}

public class StarConversion : ModBiomeConversion
{
	public static int ConversionType { get; private set; }

	private static readonly Dictionary<int, int> Plants = new()
	{
		{ TileID.Plants, ModContent.TileType<StargrassFlowers>() },
		{ TileID.Plants2, ModContent.TileType<StargrassFlowers>() }
	};

	public override void SetStaticDefaults()
	{
		ConversionType = Type;

		TileLoader.RegisterConversion(TileID.GolfGrass, ConversionType, static (i, j, type, conversionType) =>
		{
			WorldGen.ConvertTile(i, j, ModContent.TileType<StargrassMowed>());
			return true;
		});

		TileLoader.RegisterConversion(TileID.Sunflower, ConversionType, static (i, j, type, conversionType) =>
		{
			if (Framing.GetTileSafely(i, j + 1).TileType == type)
				return false; //Return if this is not the base of the flower

			TileExtensions.GetTopLeft(ref i, ref j);
			return ConversionHelper.ConvertTiles(i, j, 2, 4, ModContent.TileType<Starflower>());
		});

		TileLoader.RegisterConversion(TileID.Grass, ConversionType, static (i, j, type, conversionType) =>
		{
			var above = Framing.GetTileSafely(i, j - 1);

			if (Plants.TryGetValue(above.TileType, out int value))
			{
				above.TileType = (ushort)value;
				WorldGen.Reframe(i, j - 1, true);
			}

			WorldGen.ConvertTile(i, j, ModContent.TileType<StargrassTile>());

			return true;
		});
	}
}