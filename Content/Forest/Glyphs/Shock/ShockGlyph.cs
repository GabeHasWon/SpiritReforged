using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Renderers;
using System.Linq;
using SpiritReforged.Common.ProjectileCommon;

namespace SpiritReforged.Content.Forest.Glyphs.Shock;

public class ShockGlyph : GlyphItem
{
	public sealed class ShockPlayer : ModPlayer
	{
		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (hit.Crit && item.GetGlyph().ItemType == ModContent.ItemType<ShockGlyph>())
				ChannelLightning(target, damageDone);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (hit.Crit && proj.GetGlyph().ItemType == ModContent.ItemType<ShockGlyph>() && proj.type != ModContent.ProjectileType<ShockGlyphLightningBolt>())
				ChannelLightning(target, damageDone);
		}

		private void ChannelLightning(NPC target, int damage)
		{
			NPC[] closestNPCs = Main.npc.Where(n => n.whoAmI != target.whoAmI && n.CanBeChasedBy(Player) && n.DistanceSQ(target.Center) < 250000f).OrderBy(n => n.DistanceSQ(target.Center)).Take(3).ToArray();

			if (closestNPCs.Length <= 0)
				return;

			for (int i = 0; i < closestNPCs.Length; i++)
			{
				Projectile.NewProjectile(Player.GetSource_OnHit(target), target.Center, Vector2.Zero,
					ModContent.ProjectileType<ShockGlyphLightningBolt>(), (int)(damage * 0.25f), 1f, Player.whoAmI, closestNPCs[i].whoAmI);
			}

			SoundEngine.PlaySound(ElectricSting, target.Center);
			SoundEngine.PlaySound(ElectricZap, target.Center);

			ScreenshakeHelper.Shake(target.Center, Main.rand.NextVector2Circular(1f, 1f), 1, 4, 10);

			for (int i = 0; i < 3; i++)
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
	}

	public class ShockGlyphLightningBolt : ModProjectile
	{
		public bool _dying;

		private readonly ParticleRenderer _lightningParticleRenderer = new();
		private VertexTrail[] _trails;

		public Vector2 startPos;
		public int delay;
		public override string Texture => AssetLoader.EmptyTexture;
		public int TargetWhoAmI => (int)Projectile.ai[0];
		public float Progress => 1f - Projectile.timeLeft / 40f;

		public override void SetDefaults()
		{
			Projectile.Size = new Vector2(16);

			Projectile.DamageType = DamageClass.Generic;

			Projectile.hostile = false;
			Projectile.friendly = true;

			Projectile.tileCollide = false;

			Projectile.timeLeft = 40;
			Projectile.extraUpdates = 5;

			Projectile.penetrate = 1;
			Projectile.stopsDealingDamageAfterPenetrateHits = true;
		}

		public override bool? CanHitNPC(NPC target)
		{
			return target.whoAmI == TargetWhoAmI;
		}

		public override void OnSpawn(IEntitySource source)
		{
			LightningSystem.projectiles.Add(this);

			startPos = Projectile.Center;
			Projectile.netUpdate = true;

			delay = 10 * Main.rand.Next(7);
		}

		public override void OnKill(int timeLeft)
		{
			LightningSystem.projectiles.Remove(this);
		}

		public override void AI()
		{
			if (delay > 0)
			{
				delay--;
				Projectile.timeLeft = 40;
			}

			if (!Main.dedServ)
			{
				if (_trails == null)
					CreateTrail();

				foreach (VertexTrail trail in _trails)
					trail.Update();
			}

			Color color = Color.Yellow * 0.66f;

			float progress = EaseBuilder.EaseCircularInOut.Ease(Progress);
			if (_dying)
				progress = Projectile.timeLeft / 200f;

			Lighting.AddLight(Projectile.Center, color.R / 255f * progress, color.G / 255f * progress, color.B / 255f * progress);

			if (!_dying)
			{
				if (Progress > 0.25f)
				{
					if (Main.rand.NextBool(25))
						ParticleHandler.SpawnParticle(new LightningBoltParticle(Projectile.Center + Main.rand.NextVector2Circular(2f, 2f), Projectile.DirectionTo(Main.npc[TargetWhoAmI].Center).RotatedByRandom(0.3f) * Main.rand.NextFloat(5f),
								Color.Yellow, Color.Cyan, 0f, Main.rand.NextFloat(0.4f, 0.9f), 20 + Main.rand.Next(30, 60)));

					if (Main.rand.NextBool(25))
						ParticleHandler.SpawnParticle(new LightningBoltParticle(Projectile.Center + Main.rand.NextVector2Circular(2f, 2f), Projectile.DirectionTo(Main.npc[TargetWhoAmI].Center).RotatedByRandom(0.3f) * Main.rand.NextFloat(4f, 5f),
								Color.Yellow, Color.LightGoldenrodYellow, 0f, Main.rand.NextFloat(0.4f, 0.9f), 20 + Main.rand.Next(30, 60)));
				}

				Projectile.Center = Vector2.Lerp(startPos, Main.npc[TargetWhoAmI].Center, Progress) + Main.rand.NextVector2CircularEdge(11f, 11f) * MathHelper.Lerp(0.4f, 1f, 1f - Progress);
			}

			if (Projectile.timeLeft == 1 && !_dying)
			{
				_dying = true;

				Projectile.timeLeft = 200;
				Projectile.Center = Main.npc[TargetWhoAmI].Center;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
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

			static void DecelerateAction(Particle p)
			{
				p.Velocity *= 0.9f;
			}
		}

		private void CreateTrail()
		{
			ITrailCap tCap = new RoundCap();
			ITrailPosition tPos = new EntityTrailPosition(Projectile);
			ITrailShader tShader = new ImageShader(AssetLoader.LoadedTextures["GlowTrail"].Value, Vector2.One);

			_trails =
			[
				new VertexTrail(new GradientTrail(new Color(255, 240, 65), new Color(0, 255, 255), EaseFunction.EaseQuarticInOut), tCap, tPos, tShader, 30, 360, 1),
				new VertexTrail(new GradientTrail(Color.White, Color.Transparent, EaseFunction.EaseQuarticOut), tCap, tPos, tShader, 25, 360, 1),
			];
		}

		public override bool PreDraw(ref Color lightColor)
		{
			var tex = AssetLoader.LoadedTextures["Bloom"].Value;

			float progress = EaseBuilder.EaseCircularInOut.Ease(Progress);
			if (_dying)
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
			{
				foreach (VertexTrail trail in _trails)
				{
					trail.Opacity = EaseBuilder.EaseCircularInOut.Ease(Progress);
					if (_dying)
						trail.Opacity = Projectile.timeLeft / 200f;

					trail?.Draw(TrailSystem.TrailShaders, AssetLoader.BasicShaderEffect, spriteBatch.GraphicsDevice);
				}
			}
		}
	}

	public sealed class ShockGlobalItem : GlobalItem
	{
		public override bool InstancePerEntity => true;

		public int shockTimer;

		public override void Update(Item item, ref float gravity, ref float maxFallSpeed)
		{
			if (shockTimer > 0)
				shockTimer--;
		}
	}

	public static readonly SoundStyle ElectricSting = new("SpiritReforged/Assets/SFX/Projectile/ElectricSting")
	{
		Volume = 1.5f
	};

	public static readonly SoundStyle ElectricZap = new("SpiritReforged/Assets/SFX/Projectile/ElectricZap")
	{
		Volume = 0.5f
	};

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.Green;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(Color.Yellow);
	}

	public override void DrawInWorld(Item item, SpriteBatch spriteBatch, ItemMethods.ItemDrawParams parameters)
	{
		Texture2D whiteTexture = TextureColorCache.ColorSolid(parameters.Texture, Color.White);
		Effect effect = AssetLoader.LoadedShaders["GlyphShader"].Value;

		effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects * 0.0025f);
		effect.Parameters["screenPos"].SetValue(Main.screenPosition * new Vector2(0.5f, 0.1f) / new Vector2(Main.screenWidth, Main.screenHeight));
		effect.Parameters["intensity"].SetValue(MathHelper.Lerp(0.03f, 0.3f, (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.02f))));

		effect.Parameters["uImage1"].SetValue(AssetLoader.LoadedTextures["swirlNoise2"].Value);
		effect.Parameters["uImage2"].SetValue(AssetLoader.LoadedTextures["ElectricNoise"].Value);
		effect.Parameters["itemSize"].SetValue(parameters.Texture.Size() / 2);

		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.03f));

		effect.Parameters["uColor1"].SetValue(Color.Cyan.ToVector4() * 0.5f);
		effect.Parameters["uColor2"].SetValue(Color.Lerp(Color.LightYellow, Color.CornflowerBlue, cos).ToVector4() * 0.5f);
		effect.Parameters["uColor3"].SetValue(Color.Yellow.Additive().ToVector4());

		effect.Parameters["baseDepth"].SetValue(4f);
		effect.Parameters["scale"].SetValue(1f);

		var globalItem = item.GetGlobalItem<ShockGlobalItem>();

		Vector2 pos = parameters.Position;
		if (globalItem.shockTimer > 0)
			pos += Main.rand.NextVector2CircularEdge(1.5f, 1.5f) * globalItem.shockTimer / 40f;

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			spriteBatch.Draw(whiteTexture, pos + offset, parameters.Source, Color.CornflowerBlue.Additive() * 0.05f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);

			offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;
			spriteBatch.Draw(whiteTexture, pos + offset, parameters.Source, Color.Cyan.Additive() * 0.05f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
		}

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			spriteBatch.Draw(whiteTexture, pos + offset, parameters.Source, Color.White, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
		}

		spriteBatch.RestartToDefault();

		base.DrawInWorld(item, spriteBatch, parameters);
	}

	public override void UpdateInWorld(Item item, ref float gravity, ref float maxFallSpeed)
	{
		if (Main.dedServ)
			return;

		ShockGlobalItem globalItem = item.GetGlobalItem<ShockGlobalItem>();

		if (Main.rand.NextBool(120) && globalItem.shockTimer <= 0)
		{
			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/ElectricZap") with { Volume = 0.3f }, item.Center);

			globalItem.shockTimer = 40;
			for (int i = 0; i < 5; i++)
			{
				Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);
				ParticleHandler.SpawnParticle(new LightningBoltParticle(pos, Main.rand.NextVector2CircularEdge(4f, 4f) * Main.rand.NextFloat(0.5f, 1.1f), Color.Yellow, Color.Cyan, 0f, Main.rand.NextFloat(0.4f, 0.9f), 20 + Main.rand.Next(20, 50)));
			}
		}

		if (Main.rand.NextBool(50))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);
			ParticleHandler.SpawnParticle(new LightningBoltParticle(pos, Main.rand.NextVector2CircularEdge(4f, 4f) * Main.rand.NextFloat(0.5f, 1.1f), Color.Yellow, Color.Cyan, 0f, Main.rand.NextFloat(0.4f, 0.9f), 20 + Main.rand.Next(20, 50)));
		}
	}
}