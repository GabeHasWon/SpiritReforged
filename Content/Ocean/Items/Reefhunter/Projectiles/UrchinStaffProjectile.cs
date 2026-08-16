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

	private Vector2 UrchinPos => Projectile.Center + new Vector2(35, -35).RotatedBy(Projectile.rotation) * Projectile.scale;

	public override LocalizedText DisplayName => Language.GetText("Mods.SpiritReforged.Items.UrchinStaff.DisplayName");

	public override void SetStaticDefaults() => HeldProjectileSet.HeldProjectile[Type] = true;

	public override void SetDefaults()
	{
		Projectile.Size = new Vector2(2);
		Projectile.friendly = true;
		Projectile.penetrate = -1;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.DamageType = DamageClass.Magic;
		Projectile.aiStyle = -1;

		DrawHeldProjInFrontOfHeldItemAndArms = false;
	}

	public override void AI()
	{
		Player owner = Main.player[Projectile.owner];
		owner.heldProj = Projectile.whoAmI;

		Projectile.timeLeft = owner.itemAnimation;
		float animationProgress = owner.itemAnimation / (float)owner.itemAnimationMax;

		animationProgress = EaseFunction.EaseQuadIn.Ease(animationProgress);
		if (owner.direction < 0)
			Projectile.spriteDirection = -1;

		float rotation = ShotTrajectory.ToRotation() + MathHelper.WrapAngle(MathHelper.Lerp(MathHelper.PiOver2 * 1.25f * owner.direction, -MathHelper.Pi * owner.direction, animationProgress));

		Projectile.rotation = rotation - MathHelper.PiOver4;
		if (owner.direction < 0)
			Projectile.rotation += MathHelper.Pi;

		float armRot = MathHelper.Pi + rotation;
		if (owner.direction < 0)
			armRot -= MathHelper.Pi;

		owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRot);
		owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armRot);
		Projectile.Center = owner.MountedCenter;
		Projectile.scale = EaseFunction.EaseCircularOut.Ease((float)Math.Sin((owner.itemAnimation / (float)owner.itemAnimationMax) * MathHelper.Pi));

		float shotTime = 0.7f;

		if (owner.itemAnimation == (int)(shotTime * owner.itemAnimationMax))
		{
			if (Projectile.owner == Main.myPlayer)
				ShootUrchin(owner);

			SoundEngine.PlaySound(SoundID.Item1, Projectile.Center);
		}
	}

	public override bool ShouldUpdatePosition() => false;

	public override bool? CanDamage() => false;

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
