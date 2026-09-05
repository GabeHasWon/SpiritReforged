using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Ocean.Items.Reefhunter.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Ocean.Items.PoolNoodle;

internal class BubbledGlobalNPC : GlobalNPC
{
	public class Bubbled : ModBuff
	{
		public override string Texture => "Terraria/Images/Buff";

		public override void SetStaticDefaults()
		{
			Main.debuff[Type] = true;
			Main.buffNoSave[Type] = true;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			if (!Main.dedServ && Main.rand.NextBool(35))
				ParticleHandler.SpawnParticle(new BubbleParticle(npc.Center, new Vector2(0, Main.rand.NextFloat(-1.5f, 0.5f)), Main.rand.NextFloat(0.1f, 0.3f), 40));
		}
	}

	private static readonly SoundStyle Pop = new("SpiritReforged/Assets/SFX/Projectile/Impact_LightPop")
	{
		PitchVariance = 0.4f,
		Pitch = 0.5f
	};

	private static readonly SoundStyle BalloonPop = new("SpiritReforged/Assets/SFX/Projectile/Explosion_Balloon")
	{
		PitchVariance = 0.2f
	};

	private static readonly SoundStyle LiquidPop = new("SpiritReforged/Assets/SFX/Projectile/Explosion_Liquid")
	{
		Volume = 0.75f,
		PitchVariance = 0.2f
	};

	public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
	{
		const int radius = 40;

		if (projectile.IsMinionOrSentryRelated && npc.HasBuff<Bubbled>() && Main.rand.NextBool(5))
		{
			ParticleHandler.SpawnParticle(new BubblePop(npc.Center, 0.6f, 0.9f, 35, Main.rand.NextFloat(-5f, 5f)));

			SoundEngine.PlaySound(SoundID.Item54, npc.Center);
			SoundEngine.PlaySound(SoundID.Item86, npc.Center);

			SoundEngine.PlaySound(Pop, npc.Center);
			SoundEngine.PlaySound(BalloonPop, npc.Center);
			SoundEngine.PlaySound(LiquidPop, npc.Center);

			foreach (var other in Main.ActiveNPCs)
			{
				if (other.whoAmI != npc.whoAmI && other.CanBeChasedBy() && other.DistanceSQ(projectile.Center) < radius * radius)
					other.SimpleStrikeNPC((int)(damageDone * 1.5f), (other.Center.X < npc.Center.X) ? -1 : 1, false, 4f);
			}
		}
	}
}