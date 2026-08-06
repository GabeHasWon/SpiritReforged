using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Glyphs.Shock;
using SpiritReforged.Content.Jungle.Bamboo.Items;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.SaltFlats.NPCs;
using Terraria.Audio;

namespace SpiritReforged.Content.Forest.Katanas.LightningSword;

public class VajraSwing : SwungProjectile, IDrawPixelated
{
	public bool Secondary { get => Projectile.ai[0] == 1; set => Projectile.ai[0] = value ? 1 : 0; }

	public override LocalizedText DisplayName => ModContent.GetInstance<Vajra>().DisplayName;

	private int _npcTarget = -1;
	private BasicNoiseCone _noiseCone;

	public override void SetStaticDefaults()
	{
		Main.projFrames[Type] = 4;
		ProjectileID.Sets.TrailCacheLength[Type] = 8;
		ProjectileID.Sets.TrailingMode[Type] = 0;
	}

	public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.EaseQuarticOut, 84, 25);

	public override void AI()
	{
		Player owner = Main.player[Projectile.owner];
		bool justSpawned = Counter == 0;

		if (Secondary)
		{
			//Set swung projectile default AI
			Projectile.spriteDirection = Projectile.direction = owner.direction = (Projectile.velocity.X > 0) ? 1 : -1;
			Projectile.Center = owner.Center;

			owner.heldProj = Projectile.whoAmI;

			if (++Counter < SwingTime - 2)
				owner.itemAnimation = owner.itemTime = Projectile.timeLeft = 2;

			DashAI(owner);
		}
		else
		{
			base.AI();

			if (SwingArc == 0)
			{
				float offset = Math.Max(40 * (0.5f - Progress * 2), -10);
				HoldDistance = offset;

				if (!Main.dedServ && justSpawned)
					_noiseCone = (BasicNoiseCone)new BasicNoiseCone(Projectile.Center, Projectile.velocity, 20, new(50, 250)).SetColors(Color.White.Additive(100), Color.Goldenrod).SetIntensity(2).AttachTo(Projectile);
			}
			else
			{
				HoldDistance = -18 * Progress;

				if (Progress < 0.5f && Main.rand.NextBool())
				{
					Vector2 velocity = Vector2.UnitY.RotatedBy(Projectile.rotation) * Main.rand.NextFloat(2f) * SwingDirection;
					ParticleHandler.SpawnParticle(new EmberParticle(GetEndPosition(-10) + Main.rand.NextVector2Circular(5, 5), velocity, Color.Goldenrod, Color.PaleVioletRed, Main.rand.NextFloat(0.1f, 0.5f), 25, 3));
				}
			}
		}

		if (justSpawned)
			SoundEngine.PlaySound(KendoBladeLunge.BigSwing with { Pitch = (SwingArc == 0) ? 1 : 0.5f, PitchVariance = 0.2f }, Projectile.Center);

		if (_noiseCone != null) //Update the noise cone if any
		{
			_noiseCone.TimeActive++;
			_noiseCone.Position += _noiseCone.Velocity;

			_noiseCone.Update();

			if (_noiseCone.TimeActive > _noiseCone.MaxTime && _noiseCone.MaxTime > 0)
				_noiseCone.Kill();
		}
	}

	private void DashAI(Player owner)
	{
		DashSwordPlayer mp = owner.GetModPlayer<DashSwordPlayer>();

		if (FindNearestTarget(Main.player[Projectile.owner], out NPC nearestNPC))
			Projectile.velocity = Projectile.DirectionTo(nearestNPC.Center);

		Projectile.Opacity = 0;
		HoldDistance = Math.Max((1 - EaseFunction.EaseCubicOut.Ease(Progress) * 3) * 24, -8);
		mp.SetDash(30);

		if (Counter > SwingTime - 5)
		{
			owner.velocity *= 0.5f;

			if (Counter > SwingTime - 3)
				owner.opacityForAnimation = 1;
		}
		else
		{
			const int magnitude = 20;

			owner.velocity = Vector2.Lerp(owner.velocity, Projectile.velocity * magnitude * 2, EaseFunction.EaseQuinticIn.Ease(Progress));
			owner.opacityForAnimation = 0.5f - Progress;

			if (Counter == SwingTime / 2)
			{
				owner.velocity = Projectile.velocity * magnitude;

				SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, Projectile.Center);
				SoundEngine.PlaySound(Wisp.Death with { Pitch = 0.8f, PitchVariance = 0.2f }, Projectile.Center);
			}
			else if (Counter >= SwingTime / 2 && owner.TryGetModPlayer(out CollisionPlayer cPlayer))
			{
				cPlayer.IgnorePlatforms = true;
			}

			if (Counter == 1)
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -1 }, Projectile.Center);

			if (!Main.dedServ)
				ParticleHandler.SpawnParticle(new EmberParticle(Main.rand.NextVector2FromRectangle(owner.Hitbox), owner.velocity * Main.rand.NextFloat(0.1f, 0.2f), Color.Goldenrod, Color.PaleVioletRed, Main.rand.NextFloat(0.1f, 1), 25, 3));
		}

		if (Progress > 0.4f)
		{
			for (int i = 0; i < 3; i++)
			{
				var dust = Dust.NewDustDirect(owner.position, owner.width, owner.height, DustID.Ash, 0, 0, 120, default, Main.rand.NextFloat() * 1.5f);
				dust.noGravity = true;
				dust.velocity = Projectile.velocity * 3;
			}

			for (int i = 0; i < 2; i++)
				ParticleHandler.SpawnParticle(new CompositeSmoke(Main.rand.NextVector2FromRectangle(Projectile.Hitbox), Projectile.velocity, Color.PaleGoldenrod * 0.7f, 15, false));
		}
	}

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		if (Secondary)
		{
			Rectangle playerHitbox = Main.player[Projectile.owner].Hitbox;
			playerHitbox.Inflate(20, 20);

			return playerHitbox.Intersects(targetHitbox);
		}
		else
		{
			return base.Colliding(projHitbox, targetHitbox);
		}
	}

	public override float GetRotation(out float armRotation, out Player.CompositeArmStretchAmount stretch)
	{
		float value = GetAbsoluteAngle();
		armRotation = value - 1.57f;
		stretch = ProgressiveStretch();

		return value + 0.5f * Progress * SwingDirection;
	}

	/// <summary> Finds the nearest NPC for dash purposes. </summary>
	private bool FindNearestTarget(Player owner, out NPC nearestNPC)
	{
		const int max_distance = 500;
		int buffType = BuffAutoloader.GetAutoloadedBuffType<Vajra>();

		if (_npcTarget != -1 && Main.npc[_npcTarget] is NPC cachedNPC && cachedNPC.active && cachedNPC.HasBuff(buffType)) //Use the cached target
		{
			nearestNPC = Main.npc[_npcTarget];
			return true;
		}

		float distance = max_distance;
		int npcWhoAmI = _npcTarget = -1;

		foreach (NPC npc in Main.ActiveNPCs)
		{
			float currentDistance = npc.Distance(owner.Center);
			if (npc.HasBuff(buffType) && currentDistance < distance)
			{
				distance = currentDistance;
				npcWhoAmI = npc.whoAmI;
			}
		}

		if (npcWhoAmI < 0 || npcWhoAmI >= Main.maxNPCs)
		{
			nearestNPC = null;
			return false;
		}

		nearestNPC = Main.npc[_npcTarget = npcWhoAmI];
		return true;
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (!Secondary) //Apply the buff
		{
			int buffType = BuffAutoloader.GetAutoloadedBuffType<Vajra>();
			if (!target.HasBuff(buffType))
			{
				SoundEngine.PlaySound(ShockGlyph.ElectricSting, target.Center);
				SoundEngine.PlaySound(ShockGlyph.ElectricZap, target.Center);

				ParticleHandler.SpawnParticle(new SharpStarParticle(target.Center, Vector2.Zero, Color.Goldenrod, Color.PaleVioletRed, 1, 14));
				ParticleHandler.SpawnParticle(new SharpStarParticle(target.Center, Vector2.Zero, Color.White.Additive(), 0.5f, 14));
			}

			target.AddBuff(buffType, 480);
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		const int afterimages = 5;

		Texture2D texture = TextureAssets.Projectile[Type].Value;
		SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipVertically : default;
		Vector2 origin = new(4, 28); //The handle

		Rectangle source;
		float visCounter = MathHelper.Min(Counter / (SwingTime / (Secondary ? 2.5f : 1.5f)), 1);
		int frameY = (int)(visCounter * (Main.projFrames[Type] - 1));

		if (SwingArc == 0)
		{
			source = Secondary ? texture.Frame(2, Main.projFrames[Type], 1, frameY, -2, -2) :  texture.Frame(2, Main.projFrames[Type], 0, Main.projFrames[Type] - 1, -2, -2);
		}
		else
		{
			source = texture.Frame(2, Main.projFrames[Type], 0, frameY, -2, -2);

			for (int i = 0; i < afterimages; i++)
			{
				float progress = 1f / afterimages * i;
				float rotation = Projectile.rotation - progress * SwingDirection * GetConfig<BasicConfiguration>().Easing.Ease(1f - Progress) * 0.5f;

				DrawHeld(Projectile.GetAlpha(lightColor).Additive(100) * (1f - progress) * 0.2f, origin, rotation, effects, source);
			}
		}

		DrawHeld(Projectile.GetAlpha(lightColor), origin, Projectile.rotation, effects, source);
		return false;
	}

	void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch)
	{
		Player owner = Main.player[Projectile.owner];
		if (Secondary)
		{
			if (Progress > 0.5f)
			{
				Vector2 startPosition = Projectile.oldPos[ProjectileID.Sets.TrailCacheLength[Type] - 1];
				float progress = (Progress - 0.7f) / 0.3f;

				LightningChain chain = new(startPosition, owner.Center, Color.Goldenrod.Additive(), (int)(50 * (1f - progress)));

				chain.Reconfigure(Projectile.whoAmI);
				chain.Update();
				chain.Draw(spriteBatch, Matrix.Identity);
			}

			Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
			Vector2 position = owner.Center - Main.screenPosition;
			float opacity = Progress * 1.2f;

			IDrawPixelated.PixelateDrawPosition(ref position);

			spriteBatch.Draw(bloom, position, null, Color.Lerp(Color.Goldenrod, Color.Orange, Progress).Additive() * opacity, 0, bloom.Size() / 2, 0.1f, 0, 0);
			spriteBatch.Draw(bloom, position, null, Color.White.Additive() * opacity, 0, bloom.Size() / 2, 0.05f, 0, 0);
		}

		IDrawPixelated.PrimitiveDrawing = true;
		_noiseCone?.CustomDraw(spriteBatch);
		IDrawPixelated.PrimitiveDrawing = false;
	}
}