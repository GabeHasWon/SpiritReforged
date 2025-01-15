﻿using static Terraria.GameContent.Drawing.TileDrawing;

namespace SpiritReforged.Common.TileCommon.TileSway;

internal class TileSwaySystem : ModSystem
{
	public static TileSwaySystem Instance;

	public static event Action PreUpdateWind;
	private static readonly Dictionary<int, int> tileSwayTypes = [];

	public double TreeWindCounter { get; private set; }
	public double GrassWindCounter { get; private set; }
	public double SunflowerWindCounter { get; private set; }

	/// <summary> Checks whether this tile type is an <see cref="ISwayTile"/> and outputs the counter corresponding to <see cref="ISwayTile.Style"/>. </summary>
	/// <param name="type"> The tile type. </param>
	/// <param name="counter"> The counter type. </param>
	/// <returns> Whether this tile sways (implements ISwayTile) </returns>
	public static bool DoesSway(int type, out TileCounterType counter)
	{
		if (tileSwayTypes.TryGetValue(type, out int value))
		{
			counter = (TileCounterType)value;
			return true;
		}

		counter = (TileCounterType)(-1);
		return false;
	}

	public override void Load() => Instance = this;

	public override void PostSetupContent()
	{
		var modTiles = ModContent.GetContent<ModTile>();

		foreach (var tile in modTiles)
		{
			if (tile is ISwayTile sway)
			{
				tileSwayTypes.Add(tile.Type, sway.Style);
				var counter = (TileCounterType)sway.Style;

				if (counter is TileCounterType.MultiTileVine or TileCounterType.MultiTileGrass) //Assign required sets
					TileID.Sets.MultiTileSway[tile.Type] = true;
				else if (counter == TileCounterType.Vine)
					TileID.Sets.VineThreads[tile.Type] = true;
				else if (counter == TileCounterType.ReverseVine)
					TileID.Sets.ReverseVineThreads[tile.Type] = true;
			}
		}
	}

	public override void PreUpdateWorld()
	{
		if (Main.dedServ)
			return;

		PreUpdateWind?.Invoke();

		double num = Math.Abs(Main.WindForVisuals);
		num = Utils.GetLerpValue(0.08f, 1.2f, (float)num, clamped: true);

		TreeWindCounter += 0.0041666666666666666 + 0.0041666666666666666 * num * 2.0;
		GrassWindCounter += 0.0055555555555555558 + 0.0055555555555555558 * num * 4.0;
		SunflowerWindCounter += 0.002380952380952 + 0.0023809523809523810 * num * 5.0;
	}
}
