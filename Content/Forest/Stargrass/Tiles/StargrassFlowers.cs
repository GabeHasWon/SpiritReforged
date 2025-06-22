using SpiritReforged.Common.TileCommon.Conversion;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Savanna.Items;
using SpiritReforged.Content.Savanna.Tiles;
using Terraria.GameContent.Metadata;

namespace SpiritReforged.Content.Forest.Stargrass.Tiles;

[AutoloadGlowmask("Method:Content.Forest.Stargrass.Tiles.StargrassFlowers Glow")]
public class StargrassFlowers : ModTile
{
	public const int StyleRange = 27;

	private static readonly Dictionary<int, int> Typed = new() 
	{ 
		{ BiomeConversionID.Corruption, TileID.CorruptPlants }, 
		{ BiomeConversionID.Crimson, TileID.CrimsonPlants }, 
		{ BiomeConversionID.Hallow, TileID.HallowedPlants },
		{ BiomeConversionID.Purity, TileID.Plants }, 
		{ BiomeConversionID.PurificationPowder, TileID.Plants }, 
		{ SavannaConversion.ConversionType, ModContent.TileType<SavannaFoliage>() }
	};

	public static Color Glow(object obj)
	{
		const float MinBrightness = 0.4f;
		const float MaxDist = 140 * 140;

		var pos = (Point)obj;
		float dist = Main.player[Player.FindClosest(new Vector2(pos.X, pos.Y) * 16, 16, 16)].DistanceSQ(new Vector2(pos.X, pos.Y) * 16 + new Vector2(8));
		float strength = MinBrightness;

		if (dist < MaxDist)
			strength = MathHelper.Lerp(MinBrightness, 1f, 1 - dist / MaxDist);

		return StargrassTile.Glow(pos) * strength;
	}

	public override void SetStaticDefaults()
	{
		const int TileHeight = 24;

		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoFail[Type] = true;
		Main.tileCut[Type] = true;
		Main.tileLighted[Type] = true;

		TileID.Sets.SwaysInWindBasic[Type] = true;
		TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.LavaDeath = true;
		TileObjectData.newTile.WaterDeath = false;
		TileObjectData.newTile.CoordinatePadding = 2;
		TileObjectData.newTile.CoordinateWidth = 16;
		TileObjectData.newTile.CoordinateHeights = [TileHeight];
		TileObjectData.newTile.DrawYOffset = -(TileHeight - 18);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = StyleRange;
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<StargrassTile>(), TileID.CorruptGrass, TileID.CrimsonGrass, TileID.HallowedGrass, TileID.Grass, ModContent.TileType<SavannaGrass>()];
		TileObjectData.newTile.AnchorAlternateTiles = [TileID.ClayPot, TileID.PlanterBox];
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(20, 190, 130));
		DustType = DustID.Grass;
		HitSound = SoundID.Grass;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 2;

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		int frame = Main.tile[i, j].TileFrameX / 18;
		if (frame >= 6)
			(r, g, b) = (0.025f, 0.1f, 0.25f);
	}

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		if (Main.player[Player.FindClosest(new Vector2(i, j).ToWorldCoordinates(0, 0), 16, 16)].HeldItem.type == ItemID.Sickle)
			yield return new Item(ItemID.Hay, Main.rand.Next(1, 3));

		if (Main.player[Player.FindClosest(new Vector2(i, j).ToWorldCoordinates(0, 0), 16, 16)].HasItem(ItemID.Blowpipe))
			yield return new Item(ItemID.Seed, Main.rand.Next(2, 4));
	}

	public override void Convert(int i, int j, int conversionType)
	{
		if (ConversionHelper.FindType(conversionType, Main.tile[i, j].TileType, Typed) is int value && value != -1)
			WorldGen.ConvertTile(i, j, value);
	}
}