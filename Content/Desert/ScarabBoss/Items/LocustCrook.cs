using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using System.IO;
using System.Linq;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items;

public class LocustCrook : ModItem
{
	// file is slightly messy
	public sealed class LocustCrookProjectile : ModProjectile 
	{
		private const int MAX_ATTACK_COOLDOWN = 300;
		public static readonly SoundStyle EmbedSound = SoundID.DD2_MonkStaffGroundMiss with { Volume = 0.5f, PitchVariance = 0.2f };

		public bool HitTile
		{
			get => (int)Projectile.ai[0] == 1;
			set => Projectile.ai[0] = value ? 1 : 0;
		}

		public int TargetWhoAmI
		{
			get => (int)Projectile.ai[1];
			set => Projectile.ai[1] = value;
		}

		public int AttackTimer
		{
			get => (int)Projectile.ai[2];
			set => Projectile.ai[2] = value;
		}

		public int Timer
		{
			get => (int)Projectile.localAI[0];
			set => Projectile.localAI[0] = value;
		}

		public NPC MinionTarget
		{
			get
			{
				if (Owner.HasMinionAttackTargetNPC && Main.npc[Owner.MinionAttackTargetNPC].Distance(Projectile.Center) < 1000f)
					return Main.npc[Owner.MinionAttackTargetNPC];

				return null;
			}
		}

		public Player Owner => Main.player[Projectile.owner];

		public NPC Target => TargetWhoAmI > 0 ? Main.npc[(int)TargetWhoAmI] : null;

		private readonly List<BabyLocust> _orbitingLocusts = new();
		private Vector2 _oldVelocity = Vector2.Zero;
		private int _tileHitTimer;

		public override string Texture => ModContent.GetInstance<LocustCrook>().Texture;

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailingMode[Type] = 0;
			ProjectileID.Sets.TrailCacheLength[Type] = 7;
		}

		public override void SetDefaults()
		{
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.Size = new Vector2(14);
			Projectile.timeLeft = Projectile.SentryLifeTime;
			Projectile.sentry = true;

			Projectile.hide = true;
		}

		public override bool? CanDamage() => false;

		public override bool ShouldUpdatePosition() => !HitTile;

		public override bool PreAI()
		{
			Timer++;

			if (HitTile)
			{
				foreach (BabyLocust l in _orbitingLocusts)
				{
					l.Update();
				}

				NPC target = FindTarget();
				if (target != default)
					TargetWhoAmI = target.whoAmI;

				if (MinionTarget != null)
					TargetWhoAmI = MinionTarget.whoAmI;

				if (Target is null)
				{
					AttackTimer = MAX_ATTACK_COOLDOWN - 60;
					TargetWhoAmI = -1;
				}
				else
				{
					if (Main.rand.NextBool(15))
					{
						Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5, 5), DustID.Poisoned,
							-Projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.5f), 100 + Main.rand.Next(100), default, Main.rand.NextFloat(1f, 1.5f)).noGravity = true;
					}

					if (!Target.active || Target is null)
					{
						TargetWhoAmI = -1;
						return false;
					}

					if (++AttackTimer % MAX_ATTACK_COOLDOWN == 0)
					{
						if (Main.myPlayer == Projectile.owner)
							Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, -Projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(1f),
								ModContent.ProjectileType<CrookLocust>(), Projectile.damage, Projectile.knockBack, Projectile.owner, TargetWhoAmI, 60);

						SoundEngine.PlaySound(SoundID.Item97 with { Volume = 0.33f}, Projectile.Center);
						_tileHitTimer = 15;
					}
				}

				if (_tileHitTimer > 0)
					_tileHitTimer--;

				float progress = _tileHitTimer / 20f;

				Projectile.velocity = _oldVelocity;

				if (progress < 1f)
					Projectile.rotation = Projectile.velocity.ToRotation() + (Main.rand.NextBool() ? -1 : 1) * (Main.rand.NextFloat(0.15f, 0.3f) * EaseBuilder.EaseCircularIn.Ease(progress)) + MathHelper.PiOver4;
				else
					Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

				return false;
			}

			return base.PreAI();
		}
		public override void AI()
		{
			AttackTimer++;

			if (Main.rand.NextBool(15))
			{
				ParticleHandler.SpawnParticle(new SharpStarParticle(Projectile.Center + Main.rand.NextVector2Circular(25f, 25f),
					-Projectile.velocity * 0.05f, Color.White.Additive() * 0.5f, new Color(255, 120, 0, 0), Main.rand.NextFloat(0.1f, 0.2f), 20, 0f));
			}

			Projectile.velocity *= 0.98f;
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

			if (AttackTimer > 20)
				if (Projectile.velocity.Y < 16f)
				{
					Projectile.velocity.Y += 0.3f;

					if (Projectile.velocity.Y > 0)
						Projectile.velocity.Y *= 1.1f;
				}
				else
					Projectile.velocity.Y = 16f;
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			if (!HitTile)
			{
				for (int i = 0; i < 3; i++)
				{
					_orbitingLocusts.Add(new BabyLocust(60, Projectile.whoAmI, false));
				}

				HitTile = true;
				_tileHitTimer = 20;
				AttackTimer = MAX_ATTACK_COOLDOWN - 60;

				Projectile.position += oldVelocity * 1.25f;
				_oldVelocity = oldVelocity;
				Projectile.rotation = oldVelocity.ToRotation() + MathHelper.PiOver4;
				SoundEngine.PlaySound(EmbedSound, Projectile.Center);
			}

			return false;
		}

		public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => behindNPCsAndTiles.Add(index);

		public override bool PreDraw(ref Color lightColor)
		{
			foreach (BabyLocust locust in _orbitingLocusts.Where(l => l.drawBehind))
			{
				locust.DrawSelf(Main.spriteBatch, Main.screenPosition, lightColor);
			}

			var tex = TextureAssets.Projectile[Type].Value;
			var solid = TextureColorCache.ColorSolid(tex, Color.White);
			var bloom = AssetLoader.LoadedTextures["BloomNonPremult"].Value;

			float fade = 1f;
			if (Timer < 30f)
				fade = Timer / 30f;

			Vector2 drawPos = Projectile.Center + new Vector2(-20f, 0).RotatedBy(Projectile.rotation - MathHelper.PiOver4);

			if (!HitTile)
			{
				for (int i = 0; i < Projectile.oldPos.Length; i++)
				{
					Vector2 pos = Projectile.oldPos[i] + new Vector2(-20f, 0).RotatedBy(Projectile.rotation - MathHelper.PiOver4) + Projectile.Size / 2f;
					float lerp = 1f - i / (float)Projectile.oldPos.Length;

					Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(255, 150, 0, 0) * 0.25f * fade * lerp,
					  Projectile.rotation, tex.Size() / 2f, Projectile.scale, 0, 0f);
				}
			}			
			else 
			{
				float outLineFade = 1f;

				if (_tileHitTimer > 0)
				{
					outLineFade = 1f - _tileHitTimer / 20f;

					for (int i = 0; i < Projectile.oldPos.Length; i++)
					{
						Vector2 pos = Projectile.oldPos[i] + new Vector2(-20f, 0).RotatedBy(Projectile.rotation - MathHelper.PiOver4) + Projectile.Size / 2f;
						float lerp = 1f - i / (float)Projectile.oldPos.Length;

						Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(255, 150, 0, 0) * 0.25f * fade * lerp,
						  Projectile.rotation, tex.Size() / 2f, Projectile.scale, 0, 0f);
					}
				}

				float sinFade = (float)Math.Abs(Math.Sin(Timer * 0.025f));

				Main.EntitySpriteDraw(solid, drawPos + Main.rand.NextVector2CircularEdge(1.5f, 1.5f) * sinFade - Main.screenPosition, null, Color.Black * sinFade * outLineFade * 0.5f, Projectile.rotation, solid.Size() / 2, 1f, SpriteEffects.None);
			}

			Main.EntitySpriteDraw(tex, drawPos - Main.screenPosition, null, lightColor * fade, Projectile.rotation, tex.Size() / 2f, 1f, 0, 0);

			if (HitTile)
			{
				float sinFade = (float)Math.Abs(Math.Sin(Timer * 0.025f));

				Main.EntitySpriteDraw(solid, drawPos + Main.rand.NextVector2CircularEdge(1.5f, 1.5f) * sinFade - Main.screenPosition, null, Color.DarkOliveGreen * sinFade * 0.5f, Projectile.rotation, solid.Size() / 2, 1f, SpriteEffects.None);

				Main.spriteBatch.End();
				Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

				float fadeIn = 1f - _tileHitTimer / 20f;

				Main.spriteBatch.Draw(bloom, drawPos + new Vector2(20f, 0f).RotatedBy(Projectile.rotation - MathHelper.PiOver4) - Main.screenPosition, null, Color.Black * fadeIn * 0.4f,
					  Projectile.rotation, bloom.Size() / 2f, 1.5f, 0, 0f);
				
				Main.spriteBatch.Draw(bloom, drawPos + new Vector2(20f, 0f).RotatedBy(Projectile.rotation - MathHelper.PiOver4) - Main.screenPosition, null, Color.DarkGreen * fadeIn * 0.25f,
					  Projectile.rotation, bloom.Size() / 2f, 1f, 0, 0f);

				Main.spriteBatch.End();
				Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
			}

			foreach (BabyLocust locust in _orbitingLocusts.Where(l => !l.drawBehind))
			{
				locust.DrawSelf(Main.spriteBatch, Main.screenPosition, lightColor);
			}

			return false;
		}

		public override void SendExtraAI(BinaryWriter writer)
		{
			writer.WriteVector2(_oldVelocity);
			writer.Write(_tileHitTimer);
		}

		public override void ReceiveExtraAI(BinaryReader reader)
		{
			_oldVelocity = reader.ReadVector2();
			_tileHitTimer = reader.ReadInt32();
		}

		internal NPC FindTarget() => Main.npc.Where(n => n.CanBeChasedBy() && n.Distance(Owner.Center) < 1000f).OrderBy(n => n.Distance(Projectile.Center)).FirstOrDefault();
	}

	public sealed class CrookLocust : ModProjectile
	{
		public sealed class LocustExplosion : ModProjectile
		{
			public override string Texture => AssetLoader.EmptyTexture;

			public override void SetDefaults()
			{
				Projectile.friendly = true;
				Projectile.DamageType = DamageClass.Summon;
				Projectile.Size = new Vector2(90);
				Projectile.timeLeft = 10;

				Projectile.usesLocalNPCImmunity = true;
				Projectile.localNPCHitCooldown = -1;

				Projectile.penetrate = -1;
				Projectile.tileCollide = false;
			}

			public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
			{
				target.TryGetGlobalNPC<LocustDamageGlobalNPC>(out var globalNPC);

				for (int i = 0; i < 2 + Main.rand.Next(3); i++)
				{
					if (globalNPC?.locusts.Count < LocustDamageGlobalNPC.MAX_LOCUSTS)
					{
						globalNPC.AddLocust(target.whoAmI);
						globalNPC.AttackerWhoAmI = Projectile.owner;
					}
				}
			}
		}

		public sealed class CrookLocustGore : ModProjectile
		{
			public override void SetDefaults()
			{
				Projectile.Size = new Vector2(6);
				Projectile.timeLeft = 120;
				Projectile.tileCollide = true;
			}

			public override void AI()
			{
				Projectile.velocity *= 0.98f;
				Projectile.rotation += Projectile.velocity.X * 0.05f;

				if (Projectile.timeLeft < 110)
					if (Projectile.velocity.Y < 16f)
					{
						Projectile.velocity.Y += 0.15f;

						if (Projectile.velocity.Y > 0)
							Projectile.velocity.Y *= 1.1f;
					}
					else
						Projectile.velocity.Y = 16f;
			}

			public override void OnKill(int timeLeft)
			{
				for (int i = 0; i < 5; i++)
				{
					Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5, 5), DustID.Poisoned,
							Main.rand.NextVector2Circular(3f, 3f), 50 + Main.rand.Next(100), default, Main.rand.NextFloat(1.2f, 2f)).noGravity = true;

					Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(50, 50), DustID.Poisoned, 
						-Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.2f), 50 + Main.rand.Next(100), default, Main.rand.NextFloat(1.2f, 2f)).noGravity = true;
				}
			}

			public override bool PreDraw(ref Color lightColor)
			{
				var tex = TextureAssets.Projectile[Type].Value;
				var solid = TextureColorCache.ColorSolid(tex, Color.White);

				Rectangle sourceRectangle = tex.Frame(1, 3, frameY: (int)Projectile.ai[0]);

				Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, sourceRectangle, Projectile.GetAlpha(lightColor), Projectile.rotation, sourceRectangle.Size() / 2f,
					Projectile.scale, 0, 0f);

				if (Projectile.timeLeft > 90)
				{
					float lerp = (Projectile.timeLeft - 90) / 30f;

					Main.spriteBatch.Draw(solid, Projectile.Center - Main.screenPosition, sourceRectangle, Color.DarkOliveGreen * lerp, Projectile.rotation, sourceRectangle.Size() / 2f,
						Projectile.scale, 0, 0f);
				}

				return false;
			}
		}

		public static readonly SoundStyle HitSound = new SoundStyle("SpiritReforged/Assets/SFX/Projectile/Impact_Slimy") with { Volume = 0.66f, PitchVariance = 0.15f };
		public static readonly SoundStyle DeathSound_01 = SoundID.NPCDeath1 with { Volume = 0.5f };
		public static readonly SoundStyle DeathSound_02 = new SoundStyle("SpiritReforged/Assets/SFX/NPCDeath/BugDeath") with { Volume = 0.33f, PitchVariance = 0.2f };

		public NPC TargetNPC => Main.npc[Target];
		public Projectile ParentProj => Main.projectile[(int)ProjOwner];
		private Player Owner => Main.player[Projectile.owner];

		int Target
		{
			get => (int)Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}

		int AttackTimer
		{
			get => (int)Projectile.ai[1];
			set => Projectile.ai[1] = value;
		}

		ref float Timer => ref Projectile.ai[2];

		ref float ProjOwner => ref Projectile.localAI[0];

		int DashTimer
		{
			get => (int)Projectile.localAI[1];
			set => Projectile.localAI[1] = value;
		}

		internal bool RareDraw;

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.SentryShot[Type] = true;
			Main.projFrames[Type] = 4;
		}

		public override void SetDefaults()
		{
			Projectile.friendly = true;
			Projectile.Size = new Vector2(12);
			Projectile.timeLeft = 600;
			Projectile.aiStyle = -1;
			Projectile.penetrate = 3;
			Projectile.stopsDealingDamageAfterPenetrateHits = true;

			Projectile.usesIDStaticNPCImmunity = true;
			Projectile.idStaticNPCHitCooldown = 20;

			Projectile.tileCollide = false;
			Projectile.hide = true;

			RareDraw = Main.rand.NextBool(5);

			ProjectileID.Sets.TrailCacheLength[Type] = 6;
			ProjectileID.Sets.TrailingMode[Type] = 0;
		}

		public override void AI()
		{
			if (DashTimer > 0)
				DashTimer--;

			if (Projectile.penetrate > 0)
				Timer++;

			// Animates faster when moving faster
			float xVelocity = Utils.Clamp(Math.Abs(Projectile.velocity.X), 0, 15);
			int frameTimer = (int)MathHelper.Lerp(6, 2, xVelocity / 15f);
			
			if (++Projectile.frameCounter >= frameTimer)
			{
				Projectile.frameCounter = 0;
				if (++Projectile.frame >= Main.projFrames[Projectile.type])
					Projectile.frame = 0;
			}

			if (TargetNPC is null || !TargetNPC.active)
			{
				NPC newTarget = FindTarget();

				if (newTarget is not null)
					Target = newTarget.whoAmI;
				else
				{
					Projectile.velocity *= 0.95f;
					if (Projectile.velocity.Length() < 1f)
					{
						Explode(true);
						Projectile.Kill();
					}				
				}

				return;
			}

			Projectile.spriteDirection = -Projectile.direction;

			float dist = Projectile.Distance(TargetNPC.Center);
			
			float velocityRamp = 1f;
			if (Timer < 60f)
				velocityRamp = Timer / 60f;

			if (Projectile.penetrate == -1 && AttackTimer <= 0)
			{
				ExplodingBehavior(dist);
				return;
			}		
			
			Projectile.rotation = Projectile.velocity.X * 0.05f;

			if (AttackTimer > 0)
			{
				AttackTimer--;
				FlyToTarget(dist, velocityRamp);
			}
			else if (Projectile.penetrate > 0)
			{
				Dash();
			}
		}

		internal void ExplodingBehavior(float dist)
		{
			Vector2 direction = TargetNPC.Center - Projectile.Center;
			direction.Normalize();
			if (dist > 100f)
				direction *= 15f;
			else
			{
				if (Main.rand.NextBool(2))
				{
					Color smokeColor = new Color(5, 5, 5) * 0.2f;
					float scale = Main.rand.NextFloat(0.1f, 0.2f) * Timer / 30f;
					var velSmoke = -Projectile.velocity * 0.05f;
					ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), velSmoke, Color.DarkSeaGreen * 0.25f, smokeColor, scale, EaseFunction.EaseQuadOut, Main.rand.Next(30, 40)));

					Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5, 5), DustID.Poisoned,
						Main.rand.NextVector2Circular(15f, 15f) * Timer / 30f, 50 + Main.rand.Next(100), default, Main.rand.NextFloat(1.2f, 2f)).noGravity = true;
				}

				Projectile.rotation += Main.rand.NextFloat(-0.2f, 0.2f);
				Projectile.position += TargetNPC.velocity / 2;
				Projectile.velocity *= 0.9f;
				if (++Timer > 30)
					Explode();

				return;
			}

			Projectile.velocity = Vector2.Lerp(Projectile.velocity, direction, 0.05f);
		}

		// natural death is whether it explodes on an enemy or explodes from being inactive
		internal void Explode(bool naturalDeath = false)
		{
			int loops = naturalDeath ? 3 : 15;

			for (int i = 0; i < loops; i++)
			{
				Color smokeColor = new Color(5, 5, 5) * 0.25f;
				float scale = Main.rand.NextFloat(0.07f, 0.15f);
				var velSmoke = -Vector2.UnitY * Main.rand.NextFloat(2f, 5f);
				ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Center + Main.rand.NextVector2Circular(50f, 50f), velSmoke, Color.DarkSeaGreen * 0.35f, smokeColor, scale, EaseFunction.EaseQuadOut, Main.rand.Next(30, 40)));

				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5, 5), DustID.Poisoned,
							Main.rand.NextVector2Circular(9f, 9f), 50 + Main.rand.Next(100), default, Main.rand.NextFloat(1.2f, 2f)).noGravity = true;

				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(50, 50), DustID.Poisoned,
							velSmoke, 50 + Main.rand.Next(100), default, Main.rand.NextFloat(1.2f, 2f)).noGravity = true;

				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(25, 25), DustID.Sluggy,
							Main.rand.NextVector2Circular(3f, 3f), 50 + Main.rand.Next(100), default, Main.rand.NextFloat(0.2f, 1f));
			}

			if (Main.myPlayer == Projectile.owner)
			{
				if (!naturalDeath)
					Projectile.NewProjectile(Projectile.GetSource_Death("Crook Locust Explode"), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<LocustExplosion>(),
						22, 2f, Projectile.owner);

				for (int i = 0; i < 3; i++)
				{
					float mult = Main.rand.NextFloat(5f, 8f);
					if (naturalDeath)
						mult *= 0.2f;

					Projectile.NewProjectile(Projectile.GetSource_Death("Locust Gore"), Projectile.Center, -Vector2.UnitY.RotatedByRandom(Math.PI) * mult, ModContent.ProjectileType<CrookLocustGore>(), 0, 0, Projectile.owner, i);
				}
			}

			Projectile.Kill();
		}

		internal void FlyToTarget(float dist, float velocityRamp)
		{
			if (DashTimer <= 0)
			{
				if (Main.rand.NextBool(4))
				{
					Color smokeColor = new Color(5, 5, 5) * 0.16f;
					float scale = Main.rand.NextFloat(0.1f, 0.15f);
					var velSmoke = -Projectile.velocity * 0.05f;
					ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), velSmoke, Color.DarkSeaGreen * 0.25f, smokeColor, scale, EaseFunction.EaseQuadOut, Main.rand.Next(30, 40)));

					Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(25, 25), DustID.Poisoned,
						-Projectile.velocity * Main.rand.NextFloat(), 50 + Main.rand.Next(100), default, Main.rand.NextFloat(0.9f, 1.25f)).noGravity = true;

					if (Main.rand.NextBool())
					{
						Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(25, 25), DustID.Poisoned,
							-Vector2.UnitY, 50 + Main.rand.Next(100), default, Main.rand.NextFloat(1.2f, 2.25f)).noGravity = true;
					}
				}
			}

			Vector2 direction = TargetNPC.Center - Projectile.Center;
			direction.Normalize();
			if (dist > 400f)
			{
				direction *= 15f * velocityRamp;
			}
			else
			{
				float mult = MathHelper.Lerp(15f, 5f, 1f - Projectile.Distance(TargetNPC.Center) / 500f);
				direction *= mult * velocityRamp;
			}

			Projectile.velocity = Vector2.Lerp(Projectile.velocity, direction, 0.05f);
		}

		internal void Dash()
		{
			Projectile.velocity = Projectile.DirectionTo(TargetNPC.Center) * 15f;
			AttackTimer = 30;
			DashTimer = 30;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			SoundEngine.PlaySound(HitSound, Projectile.Center);

			if (Projectile.penetrate == 1)
				Timer = 0;

			ParticleHandler.SpawnParticle(new CartoonHit(Projectile.Center, 10, 1, Main.rand.NextFloat(2)));

			for (int i = 0; i < 4; i++)
			{
				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(25, 25), DustID.Sluggy, 
					-Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(1f), 50 + Main.rand.Next(100), default, Main.rand.NextFloat(1.2f, 2.25f)).noGravity = true;

				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(25, 25), DustID.Ichor, 
					-Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(1f), 50 + Main.rand.Next(100), default, Main.rand.NextFloat(0.5f, 1f)).noGravity = true;

				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(25, 25), DustID.Poisoned, 
					-Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.5f), 50 + Main.rand.Next(100), default, Main.rand.NextFloat(1.2f, 2.25f)).noGravity = true;
			}
		}

		public override void OnKill(int timeLeft)
		{
			SoundEngine.PlaySound(DeathSound_01, Projectile.Center);
			SoundEngine.PlaySound(DeathSound_02, Projectile.Center);
		}

		public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
		{
			if (Projectile.penetrate > 0 && Timer < 30)
				behindNPCsAndTiles.Add(index);
			else
				behindProjectiles.Add(index);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float fadeIn = 1f;
			if (Timer < 30f && Projectile.penetrate > 0)
				fadeIn = Timer / 30f;

			var tex = TextureAssets.Projectile[Type].Value;
			var bloom = AssetLoader.LoadedTextures["Bloom"].Value;
			var star = AssetLoader.LoadedTextures["Star"].Value;
			var solid = TextureColorCache.ColorSolid(tex, Color.White);

			Rectangle sourceRectangle = tex.Frame(2, Main.projFrames[Projectile.type], frameX: RareDraw ? 0 : 1, frameY: Projectile.frame);

			float scale = 1f;

			Vector2 drawPos = Projectile.Center;

			if (Projectile.penetrate < 0)
			{
				float shakeStrength = MathHelper.Lerp(0.25f, 2f, EaseBuilder.EaseQuadOut.Ease(Timer / 30f));

				scale = MathHelper.Lerp(1f, 1.25f, EaseBuilder.EaseQuadOut.Ease(Timer / 30f));
				drawPos += Main.rand.NextVector2Circular(shakeStrength, shakeStrength);
			}

			for (int i = 0; i < Projectile.oldPos.Length; i++)
			{
				Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f;
				float lerp = 1f - i / (float)Projectile.oldPos.Length;

				Color drawColor = Color.DarkSeaGreen * 0.5f;
				if (DashTimer > 0)
					drawColor = Color.Lerp(Color.GreenYellow.Additive(), Color.DarkSeaGreen * 0.5f, 1f - DashTimer / 30f);

				Main.spriteBatch.Draw(tex, pos - Main.screenPosition, sourceRectangle, drawColor * lerp * 0.5f * fadeIn,
				  Projectile.rotation, sourceRectangle.Size() / 2f, Projectile.scale, Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
			}

			Main.spriteBatch.Draw(tex, drawPos - Main.screenPosition, sourceRectangle, Projectile.GetAlpha(lightColor) * fadeIn, Projectile.rotation, sourceRectangle.Size() / 2f,
				Projectile.scale * scale, Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);

			if (DashTimer > 0)
			{
				float flashTimer = DashTimer / 30f;

				Vector2 eyePos = drawPos + new Vector2(6f * -Projectile.spriteDirection, 0f).RotatedBy(Projectile.rotation);

				Main.spriteBatch.Draw(bloom, eyePos - Main.screenPosition, null, new Color(255, 150, 0, 0) * 0.5f, 0f, bloom.Size() / 2f, 0.3f * flashTimer, 0f, 0f);
				Main.spriteBatch.Draw(star, eyePos - Main.screenPosition, null, new Color(255, 150, 0, 0), Projectile.rotation + MathHelper.TwoPi * EaseBuilder.EaseQuadIn.Ease(flashTimer), star.Size() / 2f, 0.2f * flashTimer, 0f, 0f);
				Main.spriteBatch.Draw(star, eyePos - Main.screenPosition, null, new Color(255, 255, 255, 0) * 0.8f, Projectile.rotation + MathHelper.TwoPi * EaseBuilder.EaseQuadIn.Ease(flashTimer), star.Size() / 2f, 0.1f * flashTimer, 0f, 0f);
			}

			if (Projectile.penetrate < 0)
			{
				float progress = Timer / 30f;

				Main.spriteBatch.Draw(bloom, drawPos - Main.screenPosition, null, Color.DarkOliveGreen.Additive() * 0.5f, 0f, bloom.Size() / 2f, 0.5f * progress, 0f, 0f);

				Main.spriteBatch.Draw(solid, drawPos - Main.screenPosition, sourceRectangle, Color.Lerp(Color.Black, Color.DarkOliveGreen, progress) * progress, Projectile.rotation, sourceRectangle.Size() / 2f,
					Projectile.scale * scale, Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
			}

			return false;
		}

		internal NPC FindTarget()
		{
			if (Owner.HasMinionAttackTargetNPC && Main.npc[Owner.MinionAttackTargetNPC].Distance(Projectile.Center) < 1000f)
				return Main.npc[Owner.MinionAttackTargetNPC];

			return Main.npc.Where(n => n.CanBeChasedBy() && n.Distance(Owner.Center) < 1000f).OrderBy(n => n.Distance(Projectile.Center)).FirstOrDefault();
		}
	}

	// These are purely visual
	protected class BabyLocust(int Lifetime, int ParentWhoAmI, bool ParentingNPC = true)
	{
		internal string Texture = "SpiritReforged/Content/Desert/ScarabBoss/Items/CrookBabyLocust";

		public Entity Parent => parentingNPC ? Main.npc[parentWhoAmI] : Main.projectile[parentWhoAmI];

		public float AnimationSpeed => lifetime * 0.125f * (2 - scale);

		public bool drawBehind;
		public bool parentingNPC = ParentingNPC;

		public int lifetime = Lifetime;
		public int parentWhoAmI = ParentWhoAmI;

		internal int fadeInTimer;

		internal int frame;
		internal int frameCounter;
		internal float rotationOffset = Main.rand.NextFloat(MathHelper.TwoPi);
		internal float scale = Main.rand.NextFloat(0.8f, 1.2f);
		internal float direction;

		public Vector2 position;
		public float rotation;

		public void Update()
		{
			if (fadeInTimer < 20)
				fadeInTimer++;

			rotationOffset += Main.rand.NextFloat(0.05f);

			if (!parentingNPC)
				lifetime++;
			else
				lifetime--;

			float sin = (float)Math.Sin(AnimationSpeed);
			float cos = (float)Math.Cos(AnimationSpeed);

			if (position.X < Parent.Center.X)
				direction = -1;
			else
				direction = 1;

			Vector2 pos = Parent.Center;
			if (!parentingNPC)
				pos = Parent.Center + new Vector2(-20, 0).RotatedBy(Main.projectile[parentWhoAmI].rotation - MathHelper.PiOver4);

			position = pos + new Vector2(Parent.width * cos, 0f).RotatedBy(rotationOffset);
			rotation = MathHelper.Lerp(rotation, cos, 0.05f);

			if (sin is < 1f and > (-0.5f))
				drawBehind = true;
			else
				drawBehind = false;

			if (++frameCounter >= 5)
			{
				frameCounter = 0;
				if (++frame >= 4)
				{
					frame = 0;
				}
			}
		}

		public void DrawSelf(SpriteBatch sb, Vector2 screenPosition, Color drawColor)
		{
			Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
			Rectangle sourceRectangle = tex.Frame(1, 4, frameY: frame);

			float fadeIn = fadeInTimer / 20f;

			float sin = (float)Math.Sin(AnimationSpeed);

			Color color = drawColor;

			if (sin > 0.8f)
				color = Color.Lerp(drawColor, new Color(90, 90, 90), (sin - 0.8f) / 0.2f);

			float rot = rotation;

			SpriteEffects flip = SpriteEffects.None;

			if (direction == -1)
				flip = SpriteEffects.FlipHorizontally;

			sb.Draw(tex, position + Main.rand.NextVector2Circular(1f, 1f) * (float)Math.Abs(sin) - screenPosition,
				sourceRectangle, color * fadeIn, rot, sourceRectangle.Size() / 2f, scale, flip, 0f);
		}
	}

	private class LocustDamageGlobalNPC : GlobalNPC
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
				{
					Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(25, 25), DustID.Poisoned,
							Main.rand.NextVector2Circular(5f, 5f), 50 + Main.rand.Next(100), default, Main.rand.NextFloat(1.2f, 2.25f)).noGravity = true;
				}
				
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
			{
				locusts.Remove(locust);
			}

			if (locusts.Count > 0)
			{
				if (++AttackTimer % (180 / locusts.Count) == 0)
				{
					if (Main.netMode != NetmodeID.MultiplayerClient)
					{
						Projectile.NewProjectile(npc.GetSource_OnHurt(Main.player[AttackerWhoAmI], "SpiritReforged:LocustCrookBabyHit"), npc.Center + Main.rand.NextVector2Circular(npc.width / 2, npc.height / 2),
												Vector2.Zero, ModContent.ProjectileType<LocustDamageProjectile>(), 3 * locusts.Count, 0f, AttackerWhoAmI);
					}					
				}
			}
			else if (AttackTimer > 0)
				AttackTimer = 0;
		}

		public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			foreach (BabyLocust locust in locusts.Where(l => l.drawBehind))
			{
				locust.DrawSelf(spriteBatch, screenPos, drawColor);
			}

			return base.PreDraw(npc, spriteBatch, screenPos, drawColor);
		}

		public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			foreach (BabyLocust locust in locusts.Where(l => !l.drawBehind))
			{
				locust.DrawSelf(spriteBatch, screenPos, drawColor);
			}
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

	public override void SetDefaults()
	{
		Item.Size = new(32);
		Item.damage = 14;
		Item.knockBack = 1f;

		Item.useTime = 40;
		Item.useAnimation = 40;

		Item.rare = ItemRarityID.Green;
		Item.value = Item.sellPrice(gold: 2);
		Item.mana = 100;

		Item.DamageType = DamageClass.Summon;
		Item.noUseGraphic = true;
		Item.noMelee = true;
		Item.shoot = ModContent.ProjectileType<LocustCrookProjectile>();
		Item.shootSpeed = 1;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.autoReuse = true;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		Projectile.NewProjectile(source, position, velocity * 15, type, damage, knockback, player.whoAmI);
		player.UpdateMaxTurrets();

		return false;
	}
}
