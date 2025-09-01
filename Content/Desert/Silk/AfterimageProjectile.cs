using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Desert.Silk;

public class AfterimageProjectile : GlobalProjectile
{
	public override bool InstancePerEntity => true;
	public bool Afterimage { get; private set; }

	public override bool PreDraw(Projectile projectile, ref Color lightColor)
	{
		if (Afterimage)
		{
			var star = TextureAssets.Projectile[ProjectileID.RainbowRodBullet].Value;
			var bloom = AssetLoader.LoadedTextures["Bloom"].Value;

			Vector2 scale = new Vector2(1 + (float)Math.Sin(Main.timeForVisualEffects / 10f + projectile.whoAmI) * 0.2f, 1 - (float)Math.Sin(Main.timeForVisualEffects / 5f + projectile.whoAmI) * 0.2f) * projectile.scale * 0.75f;
			float rotation = (float)Main.timeForVisualEffects / 30f + projectile.whoAmI;
			float opacity = 0.5f;

			Main.spriteBatch.Draw(bloom, projectile.Center - Main.screenPosition, null, Color.Gold.Additive() * 0.8f * opacity, 0, bloom.Size() / 2, 0.3f, default, 0);
			Main.spriteBatch.Draw(star, projectile.Center - Main.screenPosition, null, Color.OrangeRed * 0.5f * opacity, rotation, star.Size() / 2, scale * 1.2f, default, 0);
			Main.spriteBatch.Draw(star, projectile.Center - Main.screenPosition, null, Color.Goldenrod.Additive() * 0.8f * opacity, rotation, star.Size() / 2, scale, default, 0);
			Main.spriteBatch.Draw(star, projectile.Center - Main.screenPosition, null, Color.White.Additive() * opacity, rotation, star.Size() / 2, scale * 0.7f, default, 0);
		}

		return true;
	}

	public override void OnSpawn(Projectile projectile, IEntitySource source)
	{
		if (source is EntitySource_ItemUse_WithAmmo && AfterimagePlayer.Duplicate)
		{
			Afterimage = true;
			projectile.netUpdate = true;

			CreateTrail(projectile);
		}
	}

	private static void CreateTrail(Projectile p)
	{
		if (Main.dedServ)
			return;

		ITrailShader tShader = new ImageShader(AssetLoader.LoadedTextures["GlowTrail"].Value, Vector2.One * 2);

		AssetLoader.VertexTrailManager.CreateTrail(p, new StandardColorTrail(Color.Goldenrod.Additive()), new RoundCap(), new DefaultTrailPosition(), 20, 80, tShader);
		AssetLoader.VertexTrailManager.CreateTrail(p, new LightColorTrail(Color.Goldenrod.Additive() * 0.8f, Color.Transparent), new RoundCap(), new DefaultTrailPosition(), 25, 120, new DefaultShader());
	}

	public override void AI(Projectile projectile)
	{
		if (Afterimage)
		{
			if (Main.rand.NextBool(8))
			{
				float strength = Main.rand.NextFloat();
				float scale = MathHelper.Lerp(0.5f, 1.5f, strength);
				var position = Main.rand.NextVector2FromRectangle(projectile.Hitbox);
				var velocity = projectile.velocity * MathHelper.Lerp(0.3f, 0.9f, strength);

				ParticleHandler.SpawnParticle(new EmberParticle(position, velocity, Color.Lerp(Color.OrangeRed, Color.Yellow, strength).Additive(), scale, 25, 1) { emitLight = false });
			}

			if (Main.rand.NextBool(8))
			{
				float strength = Main.rand.NextFloat();
				var dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, DustID.GoldCoin, 0, 0);
				dust.noGravity = true;
				dust.velocity = projectile.velocity * strength;
				dust.fadeIn = Main.rand.NextFloat(0.8f, 1.5f);
			}
		}
	}

	public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
	{
		if (Afterimage)
			bitWriter.WriteBit(Afterimage);
	}

	public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
	{
		bool oldValue = Afterimage;

		if (Afterimage = bitReader.ReadBit() && !oldValue)
			CreateTrail(projectile);
	}
}