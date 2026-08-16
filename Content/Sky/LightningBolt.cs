using SpiritReforged.Common;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;

namespace SpiritReforged.Content.Sky;

public class LightningBolt : ModItem
{
	public sealed class LightningBoltProj : ModProjectile, IDrawPixelated
	{
		public override string Texture => AssetLoader.EmptyTexture;

		private LightningChain _chain;

		public override void SetDefaults()
		{
			Projectile.Size = new(100);
			Projectile.friendly = true;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 20;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override void AI()
		{
			if (!Main.dedServ)
			{
				bool justSpawned = _chain == null;

				if (justSpawned)
				{
					EaseFunction ease = EaseFunction.EaseCubicOut;
					Vector2 stretch = Vector2.One;
					Player owner = Main.player[Projectile.owner];
					Vector2 velocity = owner.DirectionTo(Projectile.Center);
					float angle = Main.rand.NextFloat(MathHelper.Pi);

					ParticleHandler.SpawnParticle(new TexturedPulseCircle(Projectile.Center, Color.Goldenrod.Additive(), Color.OrangeRed.Additive(), 1f, 80, 20, "Smoke", stretch, ease).WithSkew(0.5f, angle));
					ParticleHandler.SpawnParticle(new TexturedPulseCircle(Projectile.Center, Color.White.Additive(), Color.OrangeRed.Additive(), 0.5f, 80, 20, "Smoke", stretch, ease).WithSkew(0.5f, angle));

					for (int i = 0; i < 8; i++)
						ParticleHandler.SpawnParticle(new EmberParticle(Vector2.Lerp(owner.Center, Projectile.Center, Main.rand.NextFloat()) + Main.rand.NextVector2Circular(8, 8), velocity * Main.rand.NextFloat(2), Color.Transparent, Color.OrangeRed, Main.rand.NextFloat(0.2f, 0.5f), Main.rand.Next(20, 60), 2));

					for (int i = 0; i < 8; i++)
						ParticleHandler.SpawnParticle(new EmberParticle(Projectile.Center, Main.rand.NextVector2Circular(2, 2), Color.Goldenrod, 0.2f, 20, 8));

					_chain = new(owner.Center, Projectile.Center, Color.Goldenrod.Additive(), 50);

					Point tilePosition = Projectile.Center.ToTileCoordinates();
					if (WorldGen.SolidTile(tilePosition))
					{
						for (int i = 0; i < 5; i++)
						{
							int dustWhoAmI = WorldGen.KillTile_MakeTileDust(tilePosition.X, tilePosition.Y, Framing.GetTileSafely(tilePosition));
							Main.dust[dustWhoAmI].noGravity = true;
						}
					}
				}

				_chain.Update();
			}
		}

		public override bool PreDraw(ref Color lightColor) => false;

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch)
		{
			_chain?.Draw(Main.spriteBatch, Matrix.Identity);
			Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
			float scale = Projectile.timeLeft / 20f * 0.25f * Projectile.scale;
			Vector2 position = Projectile.Center - Main.screenPosition;

			IDrawPixelated.PixelateDrawPosition(ref position);

			Main.EntitySpriteDraw(bloom, position, null, Color.Goldenrod.Additive(), 0, bloom.Size() / 2, scale, 0);
			Main.EntitySpriteDraw(bloom, position, null, Color.White.Additive(), 0, bloom.Size() / 2, scale * 0.5f, 0);
		}
	}

	public override void SetStaticDefaults() => SpiritSets.MagicBook[Type] = true;

	public override void SetDefaults()
	{
		Item.width = Item.height = 24;
		Item.damage = 9;
		Item.ArmorPenetration = 10;
		Item.knockBack = 0;
		Item.DamageType = DamageClass.Magic;
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.autoReuse = true;
		Item.channel = true;
		Item.useTime = Item.useAnimation = 30;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.value = Item.sellPrice(0, 0, 50, 0);
		Item.rare = ItemRarityID.Blue;
		Item.UseSound = SoundID.Item20;
		Item.mana = 4;
		Item.shootSpeed = 1;
		Item.shoot = ModContent.ProjectileType<LightningBoltProj>();
	}

	public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
	{
		float collisionPoint = 0;
		float lastDistance = -1;

		Vector2 start = player.Center;
		Vector2 end = start + velocity * 500;

		foreach (NPC npc in Main.ActiveNPCs)
		{
			if ((npc.type == NPCID.TargetDummy || npc.CanBeChasedBy()) && Collision.CheckAABBvLineCollision(npc.position, npc.Size, start, end, 14, ref collisionPoint))
			{
				var lerpPosition = Vector2.Lerp(start, end, collisionPoint / start.Distance(end));
				float currentDistance = lerpPosition.Distance(start);

				if (lastDistance == -1 || currentDistance < lastDistance)
				{
					position = lerpPosition;
					lastDistance = currentDistance;
				}
			}
		}

		velocity = Vector2.Zero;
	}
}