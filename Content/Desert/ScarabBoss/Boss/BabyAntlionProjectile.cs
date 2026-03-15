using Microsoft.CodeAnalysis;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using Terraria.GameContent.Drawing;
using Terraria.Graphics.CameraModifiers;
using static Terraria.GameContent.PlayerEyeHelper;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

public class BabyAntlionProjectile : ModProjectile
{
	private const int MAX_TIMELEFT = 460;

	public bool HasAScarabValid
	{
		get
		{
			if (Projectile.ai[0] < 0 || Projectile.ai[0] >= Main.maxNPCs)
				return false;
			NPC npc = Main.npc[(int)Projectile.ai[0]];
			if (!npc.active)
				return false;

			return npc.type == ModContent.NPCType<Scarabeus>();
		}
	}

	public ref float Timer => ref Projectile.ai[1];

	public NPC Scarab => Main.npc[(int)Projectile.ai[0]];

	public enum AIState
	{
		Hidden,
		Emerging,
		ChasingScarab,
		Burnt
	}

	public AIState CurrentState
	{
		get => (AIState)Projectile.ai[2];
		set => Projectile.ai[2] = (int)value;
	}

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.DontAttachHideToAlpha[Type] = true;
	}

	public override void SetDefaults()
	{
		Projectile.Size = new(16, 16);
		Projectile.hostile = true;
		Projectile.tileCollide = false;
		Projectile.penetrate = -1;
		Projectile.timeLeft = MAX_TIMELEFT;
	}

	public override bool? CanDamage() => CurrentState != AIState.Hidden && Projectile.timeLeft > 30 ? null : false;

	public override void AI()
	{
		Timer++;

		if (CurrentState == AIState.Hidden)
		{


			if (Timer > 30)
			{
				CurrentState = AIState.Emerging;
				Projectile.velocity.Y = -10f;
			}
		}

		//Falling down when burnt
		if (CurrentState == AIState.Burnt)
		{
			Projectile.velocity.X = 0;
			Projectile.velocity.Y += 0.2f;

			Projectile.rotation += Projectile.direction * 0.03f;

			//Sharticles
			if (!Main.dedServ & Main.rand.NextBool(3))
			{
				Dust d = Dust.NewDustPerfect(Main.rand.NextVector2FromRectangle(Projectile.Hitbox), DustID.Torch, Vector2.Zero, 0, Scale: Main.rand.NextFloat(0.7f, 1f));
				d.noLight = true;
			}

			if (!Main.dedServ && Main.rand.NextBool(4))
			{
				Gore g = Gore.NewGorePerfect(Projectile.GetSource_FromThis(), Projectile.Center + Main.rand.NextVector2Circular(8f, 8f), Vector2.Zero, 99, Main.rand.NextFloat(1f, 1.2f));
				g.alpha = 160;
				g.position -= Vector2.One * 10f;
			}

			return;
		}

		Lighting.AddLight(Projectile.Center, 0.3f, 0.1f, 0.1f);

		if (!HasAScarabValid)
		{
			Projectile.timeLeft = Math.Min(Projectile.timeLeft, 40);
			return;
		}

		Vector2 towardsScarab = (Scarab.Center - Vector2.UnitY * 30f - Projectile.Center);

		if (towardsScarab.Length() < 30f)
		{
			Burnt = true;
			Projectile.velocity.Y = 0;
			Projectile.timeLeft = 200;
			return;
		}

		towardsScarab.Normalize();
		towardsScarab = towardsScarab.RotatedBy(MathF.Sin(Timer * 0.02f) * 0.1) * 10;

		Projectile.velocity = towardsScarab;
		Projectile.rotation = Projectile.velocity.ToRotation();

	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Vector2 position = Projectile.Center;
		float rotation = Projectile.rotation;
		float scale = Projectile.scale;
		Rectangle frame = texture.Frame(1, 2, 0, Burnt ? 1 : 0);
		SpriteEffects effects = Projectile.direction < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

		lightColor *= Math.Max(1, Projectile.timeLeft / 40f) * Utils.GetLerpValue(MAX_TIMELEFT, MAX_TIMELEFT - 30, Projectile.timeLeft, true);

		Main.EntitySpriteDraw(texture, position - Main.screenPosition, frame, lightColor, rotation, frame.Size() / 2f, scale, effects, 0);
		return false;
	}
}