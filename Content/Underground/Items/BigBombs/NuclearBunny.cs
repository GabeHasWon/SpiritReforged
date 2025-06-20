using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underworld.Blasphemer;
using Terraria.Audio;

namespace SpiritReforged.Content.Underground.Items.BigBombs;

public class NuclearBunny : ModNPC
{
	public override string Texture => "Terraria/Images/NPC_" + NPCID.ExplosiveBunny;

	public override void Load() => On_NPC.ReleaseNPC += BoomShroomify;
	private static int BoomShroomify(On_NPC.orig_ReleaseNPC orig, int x, int y, int Type, int Style, int who)
	{
		if (Type == NPCID.ExplosiveBunny && Main.player[who].HasEquip<BoomShroom>())
		{
			int result = orig(x, y, ModContent.NPCType<NuclearBunny>(), Style, who);

			Main.npc[result].releaseOwner = (short)who;
			Main.npc[result].direction = Main.player[who].direction;
			Main.npc[result].netUpdate = true;

			return result;
		}

		return orig(x, y, Type, Style, who);
	}

	public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 7;
	public override void SetDefaults() => NPC.CloneDefaults(NPCID.ExplosiveBunny);

	public override void FindFrame(int frameHeight)
	{
		if (!NPC.IsABestiaryIconDummy && NPC.velocity.X == 0)
		{
			NPC.frameCounter = 1;
		}
		else
		{
			NPC.frameCounter += 0.25f;
			NPC.frameCounter %= Main.npcFrameCount[Type];
		}

		NPC.frame.Y = (int)NPC.frameCounter * frameHeight;
		NPC.spriteDirection = NPC.direction;
	}

	public override void OnKill()
	{
		if (Main.netMode != NetmodeID.MultiplayerClient)
			Projectile.NewProjectile(NPC.GetSource_Death(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<NuclearBomb>(), 0, 0);
	}
}

public class NuclearBomb : BombProjectile, ILargeExplosive
{
	public override string Texture => ModContent.GetInstance<NuclearBunny>().Texture;
	public int OriginalType => ProjectileID.ExplosiveBunny;

	public bool DamageTiles
	{
		get => Projectile.ai[0] == 1;
		set => Projectile.ai[0] = value ? 1 : 0;
	}

	public override void SetDefaults()
	{
		base.SetDefaults();
		Projectile.CloneDefaults(ProjectileID.ExplosiveBunny);

		SetTimeLeft(2);
		SetDamage(300, 10);

		area = 15;
	}

	public override void OnKill(int timeLeft)
	{
		if (DamageTiles)
			DestroyTiles();

		if (Main.myPlayer == Projectile.owner)
			Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Firespike>(), Projectile.damage, Projectile.knockBack, Projectile.owner);

		if (!Main.dedServ)
		{
			SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
			SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Pitch = -0.5f }, Projectile.Center);

			var ease = new PolynomialEase(x => (float)(0.5 + 0.5 * Math.Pow(x, 0.5)));
			var stretch = Vector2.One;

			const int time = 5;
			ParticleHandler.SpawnParticle(new ImpactLine(Projectile.Center, Vector2.Zero, Color.OrangeRed.Additive(), new Vector2(0.2f, 1f) * area, time));
			ParticleHandler.SpawnParticle(new ImpactLine(Projectile.Center, Vector2.Zero, Color.White.Additive(), new Vector2(0.1f, 1f) * area, time));

			for (int i = 0; i < area * 2; i++)
			{
				float magnitude = Main.rand.NextFloat();

				var color = Color.OrangeRed.Additive();
				var velocity = Main.rand.NextVector2Unit() * magnitude * 10f;
				float scale = (1f - magnitude) * 0.08f * area;

				ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center + velocity * 10, velocity, color, scale, 10, 3));
				ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center + velocity * 10, velocity, Color.White.Additive(), scale * .5f, 10, 3));

				var d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(16f * area), DustID.Torch, Scale: Main.rand.NextFloat() + .5f);
				d.noGravity = true;
			}

			for (int i = 0; i < 8; i++)
			{
				float mag = Main.rand.NextFloat();
				var velocity = Vector2.UnitY * -(mag * 3);
				var color = Color.Lerp(Color.PaleVioletRed, Color.Black, mag);

				ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Center, velocity, color, Projectile.scale * mag * 0.5f, EaseFunction.EaseCircularOut, 300));
			}
		}
	}

	public override bool? CanCutTiles() => DamageTiles ? null : false;
}