using Terraria.WorldBuilding;
using SpiritReforged.Content.Underground.Tiles;
using SpiritReforged.Content.Ocean.Hydrothermal.Tiles;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

/// <summary> A pass for cave filler, inserted after "Micro Biomes". </summary>
internal class CaveDecorMicropass : Micropass
{
	public override string WorldGenName => "Cave Objects";

	public override int GetWorldGenIndexInsert(List<GenPass> passes, ref bool afterIndex)
	{
		afterIndex = true;
		return passes.FindIndex(genpass => genpass.Name.Equals("Micro Biomes"));
	}

	public override void Run(GenerationProgress progress, Terraria.IO.GameConfiguration config)
	{
		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.Caves");

		int maxCarts = Main.maxTilesX / WorldGen.WorldSizeSmallX * 17;
		int maxShrooms = Main.maxTilesX / WorldGen.WorldSizeSmallX * WorldGen.genRand.Next(3, 5);

		WorldMethods.Generate(CreateCart, maxCarts, out _, maxTries: 1800);
		WorldMethods.Generate(CreateShroom, maxShrooms, out _, new Rectangle(20, (int)Main.rockLayer, Main.maxTilesX - 40, Main.maxTilesY - (int)Main.rockLayer - 20));
	}

	private static bool CreateCart(int x, int y)
	{
		while (Main.tile[x, y + 1].TileType != TileID.MinecartTrack && y < Main.maxTilesY - 20)
			y++;

		int type = ModContent.TileType<OreCarts>();
		WorldGen.PlaceTile(x, y, type, true, style: WorldGen.genRand.Next(8));

		return Main.tile[x, y].TileType == type;
	}

	private static bool CreateShroom(int x, int y)
	{
		WorldMethods.FindGround(x, ref y);

		if (Main.tile[x, y].TileType != TileID.Stone || Main.tile[x, y - 1].CheckingLiquid)
			return false;

		y--;
		int type = ModContent.TileType<ObsidianShroom>();
		WorldGen.PlaceTile(x, y, type, true);

		if (Main.tile[x, y].TileType == type)
			WorldGen.OreRunner(x, y + 1, WorldGen.genRand.Next(3, 6), WorldGen.genRand.Next(1, 4), (ushort)ModContent.TileType<Magmastone>());

		return Main.tile[x, y].TileType == type;
	}
}