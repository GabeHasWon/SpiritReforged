using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Content.Ocean.Items.Reefhunter.Projectiles;

namespace SpiritReforged.Content.Ocean.Items.Reefhunter;

public class ReefSpear : ModItem
{
	public override void SetStaticDefaults()
	{
		ItemID.Sets.Spears[Type] = true;

		MoRHelper.AddElement(Item, MoRHelper.Water);
	}
	public override void SetDefaults()
	{
		Item.damage = 20;
		Item.width = 28;
		Item.height = 14;
		Item.useTime = Item.useAnimation = 30;
		Item.knockBack = 2f;
		Item.shootSpeed = 0f;
		Item.noUseGraphic = true;
		Item.noMelee = true;
		Item.DamageType = DamageClass.Melee;
		Item.channel = false;
		Item.rare = ItemRarityID.Blue;
		Item.value = Item.sellPrice(gold: 2);
		Item.shoot = ModContent.ProjectileType<ReefSpearProjectile>();
		Item.useStyle = ItemUseStyleID.Shoot;
	}

	public override Vector2? HoldoutOffset() => new Vector2(-6, 0);
	public override bool AltFunctionUse(Player player) => true;

	public override bool CanUseItem(Player player)
	{
		if (player.altFunctionUse == 2)
		{
			Item.shoot = ModContent.ProjectileType<ReefSpearThrown>();
			Item.shootSpeed = ReefSpearThrown.MAX_SPEED;
			Item.channel = false;
			Item.autoReuse = true;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.UseSound = SoundID.DD2_JavelinThrowersAttack;
			Item.useTime = Item.useAnimation = 35;

		}
		else
		{
			Item.shoot = ModContent.ProjectileType<ReefSpearProjectile>();
			Item.shootSpeed = 0f;
			Item.channel = true;
			Item.autoReuse = false;
			Item.UseSound = null;
			Item.useTime = Item.useAnimation = 40;
			Item.useStyle = ItemUseStyleID.Thrust;
		}

		return true;
	}

	public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
	{
		if (player.altFunctionUse == 2)
		{
			position -= new Vector2(20 * player.direction, 0);
			velocity = ArcVelocityHelper.GetArcVel(position, Main.MouseWorld, 0.3f, Item.shootSpeed) + player.velocity / 3;
			damage = (int)(damage * 0.75f);
		}
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<MineralSlag>(), 12)
		.AddIngredient(ItemID.Coral, 3).AddTile(TileID.Anvils).Register();
}