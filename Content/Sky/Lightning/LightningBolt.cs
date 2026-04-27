using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Items.BigBombs;

namespace SpiritReforged.Content.Sky.Lightning;

public class LightningBolt : ModItem
{
	public readonly struct LightningTrailPosition(Func<Vector2> position) : ITrailPosition
	{
		public Vector2 GetNextTrailPosition() => position.Invoke();
	}

	public class LightningChain
	{
		public readonly Vector2 start;
		public readonly Vector2 end;
		public readonly int width;
		public readonly Color color;

		private VertexTrail[] _trails;
		private Vector2 _floatingPosition;

		public LightningChain(Vector2 start, Vector2 end, Color color, int width)
		{
			this.start = start;
			this.end = end;
			this.width = width;
			this.color = color;

			Reconfigure();
		}

		public void Reconfigure()
		{
			_floatingPosition = start;

			ITrailShader shader = new ImageShader(AssetLoader.LoadedTextures["GlowTrail"].Value, Vector2.One);
			ITrailPosition position = new LightningTrailPosition(() => _floatingPosition);

			float angle = start.AngleTo(end);
			float fullLength = start.Distance(end);

			_trails =
				[
				new(new StandardColorTrail(color), new TriangleCap(), position, shader, width, fullLength),
				new(new StandardColorTrail(Color.White.Additive()), new TriangleCap(), position, shader, width / 2, fullLength)
				];

			float slice = fullLength / 6;
			int div = (int)(fullLength / slice);

			for (int i = 0; i < div; i++)
			{
				float deviation = Main.rand.NextFloat(0.3f, 0.6f) * Main.rand.NextFromList(-1, 1);
				float halfSlice = slice / 2;

				_floatingPosition += new Vector2(halfSlice, halfSlice * deviation).RotatedBy(angle);

				foreach (var trail in _trails)
					trail.Update();

				_floatingPosition += new Vector2(halfSlice, -(halfSlice * deviation)).RotatedBy(angle);

				foreach (var trail in _trails)
					trail.Update();
			}
		}

		public void Update()
		{
			foreach (var trail in _trails)
			{
				trail.Update();
				trail.Dissolve();
			}
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			foreach (var trail in _trails)
				trail.Draw(TrailSystem.TrailShaders, AssetLoader.BasicShaderEffect, spriteBatch.GraphicsDevice);
		}
	}

	public class LightningBoltProj : ModProjectile
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
					var ease = Bomb.EffectEase;
					var stretch = Vector2.One;
					float angle = Main.rand.NextFloat(MathHelper.Pi);

					ParticleHandler.SpawnParticle(new TexturedPulseCircle(Projectile.Center, Color.Goldenrod.Additive(), Color.OrangeRed.Additive(), 1f, 80, 20, "Smoke", stretch, ease).WithSkew(0.5f, angle));
					ParticleHandler.SpawnParticle(new TexturedPulseCircle(Projectile.Center, Color.White.Additive(), Color.OrangeRed.Additive(), 0.5f, 80, 20, "Smoke", stretch, ease).WithSkew(0.5f, angle));

					for (int i = 0; i < 8; i++)
						ParticleHandler.SpawnParticle(new EmberParticle(Projectile.Center, Main.rand.NextVector2Circular(2, 2), Color.Goldenrod, 0.2f, 20, 8));

					_chain = new(Projectile.Center - Vector2.UnitY * 500, Projectile.Center, Color.Goldenrod.Additive(), 50);

					Point tilePosition = Projectile.Center.ToTileCoordinates();
					
					for (int i = 0; i < 5; i++)
					{
						int dustWhoAmI = WorldGen.KillTile_MakeTileDust(tilePosition.X, tilePosition.Y, Framing.GetTileSafely(tilePosition));
						Main.dust[dustWhoAmI].noGravity = true;
					}
				}

				_chain.Update();
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			_chain?.Draw(Main.spriteBatch);
			Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
			float scale = Projectile.timeLeft / 20f * 0.5f * Projectile.scale;

			Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, Color.Goldenrod.Additive(), 0, bloom.Size() / 2, scale, 0);
			Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, Color.White.Additive(), 0, bloom.Size() / 2, scale * 0.5f, 0);

			return false;
		}

		public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
		{ }
	}

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
		Item.useStyle = ItemUseStyleID.HiddenAnimation;
		Item.value = Item.sellPrice(0, 0, 50, 0);
		Item.rare = ItemRarityID.Blue;
		Item.UseSound = SoundID.Item20;
		Item.mana = 4;
		Item.shootSpeed = 0;
		Item.shoot = ModContent.ProjectileType<LightningBoltProj>();
	}

	public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
	{
		player.FindSentryRestingSpot(type, out int x, out int y, out _);
		position = new Vector2(x, y);
		float collisionPoint = 0;

		foreach (NPC npc in Main.ActiveNPCs)
		{
			if ((npc.type == NPCID.TargetDummy || npc.CanBeChasedBy()) && Collision.CheckAABBvLineCollision(npc.position, npc.Size, position, position - new Vector2(0, 800), 14, ref collisionPoint))
			{
				position = npc.Top;
				break;
			}
		}
	}
}