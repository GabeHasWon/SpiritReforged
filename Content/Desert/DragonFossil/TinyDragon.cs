using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;

namespace SpiritReforged.Content.Desert.DragonFossil;

public class TinyDragon : ModItem
{
	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.Fish);
		Item.shoot = ModContent.ProjectileType<TinyDragonPet>();
		Item.buffType = AutoloadedPetBuff.Registered[Item.shoot];
	}

	public override void UseStyle(Player player, Rectangle heldItemFrame)
	{
		if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
			player.AddBuff(Item.buffType, 3600, true);
	}

	public override bool CanUseItem(Player player) => player.miscEquips[0].IsAir;
}

[AutoloadPetBuff]
public class TinyDragonPet : ModProjectile
{
	private static readonly int[] MaxFrames = [5, 6, 3];

	public const int FLYING = 0;
	public const int TURNING = 1;
	public const int ZOOMING = 2;

	public int Style
	{
		get => (int)Projectile.ai[0];
		set => Projectile.ai[0] = value;
	}

	public override void SetStaticDefaults()
	{
		Main.projPet[Type] = true;
		Main.projFrames[Type] = 6;

		ProjectileID.Sets.CharacterPreviewAnimations[Type] = ProjectileID.Sets.SimpleLoop(0, MaxFrames[FLYING])
			.WithSpriteDirection(1)
			.WithCode(DelegateMethods.CharacterPreview.Float);
	}

	public override void SetDefaults() => Projectile.Size = new Vector2(24);

	public override void AI()
	{
		int maxFrame = MaxFrames[Style];
		Projectile.UpdateFrame(20, maxFrame: maxFrame);

		Player owner = Main.player[Projectile.owner];
		Vector2 restingSpot = owner.Center + new Vector2(30 * -owner.direction, -30);
		float distance = Projectile.Distance(restingSpot);
		var result = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(restingSpot) * Math.Clamp(distance / 16f, 0, 10), 0.1f);

		if (Style == FLYING)
		{
			if (distance < 16)
				result *= 0.9f;
			else if (distance > 16 * 20)
				ChangeStyle(TURNING);
		}
		else if (Style == TURNING)
		{
			if (Projectile.frame == 0 && Projectile.frameCounter == 0)
				ChangeStyle(ZOOMING);

			result = Vector2.Zero;
		}
		else if (Style == ZOOMING)
		{
			if (distance < 16 * 3)
				ChangeStyle(FLYING);

			if (Main.rand.NextBool(3))
				ParticleHandler.SpawnParticle(new DragonEmber(Main.rand.NextVector2FromRectangle(Projectile.Hitbox), Projectile.velocity * Main.rand.NextFloat(0.25f), 1, 20));

			if (result.Length() < 30)
				result *= 1.1f;
		}

		if (!result.HasNaNs())
			Projectile.velocity = result;

		int direction = Math.Sign(Projectile.velocity.X);
		Projectile.direction = Projectile.spriteDirection = (result.Length() < 0.2f) ? owner.direction : direction;
		Projectile.rotation = Projectile.velocity.Y * 0.1f * Projectile.direction;
	}

	public void ChangeStyle(int style)
	{
		if (style != Style)
		{
			Style = style;
			Projectile.frame = 0;
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Rectangle source = texture.Frame(3, Main.projFrames[Type], Style, Projectile.frame, -2, -2);
		SpriteEffects effects = (Projectile.spriteDirection == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), source, Projectile.GetAlpha(lightColor), Projectile.rotation, source.Size() / 2, Projectile.scale, effects, 0);
		return false;
	}
}