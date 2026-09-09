using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Desert;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Greatbows.Greatarrows;

public class Greatarrow : ModItem
{
	public override void SetStaticDefaults() => NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry(static (shop) => shop.NpcType == NPCID.Merchant, 
		new NPCShop.Entry(ModContent.ItemType<Greatarrow>())));

	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.WoodenArrow);
		Item.Size = new Vector2(14, 48);
		Item.ammo = Type;
		Item.value = Item.sellPrice(0, 0, 0, 15);
		Item.shoot = ModContent.ProjectileType<GreatarrowProjectile>();
	}
}

public class GreatarrowProjectile : ModProjectile
{
	public override string Texture => base.Texture[..^"Projectile".Length];

	public ref float Charge => ref Projectile.ai[0];
	public bool PerfectShot { get => Projectile.ai[1] == 1; set => Projectile.ai[1] = value ? 1 : 0; }
	private ref float DespawnTimer => ref Projectile.ai[2];

	private bool _spawned = false;

	public bool HasTarget => _targetIndex > 0;

	private int _targetIndex;
	private Vector2 _relativePoint = Vector2.Zero;
	private bool _stuckInTile = false;
	private Point16 _stuckTilePos = Point16.Zero;

	public override void SetDefaults()
	{
		Projectile.CloneDefaults(ProjectileID.WoodenArrowFriendly);
		Projectile.aiStyle = 0;
		Projectile.penetrate = 2;
		Projectile.stopsDealingDamageAfterPenetrateHits = true;
	}

	public void CreateTrail(ProjectileTrailRenderer renderer)
	{
		var position = new EntityTrailPosition(Projectile);

		renderer.CreateTrail(Projectile, new VertexTrail(new LightColorTrail(Color.White * 0.3f, Color.Transparent), new RoundCap(), position, new DefaultShader(), 10, 250));
	}

	public override void AI()
	{
		if (!Main.dedServ && !_spawned)
		{
			_spawned = true;
			CreateTrail(TrailSystem.ProjectileRenderer);

			/*ParticleHandler.SpawnParticle(new PulseCircle(Projectile.Center, Color.Goldenrod.Additive(100), 0.2f, 80, 15));
			ParticleHandler.SpawnParticle(new SharpStarParticle(Projectile.Center, Vector2.Zero, Color.Goldenrod.Additive(100), 0.5f, 20, 0));*/
		}

		if (HasTarget)
		{
			NPC target = Main.npc[_targetIndex];

			if (!target.CanBeChasedBy(this) && target.type != NPCID.TargetDummy)
			{
				Projectile.netUpdate = true;
				Projectile.tileCollide = true;
				Projectile.timeLeft *= 2;

				_targetIndex = -1;
				return;
			}

			Projectile.Center = target.Center + _relativePoint;
		}
		else
		{
			if (_stuckInTile) //Check if tile it's stuck in is still active
			{
				Projectile.velocity = Vector2.Zero;
				if (!Main.tile[_stuckTilePos.X, _stuckTilePos.Y].HasTile) //If not, update and let the projectile fall again
				{
					_stuckInTile = false;
					_stuckTilePos = Point16.Zero;
					Projectile.netUpdate = true;
				}
			}
			else
			{
				Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
			}
		}

		if(Projectile.penetrate == -1)
		{
			DespawnTimer++;
			Projectile.Opacity = EaseFunction.CompoundEase([EaseFunction.EaseCircularOut, EaseFunction.EaseQuadOut]).Ease(1 - (DespawnTimer / 90f));

			if (DespawnTimer > 90)
				Projectile.Kill();
		}

		if ((Charge < 1 || Projectile.penetrate == -1) && !_stuckInTile && !HasTarget)
			Projectile.velocity.Y += 0.1f;
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if(Projectile.penetrate == 1)
		{
			_targetIndex = target.whoAmI;
			_relativePoint = Projectile.Center - target.Center;
			Projectile.tileCollide = false;
			Projectile.netUpdate = true;
			Projectile.velocity = Vector2.Zero;
			TrailSystem.ProjectileRenderer.DissolveTrail(Projectile);
		}
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		Projectile.velocity = Vector2.Zero;
		_stuckInTile = true;
		_stuckTilePos = (Projectile.Center + oldVelocity).ToTileCoordinates16();
		Projectile.netUpdate = true;
		Projectile.penetrate = -1;
		TrailSystem.ProjectileRenderer.DissolveTrail(Projectile);

		return false;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Projectile.QuickDraw();
		return false;
	}
}