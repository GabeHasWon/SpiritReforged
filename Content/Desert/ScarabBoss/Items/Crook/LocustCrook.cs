using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using System.IO;
using System.Linq;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items.Crook;

public class LocustCrook : ModItem
{
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

		public NPC Target => TargetWhoAmI > 0 ? Main.npc[TargetWhoAmI] : null;

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
					l.Update();

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
						Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5, 5), DustID.Poisoned,
							-Projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.5f), 100 + Main.rand.Next(100), default, Main.rand.NextFloat(1f, 1.25f)).noGravity = true;

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
					Projectile.rotation = Projectile.velocity.ToRotation() + (Main.rand.NextBool() ? -1 : 1) * (Main.rand.NextFloat(0.15f, 0.3f) * EaseFunction.EaseCircularIn.Ease(progress)) + MathHelper.PiOver4;
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
				ParticleHandler.SpawnParticle(new SharpStarParticle(Projectile.Center + Main.rand.NextVector2Circular(25f, 25f),
					-Projectile.velocity * 0.05f, Color.White.Additive() * 0.5f, new Color(255, 120, 0, 0), Main.rand.NextFloat(0.1f, 0.2f), 20, 0f));

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
					_orbitingLocusts.Add(new BabyLocust(60, Projectile.whoAmI, false));

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
				locust.DrawSelf(Main.spriteBatch, Main.screenPosition, lightColor);

			var tex = TextureAssets.Projectile[Type].Value;
			var solid = TextureColorCache.ColorSolid(tex, Color.White);
			var bloom = AssetLoader.LoadedTextures["BloomNonPremult"].Value;

			float fade = 1f;
			if (Timer < 30f)
				fade = Timer / 30f;

			Vector2 drawPos = Projectile.Center + new Vector2(-20f, 0).RotatedBy(Projectile.rotation - MathHelper.PiOver4);

			if (!HitTile)
				for (int i = 0; i < Projectile.oldPos.Length; i++)
				{
					Vector2 pos = Projectile.oldPos[i] + new Vector2(-20f, 0).RotatedBy(Projectile.rotation - MathHelper.PiOver4) + Projectile.Size / 2f;
					float lerp = 1f - i / (float)Projectile.oldPos.Length;

					Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, new Color(255, 150, 0, 0) * 0.25f * fade * lerp,
					  Projectile.rotation, tex.Size() / 2f, Projectile.scale, 0, 0f);
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
				locust.DrawSelf(Main.spriteBatch, Main.screenPosition, lightColor);

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