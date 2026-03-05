using SpiritReforged.Common.Easing;
using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.ProjectileCommon;
using System.IO;
using Terraria.Audio;

namespace SpiritReforged.Content.Ocean.Items.Reefhunter.Projectiles;

public class UrchinStaffProjectile : ModProjectile
{
	public Vector2 ShotTrajectory { get; set; }
	public Vector2 RelativeTargetPosition { get; set; }

	public override LocalizedText DisplayName => Language.GetText("Mods.SpiritReforged.Items.UrchinStaff.DisplayName");

	public override void SetDefaults()
	{
		Projectile.width = 2;
		Projectile.height = 2;
		Projectile.friendly = true;
		Projectile.penetrate = -1;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.DamageType = DamageClass.Magic;
		Projectile.aiStyle = -1;

		DrawHeldProjInFrontOfHeldItemAndArms = false;
	}

	public override bool? CanDamage() => false;

	public override void AI()
	{
		Player p = Main.player[Projectile.owner];
		p.heldProj = Projectile.whoAmI;

		Projectile.timeLeft = p.itemAnimation;
		float animationProgress = p.itemAnimation / (float)p.itemAnimationMax;

		animationProgress = EaseFunction.EaseQuadIn.Ease(animationProgress);
		if (p.direction < 0)
			Projectile.spriteDirection = -1;

		float rotation = ShotTrajectory.ToRotation() + MathHelper.WrapAngle(MathHelper.Lerp(MathHelper.PiOver2 * 1.25f * p.direction, -MathHelper.Pi * p.direction, animationProgress));

		Projectile.rotation = rotation - MathHelper.PiOver4;
		if (p.direction < 0)
			Projectile.rotation += MathHelper.Pi;

		float armRot = MathHelper.Pi + rotation;
		if (p.direction < 0)
			armRot -= MathHelper.Pi;

		p.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRot);
		p.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armRot);
		Projectile.Center = p.MountedCenter;
		Projectile.scale = EaseFunction.EaseCircularOut.Ease((float)Math.Sin((p.itemAnimation / (float)p.itemAnimationMax) * MathHelper.Pi));

		float shotTime = 0.7f;

		if (p.itemAnimation == (int)(shotTime * p.itemAnimationMax))
		{
			if (Projectile.owner == Main.myPlayer)
				ShootUrchin(p);

			SoundEngine.PlaySound(SoundID.Item1, Projectile.Center);
		}
	}

	private void ShootUrchin(Player player)
	{
		var adjustedTrajectory = ArcVelocityHelper.GetArcVel(UrchinPos - player.MountedCenter, RelativeTargetPosition, 0.25f, ShotTrajectory.Length());

		PreNewProjectile.New(Projectile.GetSource_FromAI(), UrchinPos, adjustedTrajectory + player.velocity / 3, ModContent.ProjectileType<UrchinBall>(), Projectile.damage, Projectile.knockBack, Projectile.owner, preSpawnAction: delegate (Projectile p)
		{
			p.rotation = Projectile.rotation;
			p.Center = UrchinPos;
		});

		Projectile.netUpdate = true;
		Projectile.ai[0]++;
	}

	private Vector2 UrchinPos => Projectile.Center + new Vector2(35, -35).RotatedBy(Projectile.rotation) * Projectile.scale;

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D t = TextureAssets.Projectile[Projectile.type].Value;
		Texture2D urchinTex = TextureAssets.Projectile[ModContent.ProjectileType<UrchinBall>()].Value;
		Vector2 origin = t.Size() * new Vector2(0, 1);
		SpriteEffects flip = SpriteEffects.None;
		float rotationFlip = Projectile.rotation;
		if(Projectile.spriteDirection < 0)
		{
			rotationFlip += MathHelper.PiOver2;
			flip = SpriteEffects.FlipHorizontally;
			origin = t.Size();
		}

		if (Projectile.ai[0] == 0) //Draw the urchin seperately
			Main.spriteBatch.Draw(urchinTex, UrchinPos - Main.screenPosition, urchinTex.Bounds, lightColor, rotationFlip, urchinTex.Bounds.Size() / 2, Projectile.scale, flip, 1f);

		Main.spriteBatch.Draw(t, Projectile.Center - Main.screenPosition, t.Bounds, lightColor, rotationFlip, origin, Projectile.scale, flip, 1f);

		return false;
	}

	public override void SendExtraAI(BinaryWriter writer) => writer.WriteVector2(ShotTrajectory);
	public override void ReceiveExtraAI(BinaryReader reader) => ShotTrajectory = reader.ReadVector2();
}
