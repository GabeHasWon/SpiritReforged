using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.ButterflyStaff;

[AutoloadGlowmask("255,255,255")]
public class ButterflyStaff : ModItem
{
	public override void SetStaticDefaults()
	{
		MoRHelper.AddElement(Item, MoRHelper.Arcane);
		MoRHelper.AddElement(Item, MoRHelper.Nature, true);
	}
	public override void SetDefaults()
	{
		Item.damage = 14;
		Item.width = 40;
		Item.height = 40;
		Item.value = Item.sellPrice(0, 2, 25, 0);
		Item.rare = ItemRarityID.Blue;
		Item.mana = 10;
		Item.knockBack = 1;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.useTime = 30;
		Item.useAnimation = 30;
		Item.DamageType = DamageClass.Summon;
		Item.noMelee = true;
		Item.shoot = ModContent.ProjectileType<ButterflyMinion>();
		Item.UseSound = SoundID.Item44;
		Item.autoReuse = true;
	}

	public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI) => Lighting.AddLight(Item.position, 0.4f, .18f, .42f);

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		position = player.Center - new Vector2(0, 40);
		Projectile.NewProjectile(source, position, Main.rand.NextVector2Circular(3, 3), type, damage, knockback, player.whoAmI);
		return false;
	}
}