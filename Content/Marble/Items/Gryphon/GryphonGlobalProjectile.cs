using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Tiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Marble.Items.Gryphon;
public class GryphonGlobalProjectile : GlobalProjectile
{
	public bool active;
	public override bool InstancePerEntity => true;

	public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.CountsAsClass(DamageClass.Ranged);
	public override void AI(Projectile projectile)
	{
		if (active)
		{
			if (projectile.penetrate > -1)
				projectile.penetrate = -1;

			if (Main.rand.NextBool(30))
			{
				Vector2 vel = projectile.velocity.RotateRandom(0.1f) * Main.rand.NextFloat(0.2f);
				Vector2 pos = projectile.Center + Main.rand.NextVector2Circular(5f, 5f);

				ParticleHandler.SpawnParticle(new BloomParticle(pos, vel, Color.Cyan.Additive(), 0.2f, 45, extraUpdateAction: p => p.Velocity *= 0.9f));
				ParticleHandler.SpawnParticle(new GlowParticle(pos, vel, Color.White.Additive(), 0.2f, 45, extraUpdateAction: p => p.Velocity *= 0.9f));
			}
		}		
	}

	public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (active)
		{
			projectile.damage = (int)(projectile.damage * 0.7f);
			if (projectile.damage < 3)
				projectile.damage = 3;

			for (int i = 0; i < Main.rand.Next(1, 3); i++)
			{
				Dust.NewDustPerfect(projectile.Center, DustID.Electric, Main.rand.NextVector2Circular(16f, 16f), 0, default, Main.rand.NextFloat(0.5f)).noGravity = true;
			}
		}
	}
}
