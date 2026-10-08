using Terraria.Utilities;

namespace SpiritReforged.Common.ItemCommon.FloatingItem;

public class FloatingItemWorld : ModSystem
{
	public static int FloatingItemCount
	{
		get
		{
			int value = 0;
			foreach (Item item in Main.ActiveItems)
			{
				if (item.ModItem is FloatingItem)
					value++;
			}

			return value;
		}
	}

	private static readonly WeightedRandom<int> _floatingItemPool = new();

	public override void PostSetupContent()
	{
		foreach (FloatingItem item in Mod.GetContent<FloatingItem>())
			_floatingItemPool.Add(item.Type, item.SpawnWeight);
	}

	public override void PreUpdateWorld()
	{
		if (Main.rand.NextBool(2800) && FloatingItemCount <= 12)
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
			ItemMethods.NewItemSynced(Entity.GetSource_NaturalSpawn(), _floatingItemPool, new Vector2(x, y));
		}
	}
}