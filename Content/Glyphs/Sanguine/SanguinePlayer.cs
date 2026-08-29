using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Glyphs.Sanguine;

public partial class SanguineGlyph
{
	public sealed class SanguinePlayer : ModPlayer
	{
		internal const float HEALTH_DAMAGE_RATE = 0.0015f;

		internal float storedHealth;
		internal int lifestealCooldown;

		private int lastTickHP;
		private int buffDecayCooldown;
		private bool canStoreHealth = false; //Start with it false to prevent player initalization counting for gaining hp

		public override void ResetEffects()
		{
			if (buffDecayCooldown > 0)
			{
				storedHealth *= 0.9995f; //really small constant decay as a form of softcap
				buffDecayCooldown--;
			}
			else
			{
				storedHealth = Math.Max(0, storedHealth - 0.025f); //Slow static decay
				storedHealth *= 0.998f; //Percentage based decay to prevent the buff from getting too high while still being decent at low values
			}

			if(Player.dead)
				storedHealth = 0;

			if(storedHealth >= 1)
				Player.AddBuff(ModContent.BuffType<SanguineStackingBuff>(), 2);

			if (lifestealCooldown > 0)
				lifestealCooldown--;
		}

		public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (item.GetGlyph().ItemType == ModContent.ItemType<SanguineGlyph>())
			{
				float damageBonus = 1f + storedHealth * HEALTH_DAMAGE_RATE;

				modifiers.FinalDamage *= damageBonus;
			}
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (proj.GetGlyph().ItemType == ModContent.ItemType<SanguineGlyph>())
			{
				float damageBonus = 1f + storedHealth * HEALTH_DAMAGE_RATE;

				modifiers.FinalDamage *= damageBonus;
			}
		}

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (item.GetGlyph().ItemType == ModContent.ItemType<SanguineGlyph>())
				HitEffects(target, damageDone);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (proj.GetGlyph().ItemType == ModContent.ItemType<SanguineGlyph>())
				HitEffects(target, damageDone);
		}

		public override void PostUpdate()
		{
			//Any positive difference, including regen, counts as healed hp for the buff
			if(Player.statLife > lastTickHP && Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<SanguineGlyph>() && canStoreHealth)
			{
				if (!Player.HasBuff<SanguineStackingBuff>())
					Player.AddBuff(ModContent.BuffType<SanguineStackingBuff>(), 60);

				int difference = Player.statLife - lastTickHP;
				storedHealth += difference;
				buffDecayCooldown = 60;
			}

			//Store information for next tick

			lastTickHP = Player.statLife;
			canStoreHealth = !Player.dead; //Prevent storing health when the player respawns
		}

		public void HitEffects(NPC target, int damageDone)
		{
			if (!target.CanBeChasedBy())
				return;

			bool leechedLife = false;
			if (Player.statLife < Player.statLifeMax2 && target.canGhostHeal && lifestealCooldown <= 0)
			{
				//damageDone shouuuld always return at least 1 but I don't trust this game
				float amountToHeal = (float)Math.Log2(Math.Max(damageDone, 1));

				float healthPercentageReverse = 1f - Player.statLife / (float)Player.statLifeMax2;
				amountToHeal *= MathHelper.Lerp(0f, 1.5f, healthPercentageReverse);

				if ((int)amountToHeal < 1)
					amountToHeal = 1;

				Player.Heal((int)amountToHeal);

				leechedLife = true;
				lifestealCooldown = 30;

				HealVFX(healthPercentageReverse);
			}

			HitVFX(target, leechedLife);
		}

		private void HitVFX(NPC target, bool leechedLife)
		{
			Vector2 dir = target.DirectionTo(Player.Center);
			Vector2 position = target.Center + dir * target.width / 2;

			ParticleHandler.SpawnParticle(new SmokeCloud(position, Main.rand.NextVector2Circular(1.5f, 1.5f), Color.DarkRed * 0.3f, 0.06f, EaseFunction.EaseQuadOut, 30, false)
			{
				Pixellate = true,
				PixelDivisor = 4
			});

			var dust = Dust.NewDustPerfect(position, DustID.Blood, Main.rand.NextVector2Circular(1.5f, 1.5f), 70, default, Main.rand.NextFloat(0.6f, 1.2f));
			dust.noGravity = Main.rand.NextBool();
			dust.fadeIn = 2;

			ParticleHandler.SpawnParticle(new StickyBloodParticle(position, Main.rand.NextVector2Circular(1.5f, 1.5f), Main.rand.NextFloat(0.6f, 1.2f), Main.rand.Next(80, 120), 0.2f));

			if (storedHealth > 0)
			{

				for (int i = 0; i < 2; i++)
				{
					ParticleHandler.SpawnParticle(new BloodHit(target, dir * target.width / 2, Main.rand.Next(30, 40), dir.ToRotation(), Main.rand.NextFloat(0.9f, 1.1f)));

					dust = Dust.NewDustPerfect(position, DustID.Blood, -Vector2.UnitY * 2f + position.DirectionFrom(Player.Center).RotatedByRandom(0.3f) * Main.rand.NextFloat(1f, 6f), 70, default, Main.rand.NextFloat(0.6f, 1.2f));
					dust.noGravity = Main.rand.NextBool();
					dust.fadeIn = 2;

					ParticleHandler.SpawnParticle(new StickyBloodParticle(position, -Vector2.UnitY * 2f + position.DirectionFrom(Player.Center).RotatedByRandom(0.3f) * Main.rand.NextFloat(1f, 4f), Main.rand.NextFloat(0.6f, 1.2f), Main.rand.Next(80, 120), 0.1f));

					ParticleHandler.SpawnParticle(new SmokeCloud(position, position.DirectionFrom(Player.Center).RotatedByRandom(0.3f) * Main.rand.NextFloat(1f, 3f), Color.DarkRed * 0.5f, 0.09f, EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 3
					});
				}
			}
		}

		private void HealVFX(float strength)
		{
			int numBlood = (int)(strength * 6);
			numBlood = (int)MathHelper.Clamp(numBlood, 1, 4);
			for(int i = 0; i < numBlood; i++)
			{
				Vector2 posOffset = Main.rand.NextVector2Unit() * Main.rand.NextFloat(32, 52);
				Vector2 velocity = Vector2.Normalize(posOffset).RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(5, 8);
				float scale = Main.rand.NextFloat(0.75f, 1.33f);

				ParticleHandler.SpawnQueuedParticle(new SanguineBlood(Player, posOffset, velocity, scale, 60), Main.rand.Next(10));
			}
		}
	}
}
