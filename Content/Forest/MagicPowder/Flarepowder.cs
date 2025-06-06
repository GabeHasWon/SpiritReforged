using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Ocean.Hydrothermal;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.MagicPowder;

public class Flarepowder : ModItem
{
	private static readonly Asset<Texture2D> HeldTexture = ModContent.Request<Texture2D>(DrawHelpers.RequestLocal(typeof(Flarepowder), "PowderHeld"));
	private static readonly Dictionary<int, int> PowderTypes = [];

	public override void Load()
	{
		if (GetType() == typeof(Flarepowder)) //Prevent derived types from detouring
		{
			On_PlayerDrawLayers.DrawPlayer_27_HeldItem += DrawHeldItem;
		}
	}

	private static void DrawHeldItem(On_PlayerDrawLayers.orig_DrawPlayer_27_HeldItem orig, ref PlayerDrawSet drawinfo)
	{
		int heldType = drawinfo.drawPlayer.HeldItem.type;
		if (PowderTypes.ContainsKey(heldType))
		{
			var texture = HeldTexture.Value;
			var source = texture.Frame(1, 3, 0, PowderTypes[heldType], 0, -2);

			Vector2 origin = source.Size() / 2;
			Vector2 dirOffset = drawinfo.drawPlayer.ItemAnimationActive ? new(11, -2) : new(13, 0);

			dirOffset.X *= drawinfo.drawPlayer.direction;
			Vector2 location = (drawinfo.drawPlayer.Center - Main.screenPosition + dirOffset + new Vector2(0, drawinfo.drawPlayer.gfxOffY)).Floor();
			Color color = drawinfo.drawPlayer.HeldItem.GetAlpha(Lighting.GetColor((drawinfo.ItemLocation / 16).ToPoint()));

			drawinfo.DrawDataCache.Add(new DrawData(texture, location, source, color, drawinfo.drawPlayer.itemRotation, origin, 1, drawinfo.itemEffect));
			return;
		}

		orig(ref drawinfo);
	}

	public override void SetStaticDefaults()
	{
		Item.ResearchUnlockCount = 99;

		int frame = Name switch
		{
			nameof(VexpowderBlue) => 1,
			nameof(VexpowderRed) => 2,
			_ => 0
		};

		PowderTypes.Add(Type, frame);
	}

	public override void SetDefaults()
	{
		Item.damage = 10;
		Item.DamageType = DamageClass.Magic;
		Item.width = Item.height = 14;
		Item.useTime = Item.useAnimation = 20;
		Item.maxStack = Item.CommonMaxStack;
		Item.useStyle = ItemUseStyleID.HoldUp;
		Item.holdStyle = ItemHoldStyleID.HoldFront;
		Item.useTurn = true;
		Item.autoReuse = true;
		Item.consumable = true;
		Item.noMelee = true;
		Item.shoot = ModContent.ProjectileType<FlarepowderDust>();
		Item.shootSpeed = 5;
		Item.value = Item.sellPrice(copper: 4);
	}

	public override void UseItemFrame(Player player)
	{
		float rotation = -MathHelper.Pi * player.direction * player.itemAnimation / player.itemAnimationMax;
		player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, rotation);
		player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, -MathHelper.PiOver2 * player.direction);
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		for (int i = 0; i < 8; i++)
		{
			var vel = (velocity * Main.rand.NextFloat(0.1f, 1f)).RotatedByRandom(1f);
			Projectile.NewProjectile(source, position, vel, type, damage, knockback, player.whoAmI);
		}

		return false;
	}
}

internal class FlarepowderDust : ModProjectile, ITrailProjectile
{
	public const int TimeLeftMax = 60 * 3;

	/// <summary> Must have 3 elements. </summary>
	public virtual Color[] Colors => [Color.Orange, Color.LightCoral, Color.Goldenrod];
	public override string Texture => DrawHelpers.RequestLocal(GetType(), nameof(FlarepowderDust));

	public void DoTrailCreation(TrailManager tm)
	{
		tm.CreateTrail(Projectile, new StandardColorTrail(Colors[1].Additive()), new RoundCap(), new DefaultTrailPosition(), 10, 20);
		tm.CreateTrail(Projectile, new LightColorTrail(new Color(87, 35, 88) * 0.6f, Color.Transparent), new RoundCap(), new DefaultTrailPosition(), 15, 50);
		tm.CreateTrail(Projectile, new StandardColorTrail(Color.White.Additive()), new RoundCap(), new DefaultTrailPosition(), 5, 10);
	}

	public override void SetStaticDefaults() => Main.projFrames[Type] = 3;
	public override void SetDefaults()
	{
		Projectile.DamageType = DamageClass.Magic;
		Projectile.friendly = true;
		Projectile.penetrate = -1;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.timeLeft = TimeLeftMax;
	}

	public override void AI()
	{
		if (Projectile.timeLeft == TimeLeftMax) //On spawn
		{
			Projectile.frame = Main.rand.Next(Main.projFrames[Type]);
			Projectile.scale = Main.rand.NextFloat(0.5f, 1f);

			for (int i = 0; i < 2; i++)
			{
				float mag = Main.rand.NextFloat();
				var velocity = (Projectile.velocity * mag).RotatedByRandom(0.2f);
				var color = Color.Lerp(Colors[0], Colors[1], mag) * 3;

				ParticleHandler.SpawnParticle(new MagicParticle(Projectile.Center, velocity * 0.75f, color, Main.rand.NextFloat(0.1f, 1f), Main.rand.Next(20, 200)));
				ParticleHandler.SpawnParticle(new DissipatingSmoke(Projectile.Center, velocity * 0.8f, Color.White, color, Main.rand.NextFloat(0.05f, 0.1f), Main.rand.Next(20, 50)));
			}

			if (Projectile.owner == Main.myPlayer)
			{
				const float range = 0.01f;

				Projectile.ai[0] = Main.rand.NextFloat(-range, range);
				Projectile.timeLeft = (int)(Projectile.timeLeft * Main.rand.NextFloat(0.2f, 1f));

				Projectile.netUpdate = true;
			}
		}

		if (Projectile.velocity.Length() > 1.25f && Main.rand.NextBool(5))
		{
			Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(20f), DustID.Torch, Projectile.velocity * 0.5f).noGravity = true;
		}

		Projectile.velocity *= 0.98f;
		Projectile.velocity = Projectile.velocity.RotatedBy(Projectile.ai[0]);
		Projectile.rotation += Projectile.ai[0];

		Projectile.UpdateFrame(10);
	}

	public override void OnKill(int timeLeft)
	{
		const int explosion = 80;

		var circle = new TexturedPulseCircle(Projectile.Center, (Colors[1] * .5f).Additive(), 2, 42, 20, "Bloom", new Vector2(1), Common.Easing.EaseFunction.EaseCircularOut);
		ParticleHandler.SpawnParticle(circle);

		var circle2 = new TexturedPulseCircle(Projectile.Center, (Color.White * .5f).Additive(), 1, 40, 20, "Bloom", new Vector2(1), Common.Easing.EaseFunction.EaseCircularOut);
		ParticleHandler.SpawnParticle(circle2);

		Projectile.Resize(explosion, explosion);
		Projectile.Damage();
	}

	public override bool PreDraw(ref Color lightColor)
	{
		var texture = TextureAssets.Projectile[Type].Value;
		var source = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame, 0, -2);

		for (int i = 0; i < 3; i++)
		{
			Color tint = (i == 2) ? Color.White : ((i == 1) ? Colors[2] : Colors[1]);
			Color color = Projectile.GetAlpha(tint).Additive();
			float scale = Projectile.scale * (1f - i / 5f);

			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), source, color, Projectile.rotation, source.Size() / 2, scale, default);
		}

		return false;
	}

	public override bool? CanCutTiles() => false;
	public override bool? CanDamage() => (Projectile.timeLeft <= 1) ? null : false;
}