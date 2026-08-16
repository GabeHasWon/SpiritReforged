namespace SpiritReforged.Content.Savanna.Items;

public class SavannaGrassSeeds : ModItem
{
	public override void SetStaticDefaults()
	{
		ItemID.Sets.DisableAutomaticPlaceableDrop[Type] = true;
		Item.ResearchUnlockCount = 25;
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 14;
		Item.useAnimation = 15;
		Item.useTime = 10;
		Item.maxStack = Item.CommonMaxStack;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.useTurn = true;
		Item.autoReuse = true;
		Item.consumable = true;
		Item.value = Item.sellPrice(copper: 4);
	}

	public override void HoldItem(Player player)
	{
		if (player.IsTargetTileInItemRange(Item))
		{
			player.cursorItemIconEnabled = true;
			player.cursorItemIconID = Type;
		}
	}

	public override bool? UseItem(Player player)
	{
		if (Main.myPlayer == player.whoAmI && player.ItemAnimationJustStarted)
		{
			Tile tile = Framing.GetTileSafely(Player.tileTargetX, Player.tileTargetY);
			if (tile.HasTile && tile.TileType == ModContent.TileType<Tiles.SavannaDirt>() && player.IsTargetTileInItemRange(Item))
			{
				WorldGen.PlaceTile(Player.tileTargetX, Player.tileTargetY, ModContent.TileType<Tiles.SavannaGrass>(), forced: true);

				if (Main.netMode != NetmodeID.SinglePlayer)
					NetMessage.SendTileSquare(player.whoAmI, Player.tileTargetX, Player.tileTargetY);

				return true;
			}
		}

		return null;
	}
}