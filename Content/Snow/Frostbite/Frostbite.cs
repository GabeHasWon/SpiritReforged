using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Snow.Frostbite;

public class FrostbiteItem : ModItem
{
	public override void SetDefaults()
	{
		Item.damage = 7;
		Item.noMelee = true;
		Item.DamageType = DamageClass.Magic;
		Item.width = 64;
		Item.height = 64;
		Item.useTime = 30;
		Item.mana = 24;
		Item.useAnimation = 30;
		Item.useStyle = ItemUseStyleID.HiddenAnimation;
		Item.knockBack = 0;
		Item.value = Item.sellPrice(0, 0, 5, 0);
		Item.rare = ItemRarityID.Blue;
		Item.noUseGraphic = true;
		Item.autoReuse = true;
		Item.channel = true;
		Item.UseSound = SoundID.Item20;
		Item.shoot = ModContent.ProjectileType<FrostbiteProj>();
	}

	public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback) => position = Main.MouseWorld;

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		int heldType = ModContent.ProjectileType<FrostbiteHeldOut>();
		if (player.ownedProjectileCounts[heldType] < 1)
			Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, heldType, damage, knockback, player.whoAmI);

		return true;
	}
}

public class FrostbiteHeldOut : ModProjectile
{
	public override void SetStaticDefaults() => Main.projFrames[Type] = 4;

	public override void SetDefaults()
	{
		Projectile.Size = new Vector2(20);
		Projectile.penetrate = -1;
		Projectile.friendly = true;
		Projectile.hostile = false;
		Projectile.ignoreWater = true;
		Projectile.tileCollide = false;
	}

	public override void AI()
	{
		var owner = Main.player[Projectile.owner];

		if (Projectile.timeLeft > 2 && !Main.dedServ) //The projectile just spawned; play a sound for all clients
			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/PageFlip") with { PitchVariance = 0.3f, Volume = 0.65f }, owner.Center);

		owner.itemRotation = MathHelper.WrapAngle(Projectile.velocity.ToRotation() + (Projectile.direction < 0 ? MathHelper.Pi : 0));
		owner.heldProj = Projectile.whoAmI;

		Projectile.direction = Projectile.spriteDirection = owner.direction;
		Projectile.rotation = .4f * owner.direction;
		Projectile.Center = owner.RotatedRelativePoint(owner.MountedCenter + new Vector2(owner.direction * 14, 4));

		float rotation = Projectile.rotation - 1.57f * Projectile.direction;
		owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.ThreeQuarters, rotation);
		owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, rotation);

		if (Main.rand.NextBool(10))
		{
			var dust = Dust.NewDustDirect(Projectile.position - new Vector2(0, Projectile.height / 2), Projectile.width, Projectile.height, DustID.GemSapphire);
			dust.noGravity = true;
			dust.velocity = new Vector2(0, -Main.rand.NextFloat(2f));
		}

		if (++Projectile.frameCounter >= 4)
		{
			Projectile.frameCounter = 0;
			if (Projectile.frame < Main.projFrames[Type] - 1)
				Projectile.frame++;
		}

		if (owner.channel)
			Projectile.timeLeft = 2;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Rectangle drawFrame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame, 0, -2);
		SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), drawFrame, Projectile.GetAlpha(lightColor), Projectile.rotation, drawFrame.Size() / 2, Projectile.scale, effects, 0);
		return false;
	}

	public override bool? CanDamage() => false;
	public override bool? CanCutTiles() => false;
}
