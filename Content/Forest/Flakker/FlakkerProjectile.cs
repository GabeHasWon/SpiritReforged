using Mono.Cecil;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Subclasses.Shotguns;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using static System.Net.Mime.MediaTypeNames;

namespace SpiritReforged.Content.Forest.Flakker;

public class FlakkerProjectile : ModProjectile
{
	public int AmmoID => (int)Projectile.ai[0];

	public Flakker parent;
	bool hitTile;

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 8;
		ProjectileID.Sets.TrailingMode[Type] = 0;
	}

	public override void SetDefaults()
	{
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
		Player player = Main.player[Projectile.owner];
		SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/LMG") with { Pitch = 0.2f, PitchVariance = 0.2f }, Projectile.Center); //CACHE ME

		// Airburst
		if (Projectile.penetrate > 0 && !hitTile)
		{
			Item ammoItem = AmmoID > 0 ? ContentSamples.ItemsByType[AmmoID] : null;
			StatModifiers modifiers = new(
				new StatModifier(0.8f, 1),
				new StatModifier(0.35f, 1, @base: 0.2f));

			ShotgunGlobalItem.ShotgunShoot(modifiers, new EntitySource_ItemUse_WithAmmo(player, parent.Item, AmmoID), Projectile.Center, Projectile.velocity, ammoItem.shoot, Projectile.damage * 3, Projectile.knockBack * 5);

			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/Explosion_01") with { Volume = 0.25f, Pitch = 0.3f, PitchVariance = 0.2f }, Projectile.Center); //CACHE

			Vector2 glowPos = Projectile.Center;
			Vector2 stretch = Vector2.One;

			float strength = Main.rand.NextFloat(0.9f, 1.1f);

			ParticleRenderers.UnderProjectiles.Add(new TexturedPulseCircle(glowPos, Color.Yellow.Additive(), Color.OrangeRed, 0.6f, 60 * strength, (int)(30 * strength), "Smoke", stretch, EaseFunction.EaseQuinticOut)
			{ Angle = Main.rand.NextFloat(MathHelper.TwoPi) });

			for (int i = 0; i < (int)(4 * strength); i++)
			{
				Vector2 pos = Projectile.Center;
				Vector2 velocity = Main.rand.NextVector2CircularEdge(5f, 5f) * Main.rand.NextFloat(0.5f, 1f) * strength;

				ParticleRenderers.UnderProjectiles.Add(new SparkParticle(pos, velocity, Color.Lerp(Color.OrangeRed, Color.Yellow, Main.rand.NextFloat()), 0.65f, Main.rand.Next(30, 70), p => p.Velocity *= 0.9f, tileCollide: false));

				for (int x = 0; x < 3; x++)
				{
					Dust.NewDustPerfect(pos, DustID.Torch, Main.rand.NextVector2Circular(6f, 6f) * strength, 0, default, Main.rand.NextFloat(3f) * strength).noGravity = true;
				}
			}
		}
		else //EMPTY BLOCK
		{

		}

		ParticleRenderers.UnderProjectiles.Add(new CompositeSmoke(Projectile.Center, -Vector2.UnitY * Main.rand.NextFloat(), new(120, 120, 120), 50, default, p => p.Velocity *= 0.95f));
		ParticleRenderers.UnderProjectiles.Add(new CompositeSmoke(Projectile.Center, -Vector2.UnitY * Main.rand.NextFloat(), new(200, 200, 200), 30, default, p => p.Velocity *= 0.95f)
		{
			Variant = CompositeSmoke.SmallVariant
		});

		Dust.NewDustPerfect(Projectile.Center, DustID.Torch, Main.rand.NextVector2Circular(4, 4), 0, default, Main.rand.NextFloat(3f)).noGravity = true;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		var texture = TextureAssets.Projectile[Type].Value;

		for (int i = 0; i < Projectile.oldPos.Length; i++)
		{
			float lerp = 1f - i / (float)Projectile.oldPos.Length;

			Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size / 2 - Main.screenPosition;

			Main.spriteBatch.Draw(texture, drawPosition, null, lightColor * lerp * 0.33f, Projectile.rotation, texture.Size() / 2f, Projectile.scale, 0f, 0f); //MAIN.ENTITYSPRITEDRAW
		}

		Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, texture.Size() / 2f, Projectile.scale, 0f, 0f); //MAIN.ENTITYSPRITEDRAW

		return false;
	}
}
