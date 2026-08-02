using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Glyphs.Shock;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.SaltFlats.NPCs;
using Terraria.Audio;

namespace SpiritReforged.Content.Forest.Katanas.LightningSword;

public class VajraSwing : SwungProjectile, IDrawPixelated
{
	public bool Secondary { get => Projectile.ai[0] == 1; set => Projectile.ai[0] = value ? 1 : 0; }

	public override LocalizedText DisplayName => ModContent.GetInstance<Vajra>().DisplayName;

	private int _npcTarget = -1;

	public override void SetStaticDefaults()
	{
		Main.projFrames[Type] = 4;
		ProjectileID.Sets.TrailCacheLength[Type] = 8;
		ProjectileID.Sets.TrailingMode[Type] = 0;
	}

	public override IConfiguration SetConfiguration() => new BasicConfiguration(EaseFunction.EaseCubicOut, 84, 25);

	public override void AI()
	{
		base.AI();

		Player owner = Main.player[Projectile.owner];
		DashSwordPlayer mp = owner.GetModPlayer<DashSwordPlayer>();

		if (Secondary)
		{
			if (FindNearestTarget(Main.player[Projectile.owner], out NPC nearestNPC))
				Projectile.velocity = Projectile.DirectionTo(nearestNPC.Center);

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

				if (Counter == 0)
					SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -1 }, Projectile.Center);

				if (!Main.dedServ)
					ParticleHandler.SpawnParticle(new EmberParticle(Main.rand.NextVector2FromRectangle(owner.Hitbox), owner.velocity * Main.rand.NextFloat(0.1f, 0.2f), Color.Goldenrod, Color.Red, Main.rand.NextFloat(0.1f, 1), 25, 3));
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

		if (Secondary)
		{
			HoldDistance = -14;
		}
		else if (SwingArc == 0)
		{
			float offset = Math.Max(40 * (0.5f - Progress * 2), -10);
			HoldDistance = offset;
		}
		else
		{
			HoldDistance = -18 * Progress;
		}
	}

	public override float GetRotation(out float armRotation, out Player.CompositeArmStretchAmount stretch)
	{
		float value = GetAbsoluteAngle();
		armRotation = value - 1.57f;
		stretch = ProgressiveStretch();

		return value + 0.5f * Progress * SwingDirection;
	}

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		/*if (Secondary)
		{
			float collisionPoint = 0;
			Vector2 start = Projectile.oldPos[ProjectileID.Sets.TrailCacheLength[Type] / 2] + Projectile.Size / 2;

			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, Projectile.Center, 20, ref collisionPoint);
		}
		else*/
		{
			return base.Colliding(projHitbox, targetHitbox);
		}
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
		int buffType = BuffAutoloader.GetAutoloadedBuffType<Vajra>();
		if (!Secondary)
		{
			if (!target.HasBuff(buffType))
			{
				SoundEngine.PlaySound(ShockGlyph.ElectricSting, target.Center);
				SoundEngine.PlaySound(ShockGlyph.ElectricZap, target.Center);

				ParticleHandler.SpawnParticle(new SharpStarParticle(target.Center, Vector2.Zero, Color.Goldenrod, 1, 14));
				ParticleHandler.SpawnParticle(new SharpStarParticle(target.Center, Vector2.Zero, Color.White.Additive(), 0.5f, 14));
			}

			target.AddBuff(buffType, 480);
		}
		else
		{
			target.RemoveBuff(buffType);

			foreach (NPC npc in Main.ActiveNPCs)
			{
				if (npc != target && (npc.CanBeChasedBy() || npc.active && npc.type == NPCID.TargetDummy) && npc.DistanceSQ(target.Center) < 200 * 200)
				{
					Projectile.NewProjectile(target.GetSource_OnHurt(Projectile), target.Center, Vector2.Zero, ModContent.ProjectileType<VajraLightning>(), Projectile.damage, Projectile.knockBack, Projectile.owner, npc.whoAmI);
					break;
				}
			}
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
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
		}

		DrawHeld(lightColor, origin, Projectile.rotation, effects, source);

		if (Secondary)
			DrawHeld(Color.White.Additive() * Progress * 2f, origin, Projectile.rotation, effects, source);

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

			spriteBatch.Draw(bloom, position, null, Color.Goldenrod.Additive() * opacity, 0, bloom.Size() / 2, 0.1f, 0, 0);
			spriteBatch.Draw(bloom, position, null, Color.White.Additive() * opacity, 0, bloom.Size() / 2, 0.05f, 0, 0);

			Vector2 endPosition = GetEndPosition(-30) - Main.screenPosition;
			IDrawPixelated.PixelateDrawPosition(ref endPosition);

			Texture2D star = AssetLoader.LoadedTextures["Star"].Value; //Star drawing
			Main.EntitySpriteDraw(star, endPosition, null, Color.Goldenrod.Additive() * (1f - Progress), 0, star.Size() / 2, 0.3f * Progress, 0);
			Main.EntitySpriteDraw(star, endPosition, null, Color.White.Additive() * (1f - Progress), 0, star.Size() / 2, 0.2f * Progress, 0);
		}

		if (SwingArc != 0)
		{
			//Draw a custom smear
			Main.instance.LoadProjectile(985);
			Texture2D smear = TextureAssets.Projectile[985].Value;

			SpriteEffects effects = SwingDirection == -1 ? SpriteEffects.FlipVertically : default;
			Player player = Main.player[Projectile.owner];
			Rectangle source = smear.Frame(1, 4, 0, (int)(Progress * 18f));
			float rotation = Projectile.rotation;

			Color lightColor = Lighting.GetColor(Projectile.Center.ToTileCoordinates());
			Vector2 origin = new(source.Width, source.Height / 2);
			Vector2 smearWorldPosition = player.Center + (Vector2.UnitX * (GetConfig<BasicConfiguration>().Reach + 10)).RotatedBy(rotation);
			Vector2 smearDrawPosition = smearWorldPosition - Main.screenPosition;

			IDrawPixelated.PixelateDrawPosition(ref smearDrawPosition);

			spriteBatch.Draw(smear, smearDrawPosition, source, lightColor.MultiplyRGB(new Color(71, 59, 45)), rotation, origin, 0.5f, effects, 0);
			spriteBatch.Draw(smear, smearDrawPosition, source, lightColor.MultiplyRGB(new Color(244, 187, 82)), rotation, origin, 0.45f, effects, 0);
			spriteBatch.Draw(smear, smearDrawPosition, source, lightColor.MultiplyRGB(new Color(255, 254, 140)), rotation, origin, 0.25f, effects, 0);
		}
	}
}