using SpiritReforged.Common.Easing;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals.Glowmasks;
using System.IO;
using Terraria.Audio;

namespace SpiritReforged.Content.Ocean.Items.Reefhunter.Projectiles;

[AutoloadGlowmask("Method:Content.Ocean.Items.Reefhunter.Projectiles.UrchinSpike GlowColor")]
public class UrchinSpike : ModProjectile
{
	public static readonly SoundStyle Impact = new("SpiritReforged/Assets/SFX/Projectile/Impact_LightPop")
	{ 
		PitchVariance = 0.4f, 
		Volume = 1.1f, 
		MaxInstances = 12
	};

	public bool HasTarget => _targetIndex > 0;

	private int _targetIndex = -1;
	private bool _spawned = true;
	private Vector2 _relativePoint = Vector2.Zero;

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 20;
		ProjectileID.Sets.TrailingMode[Type] = 2;
	}

	public override void SetDefaults()
	{
		Projectile.Size = new(6);
		Projectile.DamageType = DamageClass.Magic;
		Projectile.friendly = true;
		Projectile.penetrate = -1;
		Projectile.extraUpdates = 3;
		Projectile.timeLeft = 60;
		Projectile.scale = Main.rand.NextFloat(0.7f, 1.1f);
	}

	public override bool? CanDamage() => HasTarget ? false : null;
	public override bool? CanCutTiles() => HasTarget ? false : null;

	public override void AI()
	{
		if (_spawned) //Create a trail
		{
			if (!Main.dedServ)
				TrailSystem.ProjectileRenderer.CreateTrail(Projectile, new VertexTrail(new LightColorTrail(new Color(87, 35, 88) * 0.2f, Color.Transparent), new RoundCap(), new EntityTrailPosition(Projectile), new DefaultShader(), 8 * Projectile.scale, 75));

			_spawned = false;
		}

		Projectile.alpha = 255 - (int)(Projectile.timeLeft / 60f * 255);
		Projectile.scale = EaseFunction.EaseCircularOut.Ease(Projectile.Opacity);
		Projectile.velocity *= 0.96f;

		if (HasTarget)
		{
			NPC target = Main.npc[_targetIndex];

			if (Projectile.tileCollide == true)
				PostHitNPC(Main.npc[_targetIndex]);

			Projectile.velocity *= 0.92f;
			_relativePoint += Projectile.velocity;

			if (!target.active)
				Projectile.Kill();
			else
				Projectile.Center = target.Center + _relativePoint;
		}
		else
		{
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
		}
	}

	public void PostHitNPC(NPC target)
	{
		Projectile.tileCollide = false;
		Projectile.alpha = 0;

		_relativePoint = Projectile.Center - target.Center;

		if (!Main.dedServ)
		{
			TrailSystem.ProjectileRenderer.DissolveTrail(Projectile, 12);
			SoundEngine.PlaySound(Impact, Projectile.Center);
		}
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		_targetIndex = target.whoAmI;
		Projectile.netUpdate = true;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Projectile.QuickDraw();
		Projectile.QuickDrawTrail(baseOpacity: 0.25f);

		return false;
	}

	public override void SendExtraAI(BinaryWriter writer) => writer.Write(_targetIndex);
	public override void ReceiveExtraAI(BinaryReader reader) => _targetIndex = reader.ReadInt32();

	public static Color GlowColor(object proj)
	{
		var spike = proj as Projectile;
		return UrchinBall.OrangeVFXColor(0) * EaseFunction.EaseQuadIn.Ease(spike.Opacity);
	}
}