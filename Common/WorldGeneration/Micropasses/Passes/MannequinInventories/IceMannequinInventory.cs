using SpiritReforged.Common.ModCompat;
using SpiritReforged.Content.Snow;
using Terraria.DataStructures;
using Terraria.GameContent.Biomes.CaveHouse;
using Terraria.GameContent.Tile_Entities;
using Terraria.Utilities;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes.MannequinInventories;

internal class IceMannequinInventory : MannequinInventory
{
	private static WeightedRandom<int> AccType;

	public override HouseType Biome => HouseType.Ice;

	public override void Setup() 
	{
		AccType = new(WorldGen.genRand);
		AccType.Add(ItemID.IceSkates, 0.5f);
		AccType.Add(ModContent.ItemType<FrostGiantBelt>(), 0.4f);
		AccType.Add(ItemID.BlizzardinaBottle, 0.1f);
		AccType.Add(ItemID.FlurryBoots, 0.2f);
		AccType.Add(ItemID.Compass, 0.05f);

		if (CrossMod.Thorium.Enabled)
		{
			if (CrossMod.Thorium.TryFind("FrostburnPouch", out ModItem frostburnPouch))
				AccType.Add(frostburnPouch.Type, 0.1f);
		}
	}

	public override void SetMannequin(Point16 position)
	{
		Item[] inv = [new(ItemID.EskimoHood), new(ItemID.EskimoCoat), new(ItemID.EskimoPants),
			new(), new(), new(), new(), new()];

		float chance = Main.rand.NextFloat();

		if (chance < .10f)
		{
			inv[0] = new(ItemID.VikingHelmet);
			inv[1] = new();
			inv[2] = new();
		}

		if (!TileEntity.ByPosition.TryGetValue(position, out TileEntity te) || te is not TEDisplayDoll mannequin)
		{
			int id = TEDisplayDoll.Place(position.X, position.Y);
			mannequin = TileEntity.ByID[id] as TEDisplayDoll;
		}

		int slot = WorldGen.genRand.Next(5) + 3;
		inv[slot] = new Item(AccType);
		UndergroundHouseMicropass.teDollInventory.SetValue(mannequin, inv);
	}
}
