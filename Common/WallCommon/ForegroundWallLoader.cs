using SpiritReforged.Common.TileCommon;
using Terraria.DataStructures;
using Terraria.Graphics.Light;

namespace SpiritReforged.Common.WallCommon;

public sealed class ForegroundWallLoader : ILoadable
{
	private static readonly HashSet<Point16> Points = [];

	public static void AddPoint(int i, int j) => Points.Add(new(i, j));

	public void Load(Mod mod)
	{
		DrawOrderSystem.PostDrawPlayers += DrawForeground;
		On_Main.RenderWalls += ResetPoints;
		On_TileLightScanner.LightIsBlocked += BlockLight;
	}

	private static void DrawForeground()
	{
		if (Points.Count == 0)
			return;

		Main.tileBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, Main.Rasterizer, null, Main.Transform);

		foreach (var pt in Points)
		{
			Tile tile = Main.tile[pt.X, pt.Y];
			Rectangle source = new(tile.WallFrameX, tile.WallFrameY + Main.wallFrame[tile.WallType] * 180, 32, 32);

			if (!WorldGen.SolidTile(pt.X + 1, pt.Y) && !Points.Contains(new(pt.X + 1, pt.Y)))
				source = new(36 * 4, 36 * tile.Get<TileWallWireStateData>().TileFrameNumber, 32, 32);
			else if (!WorldGen.SolidTile(pt.X - 1, pt.Y) && !Points.Contains(new(pt.X - 1, pt.Y)))
				source = new(0, 36 * tile.Get<TileWallWireStateData>().TileFrameNumber, 32, 32);

			WallMethods.DrawSingleWall(pt.X, pt.Y, tile.WallType, source, 1);
		}

		Main.tileBatch.End();
	}

	private static void ResetPoints(On_Main.orig_RenderWalls orig, Main self)
	{
		Points.Clear();
		orig(self);
	}

	private static bool BlockLight(On_TileLightScanner.orig_LightIsBlocked orig, TileLightScanner self, Tile tile) => SpiritSets.WallBlocksLight[tile.WallType] || orig(self, tile);

	public void Unload() { }
}