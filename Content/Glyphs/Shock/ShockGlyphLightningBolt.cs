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

namespace SpiritReforged.Content.Glyphs.Shock;

public partial class ShockGlyph
{
	public class ShockGlyphLightningBolt : ModProjectile, LightningSystem.ILightningProjectile
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

		public bool Invalid { get; set; }
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

			// TODO: Balance Adjustments here
			Projectile.ArmorPenetration = Main.hardMode ? 20 : 10;
		}

		public override bool? CanHitNPC(NPC target) => target.whoAmI == TargetWhoAmI;

		public override void OnKill(int timeLeft) 
		{
			Invalid = true;
			LightningSystem.projectiles.Remove(this);
		}
		

		public override void AI()
		{
			if (Delay > 0)
			{
				Delay--;
				Projectile.timeLeft = 40;
			}

			if (!Initialized)
			{
				LightningSystem.projectiles.Add(this);

				startPos = Projectile.Center;
				Projectile.netUpdate = true;

				Delay = 10 * Main.rand.Next(7);

				if (!Main.dedServ && _trails == null)
					CreateTrail();

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
						ParticleHandler.SpawnParticle(new LightningBoltParticle(pos, vel, Color.Yellow, Color.Cyan, 0f, Main.rand.NextFloat(0.4f, 0.9f), 20 + Main.rand.Next(30, 60)));
					}

					if (Main.rand.NextBool(25))
					{
						Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(2f, 2f);
						Vector2 vel = Projectile.DirectionTo(Main.npc[TargetWhoAmI].Center).RotatedByRandom(0.3f) * Main.rand.NextFloat(4f, 5f);
						ParticleHandler.SpawnParticle(new LightningBoltParticle(pos, vel, Color.Yellow, Color.LightGoldenrodYellow, 0f, Main.rand.NextFloat(0.4f, 0.9f), 20 + Main.rand.Next(30, 60)));
					}
				}

				Projectile.Center = Vector2.Lerp(startPos, Main.npc[TargetWhoAmI].Center, Progress) + Main.rand.NextVector2CircularEdge(11f, 11f) * MathHelper.Lerp(0.4f, 1f, 1f - Progress);
			}

			if (Projectile.timeLeft == 1 && !Dying && Main.myPlayer == Projectile.owner)
			{
				Dying = true;
				Projectile.netUpdate = true;

				Projectile.timeLeft = 200;
				Projectile.Center = Main.npc[TargetWhoAmI].Center + Main.npc[TargetWhoAmI].velocity;
			}
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.HideCombatText();

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			var rect = target.getRect();

			int damage = Math.Max(damageDone, 1);

			int idx = CombatText.NewText(rect, Color.White, damage, hit.Crit);

			if (Main.netMode == NetmodeID.MultiplayerClient)
				NetMessage.SendData(MessageID.CombatTextInt, number: (int)Color.White.PackedValue, number2: rect.X, number3: rect.Y, number4: damage);

			ColoredCombatText.AddCombatText(idx, Color.Cyan, Color.DarkCyan);
			
			if (Main.dedServ)
				return;

			for (int i = 0; i < 2; i++)
			{
				ParticleHandler.SpawnParticle(new LightningBoltParticle(target.Center + Main.rand.NextVector2Circular(2f, 2f), Main.rand.NextVector2CircularEdge(4f, 4f) * Main.rand.NextFloat(0.5f, 1.1f),
					Color.Yellow, Color.Cyan, 0f, Main.rand.NextFloat(0.4f, 0.9f), 10 + Main.rand.Next(10, 30)));

				ParticleHandler.SpawnParticle(new LightningBoltParticle(target.Center + Main.rand.NextVector2Circular(2f, 2f), Main.rand.NextVector2CircularEdge(5f, 5f) * Main.rand.NextFloat(0.5f, 1.1f),
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

					trail?.Draw(TrailSystem.TrailShaders, AssetLoader.BasicShaderEffect, spriteBatch.GraphicsDevice);
				}
		}

		public override void SendExtraAI(BinaryWriter writer) => writer.Write(Dying);
		public override void ReceiveExtraAI(BinaryReader reader) => Dying = reader.ReadBoolean();
	}
}