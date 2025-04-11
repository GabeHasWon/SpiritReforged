using SpiritReforged.Common.Visuals.Glowmasks;

namespace SpiritReforged.Content.Underground.Moss.Oganesson;

[AutoloadGlowmask("255,255,255")]
public class OganessonMossItem : ModItem
{
	public override void SetStaticDefaults()
	{
		ItemID.Sets.DisableAutomaticPlaceableDrop[Type] = true;
		Item.ResearchUnlockCount = 25;
		ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.RainbowMoss;
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 16;
		Item.useAnimation = 15;
		Item.useTime = 10;
		Item.maxStack = Item.CommonMaxStack;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.useTurn = true;
		Item.autoReuse = true;
		Item.consumable = true;
		Item.rare = ItemRarityID.Blue;
	}

	public override void HoldItem(Player player)
	{
		if (player.IsTargetTileInItemRange(Item))
		{
			player.cursorItemIconEnabled = true;
			player.cursorItemIconID = Type;
		}
	}

	public override void Update(ref float gravity, ref float maxFallSpeed) => Lighting.AddLight(Item.position, .252f, .252f, .252f);

	public override bool? UseItem(Player player)
	{
		if (Main.myPlayer == player.whoAmI)
		{
			Tile tile = Framing.GetTileSafely(Player.tileTargetX, Player.tileTargetY);
			if (tile.HasTile && player.IsTargetTileInItemRange(Item))
			{
				if (tile.TileType == TileID.Stone)
				{
					WorldGen.PlaceTile(Player.tileTargetX, Player.tileTargetY, ModContent.TileType<OganessonMoss>(), forced: true);

					if (Main.netMode != NetmodeID.SinglePlayer)
						NetMessage.SendTileSquare(player.whoAmI, Player.tileTargetX, Player.tileTargetY);

					return true;
				}

				if (tile.TileType == TileID.GrayBrick)
				{
					WorldGen.PlaceTile(Player.tileTargetX, Player.tileTargetY, ModContent.TileType<OganessonMossGrayBrick>(), forced: true);

					if (Main.netMode != NetmodeID.SinglePlayer)
						NetMessage.SendTileSquare(player.whoAmI, Player.tileTargetX, Player.tileTargetY);

					return true;
				}
			}
		}

		return null;
	}
}