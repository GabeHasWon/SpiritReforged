using Mono.Cecil;
using SpiritReforged.Common.Subclasses.Greatshields;
using SpiritReforged.Common.Subclasses.Shotguns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Terraria;
using Terraria.Audio;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;

namespace SpiritReforged.Content.Forest.Flakker;
public class FlakkerProjectile : ModProjectile
{
	public Flakker parent;
	public int AmmoID => (int)Projectile.ai[0];
	public ShotgunStats parentStats => parent.shotgunStats;

	bool hitTile;

	public override void SetDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 8;
		ProjectileID.Sets.TrailingMode[Type] = 0;

		Projectile.Size = new(12);

		Projectile.friendly = true;
		Projectile.timeLeft = Main.rand.Next(30, 45);
		Projectile.penetrate = 1;
		Projectile.DamageType = ModContent.GetInstance<ShotgunClass>();
		Projectile.extraUpdates = 1;
	}

	public override void AI()
	{
		Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
		Projectile.velocity *= 0.99f;

		if (Main.rand.NextBool())
			Dust.NewDustPerfect(Projectile.Center, DustID.Smoke, -Projectile.velocity.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.33f), 220, default, Main.rand.NextFloat(3f));
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		hitTile = true;
		return true;
	}

	public override void OnKill(int timeLeft)
	{
		var player = Main.player[Projectile.owner];

		SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/LMG") with { Pitch = 0.2f, PitchVariance = 0.2f }, Projectile.Center);

		// Airburst
		if (Projectile.penetrate > 0 && !hitTile)
		{
			Item ammoItem = AmmoID > 0 ? ContentSamples.ItemsByType[AmmoID] : null;

			if (ammoItem != null && ammoItem.ModItem is ShotgunAmmoItem ammo)
			{
				Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX.RotatedBy(Projectile.rotation));
				var shotgunPlayer = player.GetModPlayer<ShotgunPlayer>();

				var shotgunStats = parentStats;

				ammo._behavior.Invoke(parent.Item, player, new Terraria.DataStructures.EntitySource_ItemUse_WithAmmo(player, parent.Item, AmmoID, "SpiritReforged: Flakker Detonate"), Projectile.Center, direction,
					shotgunPlayer.ModifyShotCount(ammo._shotCount, shotgunStats._additionalShots, shotgunStats._shotMultiplier),
					shotgunPlayer.ModifySpread(ammo._spreadAmount, shotgunStats._additionalSpread, shotgunStats._spreadMultiplier),
					shotgunPlayer.ModifySpeed(ammo._speed, shotgunStats._additionalSpeed, shotgunStats._speedMultiplier),
					(int)(Projectile.damage * 3f), Projectile.knockBack * 5);
			}

			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/Explosion_01") with { Volume = 0.25f, Pitch = 0.3f, PitchVariance = 0.2f }, Projectile.Center);

			Vector2 glowPos = Projectile.Center;
			Vector2 stretch = Vector2.One;

			float strength = Main.rand.NextFloat(0.9f, 1.1f);

			ParticleHandler.SpawnParticle(new TexturedPulseCircle(glowPos, Color.Yellow.Additive(), Color.OrangeRed, 0.6f, 60 * strength, (int)(30 * strength), "Smoke", stretch, EaseFunction.EaseQuinticOut)
			{ Angle = Main.rand.NextFloat(MathHelper.TwoPi) });

			for (int i = 0; i < (int)(4 * strength); i++)
			{
				Vector2 pos = Projectile.Center;
				Vector2 velocity = Main.rand.NextVector2CircularEdge(5f, 5f) * Main.rand.NextFloat(0.5f, 1f) * strength;

				ParticleHandler.SpawnParticle(new SparkParticle(pos, velocity, Color.Lerp(Color.OrangeRed, Color.Yellow, Main.rand.NextFloat()), 0.65f, Main.rand.Next(30, 70), p => p.Velocity *= 0.9f, tileCollide: false));

				for (int x = 0; x < 3; x++)
				{
					Dust.NewDustPerfect(pos, DustID.Torch, Main.rand.NextVector2Circular(6f, 6f) * strength, 0, default, Main.rand.NextFloat(3f) * strength).noGravity = true;
				}
			}
		}
		else
		{

		}

		ParticleHandler.SpawnParticle(new CompositeSmoke(Projectile.Center, -Vector2.UnitY * Main.rand.NextFloat(), new(120, 120, 120), 50, false, false, p => p.Velocity *= 0.95f));
		
		ParticleHandler.SpawnParticle(new SmallCompositeSmoke(Projectile.Center, -Vector2.UnitY * Main.rand.NextFloat(), new(200, 200, 200), 30, false, false, p => p.Velocity *= 0.95f));

		Dust.NewDustPerfect(Projectile.Center, DustID.Torch, Main.rand.NextVector2Circular(4, 4), 0, default, Main.rand.NextFloat(3f)).noGravity = true;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		var texture = TextureAssets.Projectile[Type].Value;

		for (int i = 0; i < Projectile.oldPos.Length; i++)
		{
			float lerp = 1f - i / (float)Projectile.oldPos.Length;

			Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size / 2 - Main.screenPosition;

			Main.spriteBatch.Draw(texture, drawPosition, null, lightColor * lerp * 0.33f, Projectile.rotation, texture.Size() / 2f, Projectile.scale, 0f, 0f);
		}

		Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, texture.Size() / 2f, Projectile.scale, 0f, 0f);

		return false;
	}
}
