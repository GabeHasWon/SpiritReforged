using System.Linq;

namespace SpiritReforged.Common.WorldGeneration;

/// <summary> Compensates for hardcoded vanilla gen passes by adding to <see cref="Regions"/>. </summary>
public class WorldDetours : ModSystem
{
	public readonly record struct Region(Rectangle Area, Context Context);

	public enum Context
	{
		/// <summary> Prevents piles from generating. </summary>
		Piles,
		/// <summary> Converts lava to water. </summary>
		Lava
	}

	[WorldBound]
	public static readonly HashSet<Region> Regions = [];

	public static bool AnyContains(int i, int j, Context context) => Regions.Any(x => x.Area.Contains(i, j) && x.Context == context);

	public override void Load()
	{
		On_WorldGen.PlaceSmallPile += PreventSmallPiles;
		On_WorldGen.PlaceTile += PreventLargePiles;
	}

	private static bool PreventSmallPiles(On_WorldGen.orig_PlaceSmallPile orig, int i, int j, int X, int Y, ushort type)
	{
		if (WorldGen.generatingWorld && type == TileID.SmallPiles && AnyContains(i, j, Context.Piles))
			return false; //Skips orig

		return orig(i, j, X, Y, type);
	}

	private static bool PreventLargePiles(On_WorldGen.orig_PlaceTile orig, int i, int j, int Type, bool mute, bool forced, int plr, int style)
	{
		if (WorldGen.generatingWorld && Type == TileID.LargePiles && AnyContains(i, j, Context.Piles))
			return false; //Skips orig

		return orig(i, j, Type, mute, forced, plr, style);
	}

	public override void PostWorldGen()
	{
		foreach (var a in Regions)
		{
			if (a.Context == Context.Lava)
				StopLavaInArea(a.Area);
		}

		Regions.Clear();
	}

	/// <summary> Counteracts auto water conversion in the "Final Cleanup" Genpass. </summary>
	private static void StopLavaInArea(Rectangle area)
	{
		for (int i = area.X; i < area.X + area.Width; i++)
		{
			for (int j = area.Y; j < area.Y + area.Height; j++)
			{
				var tile = Main.tile[i, j];

				if (tile.LiquidType == LiquidID.Lava)
					tile.LiquidType = LiquidID.Water;
			}
		}
	}
}