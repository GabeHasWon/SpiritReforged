using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Subclasses.Greatshields;
using Terraria.DataStructures;

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

	public class StoneshieldBash : SwungProjectile
	{
		public override string Texture => AssetLoader.EmptyTexture;

		public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.MultistepEase(EaseFunction.EaseSine, EaseFunction.EaseCubicOut, 0.3f), 40, 25);

		public override void AI()
		{
			base.AI();

			if (Progress < 0.5f)
			{
				Dust dust = Dust.NewDustPerfect(Projectile.Center + Projectile.velocity * 10 + Main.rand.NextVector2Circular(30, 30) * Main.rand.NextFloat(), DustID.Stone, Projectile.velocity, 100);
				dust.noGravity = true;
			}

			Main.player[Projectile.owner].SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int direction = Projectile.direction;

			Texture2D texture = ModContent.GetInstance<StoneGreatshield>().HeldTexture;
			SpriteEffects effects = (direction == -1) ? SpriteEffects.FlipVertically : SpriteEffects.None;
			Color color = Projectile.GetAlpha(lightColor);
			float rotation = Projectile.rotation + EaseFunction.EaseSine.Ease(Progress * 2) * 0.2f * Projectile.direction;

			BasicConfiguration config = GetConfig<BasicConfiguration>();

			Vector2 position = Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY) 
				+ (Vector2.UnitX * (config.Reach / 2f) * config.Easing.Ease(1f - Progress)).RotatedBy(Projectile.rotation);

			Main.EntitySpriteDraw(texture, position, null, color, rotation, texture.Size() / 2, Projectile.scale, effects);

			#region wave
			Main.instance.LoadProjectile(ProjectileID.DD2SquireSonicBoom);

			Texture2D waveTexture = TextureAssets.Projectile[ProjectileID.DD2SquireSonicBoom].Value;
			Vector2 wavePosition = Projectile.Center - Main.screenPosition + (Vector2.UnitX * config.Reach * Progress).RotatedBy(Projectile.rotation);

			Main.EntitySpriteDraw(waveTexture, wavePosition, null, color * (1f - Progress) * 0.3f, Projectile.rotation + MathHelper.PiOver2, 
				waveTexture.Size() / 2, new Vector2(0.7f + Progress * 0.3f, 1f - Progress * 0.3f) * Projectile.scale * 0.7f, 0);
			#endregion

			return false;
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

	public override void DrawShield(ref PlayerDrawSet drawInfo, bool guarding)
	{
		if (drawInfo.drawPlayer.ownedProjectileCounts[ModContent.ProjectileType<StoneshieldBash>()] == 0) //Don't draw while performing a shield bash
			base.DrawShield(ref drawInfo, guarding);
	}
}