using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Underground.Moss.Oganesson;

namespace SpiritReforged.Content.Underground.Moss.Radon;

[AutoloadGlowmask("255,255,255")]
public class RadonBobber : EquippableItem
{
	public override void SetDefaults()
	{
		Item.width = 26;
		Item.height = 34;
		Item.value = Item.sellPrice(0, 1, 0, 0);
		Item.rare = ItemRarityID.Blue;
		Item.accessory = true;
	}

	public override void UpdateAccessory(Player player, bool hideVisual)
	{
		player.fishingSkill += 10;
		player.accFishingBobber = true;
		player.overrideFishingBobber = ModContent.ProjectileType<RadonBobberProjectile>();
	}

	public override void UpdateVanity(Player player) => player.overrideFishingBobber = ModContent.ProjectileType<RadonBobberProjectile>();
	public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
		=> Lighting.AddLight(Item.Center, Color.Yellow.ToVector3() * 0.25f);

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.FishingBobberGlowingStar)
		.AddIngredient(ModContent.ItemType<RadonMossItem>(), 5).AddTile(TileID.TinkerersWorkbench).Register();
}

public class RadonBobberProjectile : OganessonBobberProjectile
{
	public override ModItem Item => ModContent.GetInstance<RadonBobber>();

	public override void AI()
	{
		if (!Main.dedServ)
			Lighting.AddLight(Projectile.Center, Color.Yellow.ToVector3() * 0.3f);
	}
}