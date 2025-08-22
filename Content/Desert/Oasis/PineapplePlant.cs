using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WorldGeneration.Microbiomes;
using SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes;
using System.Linq;
using Terraria.GameContent.Metadata;

namespace SpiritReforged.Content.Desert.Oasis;

public class PineapplePlant : ModTile
{
	public const int TileWidth = 38;
	public const int TileHeight = 46;

	public override void Load() => TileEvents.OnRandomUpdate += Regrow;
	/// <summary> Causes pineapple plants to regrow inside of underground oasis microbiomes. </summary>
	private static void Regrow(int i, int j, int type)
	{
		if (type == TileID.Sand && j > Main.worldSurface && Main.rand.NextBool(20) && WorldGen.InWorld(i, j - 1) && !Main.tile[i, j - 1].HasTile && Main.tile[i, j - 1].LiquidAmount < 100)
		{
			Point pt = new(i, j);
			int tileType = ModContent.TileType<PineapplePlant>();

			if (Placer.CanPlaceHerb(i, j, tileType) && MicrobiomeSystem.Microbiomes.Any(x => x is UndergroundOasisBiome o && o.Rectangle.Contains(pt)))
				Placer.PlaceTile(i, j - 1, tileType).Send();
		}
	}

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;
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
		TileObjectData.newTile.AnchorValidTiles = [TileID.Sand];
		TileObjectData.newTile.AnchorAlternateTiles = [TileID.ClayPot, TileID.PlanterBox];
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(200, 150, 50));
		RegisterItemDrop(ItemID.Pineapple, 2);

		DustType = DustID.JungleGrass;
		HitSound = SoundID.Grass;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 8;
	public override bool IsTileSpelunkable(int i, int j) => Main.tile[i, j].TileFrameX >= TileWidth * 2;

	public override void RandomUpdate(int i, int j)
	{
		if (Main.rand.NextBool(3))
		{
			var tile = Main.tile[i, j];

			if (tile.TileFrameX < TileWidth * 2)
			{
				tile.TileFrameX += TileWidth;

				if (Main.netMode != NetmodeID.SinglePlayer)
					NetMessage.SendTileSquare(-1, i, j);
			}
		}
	}
}