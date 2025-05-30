using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Visuals.Glowmasks;

namespace SpiritReforged.Content.Underground.Moss.Oganesson;

[AutoloadGlowmask("255,255,255")]
public class OganessonBobber : EquippableItem
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
		player.overrideFishingBobber = ModContent.ProjectileType<OganessonBobberProjectile>();
	}

	public override void UpdateVanity(Player player) => player.overrideFishingBobber = ModContent.ProjectileType<OganessonBobberProjectile>();
	public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
		=> Lighting.AddLight(Item.Center, Color.White.ToVector3() * 0.25f);

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.FishingBobberGlowingStar)
		.AddIngredient(ModContent.ItemType<OganessonMossItem>(), 5).AddTile(TileID.TinkerersWorkbench).Register();
}

public class OganessonBobberProjectile : ModProjectile
{
	public virtual ModItem Item => ModContent.GetInstance<OganessonBobber>();

	public override LocalizedText DisplayName => Item.DisplayName;
	public override string Texture => Item.Texture;

	public override void SetDefaults()
	{
		Projectile.CloneDefaults(ProjectileID.BobberWooden);
		Projectile.aiStyle = ProjAIStyleID.Bobber;
		Projectile.bobber = true;

		AIType = ProjectileID.FishingBobber;
	}

	public override void AI()
	{
		if (!Main.dedServ)
			Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 0.3f);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		var texture = TextureAssets.Projectile[Type].Value;
		var glow = GlowmaskItem.ItemIdToGlowmask[Item.Type].Glowmask.Value;
		var center = Projectile.Center - Main.screenPosition + new Vector2(Projectile.width / 2, Projectile.gfxOffY);
		var origin = new Vector2(texture.Width / 2, Projectile.height / 2 + 8); //Fishing bobber drawing math is odd

		Main.EntitySpriteDraw(texture, center, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, default);
		Main.EntitySpriteDraw(glow, center, null, Projectile.GetAlpha(Color.White), Projectile.rotation, origin, Projectile.scale, default);

		return false;
	}
}