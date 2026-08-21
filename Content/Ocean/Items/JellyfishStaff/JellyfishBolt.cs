using SpiritReforged.Common.CombatTextCommon;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Content.Dusts;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.Audio;
using SpiritReforged.Common.Visuals;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using SpiritReforged.Common.ProjectileCommon;
using Terraria.Utilities;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Ocean.Items.JellyfishStaff;

public class JellyfishBolt : ModProjectile, IDrawPixelated
{
	public override string Texture => AssetLoader.EmptyTexture;
	public static int MAX_CHAIN_DISTANCE => (int)(JellyfishMinion.SHOOT_RANGE * 0.75f);
	public const int MAX_POINTS = 7;
	public const int MAX_TIMELEFT = 20;

	public int TargetWhoAmI => (int)Projectile.ai[0];
	public bool IsPink
	{
		get => (int)Projectile.ai[1] == 1;
		set => Projectile.ai[1] = value ? 1 : 0;
	}
	public bool IsGreen
	{
		get => (int)Projectile.ai[1] == 2;
		set => Projectile.ai[1] = value ? 1 : 0;
	}
	public int ChainCount => (int)Projectile.ai[2];
	public int Delay;

	public bool Initialized = false;

	public float Progress => 1f - Projectile.timeLeft / (float)MAX_TIMELEFT;
	public bool Invalid { get; set; }

	private JellyfishMinionColors JellyColors;

	public Vector2 startPos;
	private List<NPC> hitTargets = [];
	private VertexTrail[] _trails;
	private List<Vector2> offsets;
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

	public override bool? CanHitNPC(NPC target) => target.whoAmI == TargetWhoAmI && Delay <= 0 && Initialized;

	public override void OnKill(int timeLeft)
	{
		Invalid = true;
	}

	public override void OnSpawn(IEntitySource source)
	{
		if (hitTargets.Count > 0)
			Delay = Main.rand.Next(15);
	}

	public override void AI()
	{
		if (Delay > 0)
		{
			Delay--;
			Projectile.timeLeft = MAX_TIMELEFT;
			return;
		}

		float strength = MathHelper.Lerp(20, 8, Progress);

		if (!Initialized)
		{
			JellyColors = new(IsPink, IsGreen);

			if (!Main.dedServ && _trails == null)
				CreateTrail();

			startPos = Projectile.Center;
			offsets = [];

			for (int i = 0; i < MAX_POINTS + 1; i++)
			{
				offsets.Add(Main.rand.NextVector2Circular(strength, strength));
			}
			
			Initialized = true;
		}

		if (!Main.dedServ && _trails is not null)
		{
			int randomizationInterval = (int)MathHelper.Lerp(5, 3, Progress);

			List<Vector2> cache = new();

			for (int i = 0; i < MAX_POINTS; i++)
			{
				float step = 1f - i / (float)MAX_POINTS;

				if (Projectile.timeLeft % randomizationInterval == 0)
				{
					offsets[i] = Main.rand.NextVector2Circular(strength, strength) * MathHelper.Lerp(1.5f, 0.2f, step);
				}

				cache.Add(Vector2.Lerp(startPos, Main.npc[TargetWhoAmI].Center, step) + offsets[i]);
			}

			cache.Add(startPos);

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
				Vector2 vel = startPos.DirectionTo(Main.npc[TargetWhoAmI].Center).RotatedByRandom(0.15f) * Main.rand.NextFloat(9f);
				Vector2 pos = startPos + Main.rand.NextVector2Circular(5f, 5f);

				ParticleHandler.SpawnParticle(new GlowParticle(pos, vel, JellyColors.ParticleOne, 0.4f, 40, extraUpdateAction: DecelerateAction));
				ParticleHandler.SpawnParticle(new GlowParticle(pos, vel, JellyColors.ParticleTwo.Additive(), 0.25f, 40, extraUpdateAction: DecelerateAction));

				static void DecelerateAction(Particle p) => p.Velocity *= 0.9f;
			}

			if (Main.rand.NextBool(20))
			{
				Vector2 pos = _trails[0]._points[Main.rand.Next(MAX_POINTS)] + Main.rand.NextVector2Circular(2f, 2f);

				ParticleHandler.SpawnParticle(new LightningBoltParticle(pos, startPos.DirectionTo(Main.npc[TargetWhoAmI].Center).RotatedByRandom(0.5f) * 3f,
					JellyColors.LightningStart, JellyColors.LightningEnd.Additive(), 0f, 0.6f, 40));
			}
		}
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		Projectile.damage /= 2;

		for (int i = 0; i < 5; i++)
		{
			ParticleHandler.SpawnParticle(new LightningBoltParticle(target.Center, Main.rand.NextVector2Circular(5f, 5f), JellyColors.LightningStart, JellyColors.LightningEnd.Additive(), 0f, 0.6f, 40));

			ParticleHandler.SpawnParticle(new SmallCompositeSmoke(target.Center + Main.rand.NextVector2Circular(target.width / 2, target.height / 2), -Vector2.UnitY * 0.3f, JellyColors.SmokeColor * 0.8f, 50, bloomOpacity: 0.035f));
		}

		hitTargets.Add(target);

		NPC newTarget = Main.npc.Where(n => n.CanBeChasedBy() && !hitTargets.Contains(n) && n.Distance(Projectile.Center) < MAX_CHAIN_DISTANCE).OrderBy(n => n.Distance(Projectile.Center)).FirstOrDefault();
		if (newTarget != null && ChainCount < 4 && Projectile.penetrate >= 2)
		{
			int color = IsPink ? 1 : 0;
			if (IsGreen)
				color = 2;

			PreNewProjectile.New(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<JellyfishBolt>(), Projectile.damage, Projectile.knockBack, Projectile.owner, newTarget.whoAmI, color, ChainCount + 1, p => (p.ModProjectile as JellyfishBolt).hitTargets = hitTargets);
		}
	}

	private void CreateTrail()
	{
		ITrailCap tCap = new RoundCap();
		ITrailPosition tPos = new EntityTrailPosition(Projectile);
		ITrailShader tShader = new ImageShader(AssetLoader.LoadedTextures["GlowTrail"].Value, Vector2.One);

		_trails =
		[
			new VertexTrail(new GradientTrail(JellyColors.LightningEnd.Additive(), JellyColors.LightningStart, EaseFunction.EaseQuarticInOut), tCap, tPos, tShader, 25, MAX_POINTS, trailWidthFunction: hitTargets.Count > 0 ? factor => 11f : null),
			new VertexTrail(new GradientTrail(Color.White.Additive(), JellyColors.LightningStart.Additive(), EaseFunction.EaseQuarticOut), tCap, tPos, tShader, 23, MAX_POINTS, trailWidthFunction: hitTargets.Count > 0 ? factor => 7f : null),
		];
	}

	public override bool PreDraw(ref Color lightColor)
	{
		return false;
	}

	public void DrawPixelated(SpriteBatch sb)
	{
		if (Delay > 0 || JellyColors is null)
			return;

		if (_trails != null)
			foreach (VertexTrail trail in _trails)
			{
				trail.Opacity = 1f - EaseBuilder.EaseCubicIn.Ease(Progress);
				trail.WidthMultiplier = 1f - EaseBuilder.EaseCircularIn.Ease(Progress);

				trail?.Draw(TrailSystem.TrailShaders, sb.GraphicsDevice, Matrix.Identity);
			}

		var tex = AssetLoader.LoadedTextures["Bloom"].Value;

		float progress = 1f - EaseFunction.EaseCubicIn.Ease(Progress);

		Vector2 position = Projectile.Center - Main.screenPosition;
		IDrawPixelated.PixelateDrawPosition(ref position);

		Main.spriteBatch.Draw(tex, position, null, JellyColors.BaseColor.Additive() * 0.35f * progress, 0, tex.Size() / 2, 0.1f, SpriteEffects.None, 0);
		Main.spriteBatch.Draw(tex, position, null, Color.White.Additive() * 0.5f * progress, 0, tex.Size() / 2, 0.05f, SpriteEffects.None, 0);
	}
}