using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.SaltFlats.Biome;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using SpiritReforged.Content.Savanna.Tiles;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.SaltFlats.Items.Crates;

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

	public class SaltCrateTile : ModTile, ICheckItemUse
	{
		public virtual int RestoredType => ModContent.TileType<SaltCrateRestored.SaltCrateRestoredTile>();

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

		public virtual bool? CheckItemUse(int type, Player player, int i, int j)
		{
			if (RestoredType != Type && type is ItemID.PaintScraper or ItemID.SpectrePaintScraper)
			{
				TileExtensions.GetTopLeft(ref i, ref j);

				for (int x = i; x < i + 2; x++)
				{
					for (int y = j; y < j + 2; y++)
						Main.tile[x, y].TileType = (ushort)RestoredType;
				}

				for (int d = 0; d < 10; d++)
					Dust.NewDustDirect(new Vector2(i, j) * 16, 32, 32, DustID.Pearlsand);

				return true;
			}

			return null;
		}
	}

	public override void SetStaticDefaults() => Item.ResearchUnlockCount = 10;
	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<SaltCrateTile>());
		Item.rare = ItemRarityID.Green;
	}

	public override bool CanRightClick() => true;
	public override void ModifyItemLoot(ItemLoot itemLoot) => ModifyLoot(itemLoot);

	public static void ModifyLoot(ItemLoot itemLoot)
	{
		int[] dropOptions = [ModContent.ItemType<MahakalaMaskBlue>(),
			ModContent.ItemType<MahakalaMaskRed>(),
			ModContent.ItemType<BoStaff>(),
			ItemID.CloudinaBottle,
			ItemID.WaterWalkingBoots];

		IItemDropRule main = ItemDropRule.OneFromOptions(1, dropOptions);

		itemLoot.AddCommon(ItemID.LawnFlamingo, 5);

		CrateHelper.BiomeCrate(itemLoot, main,
			ItemDropRule.NotScalingWithLuck(AutoContent.ItemType<SaltBlockDull>(), 3, 20, 50),
			ItemDropRule.NotScalingWithLuck(AutoContent.ItemType<Drywood>(), 3, 20, 50));
	}
}