using SpiritReforged.Common.ModCompat;
using SpiritReforged.Content.Desert.GildedScarab;
using SpiritReforged.Content.Desert.Silk;
using SpiritReforged.Content.Vanilla.Leather.LeatherCloak;
using Terraria.DataStructures;
using Terraria.GameContent.Biomes.CaveHouse;
using Terraria.GameContent.Tile_Entities;
using Terraria.Utilities;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes.MannequinInventories;

internal class DesertMannequinInventory : MannequinInventory
{
	private enum DesertSet : byte
	{
		PharaohVanity,
		CowboyVanity,
		Sundancer,
		Fables_Prowler,
		Thorium_Sandstone,
	}

	private static WeightedRandom<int> AccType;

	public override HouseType Biome => HouseType.Desert;

	public override void Setup() 
	{
		AccType = new(WorldGen.genRand);
		AccType.Add(ItemID.SandBoots, 0.3f);
		AccType.Add(ModContent.ItemType<GildedScarab>(), 0.2f);
		AccType.Add(ModContent.ItemType<LeatherCloakItem>(), 0.4f);
		AccType.Add(ItemID.SandstorminaBottle, 0.1f);

		if (CrossMod.Thorium.Enabled)
		{
			if (CrossMod.Thorium.TryFind("PaddedGrip", out ModItem paddedGrip))
				AccType.Add(paddedGrip.Type, 0.2f);

			if (CrossMod.Thorium.TryFind("SandshroudPouch", out ModItem sandshroudPouch))
				AccType.Add(sandshroudPouch.Type, 0.2f);
		}
	}

	public override void SetMannequin(Point16 position)
	{
		Item[] inv = [new(), new(), new(), new(), new(), new(), new(), new()];
		float chance = WorldGen.genRand.NextFloat();

		WeightedRandom<DesertSet> setType = new(WorldGen.genRand);
		setType.Add(DesertSet.PharaohVanity, 0.4f);
		setType.Add(DesertSet.CowboyVanity, 0.4f);
		setType.Add(DesertSet.Sundancer, 0.5f);

		if (CrossMod.Thorium.Enabled)
		{
			setType.Add(DesertSet.Thorium_Sandstone, 0.5f);
		}

		if (CrossMod.Fables.Enabled)
		{
			setType.Add(DesertSet.Fables_Prowler, 0.5f);
		}

		DesertSet set = setType;
		HashSet<int> slots = [];
		WeightedRandom<int> availableSlots = new();
		availableSlots.Add(0);
		availableSlots.Add(1);
		availableSlots.Add(2);

		// 10% chance for all 3, 40% for 2, 50% for 1, unless vanity - then it's guaranteed
		if (chance <= .1f || set is DesertSet.CowboyVanity or DesertSet.PharaohVanity)
			slots = [0, 1, 2];
		else if (chance <= .5f)
		{
			slots.Add(availableSlots);
			slots.Add(availableSlots);
		}
		else
			slots.Add(availableSlots);

		if (slots.Contains(0))
			inv[0] = new(HeadType(set));

		if (slots.Contains(1))
			inv[1] = new(BodyType(set));

		if (slots.Contains(2) && set != DesertSet.PharaohVanity)
			inv[2] = new(LegsType(set));

		if (!TileEntity.ByPosition.TryGetValue(position, out TileEntity te) || te is not TEDisplayDoll mannequin)
		{
			int id = TEDisplayDoll.Place(position.X, position.Y);
			mannequin = TileEntity.ByID[id] as TEDisplayDoll;
		}

		int slot = WorldGen.genRand.Next(5) + 3;
		inv[slot] = new Item(AccType);
		UndergroundHouseMicropass.teDollInventory.SetValue(mannequin, inv);

		static int HeadType(DesertSet set) => set switch
		{
			DesertSet.PharaohVanity => ItemID.PharaohsMask,
			DesertSet.CowboyVanity => ItemID.CowboyHat,
			DesertSet.Sundancer => ModContent.ItemType<SunEarrings>(),
			DesertSet.Thorium_Sandstone => CrossMod.Thorium.TryFind("SandStoneHelmet", out ModItem sandstoneHelm) ? sandstoneHelm.Type : ItemID.AncientIronHelmet,
			DesertSet.Fables_Prowler => CrossMod.Thorium.TryFind("DesertProwlerHat", out ModItem prowlerHat) ? prowlerHat.Type : ItemID.AncientIronHelmet,
			_ => throw new ArgumentException("set (Head) wasn't a valid value somehow. Uh oh?"),
		};

		static int BodyType(DesertSet set) => set switch
		{
			DesertSet.PharaohVanity => ItemID.PharaohsRobe,
			DesertSet.CowboyVanity => ItemID.CowboyJacket,
			DesertSet.Sundancer => ModContent.ItemType<SilkTop>(),
			DesertSet.Thorium_Sandstone => CrossMod.Thorium.TryFind("SandStoneMail", out ModItem sandstoneBody) ? sandstoneBody.Type : ItemID.CopperChainmail,
			DesertSet.Fables_Prowler => CrossMod.Thorium.TryFind("DesertProwlerShirt", out ModItem prowlerShirt) ? prowlerShirt.Type : ItemID.CopperChainmail,
			_ => throw new ArgumentException("set (Body) wasn't a valid value somehow. Uh oh?"),
		};

		static int LegsType(DesertSet set) => set switch
		{
			// Don't include Pharaoh set as it doesn't have legs
			DesertSet.CowboyVanity => ItemID.CowboyPants,
			DesertSet.Sundancer => ModContent.ItemType<SilkSirwal>(),
			DesertSet.Thorium_Sandstone => CrossMod.Thorium.TryFind("SandStoneGreaves", out ModItem sandstoneLegs) ? sandstoneLegs.Type : ItemID.CopperGreaves,
			DesertSet.Fables_Prowler => CrossMod.Thorium.TryFind("DesertProwlerPants", out ModItem prowlerPants) ? prowlerPants.Type : ItemID.CopperGreaves,
			_ => throw new ArgumentException("set (Body) wasn't a valid value somehow. Uh oh?"),
		};
	}
}
