using SpiritReforged.Common.BuffCommon;

namespace SpiritReforged.Content.Ocean.Items.JellyCandle;

[AutoloadPetBuff]
public class JellyfishPet : ModProjectile
{
	private float frameCounter;

	public override void SetStaticDefaults()
	{
		Main.projFrames[Type] = 3;
		Main.projPet[Type] = true;

		ProjectileID.Sets.CharacterPreviewAnimations[Type] = ProjectileID.Sets.SimpleLoop(0, Main.projFrames[Type], 6)
			.WithSpriteDirection(-1)
			.WithCode(DelegateMethods.CharacterPreview.Float);
	}

	public override void SetDefaults()
	{
		Projectile.CloneDefaults(ProjectileID.ZephyrFish);
		AIType = ProjectileID.ZephyrFish;
		Projectile.width = 40;
		Projectile.height = 30;
	}

	public override void AI()
	{
		Player player = Main.player[Projectile.owner];

		player.zephyrfish = false; //Relic from AIType

		Projectile.frame = (int)(frameCounter += .2f) % Main.projFrames[Type];
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame, 0, -2);

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, frame.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
		return false;
	}
}