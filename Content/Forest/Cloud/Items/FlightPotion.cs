namespace SpiritReforged.Content.Forest.Cloud.Items;

public class FlightPotion : ModItem
{
	public override void SetStaticDefaults() => Item.ResearchUnlockCount = 20;

	public override void SetDefaults()
	{
		Item.width = 28;
		Item.height = 30;
		Item.rare = ItemRarityID.Blue;
		Item.maxStack = Item.CommonMaxStack;
		Item.useStyle = ItemUseStyleID.DrinkLiquid;
		Item.useTime = Item.useAnimation = 20;
		Item.consumable = true;
		Item.autoReuse = false;
		Item.buffType = ModContent.BuffType<FlightPotionBuff>();
		Item.buffTime = 14400;
		Item.UseSound = SoundID.Item3;
		Item.value = Item.sellPrice(silver: 2);
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.BottledWater).AddIngredient(ModContent.ItemType<Cloudstalk>())
		.AddIngredient(ItemID.SoulofFlight, 1).AddIngredient(ItemID.Damselfish).AddTile(TileID.Bottles).Register();
}

public class FlightPotionBuff : ModBuff
{
	public override void Update(Player player, ref int buffIndex) => player.GetModPlayer<FlightPlayer>().wingTimeMult += .25f;
}

public class FlightPlayer : ModPlayer
{
	public float wingTimeMult = 1;

	public override void ResetEffects() => wingTimeMult = 1;

	public override void UpdateEquips() => Player.wingTimeMax = (int)(Player.wingTimeMax * wingTimeMult);
}