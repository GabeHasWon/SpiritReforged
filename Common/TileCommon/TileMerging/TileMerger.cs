using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Desert.Tiles;

namespace SpiritReforged.Common.TileCommon.TileMerging;

public sealed class TileMerger : ModSystem
{
	private const int FullFrameWidth = 108;

	/// <summary> All possible merge tile types. </summary>
	public static int[] All { get; private set; }
	private static readonly Dictionary<int, Asset<Texture2D>> TextureByType = [];

	public override void SetStaticDefaults()
	{
		Add(TileID.Sand, "Sand");
		Add(TileID.Dirt, "Dirt");
		AddRange("RedSandstone", ModContent.TileType<RedSandstoneBrick>(), ModContent.TileType<RedSandstoneBrickCracked>(), ModContent.TileType<RedSandstoneSlab>());
		All = [.. TextureByType.Keys]; //Must be last

		static void Add(int type, string name) => TextureByType.Add(type, ModContent.Request<Texture2D>(DrawHelpers.RequestLocal(typeof(TileMerger), "Textures/" + name + "Merge")));

		static void AddRange(string name, params int[] types)
		{
			foreach (int type in types)
				Add(type, name);
		}
	}

	/// <summary> Draws merge overlays according to <paramref name="types"/>. </summary>
	public static void DrawMerge(SpriteBatch spriteBatch, int i, int j, params int[] types) => DrawMerge(spriteBatch, i, j, Lighting.GetColor(i, j), TileExtensions.TileOffset, types);
	/// <summary><inheritdoc cref="DrawMerge(SpriteBatch, int, int, int[])"/>
	/// <br/>See the overload for a simpler method approach. </summary>
	public static void DrawMerge(SpriteBatch spriteBatch, int i, int j, Color color, Vector2 offset, params int[] types)
	{
		int frameNumber = Main.tile[i, j].Get<TileWallWireStateData>().TileFrameNumber;

		foreach (int type in types)
		{
			if (!TryFindFrame(i, j, type, out var frame) || !TextureByType.TryGetValue(type, out var textureAsset))
				continue;

			var position = new Vector2(i, j) * 16 - Main.screenPosition + offset;
			spriteBatch.Draw(textureAsset.Value, position, new Rectangle(frame.X + frameNumber * FullFrameWidth, frame.Y, 16, 16), color);
		}
	}

	private static bool TryFindFrame(int i, int j, int type, out Point frame)
	{
		ushort frameX = 0;
		ushort frameY = 0;

		GetMerge(i, j, type, out bool up, out bool down, out bool left, out bool right);

		if (!up && !down && !left && !right)
		{
			frame = Point.Zero;
			return false;
		}

		if (up && left && down && right) //All sides
		{
			frameX = 18;
			frameY = 18;
		}
		else if (up && left && right) //Open ends
		{
			frameX = 72;
			frameY = 0;
		}
		else if (left && up && down)
		{
			frameX = 90;
			frameY = 0;
		}
		else if (left && right && down)
		{
			frameX = 72;
			frameY = 18;
		}
		else if (right && up && down)
		{
			frameX = 90;
			frameY = 18;
		}
		else if (up && down) //Opposites
		{
			frameX = 54;
			frameY = 0;
		}
		else if (left && right)
		{
			frameX = 54;
			frameY = 18;
		}
		else if (up && left) //Inner corners
		{
			frameX = 0;
			frameY = 0;
		}
		else if (down && left)
		{
			frameX = 0;
			frameY = 36;
		}
		else if (up && right)
		{
			frameX = 36;
			frameY = 0;
		}
		else if (down && right)
		{
			frameX = 36;
			frameY = 36;
		}
		else if (up) //Sides
		{
			frameX = 18;
			frameY = 0;
		}
		else if (down)
		{
			frameX = 18;
			frameY = 36;
		}
		else if (left)
		{
			frameX = 0;
			frameY = 18;
		}
		else if (right)
		{
			frameX = 36;
			frameY = 18;
		}

		frame = new Point(frameX, frameY);
		return true;
	}

	private static void GetMerge(int i, int j, int mergeType, out bool up, out bool down, out bool left, out bool right)
	{
		Tile tile = Main.tile[i, j];
		Tile upTile = Main.tile[i, j - 1];
		Tile downTile = Main.tile[i, j + 1];
		Tile leftTile = Main.tile[i - 1, j];
		Tile rightTile = Main.tile[i + 1, j];

		up = down = left = right = false;

		if (upTile.HasTileType(mergeType) && !tile.IsHalfBlock && (tile.BottomSlope || tile.Slope == SlopeType.Solid) && (upTile.TopSlope || upTile.Slope == SlopeType.Solid))
		{
			up = true;
		}

		if (downTile.HasTileType(mergeType) && (!tile.TopSlope || tile.Slope == SlopeType.Solid) && (downTile.BottomSlope || downTile.Slope == SlopeType.Solid))
		{
			down = true;
		}

		if (leftTile.HasTileType(mergeType) && !tile.IsHalfBlock && (tile.RightSlope || tile.Slope == SlopeType.Solid) && (leftTile.LeftSlope || leftTile.Slope == SlopeType.Solid))
		{
			left = true;
		}

		if (rightTile.HasTileType(mergeType) && !tile.IsHalfBlock && (tile.LeftSlope || tile.Slope == SlopeType.Solid) && (rightTile.RightSlope || rightTile.Slope == SlopeType.Solid))
		{
			right = true;
		}
	}
}