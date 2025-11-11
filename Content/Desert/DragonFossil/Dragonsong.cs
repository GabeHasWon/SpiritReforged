using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.DragonFossil;

public class Dragonsong : ModItem
{
	[AutoloadGlowmask("255,255,255", false)]
	public class DragonsongHeld : ModProjectile
	{
		public ref float Counter => ref Projectile.ai[0];
		private Vector2 MuzzlePosition => Projectile.Center + Vector2.Normalize(Projectile.velocity) * 30;

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

			if (owner.channel)
			{
				Projectile.timeLeft = timeLeftMax;
				owner.itemAnimation = owner.itemTime = timeLeftMax;
				int rate = Math.Max(timeLeftMax - (int)(Counter / timeLeftMax * 5), 8);

				if (Counter++ % rate == 0)
				{
					Projectile.frame = 0;

					if (owner.whoAmI == Main.myPlayer)
					{
						Vector2 oldVelocity = Projectile.velocity;
						Projectile.velocity = owner.DirectionTo(Main.MouseWorld) * oldVelocity.Length();

						if (Projectile.velocity != oldVelocity)
							Projectile.netUpdate = true;

						Shoot(Projectile.GetSource_FromAI(), Projectile.velocity, Projectile.Center, owner);

						if (!Main.dedServ)
							ParticleHandler.SpawnParticle(new SmokeCloud(MuzzlePosition, Vector2.UnitY * -Main.rand.NextFloat(3, 5), Color.DarkSlateGray, 0.05f, Common.Easing.EaseFunction.EaseCircularOut, 30)
							{
								TertiaryColor = Color.PaleVioletRed,
								Pixellate = true,
								PixelDivisor = 3,
								Layer = ParticleLayer.AbovePlayer
							});
					}
				}
			}
			else if (Main.rand.NextBool())
			{
				ParticleHandler.SpawnParticle(new SmokeCloud(MuzzlePosition, Vector2.UnitY * -Main.rand.NextFloat(3, 5), Color.DarkSlateGray, 0.05f, Common.Easing.EaseFunction.EaseCircularOut, 30)
				{
					TertiaryColor = Color.PaleVioletRed,
					Pixellate = true,
					PixelDivisor = 3,
					Layer = ParticleLayer.AbovePlayer
				});
			}

			Projectile.UpdateFrame(10, 3);

			Vector2 position = owner.MountedCenter + new Vector2(holdDistance, 5 * -Projectile.direction).RotatedBy(rotation);

			owner.direction = Projectile.direction = Projectile.spriteDirection = Math.Sign(Projectile.velocity.X);
			Projectile.Center = owner.RotatedRelativePoint(position);
			Projectile.rotation = rotation;

			owner.heldProj = Projectile.whoAmI;
			owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - 1.57f + 0.4f * owner.direction);
			owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - 1.57f + 0.4f * owner.direction);
		}

		private void Shoot(IEntitySource source, Vector2 velocity, Vector2 position, Player player)
		{
			const int muzzleLength = 50;

			SoundEngine.PlaySound(SoundID.DD2_FlameburstTowerShot with { Pitch = 0.5f }, position);
			Vector2 muzzleOffset = Vector2.Normalize(velocity) * muzzleLength;

			if (Collision.CanHit(position, 2, 2, position + muzzleOffset, 2, 2))
				position += muzzleOffset;

			player.PickAmmo(player.HeldItem, out int type, out _, out _, out _, out _);

			for (int i = 0; i < Main.rand.Next(3, 6); i++)
				Projectile.NewProjectile(source, position, (velocity * Main.rand.NextFloat(0.5f, 1)).RotatedByRandom(0.5f), type, Projectile.damage, Projectile.knockBack, player.whoAmI);
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

			Texture2D star = TextureAssets.Projectile[ProjectileID.RainbowRodBullet].Value;
			float intensity = 1f - Projectile.frame / (Main.projFrames[Type] - 1f);
			float opacity = Math.Min(Counter / 200f * intensity, 1);
			Vector2 starScale = new Vector2(1, 0.5f) * intensity;

			Main.EntitySpriteDraw(star, MuzzlePosition - Main.screenPosition, null, Color.OrangeRed.Additive() * opacity, 0, star.Size() / 2, starScale, default);
			Main.EntitySpriteDraw(star, MuzzlePosition - Main.screenPosition, null, Color.White.Additive() * opacity, 0, star.Size() / 2, starScale * 0.5f, default);

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
			ProjectileID.Sets.DontCancelChannelOnKill[Type] = true;
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
		Item.useAmmo = AmmoID.Gel;
		Item.damage = 18;
		Item.knockBack = 3;
		Item.useTime = Item.useAnimation = 35;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.value = Item.sellPrice(0, 1, 0, 0);
		Item.rare = ItemRarityID.Blue;
		Item.shoot = ModContent.ProjectileType<DragonFireball>();
		Item.shootSpeed = 8f;
		Item.autoReuse = true;
		Item.channel = true;
		Item.UseSound = SoundID.DD2_BallistaTowerShot with { Pitch = 0.9f, Volume = 0.5f };
	}

	public override bool CanConsumeAmmo(Item ammo, Player player) => Main.rand.NextFloat() >= 0.5f;

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<DragonsongHeld>(), damage, knockback, player.whoAmI);
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
					AccelerationPerFrame = Vector2.UnitY * 0.08f,
					ScaleVelocity = -new Vector2(0.005f),
					RotationVelocity = 0.04f
				});
		}
	}

	public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<LittleDragon>()).Register();
}