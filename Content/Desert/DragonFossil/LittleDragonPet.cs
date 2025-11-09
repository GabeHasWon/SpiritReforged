using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Common.ProjectileCommon;

namespace SpiritReforged.Content.Desert.DragonFossil;

[AutoloadPetBuff]
public class LittleDragonPet : ModProjectile
{
	public override void SetStaticDefaults()
	{
		Main.projPet[Type] = true;
		Main.projFrames[Type] = 5;

		ProjectileID.Sets.CharacterPreviewAnimations[Type] = ProjectileID.Sets.SimpleLoop(0, Main.projFrames[Type])
			.WithSpriteDirection(1)
			.WithCode(DelegateMethods.CharacterPreview.Float);
	}

	public override void SetDefaults() => Projectile.Size = new Vector2(24);

	public override void AI()
	{
		Projectile.UpdateFrame(20);

		Player owner = Main.player[Projectile.owner];
		Vector2 restingSpot = owner.Center + new Vector2(30 * -owner.direction, -30);
		float distance = Projectile.Distance(restingSpot);
		var result = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(restingSpot) * Math.Clamp(distance / 16f, 0, 10), 0.1f);

		if (distance < 16)
			result *= 0.9f;

		if (!result.HasNaNs())
			Projectile.velocity = result;

		int direction = Math.Sign(Projectile.velocity.X);
		Projectile.direction = Projectile.spriteDirection = (result.Length() < 0.2f) ? owner.direction : direction;
		Projectile.rotation = Projectile.velocity.Y * 0.1f * Projectile.direction;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Rectangle source = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame, 0, -2);
		SpriteEffects effects = (Projectile.spriteDirection == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), source, Projectile.GetAlpha(lightColor), Projectile.rotation, source.Size() / 2, Projectile.scale, effects, 0);
		return false;
	}
}