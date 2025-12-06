using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.SaltFlats.Biome;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using SpiritReforged.Content.Savanna.Tiles;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.SaltFlats.Items;

public class SaltCrate : ModItem
{
	public sealed class SaltFishingPlayer : ModPlayer
	{
		public override void CatchFish(FishingAttempt attempt, ref int itemDrop, ref int npcSpawn, ref AdvancedPopupRequest sonar, ref Vector2 sonarPosition)
		{
			if (Player.InModBiome<SaltBiome>() && attempt.crate)
				itemDrop = Main.hardMode ? ModContent.ItemType<SaltCrateHardmode>() : ModContent.ItemType<SaltCrate>();
		}
	}

	public class SaltCrateTile : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileSolidTop[Type] = true;
			Main.tileTable[Type] = true;
			Main.tileLavaDeath[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
			TileObjectData.newTile.CoordinateHeights = [16, 18];
			TileObjectData.addTile(Type);

			AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTable);
			AddMapEntry(new Color(123, 104, 84));
			DustType = -1;
		}
	}

	public override void SetStaticDefaults() => Item.ResearchUnlockCount = 10;
	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<SaltCrateTile>());
		Item.rare = ItemRarityID.Green;
	}

	public override bool CanRightClick() => true;
	public override void ModifyItemLoot(ItemLoot itemLoot)
	{
		int[] dropOptions = [ModContent.ItemType<MahakalaMaskBlue>(),
			ModContent.ItemType<MahakalaMaskRed>(),
			ItemID.AnkletoftheWind,
			ItemID.CloudinaBottle,
			ItemID.WaterWalkingBoots];

		IItemDropRule main = ItemDropRule.OneFromOptions(1, dropOptions);

		CrateHelper.HardmodeBiomeCrate(itemLoot, main,
			ItemDropRule.NotScalingWithLuck(AutoContent.ItemType<SaltBlockDull>(), 3, 20, 50),
			ItemDropRule.NotScalingWithLuck(AutoContent.ItemType<Drywood>(), 3, 20, 50));
	}
}