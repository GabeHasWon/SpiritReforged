using Microsoft.Build.Evaluation;
using Microsoft.Xna.Framework.Graphics;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Tiles;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.UI.States;
using Terraria.ModLoader;
using XPT.Core.Audio.MP3Sharp.Decoding.Decoders.LayerIII;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items;

internal class LocustCrook : ModItem
{
	private class LocustCrookProjectile : ModProjectile 
	{
		private Player Owner => Main.player[Projectile.owner];

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

		public NPC Target => TargetWhoAmI > -1 ? Main.npc[(int)TargetWhoAmI] : null;

		public int TileHitTimer;

		Vector2 oldVelo = Vector2.Zero;
		public override string Texture => base.Texture.Replace("Projectile", "");
		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailingMode[Type] = 0;
			ProjectileID.Sets.TrailCacheLength[Type] = 5;
		}

		public override void SetDefaults()
		{
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.Size = new Vector2(14);
			Projectile.timeLeft = Projectile.SentryLifeTime;
			Projectile.sentry = true;

			ProjectileID.Sets.TrailCacheLength[Type] = 10;
			ProjectileID.Sets.TrailingMode[Type] = 0;

			Projectile.hide = true;
		}

		public override bool? CanDamage() => false;

		public override bool ShouldUpdatePosition() => !HitTile;
		public override bool PreAI()
		{
			Timer++;

			NPC target = FindTarget();
			if (target != default)
				TargetWhoAmI = target.whoAmI;

			if (MinionTarget != null)
				TargetWhoAmI = MinionTarget.whoAmI;

			if (Target is null)
			{
				AttackTimer = 0;
				TargetWhoAmI = -1;
			}
			else
			{
				if (!Target.active || Target is null)
				{
					TargetWhoAmI = -1;
					return false;
				}

				if (++AttackTimer % 120 == 0)
				{
					if (Main.myPlayer == Projectile.owner)
						Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Main.rand.NextVector2CircularEdge(5f, 5f),
							ModContent.ProjectileType<CrookLocust>(), Projectile.damage, Projectile.knockBack, Projectile.owner, TargetWhoAmI, 60);
				}
			}
				
			if (TileHitTimer > 0)
				TileHitTimer--;

			// how i feel copy and pasting khopesh code
			if (HitTile)
			{
				float progress = TileHitTimer / 20f;

				Projectile.velocity = oldVelo;

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
				HitTile = true;
				TileHitTimer = 20;
				AttackTimer = 0;

				Projectile.position += oldVelocity * 2;
				oldVelo = oldVelocity;
				Projectile.rotation = oldVelocity.ToRotation() + MathHelper.PiOver4;
				SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundMiss with { Volume = 0.5f, PitchVariance = 0.2f }, Projectile.Center);
			}

			return false;
		}

		public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => behindNPCsAndTiles.Add(index);

		public override bool PreDraw(ref Color lightColor)
		{
			var tex = TextureAssets.Projectile[Type].Value;
			var solid = TextureColorCache.ColorSolid(tex, Color.White);

			var bloom = AssetLoader.LoadedTextures["BloomNonPremult"].Value;

			float fade = 1f;
			if (Timer < 15f)
				fade = Timer / 15f;

			Vector2 drawPos = Projectile.Center + new Vector2(-20f, 0).RotatedBy(Projectile.rotation - MathHelper.PiOver4);

			if (!HitTile)
			{
				for (int j = 0; j < 4; j++)
				{
					Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
					Color drawColor = new Color(255, 150, 0, 0);

					Main.EntitySpriteDraw(solid, drawPos + offset - Main.screenPosition, null, drawColor, Projectile.rotation, solid.Size() / 2, 1f, SpriteEffects.None);
					Main.EntitySpriteDraw(solid, drawPos + offset - Main.screenPosition, null, Color.White.Additive() * 0.5f, Projectile.rotation, solid.Size() / 2, 1f, SpriteEffects.None);
				}

				for (int i = 0; i < Projectile.oldPos.Length; i++)
				{
					Vector2 pos = Projectile.oldPos[i] + new Vector2(-20f, 0).RotatedBy(Projectile.rotation - MathHelper.PiOver4) + Projectile.Size / 2f;
					float lerp = 1f - i / (float)Projectile.oldPos.Length;

					Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(255, 150, 0, 0) * fade * lerp,
					  Projectile.rotation, tex.Size() / 2f, Projectile.scale, 0, 0f);
				}
			}			
			else 
			{
				float outLineFade = 1f;

				if (TileHitTimer > 0)
				{
					outLineFade = 1f - TileHitTimer / 20f;

					float fadeOut = TileHitTimer / 20f;

					for (int j = 0; j < 4; j++)
					{
						Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
						Color drawColor = new Color(255, 150, 0, 0);

						Main.EntitySpriteDraw(solid, drawPos + offset - Main.screenPosition, null, drawColor * fadeOut, Projectile.rotation, solid.Size() / 2, 1f, SpriteEffects.None);
						Main.EntitySpriteDraw(solid, drawPos + offset - Main.screenPosition, null, Color.White.Additive() * 0.5f * fadeOut, Projectile.rotation, solid.Size() / 2, 1f, SpriteEffects.None);
					}

					for (int i = 0; i < Projectile.oldPos.Length; i++)
					{
						Vector2 pos = Projectile.oldPos[i] + new Vector2(-20f, 0).RotatedBy(Projectile.rotation - MathHelper.PiOver4) + Projectile.Size / 2f;
						float lerp = 1f - i / (float)Projectile.oldPos.Length;

						Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(255, 150, 0, 0) * fade * lerp,
						  Projectile.rotation, tex.Size() / 2f, Projectile.scale, 0, 0f);
					}
				}

				float sinFade = (float)Math.Abs(Math.Sin(Timer * 0.025f));

				for (int j = 0; j < 4; j++)
				{
					Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;

					Main.EntitySpriteDraw(solid, drawPos + offset - Main.screenPosition, null, Color.Black * sinFade * outLineFade, Projectile.rotation, solid.Size() / 2, 1f, SpriteEffects.None);
					Main.EntitySpriteDraw(solid, drawPos + offset - Main.screenPosition, null, Color.Purple.Additive() * sinFade * outLineFade * 0.15f, Projectile.rotation, solid.Size() / 2, 1f, SpriteEffects.None);
				}
			}

			Main.EntitySpriteDraw(tex, drawPos - Main.screenPosition, null, lightColor * fade, Projectile.rotation, tex.Size() / 2f, 1f, 0, 0);

			if (HitTile)
			{
				Main.spriteBatch.End();
				Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

				float fadeIn = 1f - TileHitTimer / 20f;

				Main.spriteBatch.Draw(bloom, drawPos + new Vector2(20f, 0f).RotatedBy(Projectile.rotation - MathHelper.PiOver4) - Main.screenPosition, null, Color.Black * fadeIn * 0.7f,
					  Projectile.rotation, bloom.Size() / 2f, 2.5f, 0, 0f);
				
				Main.spriteBatch.Draw(bloom, drawPos + new Vector2(20f, 0f).RotatedBy(Projectile.rotation - MathHelper.PiOver4) - Main.screenPosition, null, Color.Purple * fadeIn * 0.45f,
					  Projectile.rotation, bloom.Size() / 2f, 2f, 0, 0f);

				Main.spriteBatch.End();
				Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
			}

			return false;
		}

		internal NPC FindTarget()
		{
			return Main.npc.Where(n => n.CanBeChasedBy() && n.Distance(Owner.Center) < 1000f).OrderBy(n => n.Distance(Projectile.Center)).FirstOrDefault();
		}
	}

	private class CrookLocust : ModProjectile
	{
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
			Projectile.tileCollide = false;

			RareDraw = Main.rand.NextBool(5);

			ProjectileID.Sets.TrailCacheLength[Type] = 10;
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
				{
					Projectile.frame = 0;
				}
			}

			if (TargetNPC is null || !TargetNPC.active)
			{
				NPC newTarget = FindTarget();

				if (newTarget is not null)
					Target = newTarget.whoAmI;
				else
				{
					Projectile.velocity *= 0.925f;
					if (Projectile.velocity.Length() < 1f)
						Projectile.Kill();
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
			{
				direction *= 15f;
			}
			else
			{
				if (Main.rand.NextBool(2))
				{
					Color smokeColor = new Color(5, 5, 5) * 0.2f;
					float scale = Main.rand.NextFloat(0.1f, 0.2f) * Timer / 30f;
					var velSmoke = -Projectile.velocity * 0.05f;
					ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), velSmoke, smokeColor, scale, EaseFunction.EaseQuadOut, Main.rand.Next(30, 40)));
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

		internal void Explode()
		{
			Projectile.Kill();
		}

		internal void FlyToTarget(float dist, float velocityRamp)
		{
			if (DashTimer <= 0)
			{
				if (Main.rand.NextBool(2))
				{
					Color smokeColor = new Color(5, 5, 5) * 0.2f;
					float scale = Main.rand.NextFloat(0.1f, 0.15f);
					var velSmoke = -Projectile.velocity * 0.05f;
					ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), velSmoke, smokeColor, scale, EaseFunction.EaseQuadOut, Main.rand.Next(30, 40)));
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
			if (Projectile.penetrate == 1)
				Timer = 0;

			target.TryGetGlobalNPC<LocustDamageGlobalNPC>(out var globalNPC);

			if (globalNPC.locusts.Count < LocustDamageGlobalNPC.MAX_LOCUSTS)
			{
				globalNPC.AddLocust(target.whoAmI);
				globalNPC.AttackerWhoAmI = Projectile.owner;
			}		
		}

		public override void OnKill(int timeLeft)
		{

		}

		public override bool PreDraw(ref Color lightColor)
		{
			float fadeIn = 1f;
			if (Timer < 30f && Projectile.penetrate > 0)
				fadeIn = Timer / 30f;

			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
			Texture2D star = AssetLoader.LoadedTextures["Star"].Value;
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

				Color drawColor = Color.Black;
				if (DashTimer > 0)
					drawColor = Color.Lerp(new Color(255, 120, 0, 0), Color.Black, 1f - DashTimer / 30f);

				Main.spriteBatch.Draw(tex, pos - Main.screenPosition, sourceRectangle, drawColor * lerp * 0.5f * fadeIn,
				  Projectile.rotation, sourceRectangle.Size() / 2f, Projectile.scale, Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
			}

			Main.spriteBatch.Draw(tex, drawPos - Main.screenPosition, sourceRectangle, Projectile.GetAlpha(lightColor) * fadeIn, Projectile.rotation, sourceRectangle.Size() / 2f,
				Projectile.scale * scale, Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);

			if (DashTimer > 0)
			{
				float flashTimer = DashTimer / 30f;

				Vector2 eyePos = drawPos + new Vector2(6f * -Projectile.spriteDirection, 0f).RotatedBy(Projectile.rotation);

				Main.spriteBatch.Draw(star, eyePos - Main.screenPosition, null, new Color(255, 120, 0, 0), Projectile.rotation + MathHelper.TwoPi * EaseBuilder.EaseQuadIn.Ease(flashTimer), star.Size() / 2f, 0.2f * flashTimer, 0f, 0f);
				Main.spriteBatch.Draw(star, eyePos - Main.screenPosition, null, new Color(255, 255, 255, 0) * 0.8f, Projectile.rotation + MathHelper.TwoPi * EaseBuilder.EaseQuadIn.Ease(flashTimer), star.Size() / 2f, 0.1f * flashTimer, 0f, 0f);
			}

			if (Projectile.penetrate < 0)
			{
				Main.spriteBatch.Draw(tex, drawPos - Main.screenPosition, sourceRectangle, Color.Black * (Timer / 30f), Projectile.rotation, sourceRectangle.Size() / 2f,
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
	protected class BabyLocust
	{
		internal string Texture = "SpiritReforged/Content/Desert/ScarabBoss/Items/CrookBabyLocust";

		public NPC Parent => Main.npc[parentNPCWhoAmI];

		public float AnimationSpeed => lifetime * 0.125f * (2 - scale);

		public bool drawBehind;

		public int lifetime;
		public int index;
		public int parentNPCWhoAmI;

		internal int frame;
		internal int frameCounter;
		internal float rotationOffset;
		internal float scale;
		internal float direction;

		public Vector2 position;
		public float rotation;

		public BabyLocust(int Lifetime, int Index, int ParentWhoAmI)
		{
			lifetime = Lifetime;
			index = Index;
			parentNPCWhoAmI = ParentWhoAmI;

			rotationOffset = Main.rand.NextFloat(MathHelper.TwoPi);
			scale = Main.rand.NextFloat(0.8f, 1.2f);
		}

		public void Update()
		{
			rotationOffset += Main.rand.NextFloat(0.05f);
			lifetime--;

			float sin = (float)Math.Sin(AnimationSpeed);
			float cos = (float)Math.Cos(AnimationSpeed);

			if (position.X < Parent.Center.X)
				direction = -1;
			else
				direction = 1;

			// starts from the bottom and works its way up the entire sprite
			float yOffset = Parent.height / 2 - Parent.height / LocustDamageGlobalNPC.MAX_LOCUSTS;

			position = Parent.Center + new Vector2(Parent.width * cos, 0f).RotatedBy(rotationOffset);
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

		public void DrawSelf(SpriteBatch sb, Vector2 screenPosition)
		{
			Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
			Rectangle sourceRectangle = tex.Frame(1, 4, frameY: frame);

			float sin = (float)Math.Sin(AnimationSpeed);
			float cos = (float)Math.Cos(AnimationSpeed);

			Color color = Color.White;

			if (sin > 0.8f)
				color = Color.Lerp(Color.White, new Color(50, 50, 50), (sin - 0.8f) / 0.2f);

			float rot = rotation;

			SpriteEffects flip = SpriteEffects.None;

			if (direction == -1)
			{
				flip = SpriteEffects.FlipHorizontally;
				//rot += MathHelper.Pi;
			}

			sb.Draw(tex, position + Main.rand.NextVector2Circular(1f, 1f) * (float)Math.Abs(sin) - screenPosition,
				sourceRectangle, color, rot, sourceRectangle.Size() / 2f, scale, flip, 0f);
		}
	}

	private class LocustDamageGlobalNPC : GlobalNPC
	{
		// purely for damage purposes because Target.StrikeNPC sucks
		internal class LocustDamageProjectile : ModProjectile
		{
			public override string Texture => AssetLoader.EmptyTexture;

			public override void SetDefaults()
			{
				Projectile.friendly = true;
				Projectile.DamageType = DamageClass.Summon;

				Projectile.penetrate = 1;
				Projectile.tileCollide = false;
			}
		}

		public const int MAX_LOCUSTS = 5;

		public int AttackTimer;
		public int AttackerWhoAmI; // the most recent player to attack this NPC with a crook locust specifically. For projectile ownership

		public List<BabyLocust> locusts = new();

		public override bool InstancePerEntity => true;
		public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.CanBeChasedBy();

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
				if (++AttackTimer % (120 / locusts.Count) == 0)
				{
					if (Main.netMode != NetmodeID.MultiplayerClient)
					{
						Projectile.NewProjectile(npc.GetSource_OnHurt(Main.player[AttackerWhoAmI], "SpiritReforged:LocustCrookBabyHit"), npc.Center,
												Vector2.Zero, ModContent.ProjectileType<LocustDamageProjectile>(), 10 * locusts.Count, 0f, AttackerWhoAmI);
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
				locust.DrawSelf(spriteBatch, screenPos);
			}

			return base.PreDraw(npc, spriteBatch, screenPos, drawColor);
		}

		public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			foreach (BabyLocust locust in locusts.Where(l => !l.drawBehind))
			{
				locust.DrawSelf(spriteBatch, screenPos);
			}
		}

		public void AddLocust(int targetWhoAmI)
		{
			int index = 0;
			// find any locusts with the same index
			while (locusts.Any(l => l.index == index))
			{
				index++;
			}

			locusts.Add(new BabyLocust(600, index, targetWhoAmI));
		}
	}

	public override void SetDefaults()
	{
		Item.Size = new(32);
		Item.damage = 25;
		Item.knockBack = 1f;

		Item.useTime = 40;
		Item.useAnimation = 40;

		Item.rare = ItemRarityID.Green;
		Item.value = Item.sellPrice(gold: 2);

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
