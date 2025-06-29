using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Common.WorldGeneration.Ecotones;
using SpiritReforged.Common.WorldGeneration.Noise;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using System.Linq;
using Terraria.GameContent.Generation;
using Terraria.WorldBuilding;

namespace SpiritReforged.Content.SaltFlats;

internal class SaltFlatsEcotone : EcotoneBase
{
	[WorldBound]
	public static Rectangle SaltArea;

	public static int AverageY { get; private set; }
	private static FastNoiseLite Noise;

	protected override void InternalLoad()
	{
		On_WorldGen.PlaceSmallPile += PreventSmallPiles;
		On_WorldGen.PlaceTile += PreventLargePiles;
	}

	private static bool PreventSmallPiles(On_WorldGen.orig_PlaceSmallPile orig, int i, int j, int X, int Y, ushort type)
	{
		if (WorldGen.generatingWorld && type == TileID.SmallPiles && SaltArea.Contains(new Point(i, j)))
			return false; //Skips orig

		return orig(i, j, X, Y, type);
	}

	private static bool PreventLargePiles(On_WorldGen.orig_PlaceTile orig, int i, int j, int Type, bool mute, bool forced, int plr, int style)
	{
		if (WorldGen.generatingWorld && Type == TileID.LargePiles && SaltArea.Contains(new Point(i, j)))
			return false; //Skips orig

		return orig(i, j, Type, mute, forced, plr, style);
	}

	public override void AddTasks(List<GenPass> tasks, List<EcotoneSurfaceMapping.EcotoneEntry> entries)
	{
		if (tasks.FindIndex(x => x.Name == "Pyramids") is int index && index != -1)
			tasks.Insert(index, new PassLegacy("Salt Flats", BaseGeneration(entries)));
	}

	private static bool CanGenerate(List<EcotoneSurfaceMapping.EcotoneEntry> entries, out (int, int) bounds)
	{
		const int offX = EcotoneSurfaceMapping.TransitionLength + 1; //Removes forest patches on the left side
		bounds = (0, 0);

		int spawn = Main.maxTilesX / 2; //DEBUG
		var valid = entries.Where(x => x.Start.X < spawn && x.End.X > spawn);

		if (valid.Any())
		{
			var e = valid.First();
			bounds = (e.Start.X - offX, e.End.X);

			return true;
		}

		return false;

		/*var validEntries = entries.Where(x => x.SurroundedBy("Desert", "Snow") && Math.Abs(x.Start.Y - x.End.Y) < 120);
		if (!validEntries.Any())
			return false;

		var entry = validEntries.ElementAt(WorldGen.genRand.Next(validEntries.Count()));
		if (entry is null)
			return false;

		bounds = (entry.Start.X - offX, entry.End.X);
		return true;*/
	}

	private static WorldGenLegacyMethod BaseGeneration(List<EcotoneSurfaceMapping.EcotoneEntry> entries) => (progress, _) =>
	{
		if (!CanGenerate(entries, out var bounds))
			return;

		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.SaltFlats");

		int xLeft = bounds.Item1;
		int xRight = bounds.Item2;

		int yLeft = EcotoneSurfaceMapping.TotalSurfaceY[(short)xLeft];
		int yRight = EcotoneSurfaceMapping.TotalSurfaceY[(short)xRight];

		Noise = new FastNoiseLite(WorldGen.genRand.Next());
		Noise.SetFrequency(0.03f);

		AverageY = (int)MathHelper.Lerp(yLeft, yRight, 0.5f);

		for (int x = xLeft; x < xRight; x++)
		{
			int depth = Math.Min((int)(Math.Sin((float)(x - xLeft) / (xRight - xLeft) * MathHelper.Pi) * 250), 50 + (int)(Noise.GetNoise(x, 300) * 10));
			int y = (int)(Main.worldSurface * 0.35); //Sky height

			while (y < AverageY + depth)
				SetTile(x, y++, GetSurfaceY(x, y));
		}

		SaltArea = new Rectangle(xLeft, yLeft - 10, xRight - xLeft, yRight - yLeft + 20);
	};

	private static void SetTile(int x, int y, int baseLine)
	{
		var t = Main.tile[x, y];

		if (y < baseLine)
		{
			t.ClearEverything();
		}
		else
		{
			t.HasTile = true;
			t.TileType = (ushort)ModContent.TileType<SaltBlock>();

			t.WallType = WallID.None;
		}
	}

	/// <summary> Gets a terrain Y value based on <see cref="AverageY"/>. </summary>
	private static int GetSurfaceY(int x, int y) => AverageY + (int)(Noise.GetNoise(x, 600) * 2);
}