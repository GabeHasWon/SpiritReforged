using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Subclasses.Greatshields;

namespace SpiritReforged.Content.Forest.Shields;

public class Firewall : GreatshieldItem
{
	public class FirewallBash : GreatshieldProjectile
	{
		public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.MultistepEase(EaseFunction.EaseSine, EaseFunction.EaseCubicOut, 0.3f), 40, 25);

		public override void AI()
		{
			base.AI();

			if (Released && Progress < 0.5f)
			{
				Dust dust = Dust.NewDustPerfect(Projectile.Center + Projectile.velocity * 10 + Main.rand.NextVector2Circular(30, 30) * Main.rand.NextFloat(), DustID.Torch, Projectile.velocity, 100);
				dust.noGravity = true;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.OnFire, 60 * 5);
	}

	public override ShieldInfo SetInfo()
	{
		Item.defense = 4;
		Item.rare = ItemRarityID.Orange;
		Item.damage = 28;
		Item.useTime = Item.useAnimation = 40;
		Item.knockBack = 12;
		Item.shoot = ModContent.ProjectileType<FirewallBash>();

		return new ShieldInfo(40, 60);
	}

	public override void HoldItem(Player player)
	{
		base.HoldItem(player);
		if (player.whoAmI == Main.myPlayer && player.TryGetModPlayer(out GreatshieldPlayer shieldPlayer) && shieldPlayer.Blocking)
		{
			//Create fire
		}
	}

	public override void OnBlockDamage(Player player, Player.HurtInfo info) { }

	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.HellstoneBar, 8).AddIngredient(ItemID.Obsidian, 20).AddTile(TileID.Anvils).Register();
}