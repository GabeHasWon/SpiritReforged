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
			Rectangle source = WallMethods.GetWallFrame(tile);

			WallMethods.DrawSingleWall(pt.X, pt.Y, tile.WallType, source, 1);
		}

		Main.tileBatch.End();
	}

	public static Rectangle SpecialWallFraming(int i, int j, int frameNumber)
	{
		Tile tile = Main.tile[i, j];
		ushort type = tile.WallType;

		int num = 0;
		if (IsFull(i, j - 1))
			num = 1;

		if (IsFull(i - 1, j))
			num |= 2;

		if (IsFull(i + 1, j))
			num |= 4;

		if (IsFull(i, j + 1))
			num |= 8;

		tile.WallFrameNumber = frameNumber;
		Point16 point = WallMethods.FrameLookup[num][frameNumber];
		tile.WallFrameX = point.X;
		tile.WallFrameY = point.Y;

		return WallMethods.GetWallFrame(tile);

		bool IsFull(int i, int j) => Framing.GetTileSafely(i, j).WallType == type || WorldGen.SolidTile(i, j);
	}

	private static void ResetPoints(On_Main.orig_RenderWalls orig, Main self)
	{
		Points.Clear();
		orig(self);
	}

	private static bool BlockLight(On_TileLightScanner.orig_LightIsBlocked orig, TileLightScanner self, Tile tile) => SpiritSets.WallBlocksLight[tile.WallType] || orig(self, tile);

	public void Unload() { }
}