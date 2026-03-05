using SpiritReforged.Content.Ocean.Items.Reefhunter.Projectiles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Ocean.Items.Reefhunter;

public class SkullSentry : ModItem
{
	const float MAX_DISTANCE = 600f;

	//temporary until dungeon room is added
	public override bool IsLoadingEnabled(Mod mod) => false;

	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.StaffoftheFrostHydra);
		Item.damage = 14;
		Item.width = 28;
		Item.height = 14;
		Item.useTime = Item.useAnimation = 30;
		Item.knockBack = 2f;
		Item.shootSpeed = 0f;
		Item.noMelee = true;
		Item.autoReuse = true;
		Item.sentry = true;
		Item.rare = ItemRarityID.Blue;
		Item.value = Item.sellPrice(gold: 2);
		Item.useStyle = ItemUseStyleID.HoldUp;
		Item.UseSound = SoundID.Item77;
		Item.shoot = ModContent.ProjectileType<SkullSentrySentry>();
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		position = Main.MouseWorld;
		if (MouseTooFar(player))
			position = player.DirectionTo(position) * MAX_DISTANCE;

		Projectile.NewProjectile(source, position.X, position.Y, velocity.X, velocity.Y, type, damage, knockback, player.whoAmI);
		player.UpdateMaxTurrets();
		return false;
	}

	public override bool CanUseItem(Player player)
	{
		if (MouseTooFar(player))
			return false;

		var dummy = new Projectile();
		dummy.SetDefaults(Item.shoot);

		Point topLeft = (Main.MouseWorld - dummy.Size / 2).ToTileCoordinates();
		Point bottomRight = (Main.MouseWorld + dummy.Size / 2).ToTileCoordinates();

		return !Collision.SolidTilesVersatile(topLeft.X, bottomRight.X, topLeft.Y, bottomRight.Y);
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<IridescentScale>(), 12)
		.AddIngredient(ItemID.Lens, 3).AddTile(TileID.Anvils).Register();

	private static bool MouseTooFar(Player player) => player.Distance(Main.MouseWorld) >= MAX_DISTANCE;
}