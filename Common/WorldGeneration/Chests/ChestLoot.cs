using SpiritReforged.Content.Desert.GildedScarab;
using SpiritReforged.Content.Forest.ArcaneNecklace;
using SpiritReforged.Content.Forest.Cloud.Items;
using SpiritReforged.Content.Forest.Misc;
using SpiritReforged.Content.Forest.Misc.Maps;
using SpiritReforged.Content.Forest.RoguesCrest;
using SpiritReforged.Content.Jungle.Misc;
using SpiritReforged.Content.Jungle.Misc.DyeCrate;
using SpiritReforged.Content.Jungle.Toucane;
using SpiritReforged.Content.Ocean.Items.PoolNoodle;
using SpiritReforged.Content.Ocean.Items.Vanity;
using SpiritReforged.Content.Ocean.Items.Vanity.Towel;
using SpiritReforged.Content.Underground.Items.BoulderClub;
using SpiritReforged.Content.Underground.Items;
using SpiritReforged.Content.Underground.Items.ExplorerTreads;
using SpiritReforged.Content.Underground.Items.Zipline;
using static SpiritReforged.Common.WorldGeneration.Chests.ChestPoolUtils;
using SpiritReforged.Content.Underworld.Blasphemer;

namespace SpiritReforged.Common.WorldGeneration.Chests;

/// <summary> Contains all additions to chest pools. </summary>
public class ChestLoot : ModSystem
{
	public override void PostWorldGen()
	{
		AddToVanillaChest(new ChestInfo(ModContent.ItemType<PoolNoodle>(), 1, 0.33f), (int)VanillaChestID.Water, 1);
		AddToVanillaChest(new ChestInfo([ModContent.ItemType<BeachTowel>(), ModContent.ItemType<BikiniBottom>(), ModContent.ItemType<BikiniTop>(), ModContent.ItemType<SwimmingTrunks>(), ModContent.ItemType<TintedGlasses>()], 1, 0.5f), (int)VanillaChestID.Water, 1);
		
		AddToVanillaChest(new ChestInfo(ModContent.ItemType<ToucaneItem>(), 1, 0.25f), (int)VanillaChestID.Ivy, 1);
		AddToVanillaChest(new ChestInfo(ModContent.ItemType<DyeCrateItem>(), 1, 0.5f), (int)VanillaChestID.Ivy, 1);
		AddToVanillaChest(new ChestInfo(ModContent.ItemType<DyeCrateItem>(), 1, 0.33f), (int)VanillaChestID.Jungle, 1);

		AddToVanillaChest(new ChestInfo([ModContent.ItemType<ZiplineGun>(), ModContent.ItemType<ExplorerTreadsItem>()], 1, 0.33f), (int)VanillaChestID.Gold, 1);

		AddToVanillaChest(new ChestInfo([ModContent.ItemType<RogueCrest>(), ModContent.ItemType<CraneFeather>()], 1, 0.33f), (int)VanillaChestID.Wood, 1);
		AddToVanillaChest(new ChestInfo([ModContent.ItemType<ArcaneNecklaceGold>(), ModContent.ItemType<ArcaneNecklacePlatinum>()], 1, 0.125f), (int)VanillaChestID.Wood, 1);
		AddToVanillaChest(new ChestInfo(ModContent.ItemType<DoubleJumpPotion>(), 3, 0.35f), (int)VanillaChestID.Wood, Main.rand.Next(1, 3));

		AddToVanillaChest(new ChestInfo(ModContent.ItemType<GildedScarab>(), 1, 0.25f), (int)VanillaChestID2.Sandstone, 1, TileID.Containers2);

		AddToVanillaChest(new ChestInfo(ModContent.ItemType<TornMapPiece>(), 2, 0.3f), (int)VanillaChestID.Wood, Main.rand.Next(1, 3));
		AddToVanillaChest(new ChestInfo(ModContent.ItemType<TornMapPiece>(), 2, 0.25f), (int)VanillaChestID.Ivy, Main.rand.Next(1, 4));
		AddToVanillaChest(new ChestInfo(ModContent.ItemType<TornMapPiece>(), 2, 0.25f), (int)VanillaChestID2.Sandstone, Main.rand.Next(1, 4), TileID.Containers2);
		AddToVanillaChest(new ChestInfo(ModContent.ItemType<TornMapPiece>(), 2, 0.25f), (int)VanillaChestID.Ice, Main.rand.Next(1, 4));
		AddToVanillaChest(new ChestInfo(ModContent.ItemType<TornMapPiece>(), 2, 0.18f), (int)VanillaChestID.Gold, Main.rand.Next(1, 4));

		AddToVanillaChest(new ChestInfo(ModContent.ItemType<Bowlder>(), 2, 0.3f), (int)VanillaChestID2.Trapped, 1, TileID.Containers2);
		AddToVanillaChest(new ChestInfo(ModContent.ItemType<Blasphemer>(), 1, 0.25f), (int)VanillaChestID.ShadowLocked, 1);
	}
}