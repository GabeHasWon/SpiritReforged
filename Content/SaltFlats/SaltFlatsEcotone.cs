using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Common.WorldGeneration.Ecotones;
using SpiritReforged.Common.WorldGeneration.Noise;
using SpiritReforged.Common.WorldGeneration.SecretSeeds;
using SpiritReforged.Common.WorldGeneration.SecretSeeds.Seeds;
using SpiritReforged.Content.SaltFlats.Tiles;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using System.Linq;
using Terraria.DataStructures;
using Terraria.GameContent.Generation;
using Terraria.WorldBuilding;

namespace SpiritReforged.Content.SaltFlats;

internal class SaltFlatsEcotone : EcotoneBase
{
	public const int SurfaceNoiseLength = 2;

	[WorldBound]
	public static Rectangle SaltArea;

	public static int AverageY { get; private set; }
	private static FastNoiseLite Noise;

	public override void AddTasks(List<GenPass> tasks, List<EcotoneSurfaceMapping.EcotoneEntry> entries)
	{
		if (tasks.FindIndex(x => x.Name == "Pyramids") is int index && index != -1)
			tasks.Insert(index, new PassLegacy("Salt Flats", BaseGeneration(entries)));
	}

	private static bool CanGenerate(List<EcotoneSurfaceMapping.EcotoneEntry> entries, out (int, int) bounds)
	{
		const int offX = EcotoneSurfaceMapping.TransitionLength * 2 + 1; //Removes forest patches on the left side

		bounds = (0, 0);
		int spawn = Main.maxTilesX / 2;

		if (SecretSeedSystem.WorldSecretSeed == SecretSeedSystem.GetSeed<SaltSeed>())
		{
			var valid = entries.Where(x => x.Start.X < spawn && x.End.X > spawn);
			if (valid.Any())
			{
				var e = valid.First();
				bounds = (e.Start.X - offX, e.End.X);

				return true;
			}

			return false;
		}
		else
		{
			//Uniquely, salt flats cannot normally generate over spawn
			var validEntries = entries.Where(x => x.SurroundedBy("Desert", "Snow") && !(x.Start.X < spawn && x.End.X > spawn) && Math.Abs(x.Start.Y - x.End.Y) < 120);
			if (!validEntries.Any())
				return false;

			var entry = validEntries.ElementAt(WorldGen.genRand.Next(validEntries.Count()));
			if (entry is null)
				return false;

			bounds = (entry.Start.X - offX, entry.End.X);
			return true;
		}
	}

	private static WorldGenLegacyMethod BaseGeneration(List<EcotoneSurfaceMapping.EcotoneEntry> entries) => (progress, _) =>
	{
		if (!CanGenerate(entries, out var bounds))
			return;

		const float baseCurveStrength = 5;
		const int baseDepth = 18;
		const float islandMargin = 0.15f;

		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.SaltFlats");

		HashSet<Point16> islands = [];
		int xLeft = bounds.Item1;
		int xRight = bounds.Item2;

		int yLeft = EcotoneSurfaceMapping.TotalSurfaceY[(short)xLeft];
		int yRight = EcotoneSurfaceMapping.TotalSurfaceY[(short)xRight];
		int fullWidth = xRight - xLeft;

		Noise = new FastNoiseLite(WorldGen.genRand.Next());
		Noise.SetFrequency(0.03f);

		//Select the lowest of both neighboring biomes
		AverageY = Math.Max(yLeft, yRight) + 1; //(int)MathHelper.Lerp(yLeft, yRight, 0.5f);

		for (int i = 0; i < 2; i++)
			HillBorder((i == 0) ? xLeft : xRight, i == 0);

		for (int x = xLeft; x < xRight; x++)
		{
			int xProgress = x - xLeft;

			int depth = Math.Min((int)(Math.Sin((float)xProgress / fullWidth * MathHelper.Pi) * (baseCurveStrength * baseDepth)), baseDepth + (int)(Noise.GetNoise(x, 600) * 8));
			int liningDepth = (int)((8 + (int)(Noise.GetNoise(x, 500) * 6)) * ((float)depth / baseDepth));

			int y = (int)(Main.worldSurface * 0.35); //Sky height

			while (y < AverageY + depth + liningDepth)
			{
				int type = (y < AverageY + depth) ? ModContent.TileType<SaltBlockReflective>() : ModContent.TileType<SaltBlockDull>();

				if (WorldMethods.CloudsBelow(x, y, out int addY))
				{
					y += addY;
					continue;
				}

				int surface = GetSurfaceY(x, y);

				if (surface == y && (xProgress < fullWidth * islandMargin || xProgress > fullWidth * (1f - islandMargin)) && WorldGen.genRand.NextBool(50))
					islands.Add(new(x, y - 1)); //Add an island position for later

				SetTile(x, y++, surface, type, type == ModContent.TileType<SaltBlockReflective>());
			}
		}

		SaltArea = new Rectangle(xLeft, yLeft - 10, Math.Abs(xRight - xLeft), Math.Abs(yRight - yLeft) + 20);
		WorldDetours.Regions.Add(new(SaltArea, WorldDetours.Context.Piles));

		AddIslands(islands);
	};

	private static void AddIslands(IEnumerable<Point16> coords)
	{
		ushort[] skipTiles = [(ushort)ModContent.TileType<SaltBlockReflective>(), (ushort)ModContent.TileType<SaltBlockDull>()];

		foreach (var c in coords)
		{
			int halfWidth = WorldGen.genRand.Next(2, 11);
			ShapeData data = new();

			WorldUtils.Gen(new(c.X, c.Y + 1), new Shapes.Slime(halfWidth, 1, 0.25), Actions.Chain(new Modifiers.SkipTiles(skipTiles), new Actions.PlaceTile((ushort)ModContent.TileType<SaltBlockDull>())).Output(data));
			WorldUtils.Gen(new(c.X, c.Y + 1), new ModShapes.All(data), Actions.Chain(new Actions.SetFrames(true), new Modifiers.Dither(0.2), new Modifiers.IsTouchingAir(), new Actions.Smooth()));
			WorldUtils.Gen(new(c.X, c.Y + 1), new ModShapes.All(data), Actions.Chain(new Modifiers.Expand(1), new Modifiers.Dither(0.9), new Actions.Custom((i, j, args) =>
			{
				Placer.PlaceTile<Saltwort>(i, j);
				return true;
			})));
		}
	}

	private static void HillBorder(int xCoord, bool left) //Linearly smooths neighboring biome heights to match the salt flat. Could be improved
	{
		const int length = 25;

		if (left)
		{
			for (int x = xCoord; x > xCoord - length; x--)
			{
				int y = (int)(Main.worldSurface * 0.35); //Sky height
				int depth = (int)MathHelper.Lerp(GetSurfaceY(x, y), EcotoneSurfaceMapping.TotalSurfaceY[(short)(xCoord - length)], -(float)(x - xCoord) / length);

				Clear(x, y, depth);
			}
		}
		else
		{
			for (int x = xCoord; x < xCoord + length; x++)
			{
				int y = (int)(Main.worldSurface * 0.35); //Sky height
				int depth = (int)MathHelper.Lerp(GetSurfaceY(x, y), EcotoneSurfaceMapping.TotalSurfaceY[(short)(xCoord + length)], (float)(x - xCoord) / length);

				Clear(x, y, depth);
			}
		}

		static void Clear(int x, int y, int depth)
		{
			while (y < depth + 3)
			{
				if (WorldMethods.CloudsBelow(x, y, out int addY))
				{
					y += addY;
					continue;
				}

				if (y >= depth)
					Main.tile[x, y].WallType = WallID.None;
				else
					Main.tile[x, y].ClearEverything();

				y++;
			}
		}
	}

	private static void SetTile(int x, int y, int baseLine, int type, bool clearWall = true)
	{
		var t = Main.tile[x, y];

		if (!TileID.Sets.GeneralPlacementTiles[t.TileType])
			return;

		if (y < baseLine)
		{
			t.ClearEverything();

			if (y == AverageY)
			{
				t.LiquidAmount = 255;
				t.LiquidType = LiquidID.Water;
			}
		}
		else
		{
			t.HasTile = true;
			t.TileType = (ushort)type;
			t.Slope = SlopeType.Solid;

			if (clearWall)
				t.WallType = WallID.None;
		}
	}

	/// <summary> Gets a terrain Y value based on <see cref="AverageY"/>. </summary>
	private static int GetSurfaceY(int x, int y) => AverageY + (int)(Noise.GetNoise(x, 600) * SurfaceNoiseLength);
}