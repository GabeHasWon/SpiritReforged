using Terraria.Utilities;

namespace SpiritReforged.Common.ItemCommon.FloatingItem;

public class FloatingItemWorld : ModSystem
{
	private static readonly WeightedRandom<int> floatingItemPool = new();

	public override void PostSetupContent()
	{
		var content = Mod.GetContent<FloatingItem>();

		foreach (var item in content)
			floatingItemPool.Add(item.Type, item.SpawnWeight);
	}

	public override void PreUpdateWorld()
	{
		if (Main.rand.NextBool(2800))
		{
			int floatingItemCount = 0;
			foreach (Item item in Main.ActiveItems)
			{
				if (item.ModItem is FloatingItem)
					floatingItemCount++;
			}

			if (floatingItemCount <= 12)
				SpawnItem();
		}
	}

	private static void SpawnItem()
	{
		int x = Main.rand.Next(600, Main.maxTilesX);
		if (Main.rand.NextBool(2))
			x = Main.rand.Next(Main.maxTilesX * 15, Main.maxTilesX * 16 - 600);

		int y = (int)(Main.worldSurface * 0.35) + 400;

		while (Framing.GetTileSafely(x / 16, y / 16).LiquidAmount < 200)
		{
			if (y / 16 > Main.worldSurface) // If we somehow miss all water, exit
				return;

			y += 16;
		}

		y += 40;
		ItemMethods.NewItemSynced(Entity.GetSource_NaturalSpawn(), floatingItemPool, new Vector2(x, y));
	}
}