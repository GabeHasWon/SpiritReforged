using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.WorldGeneration.Microbiomes;
using SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes;
using System.Linq;
using Terraria.GameContent.Metadata;

namespace SpiritReforged.Content.Jungle.Pineapple;

public class PineapplePlant : HerbTile
{
	public const int TileWidth = 38;
	public const int TileHeight = 46;

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
		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileObsidianKill[Type] = true;
		Main.tileNoFail[Type] = true;
		Main.tileCut[Type] = true;

		TileID.Sets.SwaysInWindBasic[Type] = true;
		TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.LavaDeath = true;
		TileObjectData.newTile.WaterDeath = false;
		TileObjectData.newTile.CoordinatePadding = 2;
		TileObjectData.newTile.CoordinateWidth = TileWidth - TileObjectData.newTile.CoordinatePadding;
		TileObjectData.newTile.CoordinateHeights = [TileHeight];
		TileObjectData.newTile.DrawYOffset = -(TileHeight - 18);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.AnchorValidTiles = [TileID.Sand, TileID.JungleGrass];
		TileObjectData.newTile.AnchorAlternateTiles = [TileID.ClayPot, TileID.PlanterBox];
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(200, 150, 50));
		RegisterItemDrop(ItemID.Pineapple, 2);

		DustType = DustID.JungleGrass;
		HitSound = SoundID.Grass;
	}

	public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY) { }
	public override void NumDust(int i, int j, bool fail, ref int num) => num = 8;
}