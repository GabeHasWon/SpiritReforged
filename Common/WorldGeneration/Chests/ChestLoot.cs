using SpiritReforged.Content.Desert.GildedScarab;
using SpiritReforged.Content.Forest.ArcaneNecklace;
using SpiritReforged.Content.Forest.Cloud.Items;
using SpiritReforged.Content.Forest.Misc;
using SpiritReforged.Content.Forest.RoguesCrest;
using SpiritReforged.Content.Jungle.Misc.DyeCrate;
using SpiritReforged.Content.Jungle.Toucane;
using SpiritReforged.Content.Ocean.Items.PoolNoodle;
using SpiritReforged.Content.Ocean.Items.Vanity;
using SpiritReforged.Content.Ocean.Items.Vanity.Towel;
using SpiritReforged.Content.Underground.Items.BoulderClub;
using SpiritReforged.Content.Underground.Items.ExplorerTreads;
using SpiritReforged.Content.Underground.Items.Zipline;
using SpiritReforged.Content.Forest.MagicPowder;
using static SpiritReforged.Common.WorldGeneration.Chests.ChestPoolUtils;
using SpiritReforged.Content.Forest.Cartography.Maps;

namespace SpiritReforged.Common.WorldGeneration.Chests;

/// <summary> Contains all additions to chest pools. </summary>
public class ChestLoot : ModSystem
{
	public override void PostWorldGen()
	{
		AddToVanillaChest(new ChestInfo(1, 0.33f, ModContent.ItemType<PoolNoodle>()), (int)VanillaChestID.Water, 1);
		AddToVanillaChest(new ChestInfo(1, 0.5f, ModContent.ItemType<BeachTowel>(), ModContent.ItemType<BikiniBottom>(), ModContent.ItemType<BikiniTop>(), ModContent.ItemType<SwimmingTrunks>(), ModContent.ItemType<TintedGlasses>()), (int)VanillaChestID.Water, 1);
		
		AddToVanillaChest(new ChestInfo(1, 0.25f, ModContent.ItemType<ToucaneItem>()), (int)VanillaChestID.Ivy, 1);
		AddToVanillaChest(new ChestInfo(1, 0.5f, ModContent.ItemType<DyeCrateItem>()), (int)VanillaChestID.Ivy, 1);
		AddToVanillaChest(new ChestInfo(1, 0.33f, ModContent.ItemType<DyeCrateItem>()), (int)VanillaChestID.Jungle, 1);

		AddToVanillaChest(new ChestInfo(1, 0.33f, ModContent.ItemType<ZiplineGun>(), ModContent.ItemType<ExplorerTreadsItem>()), (int)VanillaChestID.Gold, 1);

		AddToVanillaChest(new ChestInfo(1, 0.33f, ModContent.ItemType<RogueCrest>(), ModContent.ItemType<CraneFeather>()), (int)VanillaChestID.Wood, 1);
		AddToVanillaChest(new ChestInfo(1, 0.125f, ModContent.ItemType<ArcaneNecklaceGold>(), ModContent.ItemType<ArcaneNecklacePlatinum>()), (int)VanillaChestID.Wood, 1);
		AddToVanillaChest(new ChestInfo(3, 0.35f, ModContent.ItemType<DoubleJumpPotion>()), (int)VanillaChestID.Wood, Main.rand.Next(1, 3));
		AddToVanillaChest(new ChestInfo(25, 50, 0.3f, ModContent.ItemType<Flarepowder>()), (int)VanillaChestID.Wood, Main.rand.Next(1, 3));

		AddToVanillaChest(new ChestInfo(1, 0.25f, ModContent.ItemType<GildedScarab>()), (int)VanillaChestID2.Sandstone, 1, TileID.Containers2);

		AddToVanillaChest(new ChestInfo(2, 0.3f, ModContent.ItemType<TornMapPiece>()), (int)VanillaChestID.Wood, Main.rand.Next(1, 3));
		AddToVanillaChest(new ChestInfo(2, 0.25f, ModContent.ItemType<TornMapPiece>()), (int)VanillaChestID.Ivy, Main.rand.Next(1, 4));
		AddToVanillaChest(new ChestInfo(2, 0.25f, ModContent.ItemType<TornMapPiece>()), (int)VanillaChestID2.Sandstone, Main.rand.Next(1, 4), TileID.Containers2);
		AddToVanillaChest(new ChestInfo(2, 0.25f, ModContent.ItemType<TornMapPiece>()), (int)VanillaChestID.Ice, Main.rand.Next(1, 4));
		AddToVanillaChest(new ChestInfo(2, 0.18f, ModContent.ItemType<TornMapPiece>()), (int)VanillaChestID.Gold, Main.rand.Next(1, 4));

		AddToVanillaChest(new ChestInfo(1, 0.3f, ModContent.ItemType<Bowlder>()), (int)VanillaChestID2.Trapped, 1, TileID.Containers2);
		//AddToVanillaChest(new ChestInfo(ModContent.ItemType<Blasphemer>(), 1, 0.25f), (int)VanillaChestID.ShadowLocked, 1);
	}
}