using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Mage;

[AutoloadGlowmask("255,255,255")]
public class Sparkler : ModItem
{
	public class SparkleStar : ModProjectile, IDrawPixelated
	{
		public ref float Counter => ref Projectile.ai[0];

		public override string Texture => AssetLoader.EmptyTexture;

		private NPC _target;
		private Vector2 _initialVelocity;
		private VertexTrail _trail;

		public override void SetDefaults()
		{
			Projectile.Size = new(16);
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
		}

		public override void AI()
		{
			const int slow_duration = 20;

			if (_initialVelocity == Vector2.Zero)
			{
				_initialVelocity = Projectile.velocity;
				Projectile.velocity = Main.rand.NextVector2Circular(8, 8) * Main.rand.NextFloat(0.8f, 1);

				if (!Main.dedServ)
				{
					_trail = new VertexTrail(new GradientTrail(Color.White, Color.Yellow), new NoCap(), new EntityTrailPosition(Projectile), new DefaultShader(), 8, 50);

					ParticleHandler.SpawnParticle(new PulseCircle(Projectile.Center, Color.Goldenrod.Additive(100), 0.2f, 80, 15));
					ParticleHandler.SpawnParticle(new SharpStarParticle(Projectile.Center, Vector2.Zero, Color.Goldenrod.Additive(100), 0.5f, 20, 0));
				}
			} //Just spawned

			if (++Counter > slow_duration)
			{
				if (_target == null || !_target.active)
				{
					const int max_distance = 500;
					bool foundTarget = false;

					foreach (NPC npc in Main.ActiveNPCs)
					{
						if (npc.CanBeChasedBy() && npc.DistanceSQ(Projectile.Center) < max_distance * max_distance)
						{
							_target = npc;
							foundTarget = true;

							break;
						}
					}

					if (!foundTarget)
					{
						Projectile.velocity = Vector2.Lerp(Projectile.velocity, _initialVelocity, 0.1f);
					}
				}
				else
				{
					Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(_target.Center) * _initialVelocity.Length(), 0.02f);
				}
			}
			else
			{
				Projectile.velocity *= 0.95f;
			}

			if (!Main.dedServ)
				_trail?.Update();
		}

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 5; i++)
				Dust.NewDustPerfect(Projectile.Center + Projectile.velocity, DustID.YellowStarDust, Main.rand.NextVector2Circular(2, 2) * Main.rand.NextFloat(0.9f, 1f), 0, Color.White.Additive());
		}

		public override bool PreDraw(ref Color lightColor) => false;

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch)
		{
			Texture2D starTexture = AssetLoader.LoadedTextures["Star"].Value;
			Texture2D bloomTexture = AssetLoader.LoadedTextures["Bloom"].Value;
			Vector2 position = Projectile.Center - Projectile.velocity - Main.screenPosition;

			_trail?.Draw(TrailSystem.TrailShaders, spriteBatch.GraphicsDevice, Matrix.Identity);
			IDrawPixelated.PixelateDrawPosition(ref position);

			spriteBatch.Draw(bloomTexture, position, null, Color.Goldenrod.Additive() * 0.3f, 0, bloomTexture.Size() / 2, Projectile.scale * 0.15f, SpriteEffects.None, 0);
			spriteBatch.Draw(starTexture, position, null, Color.Goldenrod.Additive(), Projectile.rotation, starTexture.Size() / 2, Projectile.scale * 0.1f, SpriteEffects.None, 0);
		}
	}

	public override void SetStaticDefaults() => Item.staff[Type] = true;

	public override void SetDefaults()
	{
		Item.damage = 15;
		Item.mana = 10;
		Item.knockBack = 6.5f;
		Item.width = Item.height = 46;
		Item.useTime = Item.useAnimation = 18;
		Item.DamageType = DamageClass.Magic;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.Blue;
		Item.UseSound = SoundID.DD2_BookStaffCast with { Pitch = 0.3f };
		Item.shoot = ModContent.ProjectileType<SparkleStar>();
		Item.shootSpeed = 14f;
		Item.autoReuse = true;
		Item.noMelee = true;
	}

	public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
	{
		Vector2 offset = Vector2.Normalize(velocity) * 60;

		if (Collision.CanHit(position, 2, 2, position + velocity, 2, 2))
			position += offset;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		for (int i = 0; i < 3; i++)
			Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);

		return false;
	}
}