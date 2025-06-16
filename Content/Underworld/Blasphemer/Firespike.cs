using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Dusts;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.GameContent.Drawing;
using static Microsoft.Xna.Framework.MathHelper;
using static SpiritReforged.Common.Easing.EaseFunction;

namespace SpiritReforged.Content.Underworld.Blasphemer;

class Firespike : ModProjectile, IDrawOverTiles
{
	public const int GeyserTime = 16;
	public const int LingerTime = 300;

	public static readonly Asset<Texture2D> Cracks = ModContent.Request<Texture2D>(DrawHelpers.RequestLocal(typeof(Firespike), "TileCracks"));

	public bool Lingering => Projectile.timeLeft <= LingerTime;
	public bool CanSplit => Projectile.ai[0] > 0;
	public override string Texture => AssetLoader.EmptyTexture;

	public override void SetDefaults()
	{
		Projectile.Size = new Vector2(16);
		Projectile.timeLeft = GeyserTime + LingerTime;
		Projectile.friendly = true;
		Projectile.tileCollide = false;
		Projectile.hide = true;
		Projectile.penetrate = -1;
	}

	public override void AI()
	{
		Surface();

		if (Lingering)
		{
			if (Main.rand.NextBool(10))
			{
				var position = Projectile.Center + new Vector2(Main.rand.NextFloat(-20, 20), 0);
				for(int i = 0; i < 3; i++)
					ParticleHandler.SpawnParticle(new FireParticle(position,
													   -Vector2.UnitY / 3,
													   [new Color(255, 200, 0, 150), new Color(255, 115, 0, 150), new Color(200, 3, 33, 150)],
													   0.5f,
													   0,
													   0.1f,
													   EaseQuadIn,
													   50) { ColorLerpExponent = 3 });
			}

			if (Main.rand.NextBool(30))
			{
				var position = Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(10f);
				var velocity = (Vector2.UnitY * -Main.rand.NextFloat(2f)).RotatedByRandom(0.25f);

				ParticleHandler.SpawnParticle(new EmberParticle(position, velocity, Color.OrangeRed, Main.rand.NextFloat(0.3f), 100, 5));
			}
		}
		else
		{
			if (Main.rand.NextBool(4))
			{
				var position = Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(10f);
				var velocity = (Vector2.UnitY * -Main.rand.NextFloat(1f, 5f)).RotatedByRandom(0.25f);

				ParticleHandler.SpawnParticle(new EmberParticle(position, velocity, Color.OrangeRed, Main.rand.NextFloat(0.5f), 100, 5));

				for (int i = 0; i < 2; i++)
					Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, ModContent.DustType<FireClubDust>(), 0, -Main.rand.NextFloat(5f), Scale: Main.rand.NextFloat(0.5f));
			}
		}

		if (Projectile.timeLeft == GeyserTime + LingerTime && !Main.dedServ) //Just spawned
		{
			float step = 1f - Projectile.ai[0] / 4f;

			SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Pitch = step - 0.4f, Volume = step, MaxInstances = 3 }, Projectile.Center);
			SoundEngine.PlaySound(SoundID.Item34 with { MaxInstances = 3 }, Projectile.Center);

			ParticleHandler.SpawnParticle(new TexturedPulseCircle(Projectile.Center, Color.OrangeRed.Additive(), 0.9f, 170, 15, "supPerlin", Vector2.One).WithSkew(0.8f, PiOver2));
			ParticleHandler.SpawnParticle(new LightBurst(Projectile.Center - Vector2.UnitY * 10, 0, Color.Goldenrod.Additive(), 1f, 14));

			//black smoke particles
			for (int i = 0; i < 16; i++)
			{
				Vector2 smokePos = Projectile.Bottom + Vector2.UnitX * Main.rand.NextFloat(-10, 10);

				float easedProgress = EaseQuadOut.Ease(i / 16f);
				float scale = Lerp(0.2f, 0.07f, easedProgress);

				float speed = Lerp(0.5f, 2f, easedProgress);
				int lifeTime = (int)(Lerp(20, 40, easedProgress) + Main.rand.Next(-5, 6));

				var smokeCloud = new SmokeCloud(smokePos, -Vector2.UnitY * speed, Color.Gray, scale, EaseQuadIn, lifeTime)
				{
					SecondaryColor = Color.DarkSlateGray,
					TertiaryColor = Color.Black,
					ColorLerpExponent = 2,
					Intensity = 0.33f,
					Layer = ParticleLayer.BelowProjectile
				};
				ParticleHandler.SpawnParticle(smokeCloud);
			}

			//Sine movement ember particles
			for (int i = 0; i < 5; i++)
			{
				float maxOffset = 40;
				float offset = Main.rand.NextFloat(-maxOffset, maxOffset);
				Vector2 dustPos = Projectile.Bottom + Vector2.UnitX * offset;
				float velocity = Lerp(4, 1, EaseCircularIn.Ease(Math.Abs(offset) / maxOffset)) * Main.rand.NextFloat(0.25f, 1);

				static void ParticleDelegate(Particle p, Vector2 initialVel, float timeOffset, float rotationAmount, float numCycles)
				{
					float sineProgress = EaseQuadOut.Ease(p.Progress);

					p.Velocity = initialVel.RotatedBy(rotationAmount * (float)Math.Sin(TwoPi * (timeOffset + sineProgress) * numCycles)) * (1 - p.Progress);
				}

				float timeOffset = Main.rand.NextFloat();
				float rotationAmount = Main.rand.NextFloat(PiOver4);
				float numCycles = Main.rand.NextFloat(0.5f, 2);

				ParticleHandler.SpawnParticle(new GlowParticle(dustPos, velocity * -Vector2.UnitY, Color.Yellow, Color.Red, Main.rand.NextFloat(0.3f, 0.6f), Main.rand.Next(30, 80), 3,
					p => ParticleDelegate(p, velocity * -Vector2.UnitY, timeOffset, rotationAmount, numCycles)));
			}

			for (int i = 0; i < 8; i++)
			{
				ParticleHandler.SpawnParticle(new FireParticle(Projectile.Center,
												   Main.rand.NextVector2Unit() * Main.rand.NextFloat(5),
												   [new Color(255, 200, 0, 150), new Color(255, 115, 0, 150), new Color(200, 3, 33, 150)],
												   1.25f,
												   Main.rand.NextFloatDirection(),
												   Main.rand.NextFloat(0.06f, 0.15f),
												   EaseQuadOut,
												   50) { ColorLerpExponent = 2 });
			}
		}

		if (Main.myPlayer == Projectile.owner && Projectile.timeLeft == LingerTime + GeyserTime / 2 && CanSplit)
		{
			var position = Projectile.Center + new Vector2(Projectile.velocity.X * 40, 0);
			Projectile.NewProjectile(Projectile.GetSource_FromAI(), position, Projectile.velocity, Type, Projectile.damage, Projectile.knockBack, Projectile.owner, --Projectile.ai[0]);
		}
	}

	private void Surface()
	{
		int surfaceDuration = 0;
		while (WorldGen.SolidTile(GetOrigin()))
		{
			Projectile.position.Y--; //Move out of solid tiles

			if (TryKill())
				return;
		}

		
		while (!WorldGen.SolidTile(GetOrigin()) && !Main.tileSolidTop[Framing.GetTileSafely(GetOrigin()).TileType])
		{
			Projectile.position.Y++;

			if (TryKill())
				return;
		}

		bool TryKill()
		{
			if (++surfaceDuration > 40)
			{
				Projectile.Kill();
				return true;
			}

			return false;
		}

		Point GetOrigin() => ((Projectile.Center - Vector2.UnitY * 2) / 16).ToPoint();
	}

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		int heightOffset = Lingering ? 10 : 100;
		if (new Rectangle(projHitbox.X, projHitbox.Y - heightOffset, projHitbox.Width, projHitbox.Height + heightOffset).Intersects(targetHitbox))
			return true;

		return null;
	}

	public override bool ShouldUpdatePosition() => false;
	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.OnFire, 120);
	
	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		modifiers.FinalDamage *= Math.Max(0.2f, 1f - Projectile.numHits / 8f); //Reduce damage with hits

		if (Lingering)
			modifiers.DisableKnockback();
		else
			target.velocity.Y -= 10f * target.knockBackResist;
	}

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
		=> behindNPCsAndTiles.Add(index);

	public override bool PreDraw(ref Color lightColor)
	{
		float timeLeftProgress = (Projectile.timeLeft - LingerTime) / (float)GeyserTime;

		float scaleX = 1f - EaseCircularIn.Ease(1 - timeLeftProgress);
		float scaleY = 1f - timeLeftProgress / 2;
		float intensity = EaseQuadOut.Ease(EaseCircularOut.Ease(timeLeftProgress));
		var size = new Vector2(360 * scaleY, 80 * scaleX) * Projectile.scale;

		const int patchLingerTime = 50;
		float fade = (Projectile.timeLeft - (LingerTime + GeyserTime - patchLingerTime)) / (float)patchLingerTime;

		DrawFire(Projectile.Center, new Vector2(80 * Math.Min(fade + 0.5f, 1f), 60) * Projectile.scale, fade); //Lingering fire patch
		DrawFire(Projectile.Center, size, intensity);

		var glow = AssetLoader.LoadedTextures["Extra_49"].Value;
		var scale = new Vector2(1, 0.2f) * Projectile.scale * 0.5f;

		Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null, Color.OrangeRed.Additive() * fade, 0, glow.Size() / 2, scale, default);
		Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null, Color.White.Additive() * fade, 0, glow.Size() / 2, scale * 0.6f, default);
		return false;
	}

	private static void DrawFire(Vector2 center, Vector2 size, float intensity)
	{
		Effect effect = AssetLoader.LoadedShaders["FireStream"];
		effect.Parameters["lightColor"].SetValue(new Color(255, 200, 0, 150).ToVector4());
		effect.Parameters["midColor"].SetValue(new Color(255, 115, 0, 150).ToVector4());
		effect.Parameters["darkColor"].SetValue(new Color(200, 3, 33, 150).ToVector4());

		effect.Parameters["uTexture"].SetValue(AssetLoader.LoadedTextures["swirlNoise2"].Value);
		effect.Parameters["distortTexture"].SetValue(AssetLoader.LoadedTextures["swirlNoise"].Value);

		effect.Parameters["textureStretch"].SetValue(new Vector2(2f, 0.5f));
		effect.Parameters["distortStretch"].SetValue(new Vector2(3, 1));

		float globalTimer = Main.GlobalTimeWrappedHourly;
		float scrollSpeed = 1f;
		effect.Parameters["scroll"].SetValue(new Vector2(scrollSpeed * globalTimer));
		effect.Parameters["distortScroll"].SetValue(new Vector2(scrollSpeed * globalTimer) / 2);

		effect.Parameters["intensity"].SetValue(2.5f * intensity);
		effect.Parameters["fadePower"].SetValue(1);
		effect.Parameters["tapering"].SetValue(0.33f);

		var position = center - Main.screenPosition - Vector2.UnitY * size.X / 2;
		effect.Parameters["pixelDimensions"].SetValue(size / 2);
		effect.Parameters["numColors"].SetValue(10);

		var square = new SquarePrimitive
		{
			Color = Color.White,
			Length = size.X,
			Height = size.Y,
			Position = position,
			Rotation = -PiOver2
		};

		PrimitiveRenderer.DrawPrimitiveShape(square, effect);
	}

	public void DrawOverTiles(SpriteBatch spriteBatch)
	{
		Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
		Texture2D texture = Cracks.Value;

		float scale = 1f;
		float opacity = Projectile.timeLeft / (float)(GeyserTime + LingerTime);

		var position = Projectile.Center.ToTileCoordinates().ToWorldCoordinates();
		var source = texture.Frame(3, 1, (int)position.X % 3, 0, -2);

		spriteBatch.Draw(bloom, position - Main.screenPosition, null, Color.Red.Additive() * 0.5f * opacity, 0, bloom.Size() / 2, scale * 0.3f, SpriteEffects.None, 0);
		spriteBatch.Draw(texture, position - Main.screenPosition, source, Color.White.Additive() * opacity * 2, 0, source.Size() / 2, scale, SpriteEffects.None, 0);
	}
}