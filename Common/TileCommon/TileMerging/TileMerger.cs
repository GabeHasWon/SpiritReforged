using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Common.TileCommon.TileMerging;

internal class TileMerger : ILoadable
{
	private const int FullFrameWidth = 108;

	/// <summary> All possible merge tile types. </summary>
	public static int[] All { get; private set; }
	private static readonly Dictionary<int, Asset<Texture2D>> TextureByType = [];

	public void Load(Mod mod)
	{
		Add(TileID.Sand, "Sand");
		All = [.. TextureByType.Keys];

		static void Add(int type, string name) => TextureByType.Add(type, ModContent.Request<Texture2D>(DrawHelpers.RequestLocal(typeof(TileMerger), "Textures/" + name + "Merge")));
	}

	public void Unload() { }

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

			var position = new Vector2(i, j) * 16 - Main.screenPosition + TileExtensions.TileOffset + offset;
			spriteBatch.Draw(textureAsset.Value, position, new Rectangle(frame.X + frameNumber * FullFrameWidth, frame.Y, 16, 16), color);
		}
	}

	private static bool TryFindFrame(int i, int j, int type, out Point frame)
	{
		ushort frameX = 0;
		ushort frameY = 0;

		bool up = Main.tile[i, j - 1].TileType == type;
		bool down = Main.tile[i, j + 1].TileType == type;
		bool left = Main.tile[i - 1, j].TileType == type;
		bool right = Main.tile[i + 1, j].TileType == type;

		//bool upLeft = false;
		//bool downLeft = false;
		//bool upRight = false;
		//bool downRight = false;

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
}