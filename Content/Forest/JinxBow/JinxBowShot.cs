using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using System.IO;
using System.Linq;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Forest.JinxBow;

public class JinxBowShot : GlobalProjectile
{
	public const int TrailLength = 9;

	public bool IsJinxbowShot { get; set; } = false;
	public bool IsJinxbowSubshot { get; set; } = false;

	private readonly Vector2[] _oldPositions = new Vector2[TrailLength];

	public override bool InstancePerEntity => true;

	public override void OnSpawn(Projectile projectile, IEntitySource source)
	{
		//Initialize old positions to projectile's center on spawn
		if(IsJinxbowShot)
			for (int i = 0; i < _oldPositions.Length; i++)
				_oldPositions[i] = projectile.Center;

		//If a jinxbow arrow spawns a projectile (i.e. Holy arrows, luminite arrows), the spawned projectile counts as a summon projectile instead of ranged.
		//Additionally applies to projectiles spawned from projectiles spawned by arrows, like holy arrow stars recursively spawning
		if (source is EntitySource_Parent { Entity: Projectile parent })
		{
			if (parent.GetGlobalProjectile<JinxBowShot>().IsJinxbowShot || parent.GetGlobalProjectile<JinxBowShot>().IsJinxbowSubshot)
			{
				projectile.DamageType = DamageClass.Summon;
				IsJinxbowSubshot = true;
				projectile.netUpdate = true;
				projectile.minion = true;
			}
		}
	}

	public override void PostAI(Projectile projectile)
	{
		if (!IsJinxbowShot)
			return;

		for(int i = TrailLength - 1; i > 0; i--)
			_oldPositions[i] = _oldPositions[i - 1];

		_oldPositions[0] = projectile.Center;
	}

	public override void OnKill(Projectile projectile, int timeLeft)
	{
		if (!IsJinxbowShot || Main.dedServ)
			return;

		Texture2D arrowTex = TextureAssets.Projectile[projectile.type].Value;
		Color color = TextureColorCache.GetBrightestColor(arrowTex);

		ParticleHandler.SpawnParticle(new ImpactLinePrim(projectile.Center, Vector2.Zero, color.Additive(), new(0.66f, 2.25f), 10, 1));
		ParticleHandler.SpawnParticle(new LightBurst(projectile.Center, Main.rand.NextFloatDirection(), color.Additive(), 0.66f, 25));

		for(int i = 0; i < 12; i++) 
		{
			Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(0.5f, 4);
			float scale = Main.rand.NextFloat(0.3f, 0.7f);
			int lifeTime = Main.rand.Next(12, 40);
			static void DelegateAction(Particle p) => p.Velocity *= 0.9f;

			ParticleHandler.SpawnParticle(new GlowParticle(projectile.Center, velocity, color.Additive(), scale, lifeTime, 1, DelegateAction));
			ParticleHandler.SpawnParticle(new GlowParticle(projectile.Center, velocity, Color.White.Additive(), scale, lifeTime, 1, DelegateAction));
		}
	}

	public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (IsJinxbowShot && Main.rand.NextBool(5))
			target.GetGlobalNPC<JinxMarkNPC>().SetMark(target, projectile);
	}

	public override bool PreDraw(Projectile projectile, ref Color lightColor)
	{
		if (!IsJinxbowShot)
			return true;

		//Partially adapted from hunter rifle vfx

		//Load texture if not already loaded
		Main.instance.LoadProjectile(873);

		var defaultTexture = TextureAssets.Projectile[projectile.type].Value;
		Texture2D solid = TextureColorCache.ColorSolid(defaultTexture, Color.Lavender);
		var brightest = TextureColorCache.GetBrightestColor(defaultTexture);

		for (int i = TrailLength - 1; i >= 0; i--)
		{
			var texture = TextureAssets.Projectile[873].Value;

			float lerp = 1f - i / (float)(TrailLength - 1);
			var color = (Color.Lerp(brightest.MultiplyRGBA(Color.Black * .5f), brightest, lerp) with { A = 0 }) * lerp;
			var position = _oldPositions[i] - Main.screenPosition;
			var scale = new Vector2(.5f * lerp, 1) * projectile.scale;

			if (i == 0)
			{
				color = Color.White with { A = 200 };
				texture = defaultTexture;
				scale = new(projectile.scale);

				//Draw border around the main image
				for (int j = 0; j < 12; j++)
				{
					Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 12f) * 2;
					Main.EntitySpriteDraw(solid, position + offset, null, Color.Lerp(brightest, Color.Lavender, 0.33f).Additive(100) * 0.33f, projectile.rotation, texture.Size() / 2, scale, SpriteEffects.None);
				}
			}
			else //Otherwise draw as trail
				Main.EntitySpriteDraw(solid, Vector2.Lerp(position, _oldPositions[0] - Main.screenPosition, 0.33f), null, Color.Lerp(brightest, Color.Lavender, 0.33f).Additive(100) * EaseFunction.EaseQuadIn.Ease(lerp) * 0.5f, projectile.rotation, solid.Size() / 2, new Vector2(projectile.scale), SpriteEffects.None); 

			Main.EntitySpriteDraw(texture, position, null, color, projectile.rotation, texture.Size() / 2, scale, SpriteEffects.None);
		}

		return false;
	}

	public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
	{
		bitWriter.WriteBit(IsJinxbowShot);
		bitWriter.WriteBit(IsJinxbowSubshot);
	}

	public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
	{
		IsJinxbowShot = bitReader.ReadBit();
		IsJinxbowSubshot = bitReader.ReadBit();
	}
}