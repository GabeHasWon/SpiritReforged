using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using System.IO;
using SpiritReforged.Common.CombatTextCommon;
using SpiritReforged.Content.Dusts;
using SpiritReforged.Common.Multiplayer;

namespace SpiritReforged.Content.Glyphs.Shock;

public partial class ShockGlyph
{
	private class ShockPacket : PacketData
	{
		private readonly bool _crit;
		private readonly short _npc;
		private readonly int _damage;

		public ShockPacket() : base() { }

		public ShockPacket(short npc, int damage, bool crit)
		{
			_npc = npc;
			_damage = damage;
			_crit = crit;
		}

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			short npc = reader.ReadInt16();
			bool crit = reader.ReadBoolean();
			int damage = reader.ReadInt32();

			if (Main.netMode == NetmodeID.Server)
				new ShockPacket(npc, damage, crit).Send(-1, whoAmI);
			else if (Main.netMode == NetmodeID.MultiplayerClient)
				ShockGlyphLightningBolt.LightningHit(Main.npc[npc], damage, crit);
		}

		public override void OnSend(ModPacket modPacket)
		{
			modPacket.Write(_npc);
			modPacket.Write(_crit);
			modPacket.Write(_damage);
		}
	}

	public class ShockGlyphLightningBolt : ModProjectile, ShockGlyphLightningSystem.IDrawLightning
	{
		public override string Texture => AssetLoader.EmptyTexture;

		public int TargetWhoAmI => (int)Projectile.ai[0];

		public int Delay
		{
			get => (int)Projectile.ai[1];
			set => Projectile.ai[1] = value;
		}

		public bool Initialized = false;

		public float Progress => 1f - Projectile.timeLeft / 40f;

		public bool Dying;
		public Vector2 startPos;

		private VertexTrail[] _trails;

		public override void SetDefaults()
		{
			Projectile.Size = new Vector2(64);
			Projectile.DamageType = DamageClass.Generic;
			Projectile.hostile = false;
			Projectile.friendly = true;
			Projectile.tileCollide = false;
			Projectile.timeLeft = 40;
			Projectile.extraUpdates = 5;
			Projectile.penetrate = 1;
			Projectile.stopsDealingDamageAfterPenetrateHits = true;
			Projectile.ArmorPenetration = Main.hardMode ? 20 : 10;
		}

		public override bool? CanHitNPC(NPC target) => target.whoAmI == TargetWhoAmI;

		public override void OnKill(int timeLeft) => ShockGlyphLightningSystem.DrawQueue.Remove(this);

		public override void AI()
		{
			if (Delay > 0)
			{
				Delay--;
				Projectile.timeLeft = 40;
			}

			if (!Initialized)
			{
				if (Projectile.ai[2] == 1 && !Main.dedServ)
				{
					SoundEngine.PlaySound(ElectricSting, Projectile.Center);
					SoundEngine.PlaySound(ElectricZap, Projectile.Center);

					for (int i = 0; i < 3; i++)
					{
						ParticleHandler.SpawnParticle(new ShockBoltParticle(Projectile.Center + Main.rand.NextVector2Circular(2f, 2f), Main.rand.NextVector2CircularEdge(4f, 4f) * Main.rand.NextFloat(0.5f, 1.1f),
							Color.Yellow, Color.Cyan, 0f, Main.rand.NextFloat(0.4f, 0.9f), 10 + Main.rand.Next(10, 30)));

						ParticleHandler.SpawnParticle(new ShockBoltParticle(Projectile.Center + Main.rand.NextVector2Circular(2f, 2f), Main.rand.NextVector2CircularEdge(5f, 5f) * Main.rand.NextFloat(0.5f, 1.1f),
							Color.Yellow, Color.LightGoldenrodYellow, 0f, Main.rand.NextFloat(0.4f, 0.9f), 10 + Main.rand.Next(10, 60)));

						Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(5f, 5f);
						Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);

						ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.Yellow.Additive(), 0.6f, 40, extraUpdateAction: DecelerateAction));
						ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.White.Additive(), 0.45f, 40, extraUpdateAction: DecelerateAction));

						pos = Projectile.Center + Main.rand.NextVector2Circular(5f, 5f);
						velocity = Main.rand.NextVector2Circular(4f, 4f);

						ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.Cyan.Additive(), 0.6f, 40, extraUpdateAction: DecelerateAction));
						ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.White.Additive(), 0.45f, 40, extraUpdateAction: DecelerateAction));
					}

					for (int i = 0; i < 5; i++)
					{
						Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<YellowElectricDust>(), Main.rand.NextVector2CircularEdge(7f, 7f) * Main.rand.NextFloat(0.9f, 1.1f), 0, default, 0.65f).noGravity = true;
						Dust.NewDustPerfect(Projectile.Center, DustID.Electric, Main.rand.NextVector2CircularEdge(5f, 5f) * Main.rand.NextFloat(0.9f, 1.1f), 0, default, 0.65f).noGravity = true;
					}

					static void DecelerateAction(Particle p) => p.Velocity *= 0.9f;
				}

				ShockGlyphLightningSystem.DrawQueue.Add(this);
				if (!Main.dedServ && _trails == null)
					CreateTrail();

				startPos = Projectile.Center;

				if (Main.myPlayer == Projectile.owner)
				{
					ScreenshakeHelper.Shake(Projectile.Center, Main.rand.NextVector2Circular(1f, 1f), 1, 4, 10);

					Projectile.netUpdate = true;
					Delay = 10 * Main.rand.Next(7);
				}

				Initialized = true;
			}

			if (!Main.dedServ && _trails is not null)
			{
				foreach (VertexTrail trail in _trails)
					trail.Update();
			}

			Color color = Color.Yellow * 0.66f;
			float progress = EaseFunction.EaseCircularInOut.Ease(Progress);

			if (Dying)
				progress = Projectile.timeLeft / 200f;

			Lighting.AddLight(Projectile.Center, color.R / 255f * progress, color.G / 255f * progress, color.B / 255f * progress);

			if (!Dying && !Main.dedServ)
			{
				if (Progress > 0.25f)
				{
					if (Main.rand.NextBool(25))
					{
						Vector2 vel = Projectile.DirectionTo(Main.npc[TargetWhoAmI].Center).RotatedByRandom(0.3f) * Main.rand.NextFloat(5f);
						Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(2f, 2f);
						ParticleHandler.SpawnParticle(new ShockBoltParticle(pos, vel, Color.Yellow, Color.Cyan, 0f, Main.rand.NextFloat(0.4f, 0.9f), 20 + Main.rand.Next(30, 60)));
					}

					if (Main.rand.NextBool(25))
					{
						Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(2f, 2f);
						Vector2 vel = Projectile.DirectionTo(Main.npc[TargetWhoAmI].Center).RotatedByRandom(0.3f) * Main.rand.NextFloat(4f, 5f);
						ParticleHandler.SpawnParticle(new ShockBoltParticle(pos, vel, Color.Yellow, Color.LightGoldenrodYellow, 0f, Main.rand.NextFloat(0.4f, 0.9f), 20 + Main.rand.Next(30, 60)));
					}
				}

				Projectile.Center = Vector2.Lerp(startPos, Main.npc[TargetWhoAmI].Center, Progress) + Main.rand.NextVector2CircularEdge(11f, 11f) * MathHelper.Lerp(0.4f, 1f, 1f - Progress);
			}

			if (Projectile.timeLeft == 1 && !Dying)
			{
				Dying = true;
				Projectile.timeLeft = 200;
				Projectile.Center = Main.npc[TargetWhoAmI].Center + Main.npc[TargetWhoAmI].velocity;
			}
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.HideCombatText();

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				new ShockPacket((short)target.whoAmI, damageDone, hit.Crit).Send();

			LightningHit(target, damageDone, hit.Crit);
		}

		public static void LightningHit(NPC target, int damageDone, bool crit)
		{
			var rect = target.getRect();

			int damage = Math.Max(damageDone, 1);

			int idx = CombatText.NewText(rect, Color.White, damage, crit);

			ColoredCombatText.AddCombatText(idx, Color.Cyan, Color.DarkCyan);

			if (Main.dedServ)
				return;

			for (int i = 0; i < 2; i++)
			{
				ParticleHandler.SpawnParticle(new ShockBoltParticle(target.Center + Main.rand.NextVector2Circular(2f, 2f), Main.rand.NextVector2CircularEdge(4f, 4f) * Main.rand.NextFloat(0.5f, 1.1f),
					Color.Yellow, Color.Cyan, 0f, Main.rand.NextFloat(0.4f, 0.9f), 10 + Main.rand.Next(10, 30)));

				ParticleHandler.SpawnParticle(new ShockBoltParticle(target.Center + Main.rand.NextVector2Circular(2f, 2f), Main.rand.NextVector2CircularEdge(5f, 5f) * Main.rand.NextFloat(0.5f, 1.1f),
					Color.Yellow, Color.LightGoldenrodYellow, 0f, Main.rand.NextFloat(0.4f, 0.9f), 10 + Main.rand.Next(10, 60)));

				Vector2 pos = target.Center + Main.rand.NextVector2Circular(5f, 5f);
				Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);

				ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.Yellow.Additive(), 0.6f, 40, extraUpdateAction: DecelerateAction));
				ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.White.Additive(), 0.45f, 40, extraUpdateAction: DecelerateAction));

				pos = target.Center + Main.rand.NextVector2Circular(5f, 5f);
				velocity = Main.rand.NextVector2Circular(4f, 4f);

				ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.Cyan.Additive(), 0.6f, 40, extraUpdateAction: DecelerateAction));
				ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.White.Additive(), 0.45f, 40, extraUpdateAction: DecelerateAction));
			}

			static void DecelerateAction(Particle p) => p.Velocity *= 0.9f;
		}

		private void CreateTrail()
		{
			ITrailCap tCap = new RoundCap();
			ITrailPosition tPos = new EntityTrailPosition(Projectile);
			ITrailShader tShader = new ImageShader(AssetLoader.LoadedTextures["GlowTrail"].Value, Vector2.One);

			_trails =
			[
				new VertexTrail(new GradientTrail(new Color(255, 240, 65, 0), new Color(0, 255, 255, 0), EaseFunction.EaseQuarticInOut), tCap, tPos, tShader, 30, 360, 1),
				new VertexTrail(new GradientTrail(Color.White.Additive(), Color.Transparent, EaseFunction.EaseQuarticOut), tCap, tPos, tShader, 25, 360, 1),
			];
		}

		public override bool PreDraw(ref Color lightColor)
		{
			var tex = AssetLoader.LoadedTextures["Bloom"].Value;

			float progress = EaseFunction.EaseCircularInOut.Ease(Progress);

			if (Dying)
				progress = Projectile.timeLeft / 200f;

			Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.Yellow with { A = 0 } * 0.1f * progress, 0, tex.Size() / 2, 0.3f, SpriteEffects.None, 0);
			Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.Cyan with { A = 0 } * 0.09f * progress, 0, tex.Size() / 2, 0.25f, SpriteEffects.None, 0);

			Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.Yellow with { A = 0 } * 0.5f * progress, 0, tex.Size() / 2, 0.15f, SpriteEffects.None, 0);
			Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.Cyan with { A = 0 } * 0.4f * progress, 0, tex.Size() / 2, 0.1f, SpriteEffects.None, 0);

			Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.LightCyan with { A = 0 } * 0.4f * progress, 0, tex.Size() / 2, 0.1f, SpriteEffects.None, 0);

			return false;
		}

		public void LightningDraw(SpriteBatch spriteBatch)
		{
			if (_trails != null)
				foreach (VertexTrail trail in _trails)
				{
					trail.Opacity = EaseFunction.EaseCircularInOut.Ease(Progress);
					if (Dying)
						trail.Opacity = Projectile.timeLeft / 200f;

					trail?.Draw(TrailSystem.TrailShaders, spriteBatch.GraphicsDevice);
				}
		}

		public override void SendExtraAI(BinaryWriter writer) => writer.Write(Dying);
		public override void ReceiveExtraAI(BinaryReader reader) => Dying = reader.ReadBoolean();
	}
}