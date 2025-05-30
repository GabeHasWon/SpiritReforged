using MonoMod.RuntimeDetour;
using System.Reflection;
using Terraria.DataStructures;

namespace SpiritReforged.Common.TileCommon;

internal class ConvertAdjacentSet : GlobalTile
{
	/// <summary> Whether a tile is being converted by adjacents. Should be checked in <see cref="ModBlockType.Convert"/>. </summary>
	public static bool Converting { get; private set; }

	private static int Conversion;
	private static Hook CustomHook = null;

	public override void Load()
	{
		var type = typeof(Mod).Assembly.GetType("Terraria.WorldGen");
		MethodInfo info = type.GetMethod("Convert", BindingFlags.Static | BindingFlags.Public, [typeof(int), typeof(int), typeof(int), typeof(int), typeof(bool), typeof(bool)]);
		CustomHook = new Hook(info, HookConvert, true);
	}

	public static void HookConvert(Action<int, int, int, int, bool, bool> orig, int x, int y, int conversionType, int size, bool tiles, bool walls)
	{
		Conversion = conversionType;
		orig(x, y, conversionType, size, tiles, walls);
		Conversion = -1;
	}

	public override bool TileFrame(int i, int j, int type, ref bool resetFrame, ref bool noBreak)
	{
		if (Conversion == -1 || !SpiritSets.ConvertsByAdjacent[type])
			return true;

		Converting = true;
		TileLoader.Convert(i, j, Conversion);
		Converting = false;

		return noBreak = true;
	}

	public override void Unload()
	{
		CustomHook?.Undo();
		CustomHook = null;
	}

	public static bool CheckAnchors(int i, int j, int type)
	{
		if (TileObjectData.GetTileData(type, 0) is TileObjectData data)
		{
			if (data.AnchorBottom != AnchorData.Empty && data.isValidTileAnchor(Framing.GetTileSafely(i, j + 1).TileType))
				return true;

			if (data.AnchorLeft != AnchorData.Empty && data.isValidTileAnchor(Framing.GetTileSafely(i - 1, j).TileType))
				return true;

			if (data.AnchorRight != AnchorData.Empty && data.isValidTileAnchor(Framing.GetTileSafely(i + 1, j).TileType))
				return true;

			if (data.AnchorTop != AnchorData.Empty && data.isValidTileAnchor(Framing.GetTileSafely(i, j - 1).TileType))
				return true;
		}

		return false;
	}
}