using SpiritReforged.Common.SimpleEntity;

namespace SpiritReforged.Content.Ocean.Items;

public class InvisibleLure : ModItem
{
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
		Item.value = Item.sellPrice(silver: 10);
		Item.rare = ItemRarityID.Blue;
	}

	public override void HoldItem(Player player)
	{
		if (CanUseItem(player))
		{
			player.cursorItemIconEnabled = true;
			player.cursorItemIconID = Type;
		}
	}

	public override bool CanUseItem(Player player) => FishLure.WaterBelow() && player.IsTargetTileInItemRange(Item);
	public override bool? UseItem(Player player)
	{
		if (!Main.dedServ && player.whoAmI == Main.myPlayer && player.ItemAnimationJustStarted)
		{
			SimpleEntitySystem.NewEntity(typeof(InvisibleLureEntity), Main.MouseWorld);
			return true;
		}

		return null;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<FishLure>()).AddTile(TileID.WorkBenches).AddCondition(Condition.InGraveyard).Register();
}

public class InvisibleLureEntity : FishLureEntity
{
	protected override int ItemType => ModContent.ItemType<InvisibleLure>();

	public override void Draw(SpriteBatch spriteBatch)
	{
		if (Main.LocalPlayer.CanSeeInvisibleBlocks || Main.SceneMetrics.EchoMonolith)
		{
			var drawPosition = Center - Main.screenPosition + new Vector2(0, SolidCollision ? 0 : Sin(30f));
			spriteBatch.Draw(Texture.Value, drawPosition, null, Color.White, GetRotation() * .1f, Texture.Size() / 2, 1, SpriteEffects.None, 0f);
		}
	}
}