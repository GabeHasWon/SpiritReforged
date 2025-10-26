using SpiritReforged.Common.Easing;
using SpiritReforged.Common.WorldGeneration.Microbiomes;
using SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes.Ziggurat;
using SpiritReforged.Common.WorldGeneration.Noise;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class ZigguratMicropass : Micropass
{
	public override string WorldGenName => "Ziggurat";

	public override int GetWorldGenIndexInsert(List<GenPass> tasks, ref bool afterIndex) => tasks.FindIndex(x => x.Name == "Pyramids");
	public override void Run(GenerationProgress progress, GameConfiguration config)
	{
		const int scanRadius = 50;
		const int range = ZigguratBiome.Width / 2;

		Rectangle loc = GenVars.UndergroundDesertLocation;
		for (int a = 0; a < 300; a++)
		{
			int rangeLeft = WorldGen.genRand.Next(loc.Left, Math.Max((int)(loc.Center().X - range), loc.Left + 20));
			int rangeRight = WorldGen.genRand.Next(Math.Min((int)(loc.Center().X + range), loc.Right - 20), loc.Right);

			int x = WorldGen.genRand.Next([rangeLeft, rangeRight]);
			int y = loc.Y - 40;

			if (!WorldUtils.Find(new(x, y), new Searches.Down(1500).Conditions(new Conditions.IsSolid()), out Point foundPos))
				return; // ?? big hole where the desert is?

			Point zigguratPos = new(foundPos.X, foundPos.Y + (int)(ZigguratBiome.Height * 0.3f));

			Dictionary<ushort, int> typeToCount = [];
			WorldUtils.Gen(zigguratPos, new Shapes.Rectangle(new Rectangle(-(ZigguratBiome.Width / 2), -(ZigguratBiome.Height / 2), ZigguratBiome.Width, ZigguratBiome.Height)), new Actions.TileScanner(TileID.Sand, TileID.SandstoneBrick).Output(typeToCount));

			if (typeToCount[TileID.Sand] < scanRadius * scanRadius * 0.5f || typeToCount[TileID.SandstoneBrick] > 10)
				continue;

			CreateDunes(foundPos.X - 80, foundPos.X + 80, foundPos.Y, 10);
			Microbiome.Create<ZigguratBiome>(zigguratPos);

			break;
		}
	}

	public static void CreateDunes(int left, int right, int startY, int duneHeight)
	{
		FastNoiseLite noise = new(WorldGen.genRand.Next());
		noise.SetFrequency(0.03f);

		int y = WorldMethods.FindGround(left, startY);

		for (int x = left; x < right; x++)
		{
			int groundY = WorldMethods.FindGround(x, y);

			if (CanCreateColumn(x, groundY))
			{
				float targetY = groundY - (duneHeight + noise.GetNoise(x, 100) * duneHeight) * EaseFunction.EaseSine.Ease((float)(x - left) / (right - left));
				y = (int)MathHelper.Lerp(y, targetY, 0.3f);

				FillColumn(x, y, TileID.Sand);
			}
			else
			{
				left = x;
			}
		}

		void FillColumn(int x, int top, ushort tileType)
		{
			int y = top;
			int digMax = (int)Math.Max(4f + noise.GetNoise(x, 100) * 8, 1);
			int dig = 0;

			while (WorldGen.InWorld(x, y, 20) && dig < digMax)
			{
				if (WorldGen.SolidTile3(x, y))
					dig++;

				var tile = Main.tile[x, y++];

				if (TileID.Sets.GeneralPlacementTiles[tile.TileType])
					tile.ResetToType(tileType);
			}
		}

		static bool CanCreateColumn(int x, int y) => Main.tile[x, y].TileType != ModContent.TileType<SaltBlockReflective>() && Main.tile[x, y].TileType != ModContent.TileType<SaltBlockDull>();
	}
}