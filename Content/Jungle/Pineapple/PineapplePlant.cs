using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.WorldGeneration.Microbiomes;
using SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes;
using System.Linq;
using Terraria.GameContent.Metadata;

namespace SpiritReforged.Content.Jungle.Pineapple;

public class PineapplePlant : HerbTile
{
	public override void Load() => TileEvents.OnRandomUpdate += Regrow;
	/// <summary> Causes pineapple plants to regrow inside of underground oasis microbiomes and the jungle surface. </summary>
	private static void Regrow(int i, int j, int type)
	{
		switch (type)
		{
			case TileID.Sand:
				{
					if (j > Main.worldSurface && Main.rand.NextBool(20) && WorldGen.InWorld(i, j - 1, 20) && !Main.tile[i, j - 1].HasTile && Main.tile[i, j - 1].LiquidAmount < 100)
					{
						Point pt = new(i, j);
						int tileType = ModContent.TileType<PineapplePlant>();

						if (Placer.CanPlaceHerb(i, j, tileType) && MicrobiomeSystem.Microbiomes.Any(x => x is UndergroundOasisBiome o && o.Rectangle.Contains(pt)))
							Placer.PlaceTile(i, j - 1, tileType).Send();
					}

					break;
				}

			case TileID.JungleGrass:
				{
					if (j <= Main.worldSurface && Main.rand.NextBool(30) && WorldGen.InWorld(i, j - 1, 20) && !Main.tile[i, j - 1].HasTile && Main.tile[i, j - 1].LiquidAmount < 100)
					{
						Point pt = new(i, j);
						int tileType = ModContent.TileType<PineapplePlant>();

						if (Placer.CanPlaceHerb(i, j, tileType))
							Placer.PlaceTile(i, j - 1, tileType).Send();
					}

					break;
				}
		}
	}

	public override void SetStaticDefaults()
	{
		FrameWidth = 38;

		Main.tileFrameImportant[Type] = true;
		Main.tileObsidianKill[Type] = true;
		Main.tileCut[Type] = true;
		Main.tileNoFail[Type] = true;

		TileID.Sets.ReplaceTileBreakUp[Type] = true;
		TileID.Sets.IgnoredInHouseScore[Type] = true;
		TileID.Sets.IgnoredByGrowingSaplings[Type] = true;
		TileID.Sets.SwaysInWindBasic[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.LavaDeath = true;
		TileObjectData.newTile.WaterDeath = false;
		TileObjectData.newTile.CoordinatePadding = 2;
		TileObjectData.newTile.CoordinateWidth = FrameWidth - TileObjectData.newTile.CoordinatePadding;
		TileObjectData.newTile.CoordinateHeights = [FrameWidth];
		TileObjectData.newTile.DrawYOffset = -(FrameWidth - 18);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.AnchorValidTiles = [TileID.Sand];
		TileObjectData.newTile.AnchorAlternateTiles = [TileID.ClayPot, TileID.PlanterBox];
		TileObjectData.addTile(Type);

		TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);
		AddMapEntry(new Color(200, 150, 50));
		HerbTypes.Add(Type);

		DustType = DustID.JungleGrass;
		HitSound = SoundID.Grass;

		SeedType = ModContent.ItemType<PineappleSeeds>();
		HerbType = ItemID.Pineapple;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 8;
}