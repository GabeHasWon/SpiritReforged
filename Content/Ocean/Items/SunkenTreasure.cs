using Terraria.GameContent.ItemDropRules;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ItemCommon.FloatingItem;
using SpiritReforged.Common.TileCommon;
using Terraria.Audio;
using Terraria.DataStructures;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Content.Ocean.Items.Rum;
using SpiritReforged.Common.PlayerCommon;

namespace SpiritReforged.Content.Ocean.Items;

public class SunkenTreasure : FloatingItem
{
	public override float SpawnWeight => 0.001f;
	public override float Weight => base.Weight * 0.9f;
	public override float Bouyancy => base.Bouyancy * 1.08f;

	public override void SetStaticDefaults()
	{
		Item.ResearchUnlockCount = 10;
		PlayerEvents.AddAnglerQuestReward(Type, 50);
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 16;
		Item.rare = ItemRarityID.LightRed;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.consumable = true;
		Item.maxStack = Item.CommonMaxStack;
		Item.createTile = ModContent.TileType<SunkenTreasureTilePlaced>();
		Item.useTime = Item.useAnimation = 20;
		Item.useAnimation = 15;
		Item.useTime = 10;
		Item.noMelee = true;
		Item.autoReuse = false;
		Item.value = Item.sellPrice(gold: 1);
	}

	public override bool CanRightClick() => true;
	public override void ModifyItemLoot(ItemLoot itemLoot)
	{
        // "Weighted" loot table
        int[] lootTable =
		[
			ItemID.FishHook,
			ItemID.ShipsWheel, ItemID.ShipsWheel, ItemID.ShipsWheel,
			ItemID.TreasureMap,
			ItemID.CompassRose, ItemID.CompassRose,
			ItemID.ShipInABottle,
			ItemID.Sextant
		];

		itemLoot.AddOneFromOptions(3, lootTable);
		itemLoot.Add(DropRules.LootPoolDrop.SameStack(6, 10, 1, 4, 1, ItemID.GoldBar, ItemID.SilverBar, ItemID.TungstenBar, ItemID.PlatinumBar));
		itemLoot.Add(DropRules.LootPoolDrop.SameStack(5, 7, 1, 4, 1, ItemID.Ruby, ItemID.Emerald, ItemID.Topaz, ItemID.Amethyst, ItemID.Diamond, ItemID.Sapphire, ItemID.Amber));
		itemLoot.AddCommon(ModContent.ItemType<ExplosiveRum>(), 1, 45, 71);

		//Thorium Crossmod
		if (CrossMod.Thorium.Enabled)
		{
			if (CrossMod.Thorium.TryFind("Opal", out ModItem opal) && CrossMod.Thorium.TryFind("Aquamarine", out ModItem aquamarine))
				itemLoot.Add(DropRules.LootPoolDrop.SameStack(5, 7, 1, 4, 1, opal.Type, aquamarine.Type));
		}

		var goldCoins = ItemDropRule.Common(ItemID.GoldCoin, 2, 1, 4);
		goldCoins.OnFailedRoll(ItemDropRule.Common(ItemID.Cobweb, 1, 8, 13));
		itemLoot.Add(goldCoins);
	}
}

public class SunkenTreasureTile : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;
		Main.tileSpelunker[Type] = true;
		Main.tileNoFail[Type] = true;

		TileID.Sets.PreventsSandfall[Type] = true;
		TileID.Sets.GeneralPlacementTiles[Type] = false;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
		TileObjectData.newTile.Origin = new(1, 1);
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
		TileObjectData.newTile.StyleWrapLimit = 2;
		TileObjectData.newTile.StyleMultiplier = 2;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
		TileObjectData.addAlternate(1);
		TileObjectData.addTile(Type);

		DustType = DustID.Sand;
		AddMapEntry(new Color(133, 106, 56), CreateMapEntryName());
		SolidBottomTile.TileTypes.Add(Type);
	}

	public override void MouseOver(int i, int j)
	{
		if (Main.tile[i, j].TileFrameX >= 108)
			return;

		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = ModContent.ItemType<SunkenTreasure>();
	}

	public override bool RightClick(int i, int j)
	{
		if (ProgressStage(i, j))
		{
			Drop(i, j);
			return true;
		}

		return false;
	}

	public override bool CanDrop(int i, int j) => false;
	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (!ProgressStage(i, j))
			return;

		fail = true;
		Drop(i, j);
	}

	private static void Drop(int i, int j)
	{
		TileExtensions.GetTopLeft(ref i, ref j);
		SoundEngine.PlaySound(SoundID.Coins with { Pitch = .5f }, new Vector2(i + 1, j + 1) * 16);
		SoundEngine.PlaySound(SoundID.CoinPickup, new Vector2(i + 1, j + 1) * 16);

		ItemMethods.SplitCoins(Main.rand.Next(50, 200), delegate (int type, int stack)
		{ DropItem(i, j, type, stack); });

		ItemMethods.NewItemSynced(new EntitySource_TileBreak(i, j), ModContent.ItemType<SunkenTreasure>(), new Rectangle(i * 16, j * 16, 48, 32).Center(), true);

		if (Main.netMode == NetmodeID.MultiplayerClient)
			NetMessage.SendTileSquare(-1, i, j, 3, 2);

		static void DropItem(int i, int j, int type, int stack = 1)
		{
			var source = new EntitySource_TileBreak(i, j);

			int id = Item.NewItem(source, new Rectangle(i * 16, j * 16, 48, 32), type, stack, true);
			Main.item[id].velocity = (Vector2.UnitY * -Main.rand.NextFloat(1f, 4f)).RotatedByRandom(1.5f);
			Main.item[id].noGrabDelay = 60;

			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendData(MessageID.SyncItem, number: id, number2: 100f);
		}
	}

	private static bool ProgressStage(int i, int j)
	{
		var tile = Framing.GetTileSafely(i, j);
		var data = TileObjectData.GetTileData(tile);

		if (tile.TileFrameX / data.CoordinateFullWidth > 1)
			return false;

		TileExtensions.GetTopLeft(ref i, ref j);

		for (int frameX = 0; frameX < data.Width; frameX++)
			for (int frameY = 0; frameY < data.Height; frameY++)
				Framing.GetTileSafely(i + frameX, j + frameY).TileFrameX += (short)(data.CoordinateFullWidth * 2);

		return true;
	}
}

public class SunkenTreasureTilePlaced : SunkenTreasureTile
{
	public override string Texture => base.Texture.Replace("Placed", string.Empty);

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileID.Sets.PreventsSandfall[Type] = true;
		TileID.Sets.GeneralPlacementTiles[Type] = false;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
		TileObjectData.newTile.AnchorValidTiles = [TileID.Sand, TileID.Ebonsand, TileID.Crimsand, TileID.Pearlsand];
		TileObjectData.newTile.Origin = new(1, 1);
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
		TileObjectData.newTile.StyleWrapLimit = 2;
		TileObjectData.newTile.StyleMultiplier = 2;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
		TileObjectData.addAlternate(1);
		TileObjectData.addTile(Type);

		DustType = DustID.Sand;
		AddMapEntry(new Color(133, 106, 56), Language.GetText("Mods.SpiritReforged.Tiles.SunkenTreasureTilePlaced.MapEntry"));
		//SolidBottomTile.TileTypes.Add(Type);
	}

	public override void MouseOver(int i, int j) { }
	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem) { }

	public override bool RightClick(int i, int j) => false;
	public override bool CanDrop(int i, int j) => true;
}