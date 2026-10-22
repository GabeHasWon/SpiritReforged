using SpiritReforged.Common.Misc;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Forest.Crossbows;

public class FireworkBolt : ModItem
{
	public class FireworkBoltProjectile : ModProjectile, IDrawPixelated
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<FireworkBolt>().DisplayName;

		public override string Texture => ModContent.GetInstance<FireworkBolt>().Texture;

		private VertexTrail _trail;

		public override void SetDefaults()
		{
			Projectile.CloneDefaults(ProjectileID.WoodenArrowFriendly);
			Projectile.timeLeft = 60;
			Projectile.aiStyle = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

			if (++Projectile.ai[0] > 30)
			{
				Projectile.velocity.X *= 0.98f;
				Projectile.velocity.Y -= 0.4f;
			}

			if (Projectile.ai[0] == 50)
				SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown with { Pitch = 0.5f }, Projectile.Center);

			if (Main.rand.NextBool(3))
				Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Torch).noGravity = true;
		}

		public override void PostAI() //Update trails regardless of what PreAI returns
		{
			if (!Main.dedServ)
			{
				_trail ??= new VertexTrail(new LightColorTrail(Color.MediumVioletRed * 0.5f, Color.Transparent), new RoundCap(), new EntityTrailPosition(Projectile), new DefaultShader(), 6 * Projectile.scale, 75);
				_trail.Update();
			}
		}

		public override void OnKill(int timeLeft)
		{
			Projectile.Resize(90, 90);
			Projectile.Damage();

			if (!Main.dedServ) //Firework effects
			{
				const int dust_count = 20;
				for (int i = 0; i < dust_count; i++)
				{
					Color color = Main.hslToRgb(i / (float)dust_count, 1f, 0.5f);
					ParticleRenderers.OverPlayers.Add(new EmberParticle(Projectile.Center, Main.rand.NextVector2Circular(2, 2), color, Main.rand.NextFloat(0.5f, 1f), 40, 2));

					if (Main.rand.NextBool(3))
					{
						color = Main.hslToRgb(i / (float)dust_count, 1f, 0.8f);
						Dust.NewDustPerfect(Projectile.Center, DustID.FireworksRGB, Main.rand.NextVector2Circular(5, 5), 0, color);
					}
				}

				for (int i = 0; i < 8; i++)
				{
					Dust.NewDustPerfect(Projectile.Center, DustID.FireworksRGB, Main.rand.NextVector2Circular(4, 4));
				}

				for (int i = 0; i < 4; i++)
				{
					ParticleRenderers.UnderProjectiles.Add(new CompositeSmoke(Projectile.Center, Main.rand.NextVector2Circular(1, 1), Color.Black * 0.5f, 50));
					ParticleRenderers.OverPlayers.Add(new CompositeSmoke(Projectile.Center, Main.rand.NextVector2Circular(1, 1), Color.DarkGray * 0.5f, 50) { Variant = CompositeSmoke.SmallVariant });
				}

				ParticleRenderers.OverPlayers.Add(new ImpactLinePrim(Projectile.Center, Vector2.Zero, Main.DiscoColor.Additive(), new Vector2(2, 3), 10, 0)
				{ Rotation = MathHelper.PiOver2 });
				ParticleRenderers.OverPlayers.Add(new ImpactLinePrim(Projectile.Center, Vector2.Zero, Color.White.Additive(), new Vector2(2, 3) * 0.5f, 10, 0)
				{ Rotation = MathHelper.PiOver2 });

				SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
				SoundEngine.PlaySound(SoundID.DD2_CrystalCartImpact with { Pitch = 0.5f }, Projectile.Center);
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, new Vector2(texture.Width / 2, Projectile.height / 2), Projectile.scale, 0);

			return false;
		}

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch) => _trail?.Draw(TrailSystem.TrailShaders, Main.spriteBatch.GraphicsDevice, Matrix.Identity);
	}

	public override void SetDefaults()
    {
		Item.ammo = ModContent.ItemType<Bolt>();
		Item.maxStack = Item.CommonMaxStack;
		Item.consumable = true;
		Item.damage = 10;
		Item.knockBack = 1f;
		Item.rare = ItemRarityID.Blue;
		Item.shoot = ModContent.ProjectileType<FireworkBoltProjectile>();
	}
}