using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Ocean.Items.Reefhunter.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Ocean.Items.PoolNoodle;

internal class PoolNoodleGNPC : GlobalNPC
{
	public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
	{
		const int radius = 40;

		if (!npc.HasBuff<PoolNoodleBubbleBuff>() || !projectile.IsMinionOrSentryRelated || !Main.rand.NextBool(5))
			return;

		ParticleHandler.SpawnParticle(new BubblePop(npc.Center, .6f, 0.9f, 35, Main.rand.NextFloat(-5f, 5f)));

		SoundEngine.PlaySound(SoundID.Item54, npc.Center);
		SoundEngine.PlaySound(SoundID.Item86, npc.Center);
		SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/Impact_LightPop") with { PitchVariance = 0.4f, Pitch = .5f, MaxInstances = 10 }, npc.Center);
		SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/Explosion_Balloon") with { PitchVariance = 0.2f, MaxInstances = 10 }, npc.Center);
		SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/Explosion_Liquid") with { Volume = .75f, PitchVariance = 0.2f, MaxInstances = 10 }, npc.Center);

		foreach (var other in Main.ActiveNPCs)
		{
			if (other.whoAmI != npc.whoAmI && other.CanBeChasedBy() && other.DistanceSQ(projectile.Center) < radius * radius)
				other.SimpleStrikeNPC((int)(damageDone * 1.5f), (other.Center.X < npc.Center.X) ? -1 : 1, false, 4f);
		}
	}
}