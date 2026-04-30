using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Subclasses.Wrenches;

namespace SpiritReforged.Content.Forest.OreWrenches;

internal class CopperSpanner : ModItem, ISentryHitItem, IScrapDropItem
{
	internal class SpannerProjectile : GlobalProjectile
	{
		public override bool InstancePerEntity => true;

		internal short empoweredTime = 0;

		public override bool PreAI(Projectile projectile)
		{
			empoweredTime = (short)Math.Max(empoweredTime - 1, 0);

			if (empoweredTime > 0)
			{
				projectile.GetGlobalProjectile<SpeedModifierProjectile>().speed += 0.12f;

				if (Main.rand.NextBool(16))
					Dust.NewDust(projectile.position, projectile.width, projectile.height, DustID.Electric);
			}

			return true;
		}
	}

	public override void SetDefaults()
	{
		Item.Size = new(38, 40);
		Item.damage = 12;
		Item.useTime = Item.useAnimation = 22;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.DamageType = ModContent.GetInstance<WrenchClass>();
		Item.knockBack = 4;
		Item.useTurn = true;
	}

	bool ISentryHitItem.CanHitSentry(Player player, Projectile sentry) => player.HasItem(ModContent.ItemType<ScrapItem>());

	public void OnHitSentry(Player player, Projectile sentry)
	{
		player.ConsumeItem(ModContent.ItemType<ScrapItem>());
		sentry.GetGlobalProjectile<SpannerProjectile>().empoweredTime = 5 * 60;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.CopperBar, 12).AddTile(TileID.Anvils).Register();
}

internal class TinSpanner : CopperSpanner
{
	public override void SetDefaults()
	{
		base.SetDefaults();

		Item.damage = 13;
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.TinBar, 12).AddTile(TileID.Anvils).Register();
}