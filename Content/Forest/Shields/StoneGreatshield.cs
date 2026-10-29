using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Subclasses.Greatshields;

namespace SpiritReforged.Content.Forest.Shields;

public class StoneGreatshield : GreatshieldItem
{
	public sealed class StonemasonBuff : ModBuff
	{
		public override void Update(Player player, ref int buffIndex)
		{
			player.buffTime[buffIndex]++; //Endless duration
			player.GetKnockback(DamageClass.Generic) += 1;
		}
	}

	public sealed class StonemasonPlayer : ModPlayer
	{
		public override void OnHitAnything(float x, float y, Entity victim) => Player.ClearBuff(ModContent.BuffType<StonemasonBuff>());
	}

	public class StoneshieldBash : GreatshieldProjectile
	{
		public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.MultistepEase(EaseFunction.EaseSine, EaseFunction.EaseCubicOut, 0.3f), 40, 25);

		public override void AI()
		{
			base.AI();

			if (Released && Progress < 0.5f)
			{
				Dust dust = Dust.NewDustPerfect(Projectile.Center + Projectile.velocity * 10 + Main.rand.NextVector2Circular(30, 30) * Main.rand.NextFloat(), DustID.Stone, Projectile.velocity, 100);
				dust.noGravity = true;
			}
		}
	}

	public override ShieldInfo SetInfo()
	{
		Item.defense = 2;
		Item.damage = 4;
		Item.useTime = Item.useAnimation = 20;
		Item.knockBack = 12;
		Item.shoot = ModContent.ProjectileType<StoneshieldBash>();

		return new ShieldInfo(20, 60);
	}

	public override void OnBlockDamage(Player player, Player.HurtInfo info) => player.AddBuff(ModContent.BuffType<StonemasonBuff>(), 2);
}