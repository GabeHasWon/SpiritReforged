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
		Item.createTile = ModContent.TileType<OganessonMoss>();
	}

	public override void HoldItem(Player player)
	{
		if (player.IsTargetTileInItemRange(Item))
		{
			player.cursorItemIconEnabled = true;
			player.cursorItemIconID = Type;
		}
	}

	public override bool CanUseItem(Player player)
	{
		if (player.whoAmI == Main.myPlayer)
		{
			Item.createTile = GetPlaceType(player.TargetTile().TileType);
			if (Item.createTile != -1 && player.IsTargetTileInItemRange(Item))
			{
				int i = Player.tileTargetX;
				int j = Player.tileTargetY;

				Main.tile[i, j].TileType = (ushort)Item.createTile;
				WorldGen.Reframe(i, j);

				if (Main.netMode != NetmodeID.SinglePlayer)
					NetMessage.SendTileSquare(-1, i, j);
			}
		}
		else
		{
			Item.createTile = -1;
		}

		return true;
	}

	/// <param name="tileType"> The target tile type. </param>
	public virtual int GetPlaceType(int tileType) => tileType switch
	{
		TileID.GrayBrick => ModContent.TileType<OganessonMossGrayBrick>(),
		TileID.Stone => ModContent.TileType<OganessonMoss>(),
		_ => -1
	};

	public override bool? UseItem(Player player)
	{
		Item.createTile = ModContent.TileType<OganessonMoss>();
		return null;
	}

	public override void Update(ref float gravity, ref float maxFallSpeed) => Lighting.AddLight(Item.position, 0.252f, 0.252f, 0.252f);
	public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
	{
		Item.DrawInWorld(Color.White, rotation, scale);
		return false;
	}
}