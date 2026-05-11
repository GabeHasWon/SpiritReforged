using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Subclasses.Magazine;
using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underground.Items;

[AutoloadGlowmask("255,255,255")]
public class Pepperbox : ModItem, IMagazineWeapon
{
	MagazineData IMagazineWeapon.Data => new(4, 1.2f);

	public override void SetStaticDefaults() => NPCShopHelper.AddEntry(NPCShopHelper.ConditionalEntry.FromNPC(NPCID.ArmsDealer, new NPCShop.Entry(Type, Condition.DownedEyeOfCthulhu)));

	public override void SetDefaults()
	{
		Item.DamageType = DamageClass.Ranged;
		Item.damage = 8;
		Item.knockBack = 6;
		Item.width = 40;
		Item.height = 20;
		Item.useTime = Item.useAnimation = 15;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.noMelee = true;
		Item.noUseGraphic = false;
		Item.value = Item.buyPrice(0, 1, 50, 0);
		Item.rare = ItemRarityID.Blue;
		Item.autoReuse = true;
		Item.useAmmo = AmmoID.Bullet;
		Item.shoot = ProjectileID.Bullet;
		Item.shootSpeed = 8f;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		for (int i = 0; i < 3; ++i)
		{
			Vector2 vel = i == 0 ? velocity : velocity.RotatedByRandom(0.2f) * Main.rand.NextFloat(0.8f, 1);
			Projectile.NewProjectile(source, position, vel, Item.shoot, damage, knockback, player.whoAmI);
		}

		return false;
	}

	public override bool CanConsumeAmmo(Item ammo, Player player) => !player.channel; //Ammo consumption happens in BombCannonHeld
}
