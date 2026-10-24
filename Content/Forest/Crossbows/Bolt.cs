using SpiritReforged.Common;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Content.Forest.Crossbows;

[ReinitializeDuringResizeArrays]
public class BoltCache : ModSystem
{
	/// <summary>Whether the given projectile type is a crossbow bolt.</summary>
	public static readonly bool[] IsCrossbowBolt = SpiritSets.ProjectileFactory.CreateBoolSet();

	public override void PostSetupContent()
	{
		foreach (int type in ContentSamples.ItemsByType.Keys) //Automatically populate crossbow bolt types by association of ammo and Item.shoot
		{
			Item item = ContentSamples.ItemsByType[type];
			if (item.ammo == ModContent.ItemType<Bolt>())
				IsCrossbowBolt[item.shoot] = true;
		}
	}
}

public class Bolt : ModItem
{
	public class BoltProjectile : ModProjectile, IDrawPixelated
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<Bolt>().DisplayName;

		public override string Texture => ModContent.GetInstance<Bolt>().Texture;

		/// <summary>Whether this projectile is embeded into tiles or <see cref="Target"/>.</summary>
		public bool Stationary => _tileColliding || Target != null;

		public NPC Target
		{
			get
			{
				if (_targetIndex >= 0 && Main.npc[_targetIndex] is NPC npc && npc.active)
					return npc;

				return null;
			}
			set
			{
				_targetIndex = value.whoAmI;
			}
		}

		private VertexTrail _trail;
		private bool _tileColliding;
		private int _targetIndex = -1;

		public override void SetStaticDefaults() => ProjectileID.Sets.DontAttachHideToAlpha[Type] = true;

		public override void SetDefaults()
		{
			Projectile.CloneDefaults(ProjectileID.WoodenArrowFriendly);
			Projectile.extraUpdates = 1;
			Projectile.aiStyle = -1;
			Projectile.penetrate = 1;
		}

		public override void AI()
		{
			bool wasTileColliding = _tileColliding;
			_tileColliding = Collision.SolidCollision(Projectile.position - new Vector2(2), Projectile.width + 4, Projectile.height + 4);

			if (_tileColliding) //Struck tiles
			{
				Projectile.hide = true;

				if (!wasTileColliding) //Just started colliding
					Projectile.position += Projectile.velocity;
			}
			else if (Target != null) //Has a target
			{
				Projectile.Center = Target.Center + Projectile.velocity;
				Projectile.gfxOffY = Target.gfxOffY;
				Projectile.hide = true;
				Projectile.timeLeft = 2; //Ensure the projectile dies after detaching
			}
			else
			{
				if (Projectile.ai[0] > 60)
					Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.4f, 10);

				if (Projectile.ai[0] > 40)
					Projectile.velocity *= 0.99f;

				Projectile.ai[0]++;
				Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
			}
		}

		public override void PostAI() //Update trails regardless of what PreAI returns
		{
			if (!Main.dedServ)
			{
				if (Stationary)
				{
					_trail.Dissolve();
				}
				else
				{
					_trail ??= new VertexTrail(new LightColorTrail(Color.White * 0.5f, Color.Transparent), new TriangleCap(), new EntityTrailPosition(Projectile), new DefaultShader(), 6 * Projectile.scale, 75);
				}

				_trail.Update();
			}
		}

		public override bool ShouldUpdatePosition() => !Stationary;

		public override bool? CanDamage() => Stationary ? false : null;

		public override bool OnTileCollide(Vector2 oldVelocity) => false;

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Projectile.penetrate - 1 == 0 && Target == null) //Set a target
			{
				Target = target;
				Projectile.velocity = Projectile.Center - target.Center;
				Projectile.penetrate++;
				Projectile.timeLeft = 2;

				Projectile.netUpdate = true;
				OnKill(Projectile.timeLeft);
			}

			Main.NewText(Projectile.penetrate + " / " + Projectile.numHits);
		}

		public override void OnKill(int timeLeft)
		{
			Vector2 velocity = Projectile.velocity * 0.2f;

			for (int i = 0; i < 5; i++)
				Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.WoodFurniture, velocity.X, velocity.Y);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, new Vector2(texture.Width / 2, Projectile.height / 2), Projectile.scale, 0);

			return false;
		}

		public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
		{
			if (Stationary)
				behindNPCsAndTiles.Add(index);
		}

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch) => _trail?.Draw(TrailSystem.TrailShaders, Main.spriteBatch.GraphicsDevice, Matrix.Identity);
	}

	public override void SetDefaults()
    {
		Item.ammo = Type;
		Item.maxStack = Item.CommonMaxStack;
		Item.consumable = true;
		Item.damage = 10;
		Item.knockBack = 3f;
		Item.rare = ItemRarityID.White;
		Item.shoot = ModContent.ProjectileType<BoltProjectile>();
	}
}