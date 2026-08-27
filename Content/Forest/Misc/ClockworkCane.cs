using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Subclasses.Wrenches;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;

namespace SpiritReforged.Content.Forest.Misc;

public class ClockworkCane : ModItem
{
	public override void SetDefaults()
	{
		Item.DefaultToMagicWeapon(ModContent.ProjectileType<ClockworkCog>(), 25, 0, false);
		Item.value = Item.sellPrice(0, 2, 0, 0);
		Item.rare = ItemRarityID.Blue;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.DamageType = DamageClass.Summon;
		Item.mana = 20;
		Item.damage = 14;
		Item.knockBack = 2.5f;
		Item.UseSound = SoundID.Item44;
	}

	public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback) => position = Main.MouseWorld;
}

[AutoloadMinionBuff]
public class ClockworkCog : BaseMinion, IDrawPixelated
{
	public const int WINDUP_TIME = 60;

	public ref float Counter => ref Projectile.ai[0];

	public Projectile[] Partners
	{
		get
		{
			List<Projectile> result = [];
			foreach (Projectile other in Main.ActiveProjectiles)
			{
				if (other.owner == Projectile.owner && other.whoAmI != Projectile.whoAmI && other.type == Projectile.type)
					result.Add(other);
			}

			return result.ToArray();
		}
	}

	public Item InventoryItem { get; private set; }

	private VertexTrail _trail;
	private bool _inRange;

	public ClockworkCog() : base(600, 800, new Vector2(28)) { }

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		Main.projFrames[Type] = 6;
		ProjectileID.Sets.TrailingMode[Type] = 0;
		ProjectileID.Sets.TrailCacheLength[Type] = 5;
	}

	public override void SetDefaults()
	{
		base.SetDefaults();

		Projectile.tileCollide = true;
		Projectile.frame = Main.rand.Next(Main.projFrames[Type]);
	}

	public override bool PreAI()
	{
		if (!Main.dedServ)
		{
			if (Projectile.velocity.Length() > 2f)
			{
				CreateTrail();
			}
			else
			{
				_trail?.Dissolve();
			}

			_trail?.Update();
		}

		Projectile.rotation += Projectile.velocity.X / 20f;
		Projectile.velocity.Y += 0.5f;

		Collision.StepUp(ref Projectile.position, ref Projectile.velocity, Projectile.width, Projectile.height, ref Projectile.stepSpeed, ref Projectile.gfxOffY);

		int direction = Math.Sign(Projectile.velocity.X);
		Projectile.direction = Projectile.spriteDirection = (direction == 0) ? Projectile.direction : direction; //Set direction

		foreach (Projectile partner in Partners)
		{
			float intersection = Projectile.Distance(partner.Center) - Projectile.width;

			if (intersection < 0) //Unstick from nearby cogs
			{
				Projectile.velocity += Projectile.DirectionTo(partner.Center) * intersection / 4;
				Projectile.velocity.Y -= 0.3f; //Bounce up for fun
			}
		}

		if (!Main.dedServ && (Projectile.localAI[0] += Math.Abs(Projectile.velocity.X)) > 50)
		{
			SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.3f, Pitch = 0.3f, PitchVariance = 0.5f }, Projectile.Center); //Periodically play clicking sounds while moving
			Projectile.localAI[0] = 0;
		}

		return true;
	}

	public override void IdleMovement(Player player)
	{
		const float speed = 4f;
		const float idle_distance = 10;

		Vector2 targetCenter = player.Center + new Vector2(50 * -player.direction, 0);
		bool nearby = Projectile.DistanceSQ(targetCenter) < idle_distance * idle_distance;
		Vector2 result = nearby ? Projectile.velocity * 0.9f : Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(targetCenter) * speed, 0.05f);

		if (InventoryItem is Item inventoryItem && !inventoryItem.beingGrabbed)
		{
			inventoryItem.Center = Projectile.Top - new Vector2(0, 20); //Carry the item
		}
		else if (FindPickup(800, out Item item)) //Find nearby scrap pickups
		{
			result = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(item.Center) * speed, 0.1f);

			if (Projectile.Hitbox.Intersects(item.Hitbox))
				InventoryItem = item;
		}

		Projectile.velocity.X = result.X;
		ResetDash();
	}

	private bool FindPickup(int scanDistance, out Item item)
	{
		float lastDistance = scanDistance;
		item = null;

		foreach (Item activeItem in Main.ActiveItems)
		{
			float distance = Projectile.Distance(activeItem.Center);

			if (!activeItem.beingGrabbed && activeItem.type == ModContent.ItemType<ScrapPickup>() && distance < lastDistance)
			{
				item = activeItem;
				lastDistance = distance;
			}
		}

		if (item != InventoryItem)
			InventoryItem = null;

		return item != null;
	}

	public override void TargettingBehavior(Player player, NPC target)
	{
		const int dash_duration = 15;
		const int required_distance = 150;

		const float chase_speed = 4f;
		const float dash_speed = 13f;

		if (_inRange)
		{
			if (++Counter > WINDUP_TIME)
			{
				float progress = (Counter - WINDUP_TIME - 1f) / dash_duration;
				float rate = EaseFunction.EaseCubicOut.Ease(progress) / 30f;

				if (progress == 0) //Instant velocity
				{
					rate = 1f;
					SoundEngine.PlaySound(SoundID.DD2_BallistaTowerShot with { Volume = 0.5f, PitchVariance = 0.3f }, Projectile.Center);
				}

				Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(target.Center) * dash_speed, rate);

				if (progress > 1)
					ResetDash();
			}
			else
			{
				Projectile.rotation += 0.5f * (Counter / WINDUP_TIME) * Projectile.spriteDirection;
				Projectile.velocity.X *= 0.9f;

				if (Counter % 20 == 10)
					ParticleHandler.SpawnParticle(new PulseCircle(Projectile.Center, Color.Goldenrod * 0.1f, 0.1f, 150, 20, EaseFunction.EaseCubicOut, true).Attach(Projectile));
			}
		}
		else
		{
			var result = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(target.Center) * chase_speed, 0.1f);
			Projectile.velocity.X = result.X;
		}

		if (Projectile.DistanceSQ(target.Center) < required_distance * required_distance)
		{
			_inRange = true;
		}

		InventoryItem = null; //Reset inventory item
	}

	private void ResetDash()
	{
		Counter = 0;
		_inRange = false;
	}

	private void CreateTrail()
	{
		ITrailCap cap = new RoundCap();
		ITrailPosition position = new EntityTrailPosition(Projectile);
		ITrailShader shader = new DefaultShader();

		if (_trail == null || _trail.CanBeDisposed)
			_trail = new VertexTrail(new GradientTrail(Color.Transparent, Color.SandyBrown.Additive(50), EaseFunction.EaseQuarticInOut), cap, position, shader, 20, 50);
	}

	public override bool MinionContactDamage() => Counter > WINDUP_TIME;

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		if (Math.Abs(Projectile.velocity.X) > 1.5f && Main.rand.NextBool(3))
		{
			int type = Main.rand.NextFromList(DustID.Smoke, DustID.Ash);

			Dust dust = Dust.NewDustPerfect(Projectile.Bottom + new Vector2(0, 4), type, Vector2.UnitY * -Main.rand.NextFloat(), 120, default, Scale: Main.rand.NextFloat(0.5f, 1.5f));
			dust.noGravity = true;
			dust.fadeIn = 1.2f;
		}

		return false;
	}

	public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
	{
		fallThrough = false;
		return true;
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (Main.rand.NextBool(3) && HasWrench(Main.player[Projectile.owner]))
			ItemMethods.NewItemSynced(Projectile.GetSource_OnHit(target), ModContent.ItemType<ScrapPickup>(), target.Center);

		for (int i = 0; i < 3; i++)
			ParticleHandler.SpawnParticle(new EmberParticle(Projectile.Center, (Projectile.velocity * Main.rand.NextFloat(0.1f, 0.5f)).RotatedByRandom(1), Color.OrangeRed, 0.5f, 40, 3));

		Projectile.velocity *= 0.8f; //Slow down and sync
		Projectile.netUpdate = true;
	}

	public static bool HasWrench(Player player)
	{
		const int hotbar_slots = 10;
		for (int i = 0; i < hotbar_slots; i++)
		{
			if (player.inventory[i] is Item item && item.DamageType == ModContent.GetInstance<WrenchClass>())
				return true;
		}

		return false;
	}

	public override bool DoAutoFrameUpdate(ref int framespersecond, ref int startframe, ref int endframe) => false;

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Rectangle source = texture.Frame(2, Main.projFrames[Type], 0, Projectile.frame, -2, -2);
		Vector2 center = Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY);

		if (Counter > WINDUP_TIME) //Dashing
		{
			for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Type]; i++)
			{
				float opacity = (ProjectileID.Sets.TrailCacheLength[Type] - i) / (float)ProjectileID.Sets.TrailCacheLength[Type] * 0.5f;
				Vector2 trailPosition = Projectile.oldPos[i] + Projectile.Size / 2 - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY;

				Main.EntitySpriteDraw(texture, trailPosition, source, Projectile.GetAlpha(Color.White.Additive(100)) * opacity, Projectile.rotation, source.Size() / 2, Projectile.scale, 0);
			}
		}
		else //Draw an outline while spinning up
		{
			DrawHelpers.DrawOutline(default, default, default, default, (offset) =>
				Main.EntitySpriteDraw(TextureColorCache.ColorSolid(texture, Color.White), center + offset, source,
				Projectile.GetAlpha(Color.Goldenrod) * (Counter / WINDUP_TIME) * 0.2f, Projectile.rotation, source.Size() / 2, Projectile.scale, 0));
		}

		Main.EntitySpriteDraw(texture, center, source, Projectile.GetAlpha(lightColor), Projectile.rotation, source.Size() / 2, Projectile.scale, 0);

		source = texture.Frame(2, Main.projFrames[Type], 1, Projectile.frame, -2, -2);
		float eyeRotation = (_targetNPC is NPC target) ? Projectile.AngleTo(target.Center) : Projectile.AngleTo(Main.player[Projectile.owner].Center);
		Main.EntitySpriteDraw(texture, center, source, Projectile.GetAlpha(lightColor), eyeRotation, source.Size() / 2, Projectile.scale, 0);

		return false;
	}

	void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch) => _trail?.Draw(TrailSystem.TrailShaders, Main.graphics.GraphicsDevice, Matrix.Identity);
}