using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Forest.Crossbows;

public class Drillbolt : ModItem
{
	public class DrillboltProjectile : ModProjectile, IDrawPixelated
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<Drillbolt>().DisplayName;

		public override string Texture => ModContent.GetInstance<Drillbolt>().Texture;

		private VertexTrail _trail;
		private MotionNoiseCone _noiseCone;

		private int _hitBuffer;
		private bool _colliding;

		public override void SetStaticDefaults() => ProjectileID.Sets.DontAttachHideToAlpha[Type] = true;

		public override void SetDefaults()
		{
			Projectile.CloneDefaults(ProjectileID.WoodenArrowFriendly);
			Projectile.extraUpdates = 1;
			Projectile.tileCollide = false;
			Projectile.penetrate = 3;
			Projectile.aiStyle = -1;
			Projectile.hide = true;

			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

			if (!Main.dedServ)
			{
				_trail ??= new VertexTrail(new LightColorTrail(Color.LightGray * 0.5f, Color.Transparent), new RoundCap(), new EntityTrailPosition(Projectile), new DefaultShader(), 6 * Projectile.scale, 75);
				_trail.Update();

				_noiseCone ??= (BasicNoiseCone)new BasicNoiseCone(Projectile.Center, Projectile.velocity * 3, 40, new(50, 50)).SetColors(Color.White, Color.DarkGray).SetIntensity(2).AttachTo(Projectile);
				_noiseCone.Rotation = Projectile.velocity.ToRotation();
				_noiseCone.Update();

				if (++_noiseCone.TimeActive > _noiseCone.MaxTime)
					_noiseCone.Kill();
			}

			if (Projectile.ai[0] > 120)
				Projectile.tileCollide = true; //Die on collision

			if (Projectile.ai[0] > 60)
				Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.4f, 10);

			if (Projectile.ai[0] > 40)
				Projectile.velocity *= 0.99f;

			Projectile.ai[0]++;

			bool colliding = Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height);
			if (colliding != _colliding) //Started or stopped active collision
			{
				Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);

				if (Framing.GetTileSafely(Projectile.Center + Projectile.velocity) is Tile tile && WorldGen.SolidOrSlopedTile(tile))
				{
					ParticleHandler.SpawnParticle(new CompositeSmoke(Projectile.Center, -Vector2.UnitY, Color.Lerp(TileMaterial.FindMaterial(tile.TileType).Color, Color.Black, 0.2f), 30, false, false));
					ParticleHandler.SpawnParticle(new SmallCompositeSmoke(Projectile.Center, -Vector2.UnitY, TileMaterial.FindMaterial(tile.TileType).Color, 30, false, false));
					ParticleHandler.SpawnParticle(new SmallCompositeSmoke(Projectile.Center, Vector2.UnitY * -Main.rand.NextFloat(1.2f), TileMaterial.FindMaterial(tile.TileType).Color, 40, false, false));
				}

				SoundEngine.PlaySound(SoundID.WormDig, Projectile.Center);
				SoundEngine.PlaySound(SoundID.WormDigQuiet, Projectile.Center);
			}

			if (_hitBuffer > 0)
				Projectile.position -= Projectile.velocity * 0.8f; //Slow down after collision

			if (colliding)
				Projectile.position -= Projectile.velocity * 0.5f; //Slow down while colliding

			_colliding = colliding;
			_hitBuffer--;
		}

		public override void OnKill(int timeLeft) => base.OnKill(timeLeft);

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			_hitBuffer = 10;
			for (int i = 0; i < 5; i++)
				ParticleHandler.SpawnParticle(new EmberParticle(Projectile.Center, (Projectile.velocity * Main.rand.NextFloat(0.01f, 0.5f)).RotatedByRandom(0.1f), Color.Yellow, 0.3f, 20, 3));
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, new Vector2(texture.Width / 2, Projectile.height / 2), Projectile.scale, 0);

			return true;
		}

		public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => behindNPCsAndTiles.Add(index);

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch)
		{
			_trail?.Draw(TrailSystem.TrailShaders, Main.spriteBatch.GraphicsDevice, Matrix.Identity);
			_noiseCone?.CustomDraw(spriteBatch);
		}
	}

	public override void SetDefaults()
    {
		Item.ammo = ModContent.ItemType<Bolt>();
		Item.maxStack = Item.CommonMaxStack;
		Item.consumable = true;
		Item.damage = 10;
		Item.knockBack = 1f;
		Item.rare = ItemRarityID.Blue;
		Item.shoot = ModContent.ProjectileType<DrillboltProjectile>();
	}
}