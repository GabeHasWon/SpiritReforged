using SpiritReforged.Common.WorldGeneration.Ecotones;
using SpiritReforged.Common.WorldGeneration.Noise;
using SpiritReforged.Content.SaltFlats.Tiles;
using System.Linq;
using Terraria.GameContent.Generation;
using Terraria.WorldBuilding;

namespace SpiritReforged.Content.SaltFlats;

internal class SaltFlatsEcotone : EcotoneBase
{
	public static int AverageY { get; private set; }
	private static FastNoiseLite Noise;

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

		Noise = new FastNoiseLite(WorldGen.genRand.Next());
		Noise.SetFrequency(0.08f);

		AverageY = (int)MathHelper.Lerp(EcotoneSurfaceMapping.TotalSurfaceY[(short)xLeft], EcotoneSurfaceMapping.TotalSurfaceY[(short)xRight], 0.5f);
		int depth = 40;

		for (int x = xLeft; x < xRight; x++)
		{
			int y = (int)(Main.worldSurface * 0.35); //Sky height

			while (y < AverageY + depth)
				SetTile(x, y++);
		}
	};

	private static void SetTile(int x, int y)
	{
		var t = Main.tile[x, y];

		if (y < GetSurfaceY(x, y))
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