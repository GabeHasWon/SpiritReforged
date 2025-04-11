using SpiritReforged.Common.WorldGeneration;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Common.TileCommon.TileSway;

/// <summary> Assign <see cref="Style"/> for vanilla sway styles or use <see cref="DrawSway"/> for custom drawing.<br/>
/// <see cref="Physics"/> changes how the tile responds to wind and player interaction. </summary>
public interface ISwayTile
{
	#region inst rotation
	/// <summary> Coordinate specific rotation values tied to <see cref="Physics"/> by default. </summary>
	[WorldBound]
	private static readonly Dictionary<Point16, float> Rotation = [];

	public static bool SetInstancedRotation(int i, int j, float value, bool fail = true)
	{
		if (Main.dedServ)
			return false;

		TileExtensions.GetTopLeft(ref i, ref j);
		var pt = new Point16(i, j);

		if (Rotation.ContainsKey(pt))
		{
			if (!fail)
				Rotation.Remove(pt);
			else
				Rotation[pt] = value;

			return true;
		}

		return Rotation.TryAdd(pt, value);
	}

	public static float GetInstancedRotation(int i, int j)
	{
		if (Main.dedServ)
			return 0;

		TileExtensions.GetTopLeft(ref i, ref j);
		Rotation.TryGetValue(new Point16(i, j), out float value);

		return value;
	}
	#endregion

	/// <summary> The default sway physics style for this tile according to <see cref="TileDrawing.TileCounterType"/>. Defaults to -1 which enables custom drawing only. </summary>
	public int Style => -1;

	/// <summary> Add wind grid math here. Called once per multitile. </summary>
	public float Physics(Point16 topLeft)
	{
		if (Rotation.TryGetValue(topLeft, out float value))
		{
			Rotation[topLeft] = MathHelper.Lerp(value, 0, .2f);

			if ((int)(value * 100) == 0)
				Rotation.Remove(topLeft);

			return value;
		}

		return 0;
	}

	/// <summary> Draw this tile transformed by <see cref="Physics"/>. </summary>
	public void DrawSway(int i, int j, SpriteBatch spriteBatch, Vector2 offset, float rotation, Vector2 origin)
	{
		var tile = Framing.GetTileSafely(i, j);
		var data = TileObjectData.GetTileData(tile);

		var drawPos = new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y);
		var source = new Rectangle(tile.TileFrameX, tile.TileFrameY, data.CoordinateWidth, data.CoordinateHeights[tile.TileFrameY / 18]);
		var dataOffset = new Vector2(data.DrawXOffset, data.DrawYOffset);

		spriteBatch.Draw(TextureAssets.Tile[tile.TileType].Value, drawPos + offset + dataOffset,
			source, Lighting.GetColor(i, j), rotation, origin, 1, SpriteEffects.None, 0);
	}
}
