using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.SaltFlats.NPCs;
using Terraria.Audio;

namespace SpiritReforged.Content.Glyphs.Void;

public class VoidPlayer : ModPlayer
{
	public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (proj.GetGlyph().ItemType == ModContent.ItemType<VoidGlyph>() && Main.rand.NextBool() && target.CanBeChasedBy())
		{
			VoidNPC.AddVoidStack(Player, target, damageDone / 2);

			if (Main.netMode != NetmodeID.SinglePlayer)
				MultiplayerLoader.Send(nameof(VoidNPC.AddVoidStack), -1, -1, Player, target, damageDone / 2);
		}
	}

	public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (item.GetGlyph().ItemType == ModContent.ItemType<VoidGlyph>() && Main.rand.NextBool() && target.CanBeChasedBy())
		{
			VoidNPC.AddVoidStack(Player, target, damageDone / 2);

			if (Main.netMode != NetmodeID.SinglePlayer)
				MultiplayerLoader.Send(nameof(VoidNPC.AddVoidStack), -1, -1, Player, target, damageDone / 2);
		}
	}
}

public class VoidNPC : GlobalNPC
{
	private enum SingularityResult { Created, Found, NotCreated, FoundDying }

	/// <summary> Defense strength multiplier for use with <see cref="defenseReductionTimer"/>. </summary>
	public const float DEFENSE_REDUCTION_MULT = 0.8f;
	public const int MAX_STACKS = 15;

	public override bool InstancePerEntity => true;

	public int defenseReductionTimer;

	[NetSynced(true)]
	public static void AddVoidStack(Player owner, NPC target, int damageDealt)
	{
		float pitchMultiplier = 1;
		SingularityResult result = TryGetSingularity(owner, target, out SingularCollapse collapse, owner.whoAmI == Main.myPlayer && owner.ownedProjectileCounts[ModContent.ProjectileType<SingularCollapse>()] <= 0);

		if (result is SingularityResult.Created or SingularityResult.Found && collapse.Stacks < MAX_STACKS)
		{
			collapse.Stacks = Math.Min(collapse.Stacks + 1, MAX_STACKS);
			collapse.Projectile.damage += (int)Math.Ceiling(damageDealt / 6f) + (Main.hardMode ? 6 : 2);

			pitchMultiplier = collapse.Stacks;
		}

		//Still run application effects if the singularity has not been created (for non-owning clients specifically on first application)
		if (!Main.dedServ && (collapse == null || collapse.Stacks < MAX_STACKS) && result is SingularityResult.Created or SingularityResult.Found or SingularityResult.NotCreated)
		{
			SoundEngine.PlaySound(SoundID.DD2_WitherBeastAuraPulse with { Volume = 2f, Pitch = 0.1f * pitchMultiplier }, target.Center);
			SoundEngine.PlaySound(Wisp.Hit with { Volume = 2f, Pitch = -0.1f * pitchMultiplier }, target.Center);

			for (int i = 0; i < 1 + Main.rand.Next(0, 3); i++)
			{
				Vector2 velocity = Main.rand.NextVector2Circular(6f, 3f);
				float rotation = Main.rand.NextFloat(6.28f);

				ParticleHandler.SpawnParticle(new SharpStarParticle(target.Center, velocity, Color.Purple.Additive(), 0.2f, 35, 0, DecelerateAction)
				{ Rotation = rotation });

				ParticleHandler.SpawnParticle(new SharpStarParticle(target.Center, velocity, Color.LightPink.Additive(), 0.1f, 35, 0, DecelerateAction, false)
				{ Rotation = rotation });

				velocity = Main.rand.NextVector2Circular(4f, 4f);
				float scale = Main.rand.NextFloat(0.1f, 0.3f);
				bool rotDir = Main.rand.NextBool();

				ParticleHandler.SpawnParticle(new GlowParticle(target.Center, velocity, Color.Purple.Additive(), scale, 90, 12, rotDir ? SpinAction : SpinAction_2));
				ParticleHandler.SpawnParticle(new GlowParticle(target.Center, velocity, Color.White.Additive(), scale * 0.5f, 90, 12, rotDir ? SpinAction : SpinAction_2));
			}
		}

		static void DecelerateAction(Particle p)
		{
			p.Velocity *= 0.95f;
			p.Rotation += p.Velocity.Length() * 0.1f;
		}

		static void SpinAction(Particle p)
		{
			p.Velocity *= 0.97f;
			p.Velocity = p.Velocity.RotatedBy(0.08f);
		}

		static void SpinAction_2(Particle p)
		{
			p.Velocity *= 0.97f;
			p.Velocity = p.Velocity.RotatedBy(-0.08f);
		}
	}

	/// <summary> Gets a <see cref="SingularCollapse"/> instance from <paramref name="owner"/>. </summary>
	private static SingularityResult TryGetSingularity(Player owner, NPC target, out SingularCollapse singularity, bool create = true)
	{
		foreach (Projectile projectile in Main.ActiveProjectiles)
		{
			if (projectile.ModProjectile is SingularCollapse singularCollapse && singularCollapse.TargetIndex == target.whoAmI)
			{
				singularity = singularCollapse;
				return singularCollapse.dying ? SingularityResult.FoundDying : SingularityResult.Found;
			}
		}

		singularity = create ? (SingularCollapse)Projectile.NewProjectileDirect(owner.GetSource_OnHit(target, "SpiritReforged: Void Glyph Apply"), target.Center, Vector2.Zero, ModContent.ProjectileType<SingularCollapse>(), 0, 7f, owner.whoAmI, target.whoAmI).ModProjectile : null;
		return create ? SingularityResult.Created : SingularityResult.NotCreated;
	}

	public override void ResetEffects(NPC npc)
	{
		if (defenseReductionTimer > 0)
			defenseReductionTimer--;
	}

	public override void ModifyHitNPC(NPC npc, NPC target, ref NPC.HitModifiers modifiers)
	{
		if (defenseReductionTimer > 0)
			modifiers.Defense *= DEFENSE_REDUCTION_MULT;
	}

	public override void AI(NPC npc)
	{
		if (!Main.dedServ && defenseReductionTimer > 0 && Main.rand.NextBool(240))
			ParticleHandler.SpawnParticle(new VoidParticle(npc.Center, Vector2.Zero, Color.Purple.Additive(), 0f, 0.3f, 60, npc));
	}

	public override void DrawEffects(NPC npc, ref Color drawColor)
	{
		if (defenseReductionTimer > 0)
		{
			Color darken = Color.Lerp(drawColor, Color.Black, 0.5f);

			if (defenseReductionTimer < 60)
				drawColor = Color.Lerp(drawColor, darken, defenseReductionTimer / 60f);
			else
				drawColor = darken;
		}
	}
}