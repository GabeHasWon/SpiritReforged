using Terraria.WorldBuilding;
using SpiritReforged.Content.Underground.Tiles;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes;

internal class CartsMicropass : Micropass
{
	public override string WorldGenName => "Carts";

	public override int GetWorldGenIndexInsert(List<GenPass> passes, ref bool afterIndex)
	{
		afterIndex = true;
		return passes.FindIndex(genpass => genpass.Name.Equals("Micro Biomes"));
	}

	public override void Run(GenerationProgress progress, Terraria.IO.GameConfiguration config)
	{
		const int maxTries = 5000; //Failsafe

		progress.Message = Language.GetTextValue("Mods.SpiritReforged.Generation.Caves");

		int maxCarts = Main.maxTilesX / WorldGen.WorldSizeSmallX * 17;
		int carts = 0;

		for (int t = 0; t < maxTries; t++) //Generate stacked pots
		{
			int x = Main.rand.Next(20, Main.maxTilesX - 20);
			int y = Main.rand.Next((int)GenVars.worldSurfaceHigh, Main.maxTilesY - 20);

			while (Main.tile[x, y].TileType != TileID.MinecartTrack && y < Main.maxTilesY - 20)
				y++;

			if (CreateCart(x, y - 1) && ++carts >= maxCarts)
				break;
		}
	}

	private static bool CreateCart(int x, int y)
	{
		int type = ModContent.TileType<OreCarts>();
		WorldGen.PlaceTile(x, y, type, true, style: Main.rand.Next(8));

		return Main.tile[x, y].TileType == type;
	}
}