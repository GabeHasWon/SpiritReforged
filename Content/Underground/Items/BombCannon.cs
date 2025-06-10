using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Items.BigBombs;
using System.Linq;
using Terraria.Audio;
using Terraria.DataStructures;
using static Microsoft.Xna.Framework.Color;
using static Microsoft.Xna.Framework.MathHelper;
using static SpiritReforged.Common.Easing.EaseFunction;

namespace SpiritReforged.Content.Underground.Items;

[AutoloadGlowmask("255,255,255")]
public class BombCannon : ModItem
{
	public const float ContactDamagePercentage = 0.25f;

	public static readonly int[] AmmoBombIDs = [ItemID.Bomb, ItemID.StickyBomb, ItemID.BouncyBomb, ItemID.BombFish];
	public static readonly int[] BouncyBombProjIDs = [ProjectileID.BouncyBomb, ModContent.ProjectileType<BombBouncy>()];
	public static readonly int[] StickyBombProjIDs = [ProjectileID.StickyBomb, ModContent.ProjectileType<BombSticky>(), ProjectileID.BombFish, ModContent.ProjectileType<BombFish>()];

	public override void SetStaticDefaults()
	{
		ItemEvents.CreateItemDefaults(item => item.ammo = ItemID.Bomb, AmmoBombIDs);
		NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry((shop) => shop.NpcType == NPCID.Demolitionist, new NPCShop.Entry(Type, Condition.DownedEarlygameBoss)));
	}

	public override void SetDefaults()
	{
		Item.DamageType = DamageClass.Ranged;
		Item.damage = 80;
		Item.knockBack = 6;
		Item.width = 44;
		Item.height = 48;
		Item.useTime = Item.useAnimation = 30;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.value = Item.buyPrice(0, 3, 75, 0);
		Item.rare = ItemRarityID.Blue;
		Item.autoReuse = true;
		Item.useAmmo = ItemID.Bomb;
		Item.shoot = ModContent.ProjectileType<BombCannonHeld>();
		Item.channel = true;
		Item.shootSpeed = 8f;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (player.HasEquip<BoomShroom>())
			type = BoomShroomPlayer.MakeLarge(type);

		Projectile.NewProjectile(source, position, velocity, Item.shoot, damage, knockback, player.whoAmI, type, 0, velocity.Length());
		return false;
	}

	public override bool CanConsumeAmmo(Item ammo, Player player) => !player.channel; //Ammo consumption happens in BombCannonHeld

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		StatModifier rangedStat = Main.LocalPlayer.GetTotalDamage(DamageClass.Ranged);

		foreach (TooltipLine line in tooltips)
		{
			if (line.Mod == "Terraria" && line.Name == "Damage") //Replace the vanilla text with our own
				line.Text = $"{(int)rangedStat.ApplyTo(Item.damage * ContactDamagePercentage)}-{(int)rangedStat.ApplyTo(Item.damage)}" + Language.GetText("LegacyTooltip.3");
		}
	}
}

[AutoloadGlowmask("255,255,255", false)]
internal class BombCannonHeld : ModProjectile
{
	private const int ChargeTimeMax = 60 * 3;

	public int ShootType
	{
		get => (int)Projectile.ai[0];
		set => Projectile.ai[0] = value;
	}
	public ref float Charge => ref Projectile.ai[1];
	public ref float StoredVelocity => ref Projectile.ai[2];

	public float Progress => (float)Charge / ChargeTimeMax;

	public override string Texture => ModContent.GetInstance<BombCannon>().Texture;
	public override LocalizedText DisplayName => Language.GetText("Mods.SpiritReforged.Items.BombCannon.DisplayName");

	private bool _released;

	public override void SetDefaults()
	{
		Projectile.ignoreWater = true;
		Projectile.tileCollide = false;
		Projectile.timeLeft = 2;
	}

	public override void AI()
	{
		const float chargeThreshold = 0.3f;
		const int wait = 8;
		const int stagger = 10;

		var owner = Main.player[Projectile.owner];

		if (!_released)
		{
			if (!owner.channel || Progress == 1)
			{
				if (Progress > chargeThreshold) //Only fire if charged enough
					Fire();

				_released = true;
			}

			if (owner.whoAmI == Main.myPlayer) //Adjust to cursor direction
			{
				float speed = StoredVelocity;
				speed *= 1 + Progress / 3;

				Vector2 shootDirection = owner.GetArcVel(Main.MouseWorld, 0.2f, speed);
				Projectile.velocity = shootDirection;
				Projectile.netUpdate = true;
			}

			int limit = (int)(ChargeTimeMax / 3f);
			if ((int)Charge % limit == limit - 1)
			{
				SoundEngine.PlaySound(SoundID.MenuTick with { Pitch = Progress * .5f }, Projectile.Center);
				var start = Projectile.Center + new Vector2(8, 10 * Projectile.direction).RotatedBy(Projectile.rotation);

				ParticleHandler.SpawnParticle(new ImpactLine(start, Vector2.Zero, Red.Additive(), new Vector2(1, 2), 10, Projectile));
				ParticleHandler.SpawnParticle(new ImpactLine(start, Vector2.Zero, (White * .3f).Additive(), new Vector2(1, 2) * .7f, 10, Projectile));

				ParticleHandler.SpawnParticle(new PulseCircle(Projectile, Red, .1f, 50, 15, EaseQuadOut, start));
			}

			Charge = Math.Min(Charge + 1, ChargeTimeMax);
			Projectile.timeLeft = stagger + wait;
		}

		int holdDistance = 20;
		float rotation = Projectile.velocity.ToRotation();

		if (_released) //Handle recoil visuals after firing
		{
			if (Progress > chargeThreshold)
			{
				float sProgress = Math.Clamp((Projectile.timeLeft - wait) / (float)stagger, 0, 1);

				holdDistance -= (int)(sProgress * 8f);
				rotation -= .4f * Projectile.direction * sProgress;
			}
		}

		Projectile.direction = Projectile.spriteDirection = owner.direction;
		var position = owner.MountedCenter + new Vector2(holdDistance, 5 * -Projectile.direction).RotatedBy(rotation);

		Projectile.Center = owner.RotatedRelativePoint(position);
		Projectile.rotation = rotation;

		owner.heldProj = Projectile.whoAmI;
		owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - 1.57f + .4f * owner.direction);
		owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - 1.57f + .4f * owner.direction);
		owner.direction = (Projectile.velocity.X < 0) ? -1 : 1;
		owner.itemAnimation = owner.itemTime = 2;
	}

	private void Fire()
	{
		var owner = Main.player[Projectile.owner];
		owner.PickAmmo(owner.HeldItem, out _, out _, out _, out _, out _); //Consume relevant ammo

		if (Projectile.owner == Main.myPlayer)
		{
			var velocity = Projectile.velocity * Progress;
			bool boomShroom = owner.HasEquip<BoomShroom>();
			int type = boomShroom ? ModContent.ProjectileType<BigCannonBomb>() : ModContent.ProjectileType<CannonBomb>();

			PreNewProjectile.New(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity, type, (int)(Projectile.damage * BombCannon.ContactDamagePercentage), Projectile.knockBack, Projectile.owner, ShootType, 1, preSpawnAction: delegate (Projectile p)
			{
				p.timeLeft = (int)(p.timeLeft * (1f - Progress));

				var bomb = p.ModProjectile as CannonBomb;
				bomb.SetDamage((int)(Projectile.damage * (boomShroom ? 1.5f : 1)));
			});
		}

		var unit = Vector2.Normalize(Projectile.velocity);
		var muzzle = Projectile.Center + unit * 28f;

		for (int i = 0; i < 12; i++)
		{
			float mag = Main.rand.NextFloat();
			Dust.NewDustPerfect(muzzle, DustID.GemRuby, (unit * 10f * mag).RotatedByRandom(.5f * (1f - mag)), Scale: Main.rand.NextFloat(.5f, 2f)).noGravity = true;

			if (Main.rand.NextBool())
			{
				float scale = Main.rand.NextFloat(.5f, 1f);
				var velocity = (unit * Main.rand.NextFloat(3f)).RotatedByRandom(.5f);

				ParticleHandler.SpawnParticle(new GlowParticle(muzzle, velocity, Red, scale, 20, 5));
				ParticleHandler.SpawnParticle(new GlowParticle(muzzle, velocity, White, scale * .5f, 20, 5));
			}
		}

		SoundEngine.PlaySound(SoundID.Item61 with { Pitch = Progress / 2 }, Projectile.Center);
		_released = true;
	}

	public override bool ShouldUpdatePosition() => false;
	public override bool? CanCutTiles() => false;
	public override bool? CanDamage() => false;

	public override bool PreDraw(ref Color lightColor)
	{
		var texture = TextureAssets.Projectile[Type].Value;
		var glowmask = GlowmaskProjectile.ProjIdToGlowmask[Type].Glowmask.Value;

		var position = new Vector2((int)(Projectile.Center.X - Main.screenPosition.X), (int)(Projectile.Center.Y - Main.screenPosition.Y));
		var effects = (Projectile.spriteDirection == -1) ? SpriteEffects.FlipVertically : SpriteEffects.None;

		int width = 32 + (int)(6 * (Progress / .33f));
		var glowFrame = new Rectangle(0, 0, width, texture.Height);

		Main.EntitySpriteDraw(texture, position, null, Projectile.GetAlpha(lightColor), Projectile.rotation, texture.Size() / 2, Projectile.scale, effects);
		Main.EntitySpriteDraw(glowmask, position, glowFrame, Projectile.GetAlpha(White), Projectile.rotation, texture.Size() / 2, Projectile.scale, effects);
		return false;
	}
}

internal class CannonBomb : BombProjectile, ITrailProjectile
{
	/// <summary> The original projectile this one is mimicking the sprite and behavior of. </summary>
	public int CopyProj
	{
		get => (int)Projectile.ai[0];
		set => Projectile.ai[0] = value;
	}
	public override string Texture => AssetLoader.EmptyTexture;

	public bool bouncy;
	private bool _justSpawned;
	private int _lastTimeLeft;

	public override void SetDefaults()
	{
		base.SetDefaults();
		Projectile.DamageType = DamageClass.Ranged;
	}

	public virtual void DoTrailCreation(TrailManager tM)
	{
		float baseWidth = 40;
		float baseLength = 240;

		ITrailCap tCap = new RoundCap();
		ITrailPosition tPos = new DefaultTrailPosition();
		ITrailShader tShader = new ImageShader(AssetLoader.LoadedTextures["GlowTrail"].Value, Vector2.One);

		tM.CreateTrail(Projectile, new GradientTrail(Red.Additive(150) * 0.5f, Transparent, EaseQuarticOut), tCap, tPos, baseWidth * 2, baseLength, tShader);
		tM.CreateTrail(Projectile, new GradientTrail(Lerp(Red, Pink, 0.25f).Additive(150) * 0.75f, Transparent, EaseQuarticOut), tCap, tPos, baseWidth * 1.5f, baseLength, tShader);
		tM.CreateTrail(Projectile, new GradientTrail(Pink.Additive(), Transparent, EaseQuarticOut), tCap, tPos, baseWidth, baseLength, tShader); 

		tM.CreateTrail(Projectile, new LightColorTrail(White.Additive() * 0.25f, Transparent), tCap, tPos, baseWidth, baseLength, new DefaultShader());
	}

	public override void AI()
	{
		if (!_justSpawned)
		{
			bouncy = BombCannon.BouncyBombProjIDs.Contains(CopyProj);
			sticky = BombCannon.StickyBombProjIDs.Contains(CopyProj);

			_justSpawned = true;
		}

		_lastTimeLeft = Projectile.timeLeft;
		base.AI();

		if (Projectile.velocity == Vector2.Zero && Projectile.oldVelocity == Vector2.Zero)
			TrailManager.TryTrailKill(Projectile);
	}

	public override void StartExplosion() => Projectile.ResetLocalNPCHitImmunity();

	public override void FuseVisuals()
	{
		if (Main.rand.NextBool())
		{
			var position = Projectile.Center - (new Vector2(0, Projectile.height / 2 + 10) * Projectile.scale).RotatedBy(Projectile.rotation);

			var dust = Dust.NewDustPerfect(position, DustID.Smoke, Main.rand.NextVector2Unit(), 100);
			dust.scale = 0.1f + Main.rand.NextFloat(0.5f);
			dust.fadeIn = 1.5f + Main.rand.NextFloat(0.5f);
			dust.noGravity = true;

			dust = Dust.NewDustPerfect(position, DustID.GemRuby, Main.rand.NextVector2Unit(), 100);
			dust.scale = 1f + Main.rand.NextFloat(0.5f);
			dust.noGravity = true;
		}
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		if (bouncy)
		{
			Projectile.Bounce(oldVelocity);
			return false;
		}
		else if (!sticky)
		{
			Projectile.Bounce(oldVelocity, 0.3f);

			float rotationDifference = WrapAngle(oldVelocity.ToRotation() - Projectile.velocity.ToRotation());
			rotationDifference = Math.Abs(rotationDifference);

			//Create a new trail to prevent the vertex strip from freaking out over movements in opposite directions
			if (rotationDifference > PiOver2 * 1.5f)
			{
				TrailManager.TryTrailKill(Projectile);

				if (Projectile.velocity.Length() > 1)
					TrailManager.ManualTrailSpawn(Projectile);
			}

			return false;
		}

		return base.OnTileCollide(oldVelocity);
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (!DealingDamage)
			Projectile.timeLeft = _lastTimeLeft; //Preserve timeLeft because Sets.Explosive automatically sets it to 3
	}

	public override void OnKill(int timeLeft)
	{
		if (Main.dedServ)
			return;

		SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

		//Particle circles showing radius of explosion
		var ease = new PolynomialEase(x => (float)(0.5 + 0.5 * Math.Pow(x, 0.5)));
		var stretch = new Vector2(2, 1);

		ParticleHandler.SpawnParticle(new TexturedPulseCircle(Projectile.Center, Lerp(Pink, Red, 0.25f).Additive(150), Red, 1f, 30 * area, 20, "Star2", stretch, ease)
		{
			Angle = Main.rand.NextFloatDirection()
		});

		ParticleHandler.SpawnParticle(new TexturedPulseCircle(Projectile.Center, Lerp(Pink, Red, 0.5f).Additive(150), Red, .5f, 40 * area, 20, "Star2", stretch, ease)
		{
			Angle = Main.rand.NextFloatDirection()
		});

		//Burst of light in the center
		float lightburstRotation = Main.rand.NextFloatDirection();
		for(int i = 0; i < 3; i++)
			ParticleHandler.SpawnParticle(new DissipatingImage(Projectile.Center, Lerp(Pink, Red, 0.5f).Additive(), Main.rand.NextFloatDirection(), 0.02f * area, 0, "GodrayCircle", 15));

		const int time = 5;
		ParticleHandler.SpawnParticle(new ImpactLinePrim(Projectile.Center, Vector2.Zero, Red.Additive(), new Vector2(0.2f, 1f) * area, time, 1));
		ParticleHandler.SpawnParticle(new ImpactLinePrim(Projectile.Center, Vector2.Zero, Pink.Additive(), new Vector2(0.1f, 1f) * area, time, 1));

		//Glowy particles coming from the center
		for (int i = 0; i < area * 2; i++)
		{
			float magnitude = Main.rand.NextFloat(0.25f, 1);

			var color = Red.Additive();
			var velocity = Main.rand.NextVector2Unit() * magnitude * 4;
			float scale = (1f - magnitude) * 0.3f * area;
			int lifeTime = Main.rand.Next(25, 35);

			static void DelegateAction(Particle p) => p.Velocity *= 0.97f;

			ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center + velocity * 10, velocity, color, scale, 30, 3, DelegateAction));
			ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center + velocity * 10, velocity, White.Additive(), scale * .5f, lifeTime, 3, DelegateAction));

			var d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(16f * area), DustID.GemRuby, Scale: Main.rand.NextFloat() + .5f);
			d.noGravity = true;
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D copyTexture = TextureAssets.Projectile[CopyProj].Value;
		Texture2D solid = TextureColorCache.ColorSolid(copyTexture, Red.Additive(150));
		Vector2 origin = copyTexture.Size() / 2;
		origin.Y += 4;
		float lerp = GetLerp();
		float scale = Projectile.scale + lerp * .1f;

		Rectangle drawRect = copyTexture.Bounds;

		//Fix drawing for bomb fish specifically and make it animate instead of changing scale
		if (CopyProj == ProjectileID.BombFish)
		{
			drawRect.Height /= 4;
			origin.Y = drawRect.Height/2 + 4;
			int frame = lerp switch
			{
				< 0.25f => 0,
				< 0.5f => 1,
				< 0.75f => 2,
				_ => 3
			};

			drawRect.Y = drawRect.Height * frame;
			scale = 1;
		}

		Main.EntitySpriteDraw(solid, Projectile.Center - Main.screenPosition, drawRect, White, Projectile.rotation, origin, scale * 1.1f, SpriteEffects.None);
		Main.EntitySpriteDraw(copyTexture, Projectile.Center - Main.screenPosition, drawRect, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, scale, SpriteEffects.None);

		var color = Projectile.GetAlpha(Red.Additive(150)) * lerp * 2;
		Main.EntitySpriteDraw(solid, Projectile.Center - Main.screenPosition, drawRect, color, Projectile.rotation, origin, scale, SpriteEffects.None);
		return false;
	}

	public float GetLerp()
	{
		float progress = 1f - (float)Projectile.timeLeft / timeLeftMax;

		int numFlashes = 9;
		return EaseCircularIn.Ease((float)Math.Sin(EaseCircularIn.Ease(progress) * numFlashes * Pi));
	}
}

internal class BigCannonBomb : CannonBomb
{
	public override void DoTrailCreation(TrailManager tM)
	{
		float baseWidth = 60;
		float baseLength = 360;

		ITrailCap tCap = new RoundCap();
		ITrailPosition tPos = new DefaultTrailPosition();
		ITrailShader tShader = new ImageShader(AssetLoader.LoadedTextures["GlowTrail"].Value, Vector2.One);

		tM.CreateTrail(Projectile, new GradientTrail(Red.Additive(150) * 0.5f, Transparent, EaseQuarticOut), tCap, tPos, baseWidth * 2, baseLength, tShader);
		tM.CreateTrail(Projectile, new GradientTrail(Lerp(Red, Pink, 0.25f).Additive(150) * 0.75f, Transparent, EaseQuarticOut), tCap, tPos, baseWidth * 1.5f, baseLength, tShader);
		tM.CreateTrail(Projectile, new GradientTrail(Pink.Additive(), Transparent, EaseQuarticOut), tCap, tPos, baseWidth, baseLength, tShader);

		tM.CreateTrail(Projectile, new LightColorTrail(White.Additive() * 0.25f, Transparent), tCap, tPos, baseWidth, baseLength, new DefaultShader());
	}

	public override void SetDefaults()
	{
		base.SetDefaults();
		area = 15;
		Projectile.scale = 0;
	}

	public override void AI()
	{
		base.AI();

		if (!DealingDamage)
		{
			float oldScale = Projectile.scale; //Resize logic
			Projectile.scale = Math.Min(Projectile.scale + .1f, 1);

			if (Projectile.scale != oldScale)
			{
				int size = (int)Math.Max(Bomb.CommonSize * Projectile.scale, 2);
				Projectile.Resize(size, size);
			}
		}
	}

	public override void OnKill(int timeLeft)
	{
		base.OnKill(timeLeft);
		SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, Projectile.Center);
	}
}