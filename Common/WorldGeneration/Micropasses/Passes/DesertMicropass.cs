using SpiritReforged.Content.Desert.Tiles.Amber;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class DesertMicropass : Micropass
{
	private const int DefaultPatchScale = 10;

	public override string WorldGenName => "Desert Extras";

	public override int GetWorldGenIndexInsert(List<GenPass> passes, ref bool afterIndex)
	{
		afterIndex = true;
		return passes.FindIndex(genpass => genpass.Name.Equals("Webs"));
	}

	public override void Run(GenerationProgress progress, Terraria.IO.GameConfiguration config)
	{
		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.DesertExtras");

		int generated = 0;
		int maxAmount = 10 * (WorldGen.GetWorldSize() + 1);

		int top = (int)(Main.worldSurface * 1.2f);
		Rectangle region = new(GenVars.desertHiveLeft, top, GenVars.desertHiveRight - GenVars.desertHiveLeft, GenVars.desertHiveLow - top);
		
		for (int a = 0; a < 200; a++)
		{
			var coords = WorldGen.genRand.NextVector2FromRectangle(region);
			int i = (int)coords.X;
			int j = (int)coords.Y;

			if (Main.tile[i, j].TileType == TileID.Sand)
			{
				int scale = WorldGen.genRand.Next(DefaultPatchScale / 2, DefaultPatchScale * 2);

				WorldGen.OreRunner(i, j - 3, scale + 4, WorldGen.genRand.Next(1, 8), TileID.Sand);
				WorldGen.OreRunner(i, j, scale, WorldGen.genRand.Next(1, 8), (ushort)ModContent.TileType<PolishedAmber>());
				AddFossils(i, j, WorldGen.genRand.Next(scale / 2, (int)(scale * 1.5f)), scale);

				if (++generated >= maxAmount)
					break;
			}
		}
	}

	/// <summary> Randomly converts <see cref="PolishedAmber"/> into <see cref="AmberFossil"/>s around the provided coordinates, in an area based on <paramref name="scale"/>. </summary>
	private static void AddFossils(int i, int j, int count, int scale)
	{
		for (int c = 0; c < count; c++)
		{
			var coords = (new Vector2(i, j) + WorldGen.genRand.NextVector2Unit() * WorldGen.genRand.Next(scale)).ToPoint();
			
			if (Framing.GetTileSafely(coords).TileType == ModContent.TileType<PolishedAmber>())
				Framing.GetTileSafely(coords).TileType = (ushort)ModContent.TileType<AmberFossil>();
		}
	}
}