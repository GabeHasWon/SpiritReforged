using Terraria.DataStructures;

namespace SpiritReforged.Content.Ocean.Items.LadyLuck;

public class LadyLuck : ModItem
{
	public override void SetStaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<DuelistLegacy.DuelistLegacy>();

	public override void SetDefaults()
	{
		Item.damage = 34;
		Item.DamageType = DamageClass.Ranged;
		Item.width = 24;
		Item.height = 24;
		Item.useTime = 9;
		Item.useAnimation = 9;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.noMelee = true;
		Item.knockBack = 0;
		Item.rare = ItemRarityID.LightRed;
		Item.UseSound = SoundID.Item41;
		Item.shoot = ModContent.ProjectileType<LadyLuckProj>();
		Item.shootSpeed = 12f;
		Item.useAmmo = AmmoID.Bullet;
		Item.value = Item.sellPrice(0, 2, 20, 0);
		Item.autoReuse = true;
	}

	public override Vector2? HoldoutOffset() => new Vector2(-8, 0);

	public override bool AltFunctionUse(Player player) => true;

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		var direction = velocity;

		if (player.altFunctionUse == 2)
		{
			Projectile.NewProjectile(source, position, direction * 1.2f, Item.shoot, 0, knockback, player.whoAmI);
			return false;
		}
		else
		{
			Projectile.NewProjectileDirect(source, position + direction * 2.0f, direction, ModContent.ProjectileType<LadyLuckFlash>(), 0, 0, player.whoAmI).frame = Main.rand.Next(0, 3);

			var proj = Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, player.whoAmI);
			proj.GetGlobalProjectile<LadyLuckGlobalProjectile>().shotFromGun = true;
		}

		return false;
	}

	public override bool CanUseItem(Player player)
	{
		if (player.altFunctionUse == 2)
		{
			if (player.ownedProjectileCounts[ModContent.ProjectileType<LadyLuckProj>()] > 0)
				return false;

			Item.shootSpeed = 9f;
			Item.useTime = 16;
			Item.useAnimation = 16;
			Item.UseSound = SoundID.Item1;
			Item.noUseGraphic = true;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useAmmo = 0;
			Item.autoReuse = false;
		}
		else
		{
			Item.shootSpeed = 16f;
			Item.useTime = 9;
			Item.useAnimation = 9;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.noUseGraphic = false;
			Item.useAmmo = AmmoID.Bullet;
			Item.UseSound = SoundID.Item41;
			Item.autoReuse = true;
		}

		return true;
	}

	public override bool? UseItem(Player player)
	{
		if (Main.netMode != NetmodeID.Server)
			Gore.NewGore(Item.GetSource_FromThis(), player.Center, new Vector2(player.direction * -1, -0.5f) * 4, Mod.Find<ModGore>("BulletCasing").Type, 1f);

		return base.UseItem(player);
	}
}

internal class LadyLuckFlash : ModProjectile
{
	protected readonly Color color = Color.White;

	int direction = 0;

	public override void SetStaticDefaults() => Main.projFrames[Projectile.type] = 3;

	public override void SetDefaults()
	{
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Ranged;
		Projectile.tileCollide = false;
		Projectile.Size = new Vector2(20, 10);
		Projectile.penetrate = -1;
		Projectile.timeLeft = 3;
	}

	public override void AI()
	{
		if (Projectile.velocity != Vector2.Zero)
		{
			direction = Math.Sign(Projectile.velocity.X);
			Projectile.rotation = Projectile.velocity.ToRotation();
		}

		Projectile.velocity = Vector2.Zero;
		CreateParticles();
	}

	protected virtual void CreateParticles()
	{
		Vector2 lineDirection = Projectile.rotation.ToRotationVector2() * (Projectile.width * 0.7f);
		Vector2 lineOffshoot = (Projectile.rotation + 1.57f).ToRotationVector2() * Projectile.height * 0.3f;
		for (int i = 0; i < 3; i++)
		{
			Vector2 position = Projectile.Center + lineDirection * Main.rand.NextFloat() + lineOffshoot * Main.rand.NextFloat(-1f, 1f);
			Dust.NewDustPerfect(position, 6, Main.rand.NextVector2Circular(1, 1) + (Projectile.rotation + Main.rand.NextFloat(-0.35f, 0.35f)).ToRotationVector2() * 5, 0, default, 1.3f).noGravity = true;
		}
	}

	public override Color? GetAlpha(Color lightColor) => Color.White * .8f;

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
		int frameHeight = tex.Height / Main.projFrames[Projectile.type];
		var frame = new Rectangle(0, frameHeight * Projectile.frame, tex.Width, frameHeight);
		if (direction == 1)
			Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, frame, color * .8f, Projectile.rotation, new Vector2(0, frameHeight / 2), Projectile.scale, SpriteEffects.None, 0f);
		else
			Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, frame, color * .8f, Projectile.rotation + 3.14f, new Vector2(tex.Width, frameHeight / 2), Projectile.scale, SpriteEffects.FlipHorizontally, 0f);
		return false;
	}
}

public class LadyLuckGlobalProjectile : GlobalProjectile
{
	public override bool InstancePerEntity => true;

	public bool shotFromGun = false;
	public bool hit = false;
	public NPC target;
	public float initialVel = 0f;

	public override void AI(Projectile projectile)
	{
		if (!hit)
			return;

		if (!target.active)
		{
			hit = false;
			return;
		}

		projectile.velocity = Vector2.Lerp(projectile.velocity, projectile.DirectionTo(target.Center) * initialVel, 0.1f);
	}
}