using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Vanilla.Leather.MarksmanArmor;

internal class MarksmanPlayer : ModPlayer
{
	public bool Concentrated => concentratedCooldown <= 0;
	private int concentratedCooldown = 360;

	/// <summary> Whether the Marksman armor set bonus is active. </summary>
	public bool active = false;

	public override void ResetEffects() => active = false;

	public override void PostUpdateEquips()
	{
		if (!active)
		{
			concentratedCooldown = 420;
			return;
		}

		var newCol = Color.Lerp(Color.LightGoldenrodYellow, Color.Goldenrod, Main.rand.NextFloat());
		bool wasConcentrated = Concentrated;

		concentratedCooldown -= Player.velocity.X == 0f ? 2 : 1;

		if (Concentrated)
		{
			if (!wasConcentrated) //Just concentrated
			{
				SoundEngine.PlaySound(SoundID.DD2_DarkMageCastHeal with { Pitch = 1.5f }, Player.Center);

				for (int i = 0; i < 12; i++)
					ParticleHandler.SpawnParticle(new GlowParticle(Player.Center, Main.rand.NextVector2CircularEdge(1, 1) * Main.rand.NextFloat(1f, 3f), newCol, Main.rand.NextFloat(0.3f, 0.5f), Main.rand.Next(30, 50), 12, delegate (Particle p) { p.Velocity *= 0.9f; }));

				ParticleHandler.SpawnParticle(new TexturedPulseCircle(Player.Center, Color.White * .15f, .5f, 120, 15, "Extra_49", new Vector2(1), EaseFunction.EaseCubicIn, true));
			}

			if (Main.rand.NextBool(12))
			{
				var rect = Player.getRect();
				var headRect = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height / 3);
				var position = Main.rand.NextVector2FromRectangle(headRect);

				ParticleHandler.SpawnParticle(new GlowParticle(position, Vector2.UnitY * -Main.rand.NextFloat(.5f), newCol, Main.rand.NextFloat(.2f, .3f), 80, 12));
			}
		}
	}

	public override void OnHurt(Player.HurtInfo info)
	{
		if (!active)
			return;

		if (Concentrated)
		{
			SoundEngine.PlaySound(SoundID.DD2_LightningAuraZap with { PitchVariance = .5f }, Player.Center);
			SoundEngine.PlaySound(SoundID.Item98 with { Volume = .5f }, Player.Center);
		}

		concentratedCooldown = 360;
	}

	public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
	{
		if (TryDoImpact(target.Center))
		{
			modifiers.FinalDamage *= 1.2f;
			modifiers.SetCrit();
			concentratedCooldown = 300;
		}
	}

	public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
	{
		var position = proj.Center;

		if (TryDoImpact(position))
		{
			var scale = new Vector2(.5f, 1.5f);
			ParticleHandler.SpawnParticle(new ImpactLine(position, Vector2.Normalize(-proj.velocity), Color.White, scale, 8));
			ParticleHandler.SpawnParticle(new ImpactLine(position, Vector2.Normalize(-proj.velocity), Color.Orange, scale * 1.25f, 8));

			modifiers.FinalDamage *= 1.2f;
			modifiers.SetCrit();
			concentratedCooldown = 300;
		}
	}

	private bool TryDoImpact(Vector2 position)
	{
		if (!Concentrated)
			return false;

		SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundMiss with { PitchVariance = .5f }, position);
		SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact with { Pitch = .5f }, position);
		SoundEngine.PlaySound(SoundID.DD2_LightningBugZap with { Pitch = -.5f, Volume = .8f }, position);

		var newCol = Color.Lerp(Color.LightGoldenrodYellow, Color.Goldenrod, Main.rand.NextFloat());

		for (int i = 0; i < 12; i++)
		{
			var unit = Main.rand.NextVector2Unit();
			Dust.NewDustPerfect(position + unit * Main.rand.NextFloat(10f), DustID.AmberBolt, unit * Main.rand.NextFloat(2f), Scale: .75f).noGravity = true;

			if (i < 6)
				ParticleHandler.SpawnParticle(new GlowParticle(position, Main.rand.NextVector2Unit() * Main.rand.NextFloat(.5f, 2), newCol, Main.rand.NextFloat(.15f, .3f), Main.rand.Next(30, 50), 12, delegate (Particle p) { p.Velocity *= .96f; }));
		}

		for(int i = 0; i < 4; i++)
			ParticleHandler.SpawnParticle(new FireParticle(position, Main.rand.NextVector2Circular(2, 2), [Color.LightGoldenrodYellow.Additive(), Color.Goldenrod.Additive(), Color.Orange.Additive()], 1.5f, 0, Main.rand.NextFloat(0.07f, 0.12f), EaseFunction.EaseQuadIn, Main.rand.Next(20, 40)) { FinalScaleMod = 0.25f });

		ParticleHandler.SpawnParticle(new TexturedPulseCircle(position, Color.White, .5f, 80, 20, "Extra_49",
				new Vector2(1), EaseFunction.EaseCubicOut));

		ParticleHandler.SpawnParticle(new TexturedPulseCircle(position, Color.LightGoldenrodYellow * .4f, .6f, 90, 50, "Bloom",
			new Vector2(2), EaseFunction.EaseCubicOut));

		return true;
	}
}
