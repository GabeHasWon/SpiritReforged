using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using System.Linq;
using Terraria.Audio;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items.Crook;

public class LocustDamageGlobalNPC : GlobalNPC
{
	// purely for damage purposes because Target.StrikeNPC sucks
	internal class LocustDamageProjectile : ModProjectile
	{
		private static readonly SoundStyle HitSound = SoundID.NPCDeath52 with { Volume = 0.05f, PitchVariance = 0.3f };
		public override string Texture => AssetLoader.EmptyTexture;

		public override void SetDefaults()
		{
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Summon;

			Projectile.penetrate = 1;
			Projectile.tileCollide = false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			for (int i = 0; i < 5; i++)
				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(25, 25), DustID.Poisoned,
						Main.rand.NextVector2Circular(5f, 5f), 50 + Main.rand.Next(100), default, Main.rand.NextFloat(1.2f, 1.35f)).noGravity = true;

			for (int i = 0; i < 3; i++)
			{
				Color smokeColor = new Color(5, 5, 5) * 0.2f;
				float scale = Main.rand.NextFloat(0.1f, 0.2f);
				var velSmoke = -Vector2.UnitY * 2f;
				ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), velSmoke, Color.DarkSeaGreen * 0.25f, smokeColor, scale, EaseFunction.EaseQuadOut, Main.rand.Next(30, 40)));
			}

			SoundEngine.PlaySound(HitSound, target.Center);
		}
	}

	public const int MAX_LOCUSTS = 5;

	public int AttackTimer;
	public int AttackerWhoAmI; // the most recent player to attack this NPC with a crook locust specifically. For projectile ownership

	public List<BabyLocust> locusts = new();

	public override bool InstancePerEntity => true;
	public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.CanBeChasedBy();

	public override void UpdateLifeRegen(NPC npc, ref int damage)
	{
		if (locusts.Count > 0)
		{
			if (npc.lifeRegen > 0)
				npc.lifeRegen = 0;

			npc.lifeRegen -= 2 * locusts.Count;

			if (damage < 1)
				damage = 1;
		}
	}

	public override void AI(NPC npc)
	{
		List<BabyLocust> locustsToRemove = new();

		foreach (BabyLocust locust in locusts)
		{
			locust.Update();
			if (locust.lifetime <= 0)
				locustsToRemove.Add(locust);
		}

		foreach (BabyLocust locust in locustsToRemove)
			locusts.Remove(locust);

		if (locusts.Count > 0)
			if (++AttackTimer % (180 / locusts.Count) == 0)
				if (Main.netMode != NetmodeID.MultiplayerClient)
					Projectile.NewProjectile(npc.GetSource_OnHurt(Main.player[AttackerWhoAmI], "SpiritReforged:LocustCrookBabyHit"), npc.Center + Main.rand.NextVector2Circular(npc.width / 2, npc.height / 2),
											Vector2.Zero, ModContent.ProjectileType<LocustDamageProjectile>(), 3 * locusts.Count, 0f, AttackerWhoAmI);
				else if (AttackTimer > 0)
					AttackTimer = 0;
	}

	public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		foreach (BabyLocust locust in locusts.Where(l => l.drawBehind))
			locust.DrawSelf(spriteBatch, screenPos, drawColor);

		return base.PreDraw(npc, spriteBatch, screenPos, drawColor);
	}

	public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		foreach (BabyLocust locust in locusts.Where(l => !l.drawBehind))
			locust.DrawSelf(spriteBatch, screenPos, drawColor);
	}

	public void AddLocust(int targetWhoAmI)
	{
		locusts.Add(new BabyLocust(600, targetWhoAmI));

		foreach (BabyLocust l in locusts)
		{
			l.lifetime += 120;
			if (l.lifetime > 720)
				l.lifetime = 720;
		}
	}
}