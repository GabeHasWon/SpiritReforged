using SpiritReforged.Common;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.Conversion;
using SpiritReforged.Common.TileCommon.PresetTiles;

namespace SpiritReforged.Content.Savanna.Tiles;

public class SavannaGrass : GrassTile
{
	protected override int DirtType => ModContent.TileType<SavannaDirt>();
	protected virtual Color MapColor => new(104, 156, 70);

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		SpiritSets.Mowable[Type] = ModContent.TileType<SavannaGrassMowed>();
		RegisterItemDrop(AutoContent.ItemType<SavannaDirt>());
		AddMapEntry(MapColor);
		this.Merge(ModContent.TileType<SavannaGrass>(), ModContent.TileType<SavannaGrassCorrupt>(), ModContent.TileType<SavannaGrassHallow>(), ModContent.TileType<SavannaGrassCrimson>());
	}

	public override void RandomUpdate(int i, int j)
	{
		if (SpreadHelper.Spread(i, j, Type, 4, DirtType) && Main.netMode != NetmodeID.SinglePlayer)
			NetMessage.SendTileSquare(-1, i, j, 3, TileChangeType.None); //Try spread grass

		GrowTiles(i, j);
	}

	protected virtual void GrowTiles(int i, int j)
	{
		var above = Framing.GetTileSafely(i, j - 1);

		if (!above.HasTile && above.LiquidAmount < 80)
		{
			int grassChance = GrassAny() ? 6 : 30;

			if (Main.rand.NextBool(grassChance))
				Placer.PlaceTile<ElephantGrass>(i, j - 1, Main.rand.Next(5, 8)).Send();
			else if (Main.rand.NextBool(10))
				Placer.PlaceTile<SavannaFoliage>(i, j - 1).Send();
			else if (Main.rand.NextBool(1400) && WorldGen.PlaceTile(i, j - 1, TileID.DyePlants, true, style: 2))
				NetMessage.SendTileSquare(-1, i, j - 1);

			if (Main.rand.NextBool(100) && !WorldGen.PlayerLOS(i, j))
			{
				if (Main.rand.NextBool())
					Placer.PlaceTile<TermiteMoundSmall>(i, j - 1).Send();
				else if (Main.rand.NextBool(3))
					Placer.PlaceTile<TermiteMoundMedium>(i, j - 1).Send();
				else if (Main.rand.NextBool(5))
					Placer.PlaceTile<TermiteMoundLarge>(i, j - 1).Send();
			}
		}

		if (Main.rand.NextBool(5) && Main.tile[i, j + 1].LiquidType != LiquidID.Lava)
			Placer.GrowVine(i, j + 1, ModContent.TileType<SavannaVine>());

		bool GrassAny()
		{
			int type = ModContent.TileType<ElephantGrass>();
			return Framing.GetTileSafely(i - 1, j - 1).TileType == type || Framing.GetTileSafely(i + 1, j - 1).TileType == type;
		}
	}

	public override void FloorVisuals(Player player)
	{
		if (player.flowerBoots) //Flower Boots functionality
		{
			var pos = ((player.Bottom - new Vector2(0, 8 * player.gravDir)) / 16).ToPoint16();

			if (!Main.tile[pos.X, pos.Y].HasTile)
				Placer.PlaceTile<SavannaFoliage>(pos.X, pos.Y - 1).Send();
		}
	}

	public override void Convert(int i, int j, int conversionType)
	{
		if (ConversionHelper.FindType(conversionType, Main.tile[i, j].TileType, ModContent.TileType<SavannaGrassCorrupt>(), ModContent.TileType<SavannaGrassCrimson>(), ModContent.TileType<SavannaGrassHallow>(), ModContent.TileType<SavannaGrass>()) is int value && value != -1)
			WorldGen.ConvertTile(i, j, value);
	}
}

public class SavannaGrassCorrupt : SavannaGrass
{
	protected override Color MapColor => new(109, 106, 174);

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		SpiritSets.Mowable[Type] = -1;
		TileID.Sets.Corrupt[Type] = true;
		TileID.Sets.AddCorruptionTile(Type, 20);
	}

	public override void RandomUpdate(int i, int j)
	{
		base.RandomUpdate(i, j);
		WorldGen.SpreadInfectionToNearbyTile(i, j, BiomeConversionID.Corruption);
	}

	protected override void GrowTiles(int i, int j)
	{
		var above = Framing.GetTileSafely(i, j - 1);
		if (!above.HasTile && above.LiquidAmount < 80)
		{
			int grassChance = GrassAny() ? 6 : 35;

			if (Main.rand.NextBool(grassChance))
				Placer.PlaceTile<ElephantGrassCorrupt>(i, j - 1, Main.rand.Next(5, 8)).Send();
			else if (Main.rand.NextBool(15))
				Placer.PlaceTile<SavannaFoliageCorrupt>(i, j - 1).Send();
		}

		bool GrassAny()
		{
			int type = ModContent.TileType<ElephantGrassCorrupt>();
			return Framing.GetTileSafely(i - 1, j - 1).TileType == type || Framing.GetTileSafely(i + 1, j - 1).TileType == type;
		}
	}
}

public class SavannaGrassCrimson : SavannaGrass
{
	protected override Color MapColor => new(183, 69, 68);

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		SpiritSets.Mowable[Type] = -1;
		TileID.Sets.AddCrimsonTile(Type, 20);
		TileID.Sets.Crimson[Type] = true;
	}

	public override void RandomUpdate(int i, int j)
	{
		base.RandomUpdate(i, j);
		WorldGen.SpreadInfectionToNearbyTile(i, j, BiomeConversionID.Crimson);
	}

	protected override void GrowTiles(int i, int j)
	{
		var above = Framing.GetTileSafely(i, j - 1);
		if (!above.HasTile && above.LiquidAmount < 80)
		{
			int grassChance = GrassAny() ? 6 : 35;

			if (Main.rand.NextBool(grassChance))
				Placer.PlaceTile<ElephantGrassCrimson>(i, j - 1, Main.rand.Next(5, 8)).Send();
			else if (Main.rand.NextBool(15))
				Placer.PlaceTile<SavannaFoliageCrimson>(i, j - 1).Send();
		}

		bool GrassAny()
		{
			int type = ModContent.TileType<ElephantGrassCrimson>();
			return Framing.GetTileSafely(i - 1, j - 1).TileType == type || Framing.GetTileSafely(i + 1, j - 1).TileType == type;
		}
	}
}

public class SavannaGrassHallow : SavannaGrass
{
	protected override Color MapColor => new(78, 193, 227);

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		SpiritSets.Mowable[Type] = ModContent.TileType<SavannaGrassHallowMowed>();
		TileID.Sets.Hallow[Type] = true;
		TileID.Sets.HallowBiome[Type] = 20;
	}

	public override void RandomUpdate(int i, int j)
	{
		base.RandomUpdate(i, j);
		WorldGen.SpreadInfectionToNearbyTile(i, j, BiomeConversionID.Hallow);
	}

	protected override void GrowTiles(int i, int j)
	{
		var above = Framing.GetTileSafely(i, j - 1);
		if (!above.HasTile && above.LiquidAmount < 80)
		{
			int grassChance = GrassAny() ? 6 : 30;

			if (Main.rand.NextBool(grassChance))
				Placer.PlaceTile<ElephantGrassHallow>(i, j - 1, Main.rand.Next(5, 8)).Send();
			else if (Main.rand.NextBool(10))
				Placer.PlaceTile<SavannaFoliageHallow>(i, j - 1).Send();

			if (Main.rand.NextBool(1400) && WorldGen.PlaceTile(i, j - 1, TileID.DyePlants, true, style: 2))
				NetMessage.SendTileSquare(-1, i, j - 1, TileChangeType.None);
		}

		bool GrassAny()
		{
			int type = ModContent.TileType<ElephantGrassHallow>();
			return Framing.GetTileSafely(i - 1, j - 1).TileType == type || Framing.GetTileSafely(i + 1, j - 1).TileType == type;
		}
	}
}