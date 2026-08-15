using SpiritReforged.Common.WorldGeneration.Microbiomes;
using SpiritReforged.Content.Basalt.Tiles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Basalt;

internal class BasaltBiome : Microbiome
{
	protected override void OnPlace(Point16 point)
	{
		int reps = WorldGen.genRand.Next();

		PlaceMainTunnel(point);
	}

	private void PlaceMainTunnel(Point16 point)
	{
		const float AngleRange = 1f;

		int width = WorldGen.genRand.Next(100, 130);
		int height = WorldGen.genRand.Next(50, 75);
		float angle = WorldGen.genRand.NextFloat(-AngleRange, AngleRange);
		Vector2 position = point.ToVector2();

		for (float x = width / -2f; x < width / 2f; ++x)
		{
			Vector2 offset = new Vector2(x * 0.95f, 0).RotatedBy(angle);
			Point16 pos = (position + offset).ToPoint16();
			float heightAdj = Utils.GetLerpValue(width / 2f, 0, Math.Abs(x));

			for (float y = height / -2f * heightAdj; y < height / 2f * heightAdj; ++y)
			{
				Tile tile = Main.tile[pos.X, pos.Y + (int)y];
				tile.HasTile = true;
				tile.TileType = (ushort)ModContent.TileType<BasaltTile>();

				if (Math.Abs(y) <= 1f)
					tile.TileColor = PaintID.WhitePaint;
			}
		}
	}
}
