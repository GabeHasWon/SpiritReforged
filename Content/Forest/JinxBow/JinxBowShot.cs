using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Forest.JinxBow;

public class JinxBowShot : GlobalProjectile
{
	public const int TrailLength = 9;

	public override bool InstancePerEntity => true;

	/// <summary> Returns the projectile instance associated with <see cref="_parentIndex"/>. If the projectile has no parent, returns a dummy. </summary>
	public Projectile Parent => IsJinxbowShot ? Main.projectile[_parentIndex] : new();
	/// <summary> Whether this projectile has a parent. </summary>
	public bool IsJinxbowShot => _parentIndex != -1;

	private readonly Vector2[] _oldPositions = new Vector2[TrailLength];
	private int _parentIndex = -1;

	public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.friendly;

	public override void OnSpawn(Projectile projectile, IEntitySource source)
	{
		//If a jinxbow arrow spawns a projectile (i.e. Holy arrows, luminite arrows), the spawned projectile counts as a summon projectile instead of ranged.
		//Additionally applies to projectiles spawned from projectiles spawned by arrows, like holy arrow stars recursively spawning
		if (source is EntitySource_Parent { Entity: Projectile parent })
		{
			if (projectile.type == ModContent.ProjectileType<JinxArrow>())
				return;

			if (parent.ModProjectile is JinxBowMinion)
			{
				_parentIndex = parent.whoAmI;
			}
			else if (parent.TryGetGlobalProjectile(out JinxBowShot jinx) && jinx.IsJinxbowShot)
			{
				_parentIndex = jinx._parentIndex; //Sub
			}

			if (IsJinxbowShot)
			{
				OnClientSpawn(projectile);
				projectile.netUpdate = true;
			}
		}
	}

	public void OnClientSpawn(Projectile projectile)
	{
		//Initialize old positions to projectile's center on spawn
		for (int i = 0; i < _oldPositions.Length; i++)
			_oldPositions[i] = projectile.Center;

		projectile.DamageType = DamageClass.Summon;
		projectile.minion = true;
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
		if (!IsJinxbowShot)
			return;

		Projectile parent = Main.projectile[_parentIndex];

		if (parent.ModProjectile is JinxBowMinion jinxBow && jinxBow.MarkCooldown == 0)
		{
			int time = target.HasBuff<JinxMark>() ? JinxBowMinion.MARK_COOLDOWN : (int)(JinxBowMinion.MARK_COOLDOWN * JinxBowMinion.MARK_LINGER_RATIO);
			target.AddBuff(ModContent.BuffType<JinxMark>(), time, true); //Only apply the mark locally

			jinxBow.MarkCooldown = JinxBowMinion.MARK_COOLDOWN;
		}
	}

	public override bool PreDraw(Projectile projectile, ref Color lightColor)
	{
		if (!IsJinxbowShot)
			return true;

		//Partially adapted from hunter rifle vfx

		//Load texture if not already loaded
		Main.instance.LoadProjectile(ProjectileID.HallowBossRainbowStreak);

		var defaultTexture = TextureAssets.Projectile[projectile.type].Value;
		Texture2D solid = TextureColorCache.ColorSolid(defaultTexture, Color.Cyan);
		var brightest = TextureColorCache.GetBrightestColor(defaultTexture);

		for (int i = TrailLength - 1; i >= 0; i--)
		{
			var texture = TextureAssets.Projectile[ProjectileID.HallowBossRainbowStreak].Value;

			float lerp = 1f - i / (float)(TrailLength - 1);
			var color = (Color.Lerp(brightest.MultiplyRGBA(Color.Black * .5f), brightest, lerp) with { A = 0 }) * lerp;
			var position = _oldPositions[i] - Main.screenPosition;
			var scale = new Vector2(.5f * lerp, 1) * projectile.scale;

			Color brightestPurpleLerp = Color.Lerp(brightest, Color.Cyan.Additive(50), 0.33f).Additive(100);
			if (i == 0)
			{
				color = Color.White with { A = 200 };
				texture = defaultTexture;
				scale = new(projectile.scale);

				//Draw border around the main image
				for (int j = 0; j < 12; j++)
				{
					Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 12f) * 2;
					var drawColor = brightestPurpleLerp * 0.33f;
					Main.EntitySpriteDraw(solid, position + offset, null, drawColor, projectile.rotation, texture.Size() / 2, scale, SpriteEffects.None);
				}
			}
			else //Otherwise draw as trail
			{
				var drawPos = Vector2.Lerp(position, _oldPositions[0] - Main.screenPosition, 0.33f);
				var drawColor = brightestPurpleLerp * EaseFunction.EaseQuadIn.Ease(lerp) * 0.5f;
				Main.EntitySpriteDraw(solid, drawPos, null, drawColor, projectile.rotation, solid.Size() / 2, new Vector2(projectile.scale), SpriteEffects.None);
			}

			Main.EntitySpriteDraw(texture, position, null, color, projectile.rotation, texture.Size() / 2, scale, SpriteEffects.None);
		}

		return false;
	}

	public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
	{
		bitWriter.WriteBit(IsJinxbowShot);

		if (IsJinxbowShot)
			binaryWriter.Write(_parentIndex);
	}

	public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
	{
		bool isJinxbowShot = bitReader.ReadBit();

		if (isJinxbowShot)
		{
			bool wasJinxbowShot = IsJinxbowShot;
			_parentIndex = binaryReader.ReadInt32();

			if (!wasJinxbowShot)
				OnClientSpawn(projectile);
		}
	}
}