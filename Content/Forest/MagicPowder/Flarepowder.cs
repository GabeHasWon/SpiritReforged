using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.MagicPowder;

public class Flarepowder : ModItem
{
	private static readonly Asset<Texture2D> HeldTexture = ModContent.Request<Texture2D>(DrawHelpers.RequestLocal(typeof(Flarepowder), "PowderHeld"));
	private static readonly Dictionary<int, int> PowderTypes = [];

	public bool IsDerived => GetType() != typeof(Flarepowder);

	public override void Load()
	{
		if (!IsDerived) //Prevent derived types from detouring
		{
			On_PlayerDrawLayers.DrawPlayer_27_HeldItem += DrawHeldItem;
		}
	}

	private static void DrawHeldItem(On_PlayerDrawLayers.orig_DrawPlayer_27_HeldItem orig, ref PlayerDrawSet drawinfo)
	{
		int heldType = drawinfo.drawPlayer.HeldItem.type;
		if (PowderTypes.TryGetValue(heldType, out int value))
		{
			var texture = HeldTexture.Value;
			var source = texture.Frame(1, 3, 0, value, 0, -2);

			Vector2 origin = source.Size() / 2;
			Vector2 dirOffset = drawinfo.drawPlayer.ItemAnimationActive ? new(11, -2) : new(13, 0);

			dirOffset.X *= drawinfo.drawPlayer.direction;
			Vector2 location = (drawinfo.drawPlayer.Center - Main.screenPosition + dirOffset + new Vector2(0, drawinfo.drawPlayer.gfxOffY)).Floor();
			Color color = drawinfo.drawPlayer.HeldItem.GetAlpha(Lighting.GetColor((drawinfo.ItemLocation / 16).ToPoint()));

			drawinfo.DrawDataCache.Add(new DrawData(texture, location, source, color, drawinfo.drawPlayer.itemRotation, origin, 1, drawinfo.itemEffect));
			return; //Skips orig
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
		
		if (!IsDerived)
		{
			NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry((shop) => shop.NpcType == NPCID.Merchant, new NPCShop.Entry(Type)));
		}

		MoRHelper.AddElement(Item, MoRHelper.Arcane, true);
	}

	public override void SetDefaults()
	{
		Item.damage = 14;
		Item.DamageType = DamageClass.Magic;
		Item.width = Item.height = 14;
		Item.useTime = Item.useAnimation = 20;
		Item.maxStack = Item.CommonMaxStack;
		Item.useStyle = ItemUseStyleID.HoldUp;
		Item.holdStyle = ItemHoldStyleID.HoldFront;
		Item.UseSound = SoundID.Item1;
		Item.useTurn = true;
		Item.autoReuse = true;
		Item.consumable = true;
		Item.noMelee = true;
		Item.shoot = ModContent.ProjectileType<FlarepowderDust>();
		Item.shootSpeed = 5.7f;
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
		SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/MagicCast1") with { PitchRange = (0.5f, 1f), MaxInstances = 2 }, player.Center);

		for (int i = 0; i < 8; i++)
		{
			var vel = (velocity * Main.rand.NextFloat(0.4f, 0.8f)).RotatedByRandom(0.4f);
			Projectile.NewProjectile(source, position, vel, type, damage, knockback, player.whoAmI);
		}

		return false;
	}
}

internal class FlarepowderDust : ModProjectile, IManualTrailProjectile
{
	public const int TimeLeftMax = 60 * 3;

	/// <summary> Must have 3 elements. </summary>
	public virtual Color[] Colors => [Color.Yellow, Color.Red, Color.DarkRed];
	public override string Texture => DrawHelpers.RequestLocal(GetType(), nameof(FlarepowderDust));

	public static readonly SoundStyle Impact = new("SpiritReforged/Assets/SFX/Projectile/Impact_Hard")
	{
		PitchRange = (0f, 0.75f),
		Volume = 0.5f,
		MaxInstances = 5
	};

	/// <summary> Represents a min to max value range. </summary>
	public (float, float) randomTimeLeft;

	public void DoTrailCreation(TrailManager tm)
	{
		float scale = Projectile.scale;

		tm.CreateTrail(Projectile, new StandardColorTrail(Color.OrangeRed.Additive()), new RoundCap(), new DefaultTrailPosition(), 10 * scale, 20 * scale);
		tm.CreateTrail(Projectile, new LightColorTrail(new Color(130, 26, 12) * 0.6f, Color.Transparent), new RoundCap(), new DefaultTrailPosition(), 15 * scale, 50 * scale);
		tm.CreateTrail(Projectile, new StandardColorTrail(Color.LightGoldenrodYellow.Additive()), new RoundCap(), new DefaultTrailPosition(), 5 * scale, 10 * scale);
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
		randomTimeLeft = (0.15f, 0.3f);
	}

	public override void AI()
	{
		if (Projectile.timeLeft == TimeLeftMax)
			OnClientSpawn(true);

		if (Projectile.velocity.Length() > 1.25f && Main.rand.NextBool(5))
			SpawnDust(Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(20f));

		Projectile.velocity *= 0.98f;
		Projectile.velocity = Projectile.velocity.RotatedBy(Projectile.ai[0]);
		Projectile.rotation += Projectile.ai[0];

		Projectile.UpdateFrame(10);
	}

	public virtual void OnClientSpawn(bool doDustSpawn)
	{
		Projectile.frame = Main.rand.Next(Main.projFrames[Type]);
		Projectile.scale = Main.rand.NextFloat(0.5f, 1f);

		if (doDustSpawn)
		{
			for (int i = 0; i < 3; i++)
			{
				float mag = Main.rand.NextFloat(0.33f, 1);
				var velocity = (Projectile.velocity * mag).RotatedByRandom(0.3f);

				if(Main.rand.NextBool(3))
					ParticleHandler.SpawnParticle(new MagicParticle(Projectile.Center, velocity * 0.75f, Color.OrangeRed, Main.rand.NextFloat(0.1f, 1f), Main.rand.Next(20, 100)));

				Vector2 cloudPos = Projectile.Center + Vector2.Normalize(Projectile.velocity) * 10;
				var fireCloud = new SmokeCloud(cloudPos, velocity, Color.Yellow.Additive(80), Main.rand.NextFloat(0.05f, 0.075f), EaseFunction.EaseQuadOut, Main.rand.Next(20, 30), false)
				{
					SecondaryColor = Color.Red.Additive(80),
					TertiaryColor = Color.DarkRed.Additive(80),
					ColorLerpExponent = 0.5f,
					Intensity = 0.25f,
					Pixellate = true
				};

				ParticleHandler.SpawnParticle(fireCloud);

				var smokeCloud = new SmokeCloud(fireCloud.Position, velocity, Color.Gray, fireCloud.Scale * 1.5f, EaseFunction.EaseCubicOut, Main.rand.Next(40, 60))
				{
					SecondaryColor = Color.DarkSlateGray,
					TertiaryColor = Color.Black,
					ColorLerpExponent = 2,
					Intensity = 0.33f,
					Layer = ParticleLayer.BelowProjectile,
					Pixellate = true
				};
				ParticleHandler.SpawnParticle(smokeCloud);
			}
		}

		if (Projectile.owner == Main.myPlayer)
		{
			const float range = 0.01f;

			Projectile.ai[0] = Main.rand.NextFloat(-range, range);
			Projectile.timeLeft = (int)(Projectile.timeLeft * Main.rand.NextFloat(randomTimeLeft.Item1, randomTimeLeft.Item2));

			Projectile.netUpdate = true;
		}

		TrailManager.ManualTrailSpawn(Projectile);
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (Main.rand.NextBool())
			target.AddBuff(BuffID.OnFire, 120);
	}

	public virtual void SpawnDust(Vector2 origin) => Dust.NewDustPerfect(origin, DustID.Torch, Projectile.velocity * 0.5f).noGravity = true;

	public override void OnKill(int timeLeft)
	{
		const int explosion = 80;
		float angle = Main.rand.NextFloat(MathHelper.Pi);

		var circle = new TexturedPulseCircle(Projectile.Center, (Color.OrangeRed * .5f).Additive(), 2, 42, 20, "Bloom", new Vector2(1), Common.Easing.EaseFunction.EaseCircularOut);
		circle.Angle = angle;
		ParticleHandler.SpawnParticle(circle);

		var circle2 = new TexturedPulseCircle(Projectile.Center, (Color.LightGoldenrodYellow * .5f).Additive(), 1, 40, 20, "Bloom", new Vector2(1), Common.Easing.EaseFunction.EaseCircularOut);
		circle2.Angle = angle;
		ParticleHandler.SpawnParticle(circle2);

		if(this is not VexpowderBlueDust)
		{
			for (int i = 0; i < 3; i++)
				ParticleHandler.SpawnParticle(new FireParticle(Projectile.Center,
												   Main.rand.NextVector2Unit() * Main.rand.NextFloat() - Vector2.UnitY * Main.rand.NextFloat(2),
												   [Colors[0].Additive(30), Colors[1].Additive(30), Colors[2].Additive(30) * 0.75f],
												   0.75f,
												   0,
												   Main.rand.NextFloat(0.03f, 0.1f),
												   EaseFunction.EaseQuadOut,
												   Main.rand.Next(10, 35)) { ColorLerpExponent = 2, FinalScaleMod = 0.33f, PixelDivisor = 1.25f });

			var smokeCloud = new SmokeCloud(Projectile.Center, -Vector2.UnitY, Color.Gray, Main.rand.NextFloat(0.04f, 0.06f), EaseFunction.EaseCubicOut, Main.rand.Next(20, 40))
			{
				SecondaryColor = Color.DarkSlateGray,
				TertiaryColor = Color.Black,
				ColorLerpExponent = 2,
				Intensity = 0.6f,
				Layer = ParticleLayer.BelowProjectile,
				Pixellate = true
			};
			ParticleHandler.SpawnParticle(smokeCloud);
		}

		Projectile.Resize(explosion, explosion);
		Projectile.Damage();

		PlayDeathSound();
	}

	public virtual void PlayDeathSound()
	{
		SoundEngine.PlaySound(SoundID.DD2_LightningBugZap with { PitchRange = (0.5f, 1f), Volume = 0.35f, MaxInstances = 5 }, Projectile.Center);
		SoundEngine.PlaySound(Impact, Projectile.Center);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		var texture = TextureAssets.Projectile[Type].Value;
		var source = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame, 0, -2);

		for (int i = 0; i < 3; i++)
		{
			Color tint = (i == 2) ? Color.White : ((i == 1) ? Color.OrangeRed : Colors[0]);
			Color color = Projectile.GetAlpha(tint).Additive();
			float scale = Projectile.scale * (1f - i / 5f);

			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), source, color, Projectile.rotation, source.Size() / 2, scale, default);
		}

		return false;
	}

	public override bool? CanCutTiles() => false;
	public override bool? CanDamage() => (Projectile.timeLeft <= 1) ? null : false;

	public override void SendExtraAI(BinaryWriter writer) => writer.Write((ushort)Projectile.timeLeft);
	public override void ReceiveExtraAI(BinaryReader reader) => Projectile.timeLeft = reader.ReadInt16();
}