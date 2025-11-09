using SpiritReforged.Common.ProjectileCommon;
using Terraria.Audio;
using Terraria.DataStructures;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals.Glowmasks;

namespace SpiritReforged.Content.Desert.DragonFossil;

public class Dragonsong : ModItem
{
	[AutoloadGlowmask("255,255,255", false)]
	public class DragonsongHeld : ModProjectile
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<Dragonsong>().DisplayName;

		public override void SetStaticDefaults() => Main.projFrames[Type] = 4;
		public override void SetDefaults()
		{
			Projectile.ignoreWater = true;
			Projectile.tileCollide = false;
		}

		public override void AI()
		{
			int holdDistance = 18;
			float rotation = Projectile.velocity.ToRotation();
			Player owner = Main.player[Projectile.owner];
			int timeLeftMax = owner.itemAnimationMax;

			Projectile.timeLeft = Math.Min(Projectile.timeLeft, timeLeftMax);

			if (Projectile.timeLeft > timeLeftMax - 8)
				holdDistance -= 4;

			Vector2 position = owner.MountedCenter + new Vector2(holdDistance, 5 * -Projectile.direction).RotatedBy(rotation);

			Projectile.direction = Projectile.spriteDirection = owner.direction;
			Projectile.UpdateFrame(22, 3, 3);

			Projectile.Center = owner.RotatedRelativePoint(position);
			Projectile.rotation = rotation;

			owner.heldProj = Projectile.whoAmI;
			owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - 1.57f + 0.4f * owner.direction);
			owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - 1.57f + 0.4f * owner.direction);
		}

		public override bool ShouldUpdatePosition() => false;
		public override bool? CanCutTiles() => false;
		public override bool? CanDamage() => false;

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;

			var position = new Vector2((int)(Projectile.Center.X - Main.screenPosition.X), (int)(Projectile.Center.Y - Main.screenPosition.Y));
			var frame = texture.Frame(1, Main.projFrames[Type], 0, Math.Min(Projectile.frame, Main.projFrames[Type] - 1), 0, -2);
			var effects = (Projectile.spriteDirection == -1) ? SpriteEffects.FlipVertically : SpriteEffects.None;

			Main.EntitySpriteDraw(texture, position, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, frame.Size() / 2, Projectile.scale, effects);
			Main.EntitySpriteDraw(GlowmaskProjectile.ProjIdToGlowmask[Type].Glowmask.Value, position, frame, Projectile.GetAlpha(Color.White), Projectile.rotation, frame.Size() / 2, Projectile.scale, effects);
			
			return false;
		}
	}

	public class DragonFireball : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			Main.projFrames[Type] = 4;
			ProjectileID.Sets.TrailingMode[Type] = 0;
			ProjectileID.Sets.TrailCacheLength[Type] = 8;
		}

		public override void SetDefaults()
		{
			Projectile.Size = new(12);
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.friendly = true;
			Projectile.extraUpdates = 1;
			Projectile.penetrate = 3;
			Projectile.timeLeft = 40;
			Projectile.scale = Main.rand.NextFloat(0.5f, 1.5f);
		}

		public override void AI()
		{
			if (Projectile.timeLeft < 10)
				Projectile.scale *= 0.9f;
			else if (Projectile.scale < 1.5f)
				Projectile.scale += 0.025f;

			if (Main.rand.NextBool(15))
				ParticleHandler.SpawnParticle(new DragonEmber(Main.rand.NextVector2FromRectangle(Projectile.Hitbox), Projectile.velocity * Main.rand.NextFloat(0.25f), 1, 20));

			Projectile.velocity *= 0.98f;
			Projectile.rotation = Projectile.velocity.ToRotation();
			Projectile.UpdateFrame(32, 0);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			int column = (Projectile.timeLeft < 12) ? 1 : 0;
			Rectangle source = texture.Frame(2, Main.projFrames[Type], column, Projectile.frame, -2, -2);
			int length = ProjectileID.Sets.TrailCacheLength[Type];

			for (int i = 0; i < length; i++)
			{
				float progress = i / (length - 1f);
				Color color = Projectile.GetAlpha(Color.Lerp(Color.Red, Color.Gray, progress)) * (1f - progress) * 0.5f;
				Main.EntitySpriteDraw(texture, Projectile.oldPos[i] + Projectile.Size / 2 - Main.screenPosition, source, color, Projectile.rotation, source.Size() / 2, Projectile.scale * (1f - progress), default);
			}

			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, Projectile.GetAlpha(Color.White), Projectile.rotation, source.Size() / 2, Projectile.scale, default);
			return false;
		}
	}

	public override void SetStaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<PileOfConsequences>();

	public override void SetDefaults()
	{
		Item.width = 44;
		Item.height = 48;
		Item.DamageType = DamageClass.Ranged;
		Item.ammo = AmmoID.Gel;
		Item.damage = 14;
		Item.knockBack = 3;
		Item.useTime = Item.useAnimation = 20;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.value = Item.sellPrice(0, 1, 0, 0);
		Item.rare = ItemRarityID.Blue;
		Item.autoReuse = true;
		Item.shoot = ModContent.ProjectileType<DragonFireball>();
		Item.shootSpeed = 8f;
		Item.UseSound = SoundID.DD2_BallistaTowerShot with { Pitch = 0.9f, Volume = 0.5f };
	}

	public override bool CanConsumeAmmo(Item ammo, Player player) => Main.rand.NextFloat() >= 0.5f;

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		const int muzzleLength = 50;

		int heldType = ModContent.ProjectileType<DragonsongHeld>();
		if (player.ownedProjectileCounts[heldType] == 0)
			Projectile.NewProjectile(source, position, Vector2.Normalize(velocity), heldType, 0, 0, player.whoAmI);

		SoundEngine.PlaySound(SoundID.DD2_FlameburstTowerShot with { Pitch = 0.5f }, position);
		Vector2 muzzleOffset = Vector2.Normalize(velocity) * muzzleLength;

		if (Collision.CanHit(position, 2, 2, position + muzzleOffset, 2, 2))
			position += muzzleOffset;

		for (int i = 0; i < 3; i++)
			Projectile.NewProjectile(source, position, (velocity * Main.rand.NextFloat(0.5f, 1)).RotatedByRandom(0.5f), type, damage, knockback, player.whoAmI);

		return false;
	}

	public override void OnCreated(ItemCreationContext context)
	{
		if (context is RecipeItemCreationContext)
		{
			SoundEngine.PlaySound(SoundID.NPCHit2);
			SoundEngine.PlaySound(SoundID.DD2_SkeletonDeath);

			for (int i = 0; i < 4; i++)
				TerrariaParticles.OverInventory.Add(new DragonBoneParticle(i)
				{
					LocalPosition = Main.MouseScreen,
					Scale = Vector2.One,
					Velocity = (Vector2.UnitY * -Main.rand.NextFloat(2)).RotatedByRandom(1),
					AccelerationPerFrame = Vector2.UnitY * 0.1f,
					ScaleVelocity = -new Vector2(0.01f),
					RotationVelocity = 0.05f
				});
		}
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<LittleDragon>()).Register();
}