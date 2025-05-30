using SpiritReforged.Common.ModCompat;
using SpiritReforged.Content.Underground.WayfarerSet;
using Terraria.DataStructures;
using Terraria.GameContent.Biomes.CaveHouse;
using Terraria.GameContent.Tile_Entities;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Micropasses.Passes.MannequinInventories;

internal class UndergroundMannequinInventory : MannequinInventory
{
	private static WeightedRandom<int> AccType;

	public override HouseType Biome => HouseType.Wood;

	public override void Setup() 
	{
		AccType = new(WorldGen.genRand);
		AccType.Add(ItemID.Aglet, 0.5f);
		AccType.Add(ItemID.HermesBoots, 0.1f);
		AccType.Add(ItemID.FartinaJar, 0.01f);
		AccType.Add(ItemID.FrogLeg, 0.3f);
		AccType.Add(ItemID.ClimbingClaws, 0.25f);
		AccType.Add(ItemID.ShoeSpikes, 0.25f);
		AccType.Add(ItemID.BandofRegeneration, 0.4f);
		AccType.Add(GenVars.gold == TileID.Gold ? ItemID.GoldWatch : ItemID.PlatinumWatch, 0.4f);

		if (CrossMod.Thorium.Enabled)
		{
			if (CrossMod.Thorium.TryFind(GenVars.iron == TileID.Iron ? "IronShield" : "LeadShield", out ModItem shield))
				AccType.Add(shield.Type, 0.1f);

			if (CrossMod.Thorium.TryFind("LeatherSheath", out ModItem leatherSheath))
				AccType.Add(leatherSheath.Type, 0.3f);

			if (CrossMod.Thorium.TryFind("DartPouch", out ModItem dartPouch))
				AccType.Add(dartPouch.Type, 0.2f);

			if (CrossMod.Thorium.TryFind("Wreath", out ModItem wreath))
				AccType.Add(wreath.Type, 0.1f);
		}

		if (CrossMod.Redemption.Enabled)
		{
			if (CrossMod.Redemption.TryFind("LeatherSheath", out ModItem leatherSheath))
				AccType.Add(leatherSheath.Type, 0.25f);

			if (CrossMod.Redemption.TryFind("DurableBowString", out ModItem bowString))
				AccType.Add(bowString.Type, 0.25f);

			if (CrossMod.Redemption.TryFind("ShellNecklace", out ModItem shellNecklace))
				AccType.Add(shellNecklace.Type, 0.1f);
		}
	}

	public override void SetMannequin(Point16 position)
	{
		Item[] inv = [new(ModContent.ItemType<WayfarerHead>()), new(ModContent.ItemType<WayfarerBody>()), new(ModContent.ItemType<WayfarerLegs>()),
			new(), new(), new(), new(), new()];

		float chance = Main.rand.NextFloat();

		if (chance < .10f)
		{
			inv[0] = new(ItemID.AncientGoldHelmet);
			inv[1] = new();
			inv[2] = new();
		}
		else if (chance < .15f)
		{
			inv[0] = new(ItemID.AncientIronHelmet);
			inv[1] = new();
			inv[2] = new();
		}

		if (CrossMod.Redemption.Enabled)
		{
			if (chance < .1f)
			{
				if (Main.rand.NextBool(2))
				{
					if (CrossMod.Redemption.TryFind("CommonGuardHelm1", out ModItem commonHelm1))
						inv[0] = new(commonHelm1.Type);
				}
				else
				{
					if (CrossMod.Redemption.TryFind("CommonGuardHelm2", out ModItem commonHelm2))
						inv[0] = new(commonHelm2.Type);
				}

				if (CrossMod.Redemption.TryFind("CommonGuardPlateMail", out ModItem commonPlate))
					inv[1] = new(commonPlate.Type);
				if (CrossMod.Redemption.TryFind("CommonGuardGreaves", out ModItem commonGreaves))
					inv[2] = new(commonGreaves.Type);
			}
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
