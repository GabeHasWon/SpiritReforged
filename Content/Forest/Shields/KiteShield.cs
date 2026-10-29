using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Subclasses.Greatshields;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Shields;

public class KiteShield : GreatshieldItem
{
	public class KiteShieldSwing : GreatshieldProjectile
	{
		public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.MultistepEase(EaseFunction.EaseSine, EaseFunction.EaseCubicOut, 0.3f), 40, 25);

		public override void AI()
		{
			base.AI();

			if (Released && Progress < 0.5f)
			{
				Dust dust = Dust.NewDustPerfect(Projectile.Center + Projectile.velocity * 10 + Main.rand.NextVector2Circular(30, 30) * Main.rand.NextFloat(), DustID.Cloud, Projectile.velocity, 100);
				dust.noGravity = true;
			}
		}
	}

	public override ShieldInfo SetInfo()
	{
		Item.defense = 3;
		Item.rare = ItemRarityID.Green;
		Item.damage = 28;
		Item.useTime = Item.useAnimation = 40;
		Item.knockBack = 12;
		Item.shoot = ModContent.ProjectileType<KiteShieldSwing>();

		return new ShieldInfo(30, 60);
	}

	public override void OnBlockDamage(Player player, Player.HurtInfo info) { }
}