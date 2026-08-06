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
		internal List<SanguineStack> stacks = new();
		internal int lifestealCooldown;

		public override void ResetEffects()
		{
			stacks ??= new();

			foreach (SanguineStack stack in stacks)
				if (stack.timer > 0)
					stack.timer--;

			stacks.RemoveAll(s => s.timer <= 0);

			if (lifestealCooldown > 0)
				lifestealCooldown--;
		}

		public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (item.GetGlyph().ItemType == ModContent.ItemType<SanguineGlyph>())
			{
				float damageBonus = 1f;
				foreach (SanguineStack stack in stacks)
					damageBonus += stack.damageBonus;

				modifiers.FinalDamage *= damageBonus;
			}
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (proj.GetGlyph().ItemType == ModContent.ItemType<SanguineGlyph>())
			{
				float damageBonus = 1f;
				foreach (SanguineStack stack in stacks)
					damageBonus += stack.damageBonus;

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

		public void HitEffects(NPC target, int damageDone)
		{
			if (!target.CanBeChasedBy())
				return;

			bool leechedLife = false;
			if (Player.statLife < Player.statLifeMax2 && target.canGhostHeal && lifestealCooldown <= 0)
			{
				float amountToHeal = (float)damageDone / 10;

				amountToHeal *= MathHelper.Lerp(1f, 3f, 1f - Player.statLife / (float)Player.statLifeMax2);
				if ((int)amountToHeal < 1)
					amountToHeal = 1;

				if (!Player.HasBuff<SanguineStackingBuff>())
					Player.AddBuff(ModContent.BuffType<SanguineStackingBuff>(), 60);

				if (amountToHeal > 6)
					amountToHeal = 6;

				Player.Heal((int)amountToHeal);

				if (stacks.Count < 15)
					stacks.Add(new SanguineStack(180, 0.03f + damageDone * 0.001f)); // 3% increase, plus 0.1% of the damage dealt, ex: 3% + (10 * 0.001) = 4% boost

				leechedLife = true;
				lifestealCooldown = 30;
			}

			float angle = Main.rand.NextFloat(MathHelper.Pi);

			Vector2 dir = target.DirectionTo(Player.Center);
			Vector2 position = target.Center + dir * target.width / 2;

			Color c1, c2;
			c1 = Color.DarkRed;
			c2 = new Color(200, 25, 100);

			ParticleHandler.SpawnParticle(new SmokeCloud(position, Main.rand.NextVector2Circular(1.5f, 1.5f), Color.DarkRed * 0.3f, 0.06f, EaseFunction.EaseQuadOut, 30, false)
			{
				Pixellate = true,
				PixelDivisor = 4
			});

			var dust = Dust.NewDustPerfect(position, DustID.Blood, Main.rand.NextVector2Circular(1.5f, 1.5f), 70, default, Main.rand.NextFloat(0.6f, 1.2f));
			dust.noGravity = Main.rand.NextBool();
			dust.fadeIn = 2;

			if (Main.rand.NextBool())
				ParticleHandler.SpawnParticle(new StickyBloodParticle(position, Main.rand.NextVector2Circular(1.5f, 1.5f), Main.rand.NextFloat(0.6f, 1.2f), Main.rand.Next(80, 120), 0.2f));

			if (leechedLife)
			{
				SoundEngine.PlaySound(SoundID.NPCHit1 with { Pitch = -0.3f, PitchVariance = 0.1f }, target.Center);

				ParticleHandler.SpawnParticle(new BloodHit(target, dir * target.width / 2, Main.rand.Next(20, 35), dir.ToRotation(), Main.rand.NextFloat(0.9f, 1.1f)));

				for (int i = 0; i < 2; i++)
				{
					dust = Dust.NewDustPerfect(position, DustID.Blood, -Vector2.UnitY * 2f + position.DirectionTo(Player.Center).RotatedByRandom(0.3f) * Main.rand.NextFloat(1f, 6f), 70, default, Main.rand.NextFloat(0.6f, 1.2f));
					dust.noGravity = Main.rand.NextBool();
					dust.fadeIn = 2;

					ParticleHandler.SpawnParticle(new StickyBloodParticle(position, -Vector2.UnitY * 2f + position.DirectionTo(Player.Center).RotatedByRandom(0.3f) * Main.rand.NextFloat(1f, 7f), Main.rand.NextFloat(0.6f, 1.2f), Main.rand.Next(80, 120), 0.1f));

					ParticleHandler.SpawnParticle(new SmokeCloud(position, position.DirectionTo(Player.Center).RotatedByRandom(0.3f) * Main.rand.NextFloat(1f, 3f), Color.DarkRed * 0.5f, 0.09f, EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 3
					});
				}
			}
		}
	}
}
