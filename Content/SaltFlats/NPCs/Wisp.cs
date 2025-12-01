using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.SaltFlats.Biome;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using System.IO;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.SaltFlats.NPCs;

public class Wisp : ModNPC
{
	public class TwirlyParticle(NPC parent, Color tint) : ABasicParticle
	{
		public float Opacity;
		public int TimeToLive = 60;

		private readonly Color _colorTint = tint;
		private readonly NPC _parent = parent;
		private int _timeSinceSpawn;

		public override void Update(ref ParticleRendererSettings settings)
		{
			const int fadeTime = 30;

			base.Update(ref settings);

			if (++_timeSinceSpawn >= TimeToLive)
			{
				ShouldBeRemovedFromRenderer = true;
			}
			else
			{
				float lifeProgress = (float)_timeSinceSpawn / TimeToLive;
				Vector2 targetCenter = _parent.Center;
				Vector2 position = settings.AnchorPosition + LocalPosition;
				float distanceSQ = position.DistanceSQ(targetCenter);

				if (distanceSQ > 20 * 20 * (1 + Scale.Length()) * lifeProgress)
					Velocity = Vector2.Lerp(Velocity, position.DirectionTo(targetCenter) * (float)Math.Sqrt(distanceSQ) / 2, 0.05f);

				if (_timeSinceSpawn > TimeToLive - fadeTime)
					Opacity -= 1f / fadeTime;
				else
					Opacity = Math.Min(Opacity + 1f / fadeTime, 1);
			}
		}

		public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
		{
			Texture2D texture = TextureAssets.Extra[ExtrasID.SharpTears].Value;
			float underlayProgress = 0.5f + Math.Clamp(Velocity.X / 3, -0.5f, 0.5f);

			float opacity = Opacity * MathHelper.Lerp(0.5f, 1, underlayProgress);
			Color brightColor = (Color.White * opacity * 0.9f).Additive(125);
			Color tintColor = (Color.Lerp(Color.MediumVioletRed, _colorTint, underlayProgress) * opacity * 0.5f).Additive(100);

			Vector2 scale = Vector2.One * Scale * MathHelper.Lerp(0.5f, 1, underlayProgress);
			Vector2 position = settings.AnchorPosition + LocalPosition - (_parent.IsABestiaryIconDummy ? Vector2.Zero : Main.screenPosition);
			SpriteEffects effects = SpriteEffects.None;

			spritebatch.Draw(texture, position, null, tintColor, (float)Math.PI / 2f + Rotation, texture.Size() / 2, scale, effects, 0f);
			spritebatch.Draw(texture, position, null, tintColor, Rotation, texture.Size() / 2, scale, effects, 0f);
			spritebatch.Draw(texture, position, null, brightColor, (float)Math.PI / 2f + Rotation, texture.Size() / 2, scale * 0.6f, effects, 0f);
			spritebatch.Draw(texture, position, null, brightColor, Rotation, texture.Size() / 2, scale * 0.6f, effects, 0f);
		}
	}

	public class EnergyLaser : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.LastPrismLaser;

		public ref float BeamLength => ref Projectile.ai[0];

		public int OwnerNPC
		{
			get => (int)Projectile.ai[1];
			set => Projectile.ai[1] = value;
		}

		public bool Damaging => Projectile.timeLeft < 10;

		public override void SetDefaults()
		{
			Projectile.Size = new(18);
			Projectile.hostile = true;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.timeLeft = 40;
		}

		public override void AI()
		{
			const int maxLength = 2000;
			BeamLength = PerformHitscan(3, maxLength);

			if (Main.npc[OwnerNPC] is NPC owner && owner.active)
			{
				Projectile.Center = owner.Center;
				owner.velocity *= 0.8f; //Slow the owner
			}

			if (Damaging)
			{
				CreateDust(Color.MediumVioletRed.Additive(150));

				if (!Main.dedServ && Projectile.timeLeft == 2)
					SoundEngine.PlaySound(SoundID.DD2_SkyDragonsFuryShot with { Pitch = 0.5f }, Projectile.Center);
			}
		}

		private void CreateDust(Color beamColor)
		{
			Vector2 endPosition = Projectile.Center + Projectile.velocity * (BeamLength * Projectile.scale);

			float angle = Projectile.rotation + (Main.rand.NextBool() ? 1f : -1f) * MathHelper.PiOver2;
			float startDistance = Main.rand.NextFloat(1f, 1.8f);
			float scale = Main.rand.NextFloat(1.5f, 2f);
			Vector2 velocity = angle.ToRotationVector2() * startDistance;
			var dust = Dust.NewDustDirect(endPosition, 0, 0, DustID.RedTorch, velocity.X, velocity.Y, 0, beamColor, scale);
			dust.color = beamColor;
			dust.noGravity = true;
		}

		private float PerformHitscan(int samples, float maxLength)
		{
			Vector2 samplingPoint = Projectile.Center;
			Player owner = Main.player[Projectile.owner];

			if (!Collision.CanHitLine(owner.Center, 0, 0, Projectile.Center, 0, 0))
				samplingPoint = owner.Center;

			float[] laserScanResults = new float[samples];
			Collision.LaserScan(samplingPoint, Projectile.velocity, 0 * Projectile.scale, maxLength, laserScanResults);

			float averageLengthSample = 0f;
			for (int i = 0; i < laserScanResults.Length; ++i)
				averageLengthSample += laserScanResults[i];

			return averageLengthSample / samples;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (!Damaging)
				return false;

			if (projHitbox.Intersects(targetHitbox))
				return true;

			float _ = 0;
			Vector2 beamEndPos = Projectile.Center + Projectile.velocity * BeamLength;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, beamEndPos, 10 * Projectile.scale, ref _);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Vector2 startPosition = Projectile.Center.Floor() + Projectile.velocity - Main.screenPosition;
			Vector2 endPosition = startPosition + Projectile.velocity * BeamLength;
			float scale = Damaging ? Projectile.scale * EaseFunction.EaseCircularOut.Ease(Projectile.timeLeft / 30f) * 1.5f : 0.25f;

			Texture2D star = TextureAssets.Projectile[ProjectileID.RainbowRodBullet].Value;
			Main.EntitySpriteDraw(star, startPosition, null, Projectile.GetAlpha(Color.PaleVioletRed) * 0.25f, 0, star.Size() / 2, scale * 2, default);
			Main.EntitySpriteDraw(star, startPosition, null, Projectile.GetAlpha(Color.MediumVioletRed).Additive(150), 0, star.Size() / 2, scale, default);
			Main.EntitySpriteDraw(star, startPosition, null, Projectile.GetAlpha(Color.White).Additive(), 0, star.Size() / 2, scale * 0.8f, default);

			DelegateMethods.f_1 = 1f;
			DrawBeam(Main.spriteBatch, texture, startPosition, endPosition, Vector2.One * scale, (Damaging ? Color.OrangeRed : Color.PaleVioletRed * 0.3f).Additive(150));
			DrawBeam(Main.spriteBatch, texture, startPosition, endPosition, Vector2.One * scale * 0.5f, (Damaging ? Color.White : Color.PaleVioletRed * 0.3f).Additive(150));

			return false;
		}

		private static void DrawBeam(SpriteBatch spriteBatch, Texture2D texture, Vector2 startPosition, Vector2 endPosition, Vector2 drawScale, Color beamColor)
		{
			Utils.LaserLineFraming lineFraming = new(DelegateMethods.RainbowLaserDraw);

			DelegateMethods.c_1 = beamColor; //Render color
			Utils.DrawLaser(spriteBatch, texture, startPosition, endPosition, drawScale, lineFraming);
		}
	}

	private readonly ParticleRenderer _twirlParticleRenderer = new();
	private VertexTrail[] _trails;
	private int _counter;
	private bool _isHostile;

	public override void SetStaticDefaults() => NPCID.Sets.CountsAsCritter[Type] = true;

	public override void SetDefaults()
	{
		NPC.Size = new(20);
		NPC.aiStyle = NPCAIStyleID.Butterfly;
		NPC.noGravity = true;
		NPC.lifeMax = 30;
		NPC.catchItem = 0;
		NPC.value = 100;
		NPC.HitSound = SoundID.NPCHit1 with { Pitch = 0.25f };
		NPC.DeathSound = SoundID.NPCDeath1 with { Pitch = 0.5f };
		NPC.scale = Main.rand.NextFloat(0.75f, 1.25f);

		SpawnModBiomes = [ModContent.GetInstance<SaltBiome>().Type];
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "NightTime");

	public override void AI()
	{
		const int hostileThreshold = 60;

		if (!Main.dedServ)
		{
			if (_trails == null)
				CreateTrail();

			foreach (VertexTrail trail in _trails)
				trail.Update();

			if (NPC.velocity.Length() > 1 && Main.rand.NextBool(3))
			{
				var dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, _isHostile ? DustID.RedTorch : DustID.BlueCrystalShard);
				dust.noGravity = true;
				dust.velocity = Vector2.Zero;
			}
		}

		if (_counter != 0)
		{
			if (++_counter >= hostileThreshold)
			{
				if (!Main.dedServ && !_isHostile)
				{
					_trails = null;
					ParticleHandler.SpawnParticle(new TexturedPulseCircle(NPC.Center, Color.OrangeRed.Additive(100), 1f, 100, 30, "supPerlin", Vector2.One, EaseFunction.EaseCircularOut));
					ParticleHandler.SpawnParticle(new TexturedPulseCircle(NPC.Center, Color.White.Additive(), 0.5f, 100, 30, "supPerlin", Vector2.One, EaseFunction.EaseCircularOut));

					for (int i = 0; i < 3; i++)
						Main.ParticleSystem_World_OverPlayers.Add(new PrettySparkleParticle()
						{
							LocalPosition = NPC.Center,
							ColorTint = Color.OrangeRed,
							Scale = Vector2.One,
							TimeToLive = 40
						});
				}

				NPC.damage = 10;
				NPC.aiStyle = NPCAIStyleID.Bat;

				_isHostile = true;
			}
			else
			{
				NPC.velocity *= 0.95f;
			}
		}

		if (_isHostile)
		{
			if (_counter == hostileThreshold + 480 && NPC.HasPlayerTarget)
			{
				_counter = hostileThreshold;

				Player target = Main.player[NPC.target];
				Vector2 direction = NPC.DirectionTo(target.Center);

				if (Main.zenithWorld && Main.netMode != NetmodeID.MultiplayerClient)
					Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, direction, ModContent.ProjectileType<EnergyLaser>(), NPC.damage, 1, -1, 0, NPC.whoAmI);
			}

			if (_counter > hostileThreshold + 400)
				NPC.velocity.Y -= 0.2f;

			NPC.velocity *= 0.985f; //Natural hostile velocity
			_counter++;
		}

		if (Main.dayTime && NPC.position.Y / 16 < Main.worldSurface && (NPC.Opacity -= 0.05f) <= 0)
			NPC.active = false; //Vanish during the day

		if (!HoveringOverSurface(10))
			NPC.velocity.Y += 0.1f;

		NPC.rotation += NPC.velocity.Length() * 0.01f;
	}

	private bool HoveringOverSurface(int length)
	{
		for (int i = 0; i < length; i++)
		{
			if (Collision.SolidTiles(NPC.position, NPC.width, NPC.height + 16 * i))
				return true;
		}

		return false;
	}

	private void CreateTrail()
	{
		ITrailCap tCap = new RoundCap();
		ITrailPosition tPos = new EntityTrailPosition(NPC);
		ITrailShader tShader = new ImageShader(AssetLoader.LoadedTextures["GlowTrail"].Value, Vector2.One);

		_trails =
		[
			new VertexTrail(new GradientTrail(Color.PaleVioletRed.Additive(150) * 0.5f, Color.Transparent, EaseFunction.EaseQuarticOut), tCap, tPos, tShader, 40, 150),
			new VertexTrail(new StandardColorTrail((_isHostile ? Color.Red : Color.Cyan).Additive(150)), tCap, tPos, tShader, 30, 30)
		];
	}

	public override void FindFrame(int frameHeight)
	{
		if (!Main.dedServ)
		{
			if (Main.rand.NextBool(10))
				_twirlParticleRenderer.Add(new TwirlyParticle(NPC, _isHostile ? Color.Red : Color.Cyan)
				{
					LocalPosition = NPC.Center + new Vector2(Main.rand.NextFloat(22f, 30f), 0).RotatedByRandom(1),
					Scale = Vector2.One * Main.rand.NextFloat(0.2f, 0.3f),
					TimeToLive = 100,
					RotationVelocity = 0.1f
				});

			_twirlParticleRenderer.Update();

			if (NPC.IsABestiaryIconDummy) //Bestiary shenanigans
			{
				if (NPC.Hitbox.Contains(Main.MouseScreen.ToPoint()))
				{
					if (++_counter == 5)
						for (int i = 0; i < 3; i++)
							_twirlParticleRenderer.Add(new PrettySparkleParticle()
							{
								LocalPosition = NPC.Center,
								ColorTint = Color.OrangeRed,
								Scale = Vector2.One,
								TimeToLive = 20
							});

					if (_counter >= 10)
						_isHostile = true;
				}
				else if (_isHostile)
				{
					if (--_counter == 5)
						for (int i = 0; i < 3; i++)
							_twirlParticleRenderer.Add(new PrettySparkleParticle()
							{
								LocalPosition = NPC.Center,
								ColorTint = Color.OrangeRed,
								Scale = Vector2.One,
								TimeToLive = 20
							});

					if (_counter <= 0)
						_isHostile = false;
				}
			}
		}
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (!Main.dedServ)
		{
			if (NPC.life <= 0)
			{
				for (int i = 0; i < 2; i++)
					ParticleHandler.SpawnParticle(new EmberParticle(NPC.Center, -Vector2.UnitY, Color.PaleVioletRed, 1, 30));

				for (int i = 0; i < 20; i++)
				{
					var dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, _isHostile ? DustID.RedTorch : DustID.BlueCrystalShard, Scale: 1.5f);
					dust.noGravity = true;
					dust.velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3);
				}
			}
		}

		if (_counter == 0)
			_counter++;
	}

	public override void SendExtraAI(BinaryWriter writer) => writer.Write(_isHostile);
	public override void ReceiveExtraAI(BinaryReader reader) => _isHostile = reader.ReadBoolean();
	public override float SpawnChance(NPCSpawnInfo spawnInfo) => (!Main.dayTime && spawnInfo.SpawnTileType == ModContent.TileType<SaltBlockReflective>()) ? 0.1f : 0;

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		Color coreColor = _isHostile ? Color.Red : Color.Cyan;
		Lighting.AddLight(NPC.Center, coreColor.ToVector3());

		Texture2D star = TextureAssets.Projectile[ProjectileID.RainbowRodBullet].Value;
		Texture2D bloom = AssetLoader.LoadedTextures["Extra_49"].Value;
		float pulse = (float)Math.Sin(Main.timeForVisualEffects / 20f);
		float rotation = NPC.rotation;

		spriteBatch.Draw(bloom, NPC.Center - screenPos, null, GetAdditive(Color.PaleVioletRed) * 0.3f, rotation, bloom.Size() / 2, NPC.scale * (0.2f + pulse * 0.05f), default, 0);
		spriteBatch.Draw(bloom, NPC.Center - screenPos, null, GetAdditive(coreColor) * 0.8f, rotation, bloom.Size() / 2, NPC.scale * 0.15f, default, 0);

		var starScale = new Vector2(1 + pulse * 0.2f, 0.5f) * NPC.scale * 0.5f;
		spriteBatch.Draw(star, NPC.Center - screenPos, null, GetAdditive(coreColor, 200), rotation * 0.9f - MathHelper.PiOver2, star.Size() / 2, starScale, default, 0);

		spriteBatch.Draw(star, NPC.Center - screenPos, null, GetAdditive(Color.PaleVioletRed, 255) * 0.25f, rotation, star.Size() / 2, starScale * 2, default, 0);
		spriteBatch.Draw(star, NPC.Center - screenPos, null, GetAdditive(coreColor, 200), rotation, star.Size() / 2, starScale, default, 0);
		spriteBatch.Draw(star, NPC.Center - screenPos, null, GetAdditive(Color.White), rotation, star.Size() / 2, starScale * 0.8f, default, 0);

		_twirlParticleRenderer.Draw(spriteBatch);

		if (_trails != null)
		{
			foreach (VertexTrail trail in _trails)
				trail?.Draw(TrailSystem.TrailShaders, AssetLoader.BasicShaderEffect, spriteBatch.GraphicsDevice);
		}

		return false;

		Color GetAdditive(Color tint, byte additive = 0) => NPC.IsABestiaryIconDummy ? tint.Additive(additive) : NPC.GetAlpha(tint).Additive(additive);
	}
}