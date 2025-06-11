using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.PlayerCommon;

namespace SpiritReforged.Content.Underground.Moss.Oganesson;

public class OganessonMossItem : ModItem
{
	public override void SetStaticDefaults()
	{
		Item.ResearchUnlockCount = 25;

		ItemID.Sets.ExtractinatorMode[Type] = ItemID.LavaMoss;
		ItemID.Sets.DisableAutomaticPlaceableDrop[Type] = true;
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

	public override bool CanUseItem(Player player) => player.IsTargetTileInItemRange(Item);

	public override bool? UseItem(Player player)
	{
		var tile = player.TargetTile();
		if (tile.TileType == TileID.Stone)
		{
			TryPlace(ModContent.TileType<OganessonMoss>());
			return true;
		}
		else if (tile.TileType == TileID.GrayBrick)
		{
			TryPlace(ModContent.TileType<OganessonMossGrayBrick>());
			return true;
		}

		return null;

		static void TryPlace(int type)
		{
			WorldGen.PlaceTile(Player.tileTargetX, Player.tileTargetY, type, forced: true);
			var t = Main.tile[Player.tileTargetX, Player.tileTargetY];

			if (t.TileType == type && Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendTileSquare(-1, Player.tileTargetX, Player.tileTargetY);
		}
	}

	public override void Update(ref float gravity, ref float maxFallSpeed) => Lighting.AddLight(Item.position, .252f, .252f, .252f);
	public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
	{
		Item.DrawInWorld(Color.White, rotation, scale);
		return false;
	}
}