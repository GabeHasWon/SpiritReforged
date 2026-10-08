using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Content.Particles;
using SpiritReforged.Common.Visuals;
using System.Linq;
using SpiritReforged.Common.ProjectileCommon;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Ocean.Items.JellyMinion;

public class JellyfishBolt : ModProjectile, IDrawPixelated
{
	public static int MAX_CHAIN_DISTANCE => (int)(JellyfishMinion.SHOOT_RANGE * 0.75f);

	public const int MAX_POINTS = 7;
	public const int MAX_TIMELEFT = 20;

	public override string Texture => AssetLoader.EmptyTexture;

	public int TargetWhoAmI => (int)Projectile.ai[0];

	public JellyfishColors.ColorStyle ColorStyle
	{
		get => (JellyfishColors.ColorStyle)Projectile.ai[1];
		set => Projectile.ai[1] = (int)value;
	}

	public int ChainCount => (int)Projectile.ai[2];

	public float Progress => 1f - Projectile.timeLeft / (float)MAX_TIMELEFT;

	public bool invalid;
	public int delay;
	public bool initialized;

	private JellyfishColors _jellyColors;
	public Vector2 _startPos;
	private List<NPC> _hitTargets = [];
	private VertexTrail[] _trails;
	private List<Vector2> _offsets;

	public override void SetStaticDefaults() => ProjectileID.Sets.MinionShot[Type] = true;

	public override void SetDefaults()
	{
		Projectile.Size = new Vector2(4);
		Projectile.DamageType = DamageClass.Summon;
		
		Projectile.hostile = false;
		Projectile.friendly = true;
		Projectile.tileCollide = false;

		Projectile.timeLeft = MAX_TIMELEFT;

		Projectile.penetrate = 2;
		Projectile.stopsDealingDamageAfterPenetrateHits = true;

		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = MAX_TIMELEFT / 2;

		Projectile.ArmorPenetration += 5;
	}

	public override bool? CanHitNPC(NPC target) => target.whoAmI == TargetWhoAmI && delay <= 0 && initialized;

	public override void OnKill(int timeLeft) => invalid = true;

	public override void OnSpawn(IEntitySource source)
	{
		if (_hitTargets.Count > 0)
			delay = Main.rand.Next(15);
	}

	public override void AI()
	{
		if (delay > 0)
		{
			delay--;
			Projectile.timeLeft = MAX_TIMELEFT;

			return;
		}

		float strength = MathHelper.Lerp(20, 8, Progress);

		if (!initialized)
		{
			_jellyColors = new(ColorStyle);

			if (!Main.dedServ && _trails == null)
				CreateTrail();

			_startPos = Projectile.Center;
			_offsets = [];

			for (int i = 0; i < MAX_POINTS + 1; i++)
				_offsets.Add(Main.rand.NextVector2Circular(strength, strength));

			initialized = true;
		}

		if (!Main.dedServ && _trails is not null)
		{
			int randomizationInterval = (int)MathHelper.Lerp(5, 3, Progress);
			List<Vector2> cache = [];

			for (int i = 0; i < MAX_POINTS; i++)
			{
				float step = 1f - i / (float)MAX_POINTS;

				if (Projectile.timeLeft % randomizationInterval == 0)
					_offsets[i] = Main.rand.NextVector2Circular(strength, strength) * MathHelper.Lerp(1.5f, 0.2f, step);

				cache.Add(Vector2.Lerp(_startPos, Main.npc[TargetWhoAmI].Center, step) + _offsets[i]);
			}

			cache.Add(_startPos);

			foreach (VertexTrail trail in _trails)
			{
				trail.Update();
				trail._points = cache;
			}
		}

		if (!Main.dedServ)
		{
			Projectile.Center = Main.npc[TargetWhoAmI].Center;

			if (Main.rand.NextBool(12))
			{
				Vector2 vel = _startPos.DirectionTo(Main.npc[TargetWhoAmI].Center).RotatedByRandom(0.15f) * Main.rand.NextFloat(9f);
				Vector2 pos = _startPos + Main.rand.NextVector2Circular(5f, 5f);

				ParticleHandler.SpawnParticle(new GlowParticle(pos, vel, _jellyColors.ParticleOne, 0.4f, 40, extraUpdateAction: DecelerateAction));
				ParticleHandler.SpawnParticle(new GlowParticle(pos, vel, _jellyColors.ParticleTwo.Additive(), 0.25f, 40, extraUpdateAction: DecelerateAction));
			}

			if (Main.rand.NextBool(20))
			{
				Vector2 pos = _trails[0]._points[Main.rand.Next(MAX_POINTS)] + Main.rand.NextVector2Circular(2f, 2f);

				ParticleHandler.SpawnParticle(new LightningBoltParticle(pos, _startPos.DirectionTo(Main.npc[TargetWhoAmI].Center).RotatedByRandom(0.5f) * 3f,
					_jellyColors.LightningStart, _jellyColors.LightningEnd.Additive(), 0f, 0.6f, 40));
			}
		}

		static void DecelerateAction(Particle p) => p.Velocity *= 0.9f;
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		Projectile.damage /= 2;

		for (int i = 0; i < 5; i++)
		{
			ParticleHandler.SpawnParticle(new LightningBoltParticle(target.Center, Main.rand.NextVector2Circular(5f, 5f), _jellyColors.LightningStart, _jellyColors.LightningEnd.Additive(), 0f, 0.6f, 40));
			ParticleHandler.SpawnParticle(new SmallCompositeSmoke(target.Center + Main.rand.NextVector2Circular(target.width / 2, target.height / 2), -Vector2.UnitY * 0.3f, _jellyColors.SmokeColor * 0.8f, 50, bloomOpacity: 0.035f));
		}

		_hitTargets.Add(target);

		NPC newTarget = Main.npc.Where(n => n.CanBeChasedBy() && !_hitTargets.Contains(n) && n.Distance(Projectile.Center) < MAX_CHAIN_DISTANCE).OrderBy(n => n.Distance(Projectile.Center)).FirstOrDefault();
		
		if (newTarget != null && ChainCount < 4 && Projectile.penetrate >= 2)
			PreNewProjectile.New(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<JellyfishBolt>(), Projectile.damage, Projectile.knockBack, Projectile.owner, newTarget.whoAmI, (int)ColorStyle, ChainCount + 1, p => (p.ModProjectile as JellyfishBolt)._hitTargets = _hitTargets);
	}

	private void CreateTrail()
	{
		ITrailCap tCap = new RoundCap();
		ITrailPosition tPos = new EntityTrailPosition(Projectile);
		ITrailShader tShader = new ImageShader(AssetLoader.LoadedTextures["GlowTrail"].Value, Vector2.One);

		_trails =
		[
			new VertexTrail(new GradientTrail(_jellyColors.LightningEnd.Additive(), _jellyColors.LightningStart, EaseFunction.EaseQuarticInOut), tCap, tPos, tShader, 25, MAX_POINTS, trailWidthFunction: _hitTargets.Count > 0 ? factor => 11f : null),
			new VertexTrail(new GradientTrail(Color.White.Additive(), _jellyColors.LightningStart.Additive(), EaseFunction.EaseQuarticOut), tCap, tPos, tShader, 23, MAX_POINTS, trailWidthFunction: _hitTargets.Count > 0 ? factor => 7f : null),
		];
	}

	public override bool PreDraw(ref Color lightColor) => false;

	public void DrawPixelated(SpriteBatch sb)
	{
		if (delay > 0 || _jellyColors == default)
			return;

		if (_trails != null)
			foreach (VertexTrail trail in _trails)
			{
				trail.Opacity = 1f - EaseFunction.EaseCubicIn.Ease(Progress);
				trail.WidthMultiplier = 1f - EaseFunction.EaseCircularIn.Ease(Progress);

				trail?.Draw(TrailSystem.TrailShaders, sb.GraphicsDevice, Matrix.Identity);
			}

		Texture2D tex = AssetLoader.LoadedTextures["Bloom"].Value;
		float progress = 1f - EaseFunction.EaseCubicIn.Ease(Progress);

		Vector2 position = Projectile.Center - Main.screenPosition;
		IDrawPixelated.PixelateDrawPosition(ref position);

		Main.spriteBatch.Draw(tex, position, null, _jellyColors.BaseColor.Additive() * 0.35f * progress, 0, tex.Size() / 2, 0.1f, SpriteEffects.None, 0);
		Main.spriteBatch.Draw(tex, position, null, Color.White.Additive() * 0.5f * progress, 0, tex.Size() / 2, 0.05f, SpriteEffects.None, 0);
	}
}