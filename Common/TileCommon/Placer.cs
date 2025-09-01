using SpiritReforged.Common.WorldGeneration;
using System.Linq;
using Terraria.DataStructures;
using Terraria.Utilities;

namespace SpiritReforged.Common.TileCommon;

public struct PlaceAttempt(bool success)
{
	/// <summary> Whether the last chained step has succeeded. </summary>
	public bool success = success;
	/// <summary> Can only be safely used if <see cref="success"/> is true. </summary>
	public TileObject data;

	public readonly Point16 Coords => new(data.xCoord, data.yCoord);
}

/// <summary> Includes helper methods related to placing tiles. </summary>
public static class Placer
{
	public static int HerbRadius => WorldGen.GetWorldSize() switch
	{
		1 => 45,
		2 => 61,
		_ => 31
	};

	/// <summary> Picks between <see cref="WorldGen.genRand"/> and <see cref="Main.rand"/> depending on <see cref="WorldGen.generatingWorld"/>. </summary>
	public static UnifiedRandom Rand => WorldGen.generatingWorld ? WorldGen.genRand : Main.rand;
	private static readonly Point16[] CardinalDirections = [new Point16(0, -1), new Point16(-1, 0), new Point16(1, 0), new Point16(0, 1)];

	private static readonly int[] Replaceable = [TileID.Plants, TileID.Plants2, TileID.JunglePlants, TileID.JunglePlants2, 
		TileID.CorruptPlants, TileID.CrimsonPlants, TileID.HallowedPlants, TileID.HallowedPlants2];

	/// <returns> Whether the tile at the given coordinates is generally replaceable and safe to be cleared. Useful for random plant growth. </returns>
	public static bool IsReplaceable(int i, int j)
	{
		var tile = Main.tile[i, j];

		if (!tile.HasTile)
			return true;

		if (TileObjectData.GetTileData(tile.TileType, 0) != null)
			return false;

		return Replaceable.Contains(tile.TileType) || Main.tileCut[tile.TileType] && TileID.Sets.BreakableWhenPlacing[tile.TileType];
	}

	#region placeAttempt
	/// <summary> Places a tile of <paramref name="type"/> at the given coordinates and returns the resulting <see cref="PlaceAttempt"/>.<br/>
	/// This method is the combined version of <see cref="Check"/> and <see cref="Place"/>. <para/>
	/// <see cref="Placer"/> Contains various methods to chain for additional functionality. </summary>
	/// <param name="type"> The tile type to place. </param>
	/// <param name="style"> The tile style to place. -1 tries to place a random style. </param>
	public static PlaceAttempt PlaceTile(int i, int j, int type, int style = -1)
	{
		var result = Check(i, j, type, style);

		if (result.success)
			return result.Place();

		return new(false);
	}

	/// <summary> Checks whether the given area if valid for placement.<br/>
	/// Allows you to fit additional safety checks between placement like <see cref="IsClear"/>. </summary>
	public static PlaceAttempt Check(int i, int j, int type, int style = -1)
	{
		var data = TileObjectData.GetTileData(type, 0);

		if (data is null)
			style = 0;
		else if (style == -1)
			style = Rand.Next(data.RandomStyleRange);

		if (TileObject.CanPlace(i, j, type, style, 0, out var objectData))
		{
			objectData.random = -1;
			return new(true) { data = objectData };
		}

		return new(false);
	}

	/// <summary> Calls <see cref="WorldMethods.AreaClear"/> attuned to the tile dimensions of <paramref name="type"/>.<br/>
	/// <see cref="PlaceAttempt.success"/> can be true without providing TileObject data after this method runs. </summary>
	public static PlaceAttempt IsClear(this PlaceAttempt a)
	{
		if (a.success)
		{
			var coords = a.Coords;

			int type = a.data.type;
			var data = TileObjectData.GetTileData(type, 0);

			var size = new Point16(1, 1);
			var origin = Point16.Zero;

			if (data is not null)
			{
				size = new Point16(data.Width, data.Height);
				origin = data.Origin;
			}

			if (WorldMethods.AreaClear(coords.X - origin.X, coords.Y - origin.Y, size.X, size.Y, true))
				return a;
		}

		return a with { success = false };
	}

	/// <summary> Actually places the tile from <see cref="Check"/>. See <see cref="PlaceTile"/> for the simplified version. </summary>
	public static PlaceAttempt Place(this PlaceAttempt a)
	{
		if (a.success && TileObject.Place(a.data) && Framing.GetTileSafely(a.Coords).TileType == a.data.type)
			return a;

		return a with { success = false };
	}

	/// <summary> Calls <see cref="TileObjectData.CallPostPlacementPlayerHook"/> for this attempt, and outputs entity of T. </summary>
	public static PlaceAttempt PostPlacement<T>(this PlaceAttempt a, out T entity) where T : class
	{
		if (a.success)
		{
			var t = Framing.GetTileSafely(a.Coords);
			var data = TileObjectData.GetTileData(t);

			if (data != null)
			{
				TileObjectData.CallPostPlacementPlayerHook(a.data.xCoord, a.data.yCoord, a.data.type, a.data.style, 1, a.data.alternate, a.data);

				int i = a.Coords.X;
				int j = a.Coords.Y;

				TileExtensions.GetTopLeft(ref i, ref j);

				if (TileEntity.ByPosition.TryGetValue(new Point16(i, j), out var value) && value is T valueOfType)
				{
					entity = valueOfType;
					return a;
				}
			}
		}

		entity = null;
		return a with { success = false };
	}

	/// <summary> Calls <see cref="NetMessage.SendTileSquare"/> for this attempt.<br/>
	/// This is almost always necessary for tiles placed during gameplay to sync in multiplayer. </summary>
	public static PlaceAttempt Send(this PlaceAttempt a)
	{
		if (a.success)
		{
			var data = TileObjectData.GetTileData(a.data.type, 0);

			if (Main.netMode != NetmodeID.SinglePlayer && data != null)
			{
				int i = a.Coords.X;
				int j = a.Coords.Y;

				TileExtensions.GetTopLeft(ref i, ref j);
				NetMessage.SendTileSquare(-1, i, j, data.Width, data.Height);
			}
		}

		return a;
	}

	///<inheritdoc cref="PlaceTile(int, int, int, int)"/>
	public static PlaceAttempt PlaceTile<T>(int i, int j, int style = -1) where T : ModTile => PlaceTile(i, j, ModContent.TileType<T>(), style);
	#endregion

	/// <summary> Checks the surrounding area for herbs of <paramref name="type"/>.</summary>
	/// <returns> true if fewer than 4 herbs are in range. </returns>
	public static bool CanPlaceHerb(int i, int j, int type) => WorldGen.CountNearBlocksTypes(i, j, HerbRadius, 4, type) < 4;

	/// <summary> Tries to place or extend a vine at the given coordinates. </summary>
	/// <param name="i"> The tile's X coordinate. </param>
	/// <param name="j"> The tile's Y coordinate. </param>
	/// <param name="type"> The tile's type. </param>
	/// <param name="maxLength"> The maximum length this vine can grow. Does NOT instantly grow a vine of the given length. </param>
	/// <param name="reversed"> Whether this vine grows from the ground up. </param>
	/// <param name="sync"> Whether the tile changes should be automatically synced. </param>
	/// <returns> Whether the tile was successfully placed. </returns>
	public static bool GrowVine(int i, int j, int type, int maxLength = 15, bool reversed = false, bool sync = true)
	{
		if (reversed)
		{
			while (Main.tile[i, j + 1].HasTile && Main.tile[i, j + 1].TileType == type)
				j++; //Move to the bottom of the vine

			for (int y = 0; y < maxLength; y++)
			{
				if (Main.tile[i, j].HasTile && Main.tile[i, j].TileType == type)
					j--; //Move to the next available tile above
			}
		}
		else
		{
			while (Main.tile[i, j - 1].HasTile && Main.tile[i, j - 1].TileType == type)
				j--; //Move to the top of the vine

			for (int y = 0; y < maxLength; y++)
			{
				if (Main.tile[i, j].HasTile && Main.tile[i, j].TileType == type)
					j++; //Move to the next available tile below
			}
		}

		if (Main.tile[i, j].TileType == type)
			return false; //The tile already exists; we've hit the max length

		WorldGen.PlaceObject(i, j, type, true);

		if (Main.tile[i, j].TileType != type)
			return false; //Tile placement failed

		if (Main.netMode != NetmodeID.SinglePlayer && sync)
			NetMessage.SendTileSquare(-1, i, j, 1, 1);

		return true;
	}

	/// <summary> Places a plant (or any other object) on any cardinal side of the given tile. This accounts for half bricks and slopes. </summary>
	/// <param name="i">X position to place on.</param>
	/// <param name="j">Y position to place on.</param>
	/// <param name="type">Type of tile to place.</param>
	/// <param name="style">The style of the tile placed.</param>
	public static void PlacePlant(int i, int j, int type, int style = 0)
	{
		int offsetDir = Main.rand.Next(4);
		var coords = new Point16(i, j) + CardinalDirections[offsetDir];
		var self = Framing.GetTileSafely(i, j);
		var current = Framing.GetTileSafely(coords);

		bool badSlope = self.Slope == SlopeType.Solid || offsetDir switch
		{
			0 => !self.TopSlope && !self.IsHalfBlock,
			1 => !self.LeftSlope,
			2 => !self.RightSlope,
			_ => !self.BottomSlope
		};

		if (!current.HasTile && badSlope)
		{
			WorldGen.PlaceTile(coords.X, coords.Y, type, true, style: style);

			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendTileSquare(-1, coords.X, coords.Y);
		}
	}

	/// <inheritdoc cref="PlacePlant(int, int, int, int)"/>
	/// <typeparam name="T">The type of ModTile to place.</typeparam>
	public static void PlacePlant<T>(int i, int j, int style = 0) where T : ModTile => PlacePlant(i, j, ModContent.TileType<T>(), style);
}