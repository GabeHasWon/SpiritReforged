using SpiritReforged.Common.WorldGeneration.Chests;
using System.Runtime.CompilerServices;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Common.TileCommon;

public static class TileMethods
{
	public static Vector2 TileOffset => Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);

	#region shake
	[UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "treeShakeX")]
	private static extern ref int[] TreeShakeX(WorldGen worldGen);

	[UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "treeShakeY")]
	private static extern ref int[] TreeShakeY(WorldGen worldGen);

	[UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "numTreeShakes")]
	private static extern ref int NumTreeShakes(WorldGen worldGen);

	[UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "maxTreeShakes")]
	private static extern ref int MaxTreeShakes(WorldGen worldGen);

	/// <summary> Gets whether the tile at the given coordinates has been shook, and shakes it if not. </summary>
	public static bool ShakeTree(int i, int j)
	{
		WorldGen worldGen = default;

		if (NumTreeShakes(worldGen) == MaxTreeShakes(worldGen))
			return false;

		int[] shakeX = TreeShakeX(worldGen);
		int[] shakeY = TreeShakeY(worldGen);

		for (int k = 0; k < NumTreeShakes(worldGen); k++)
		{
			if (shakeX[k] == i && shakeY[k] == j)
				return false;
		}

		shakeX[NumTreeShakes(worldGen)] = i;
		shakeY[NumTreeShakes(worldGen)] = j;
		NumTreeShakes(worldGen)++;

		return true;
	}
	#endregion

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawSingleTile")]
	private static extern void DrawSingleTile(TileDrawing drawing, TileDrawInfo drawData, bool solidLayer, int waterStyleOverride, Vector2 screenPosition, Vector2 screenOffset, int tileX, int tileY);

	public static void DrawSingleTile(int i, int j, bool solidLayer = true, Vector2? screenOffset = null)
	{
		screenOffset ??= TileOffset;

		TileDrawing renderer = Main.instance.TilesRenderer;
		Vector2 unscaledPosition = Main.Camera.UnscaledPosition;
		TileDrawInfo data = new();

		renderer.GetTileDrawData(i, j, data.tileCache, data.typeCache, ref data.tileFrameX, ref data.tileFrameY, out data.tileWidth, out data.tileHeight, out data.tileTop, out data.halfBrickHeight, out data.addFrX, out data.addFrY, out data.tileSpriteEffect, out data.glowTexture, out data.glowSourceRect, out data.glowColor);
		DrawSingleTile(renderer, data, solidLayer, -1, unscaledPosition, screenOffset.Value, i, j);
	}

	/// <summary> Gets common visual info related to the tile at the given coordinates, such as painted color. </summary>
	/// <param name="i"> The X coordinate. </param>
	/// <param name="j"> The Y coordinate.</param>
	/// <param name="color"> The color of the tile affected by coatings. </param>
	/// <param name="texture"> The default tile texture, painted. </param>
	/// <returns> Whether the tile should be drawn based on <see cref="TileDrawing.IsVisible"/>. </returns>
	public static bool GetVisualInfo(int i, int j, out Color color, out Texture2D texture)
	{
		var t = Main.tile[i, j];
		color = t.IsTileFullbright ? Color.White : Lighting.GetColor(i, j);
		texture = TextureAssets.Tile[t.TileType].Value;

		if (!TileDrawing.IsVisible(t))
			return false;

		if (t.TileColor != PaintID.None)
		{
			var painted = Main.instance.TilePaintSystem.TryGetTileAndRequestIfNotReady(t.TileType, 0, t.TileColor);
			texture = painted ?? texture;
		}

		return true;
	}

	/// <summary> Gets a tint based on the paint type at the given coordinates.<br/>
	/// Useful for coloring non-default tile textures, like glowmasks. Otherwise, <see cref="GetVisualInfo"/> should be used. </summary>
	/// <param name="i"> The X coordinate. </param>
	/// <param name="j"> The Y coordinate.</param>
	/// <param name="color"> The color to tint. </param>
	public static Color GetTint(int i, int j, Color color)
	{
		var t = Main.tile[i, j];
		int type = Main.tile[i, j].TileColor;
		var paint = WorldGen.paintColor(type);

		if (t.IsTileFullbright)
			color = Color.White;
		else if (type is >= 13 and <= 24) //Deep paints
			color = GetIntensity(1f);
		else
			color = GetIntensity(0.5f);

		return color;

		Color GetIntensity(float value) => color.MultiplyRGB(Color.Lerp(Color.White, paint, value));
	}

	public static Color GetSpelunkerTint(Color color)
	{
		if (color.R < 200)
			color.R = 200;

		if (color.G < 170)
			color.G = 170;

		return color;
	}

	/// <summary> Default tile slope drawing. See overload for more customizeablility. </summary>
	public static void DrawSloped(int i, int j)
	{
		var texture = TextureAssets.Tile[Main.tile[i, j].TileType].Value;
		DrawSloped(i, j, texture, Lighting.GetColor(i, j), TileOffset);
	}

	public static void DrawSloped(int i, int j, Texture2D texture, Color color, Vector2 offset, Point overrideFrame = default)
	{
		Tile tile = Main.tile[i, j];
		int frameX = tile.TileFrameX;
		int frameY = tile.TileFrameY;

		if (overrideFrame != Point.Zero)
		{
			frameX = overrideFrame.X;
			frameY = overrideFrame.Y;
		}

		int width = 16;
		int height = 16;
		var location = new Vector2(i * 16, j * 16);
		Vector2 offsets = -Main.screenPosition + offset;
		Vector2 drawLoc = location + offsets;

		if (tile.Slope == 0 && !tile.IsHalfBlock || Main.tileSolid[tile.TileType] && Main.tileSolidTop[tile.TileType]) //second one should be for platforms
			Main.spriteBatch.Draw(texture, drawLoc, new Rectangle(frameX, frameY, width, height), color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
		else if (tile.IsHalfBlock)
			Main.spriteBatch.Draw(texture, new Vector2(drawLoc.X, drawLoc.Y + 8), new Rectangle(frameX, frameY, width, 8), color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
		else
		{
			SlopeType b = tile.Slope;
			Rectangle frame;
			Vector2 drawPos;

			if (b is SlopeType.SlopeDownLeft or SlopeType.SlopeDownRight)
			{
				int length;
				int height2;

				for (int a = 0; a < 8; ++a)
				{
					if (b == SlopeType.SlopeDownRight)
					{
						length = 16 - a * 2 - 2;
						height2 = 14 - a * 2;
					}
					else
					{
						length = a * 2;
						height2 = 14 - length;
					}

					frame = new Rectangle(frameX + length, frameY, 2, height2);
					drawPos = new Vector2(i * 16 + length, j * 16 + a * 2) + offsets;
					Main.spriteBatch.Draw(texture, drawPos, frame, color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.0f);
				}

				frame = new Rectangle(frameX, frameY + 14, 16, 2);
				drawPos = new Vector2(i * 16, j * 16 + 14) + offsets;
				Main.spriteBatch.Draw(texture, drawPos, frame, color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.0f);
			}
			else
			{
				int length;
				int height2;

				for (int a = 0; a < 8; ++a)
				{
					if (b == SlopeType.SlopeUpLeft)
					{
						length = a * 2;
						height2 = 16 - length;
					}
					else
					{
						length = 16 - a * 2 - 2;
						height2 = 16 - a * 2;
					}

					frame = new Rectangle(frameX + length, frameY + 16 - height2, 2, height2);
					drawPos = new Vector2(i * 16 + length, j * 16) + offsets;
					Main.spriteBatch.Draw(texture, drawPos, frame, color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.0f);
				}

				drawPos = new Vector2(i * 16, j * 16) + offsets;
				frame = new Rectangle(frameX, frameY, 16, 2);
				Main.spriteBatch.Draw(texture, drawPos, frame, color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.0f);
			}
		}
	}

	/// <summary> Checks if the tile at i, j is a chest, and returns what kind of chest it is if so. </summary>
	/// <param name="i">X position.</param>
	/// <param name="j">Y position.</param>
	/// <param name="type">The type of the chest, if any.</param>
	/// <returns>If the tile is a chest or not.</returns>
	public static bool TryGetChestID(int i, int j, out VanillaChestID type)
	{
		Tile tile = Main.tile[i, j];
		type = VanillaChestID.Wood;

		if (tile.HasTile && tile.TileType == TileID.Containers && tile.TileFrameX % 36 == 0 && tile.TileFrameY == 0)
		{
			type = (VanillaChestID)(tile.TileFrameX / 36);
			return true;
		}

		return false;
	}

	/// <summary> Returns whether <see cref="Tile.HasTile"/> and <see cref="Tile.TileType"/> equals <paramref name="type"/>. </summary>
	public static bool Active(this Tile tile, int type) => tile.HasTile && tile.TileType == type;

	/// <summary> Mutually merges the tile of <paramref name="thisType"/> with all of the ids in <paramref name="mergeTypes"/>. </summary>
	/// <param name="thisType"> The tile to merge with. </param>
	/// <param name="mergeTypes"> All other tiles to merge with. </param>
	public static void Merge(int thisType, params int[] mergeTypes)
	{
		foreach (int type in mergeTypes)
		{
			Main.tileMerge[thisType][type] = true;
			Main.tileMerge[type][thisType] = true;
		}
	}
}