using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.Subclasses.Greatshields;
using SpiritReforged.Common.Subclasses.Shotguns;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Forest.Ammo;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.SaltFlats.Items.Ammo;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Evil.Items.Ammo;
public class Slug : ShotgunAmmoItem
{
	public override void SetStaticDefaults() => NPCShopHelper.AddEntry(NPCShopHelper.ConditionalEntry.FromNPC(NPCID.ArmsDealer, new NPCShop.Entry(Type, Condition.DownedBrainOfCthulhu)));

	static List<Projectile> Behavior(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 direction, int shotCount, float spreadAmount, float speed, int damage, float knockback)
	{
		float damageIncrease = 1f;

		// 10% damage increase per shot count after 1
		// Only happens with accessories and such
		for (int i = 1; i < shotCount; i++)
			damageIncrease += 0.1f;

		return [Projectile.NewProjectileDirect(source, position, direction.RotatedByRandom(spreadAmount) * speed * Main.rand.NextFloat(1.25f, 1.5f), ModContent.ProjectileType<SlugProjectile>(), damage, knockback, player.whoAmI)];
	}

	public Slug() : base(Behavior, 1, .05f, 15f) { }

	public override void SafeSetDefaults()
	{
		Item.damage = 57;
		Item.value = Item.buyPrice(copper: 50);
	}

	public override void AddRecipes()
	{
		CreateRecipe(75).
			AddIngredient<Shot>(150).
			AddIngredient(ItemID.TissueSample, 3).
			AddTile(TileID.Anvils).
			Register();
	}
}

public class SlugProjectile : ModProjectile
{
	public static readonly Asset<Texture2D> BaseTexture = DrawHelpers.RequestLocal<SlugProjectile>("SlugProjectile", false);
	public static readonly Asset<Texture2D> OutlineTexture = DrawHelpers.RequestLocal<SlugProjectile>("SlugProjectile_Outline", false);

	public const int MAX_TIMELEFT = 720;
	public const int TIME_TILL_GRAVITY = 60;
	public const int TIME_TILL_FIRE_FADEOUT = 120;
	public const int FADE_OUT_TIME = 30;

	int fadeOutTimer;

	public override string Texture => AssetLoader.EmptyTexture;
	
	private readonly ParticleRenderer _trailRenderer = new();
	private VertexTrail[] _trails;

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailingMode[Type] = 0;
		ProjectileID.Sets.TrailCacheLength[Type] = 11;
	}

	public override void SetDefaults()
	{
		Projectile.friendly = true;
		Projectile.DamageType = ModContent.GetInstance<ShotgunClass>();
		Projectile.extraUpdates = 3;
		Projectile.penetrate = 1;
		Projectile.Size = new(4);
		Projectile.timeLeft = MAX_TIMELEFT;
		Projectile.scale = 1f;
		Projectile.hide = true;
		Projectile.stopsDealingDamageAfterPenetrateHits = true;
	}

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => behindNPCs.Add(index);

	public override bool ShouldUpdatePosition() => Projectile.penetrate > 0;

	public override void AI()
	{
		if (Projectile.penetrate == -1)
		{
			Projectile.scale -= 0.05f;

			if (fadeOutTimer < FADE_OUT_TIME)
				fadeOutTimer++;
			else
				Projectile.Kill();

			return;
		}

		Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

		if (Projectile.timeLeft < MAX_TIMELEFT - TIME_TILL_GRAVITY)
		{
			if (Main.rand.NextBool(5))
			{
				if (Main.rand.NextBool(3))
				{
					ParticleHandler.SpawnParticle(new CompositeSmoke(Projectile.Center + Projectile.velocity, -Projectile.velocity.RotatedByRandom(0.2f) * Main.rand.NextFloat(0.4f), Color.Lerp(new(250, 250, 250), new(30, 30, 30), 1 - Projectile.timeLeft / (float)MAX_TIMELEFT) * 0.66f, 15 + Main.rand.Next(30), false, false)
					{
						Layer = ParticleLayer.BelowProjectile
					});

					Dust.NewDustPerfect(Projectile.Center, DustID.Ash, Main.rand.NextVector2Circular(3f, 3f), 200, default, Main.rand.NextFloat(2.5f)).noGravity = true;
				}

				ParticleHandler.SpawnParticle(new SmallCompositeSmoke(Projectile.Center + Projectile.velocity, -Projectile.velocity.RotatedByRandom(0.2f) * Main.rand.NextFloat(0.2f), Color.Lerp(new(230, 230, 230), new(30, 30, 30), 1 - Projectile.timeLeft / (float)MAX_TIMELEFT) * 0.66f, 5 + Main.rand.Next(30), false, false)
				{
					Layer = ParticleLayer.BelowProjectile
				});
			}

			Projectile.velocity *= 0.99f;
			Projectile.velocity.Y += 0.01f;

			if (Projectile.velocity.Y > 0)
				Projectile.velocity.Y *= 1.03f;

			if (Projectile.velocity.Y > 16f)
				Projectile.velocity.Y = 16f;
		}
		else
		{
			Projectile.velocity.Y += 0.003f;
			Projectile.velocity *= 0.975f;

			if (Main.rand.NextBool(4))
				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(25, 25), DustID.Torch, -Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(), 0, default, Main.rand.NextFloat(3f)).noGravity = true;
		}		
	}

	public override bool PreDraw(ref Color lightColor)
	{
		if (Projectile.timeLeft > MAX_TIMELEFT - 15)
			return false;

		var tex = BaseTexture.Value;
		var texWhite = TextureColorCache.ColorSolid(tex, Color.White);

		Main.instance.LoadProjectile(873); //Ensure these textures are loaded before drawing
		Main.instance.LoadProjectile(ProjectileID.FallingStar);

		var starAura = TextureAssets.Extra[ExtrasID.FallingStar].Value;
		var glowLine = TextureAssets.Projectile[873].Value;

		float fadeOut = 1f;

		if (Projectile.timeLeft < 120)
			fadeOut = Projectile.timeLeft / 120f;

		if (Projectile.timeLeft > MAX_TIMELEFT - 25)
			fadeOut = 1f - (Projectile.timeLeft - MAX_TIMELEFT + 25) / 25f;

		if (fadeOutTimer > 0)
			fadeOut = 1f - fadeOutTimer / (float)FADE_OUT_TIME;

		int fadeTime = MAX_TIMELEFT - TIME_TILL_GRAVITY;

		if (Projectile.timeLeft > fadeTime)
		{
			const float MAX_SCALE = 2.1f;
			const float MIN_SCALE = 1.2f;

			float fade = (Projectile.timeLeft - fadeTime) / (float)TIME_TILL_GRAVITY;

			for (int i = 0; i < Projectile.oldPos.Length; i++)
			{
				float interpolant = i / (float)Projectile.oldPos.Length;

				Vector2 drawPosition = Projectile.oldPos[i] - Projectile.velocity * 1.5f - Projectile.velocity * (i / 2) + Projectile.Size / 2 - Main.screenPosition;

				Main.spriteBatch.Draw(starAura, drawPosition, null, Color.Lerp(Color.DarkOrange, Color.Goldenrod, 1 - interpolant).Additive() * (1 - interpolant) * fade * fadeOut * 0.5f, Projectile.rotation, starAura.Size() / 2f, Projectile.scale * MathHelper.Lerp(1, 0.8f, interpolant) * MathHelper.Lerp(MAX_SCALE, MIN_SCALE, interpolant) * 0.38f, 0f, 0f);

				Main.spriteBatch.Draw(texWhite, drawPosition, null, Color.Lerp(Color.Orange, Color.Yellow, 1 - interpolant).Additive() * (1 - interpolant) * fade * fadeOut, Projectile.rotation, texWhite.Size() / 2f, Projectile.scale * MathHelper.Lerp(1, 0.8f, interpolant) * MathHelper.Lerp(MAX_SCALE, MIN_SCALE, interpolant), 0f, 0f);
			}

			Main.spriteBatch.Draw(texWhite, Projectile.Center - Main.screenPosition, null, Color.Yellow.Additive() * fade * fadeOut, Projectile.rotation, texWhite.Size() / 2f, Projectile.scale * MAX_SCALE, 0f, 0f);
		}

		int otherFadeTime = MAX_TIMELEFT - TIME_TILL_FIRE_FADEOUT;

		if (Projectile.timeLeft > otherFadeTime)
		{
			float fade = EaseBuilder.EaseCircularInOut.Ease((Projectile.timeLeft - otherFadeTime) / (float)TIME_TILL_FIRE_FADEOUT);

			for (int i = 0; i < Projectile.oldPos.Length; i++)
			{
				float interpolant = i / (float)Projectile.oldPos.Length;

				Vector2 drawPosition = Projectile.oldPos[i] - Projectile.velocity * i + Projectile.Size / 2 - Main.screenPosition;

				Main.spriteBatch.Draw(glowLine, drawPosition, null, Color.Orange.Additive() * (1 - interpolant) * fade * fadeOut, Projectile.rotation, glowLine.Size() / 2f, Projectile.scale * MathHelper.Lerp(0.8f, 0.5f, interpolant), 0f, 0f);

				Main.spriteBatch.Draw(texWhite, drawPosition, null, Color.Lerp(Color.Orange, Color.Yellow, 1 - interpolant).Additive() * (1 - interpolant) * fade * fadeOut, Projectile.rotation, texWhite.Size() / 2f, Projectile.scale * MathHelper.Lerp(1.3f, 0.5f, interpolant), 0f, 0f);
			}
		}

		Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor * fadeOut, Projectile.rotation, tex.Size() / 2f, Projectile.scale, 0f, 0f);
		
		if (Projectile.timeLeft > otherFadeTime)
		{
			float fade = (Projectile.timeLeft - otherFadeTime) / (float)TIME_TILL_FIRE_FADEOUT;

			Main.spriteBatch.Draw(texWhite, Projectile.Center - Main.screenPosition, null, Color.Orange.Additive() * fade * fadeOut, Projectile.rotation, texWhite.Size() / 2f, Projectile.scale, 0f, 0f);
		}

		if (Projectile.timeLeft > fadeTime)
		{
			float fade = EaseBuilder.EaseCircularOut.Ease((Projectile.timeLeft - fadeTime) / (float)TIME_TILL_GRAVITY);

			for (int i = 0; i < Projectile.oldPos.Length; i++)
			{
				float interpolant = i / (float)Projectile.oldPos.Length;

				Vector2 drawPosition = Projectile.oldPos[i] - Projectile.velocity * i + Projectile.Size / 2 - Main.screenPosition;

				Main.spriteBatch.Draw(texWhite, drawPosition, null, Color.White * (1 - interpolant) * fade * fadeOut, Projectile.rotation, texWhite.Size() / 2f, Projectile.scale * MathHelper.Lerp(0.8f, 0.1f, interpolant), 0f, 0f);
			}

			Main.spriteBatch.Draw(texWhite, Projectile.Center - Main.screenPosition, null, Color.White * fade * fadeOut, Projectile.rotation, texWhite.Size() / 2f, Projectile.scale, 0f, 0f);
		}

		return false;
	}

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		// 100-33% of damage based on distance
		if (Projectile.timeLeft > MAX_TIMELEFT - TIME_TILL_FIRE_FADEOUT)
		{
			float interpolant = (Projectile.timeLeft - MAX_TIMELEFT + TIME_TILL_FIRE_FADEOUT) / (float)TIME_TILL_FIRE_FADEOUT;

			modifiers.FinalDamage *= MathHelper.Lerp(1f, 0.2f, 1 - interpolant);
		}
		else
			modifiers.FinalDamage *= 0.2f; // 20% of damage
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => HitEffects();

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		if (Projectile.penetrate > 0)
		{
			Collision.HitTiles(Projectile.position, oldVelocity, Projectile.width, Projectile.height);
			HitEffects();
		}

		Projectile.penetrate = -1;
		if (Projectile.timeLeft < MAX_TIMELEFT - 10)
			Projectile.velocity = oldVelocity;

		return false;
	}

	void HitEffects()
	{
		// more dramatic hit
		if (Projectile.timeLeft > MAX_TIMELEFT - TIME_TILL_FIRE_FADEOUT)
		{
			float strength = 1f;

			// Point blank shot
			if (Projectile.timeLeft > MAX_TIMELEFT - 5)
			{
				if (Main.myPlayer == Projectile.owner)
					ScreenshakeHelper.Shake(Projectile.Center, -Projectile.velocity * 0.1f, 0.66f, 2, 10);

				Projectile.velocity *= 0.5f;
				strength = 1.5f;
			}

			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/BulletHit") with { Volume = 0.66f * strength, Pitch = -0.1f }, Projectile.Center);

			float rotation = Main.rand.NextFloat(6.28f);
			float scorchScale = Main.rand.NextFloat(0.08f, 0.12f) * strength;

			ParticleHandler.SpawnParticle(new DissipatingImage(Projectile.Center, Color.Orange.Additive(), rotation, scorchScale, Main.rand.NextFloat(0.1f, 0.2f), "Fire1", new(0.5f, 0.5f), new(4, 0.5f), 15)
			{
				DistortEasing = EaseFunction.EaseQuadInOut,
				Intensity = 0.5f,
				Layer = ParticleLayer.BelowNPC,
				Pixellate = true,
				PixelDivisor = 2,
			});

			ParticleHandler.SpawnParticle(new DissipatingImage(Projectile.Center, Color.LightYellow.Additive(), rotation, scorchScale * 0.6f, Main.rand.NextFloat(0.1f, 0.2f), "Fire1", new(0.5f, 0.5f), new(4, 0.5f), 10)
			{
				DistortEasing = EaseFunction.EaseQuadInOut,
				Intensity = 0.5f,
				Layer = ParticleLayer.BelowNPC,
				Pixellate = true,
				PixelDivisor = 2,
			});

			for (int i = 0; i < (int)(4 * strength); i++)
			{
				Dust.NewDustPerfect(Projectile.Center, DustID.Torch, Main.rand.NextVector2Circular(4f, 4f), 0, default, Main.rand.NextFloat(2f)).noGravity = true;

				Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f) * strength;
				float scale = Main.rand.NextFloat(0.05f, 0.15f) * strength;

				ParticleHandler.SpawnParticle(new BloomParticle(Projectile.Center, velocity, Color.Orange, scale, 30, 1, p => p.Velocity *= 0.9f));

				ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center, velocity, Color.Yellow, scale * 2f, 30, 1, p => p.Velocity *= 0.9f));
			}
		}
		else
		{
			SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);

			for (int i = 0; i < 4; i++)	
			{
				Dust.NewDustPerfect(Projectile.Center, DustID.Ash, Main.rand.NextVector2Circular(3f, 3f), 100, default, Main.rand.NextFloat(2.5f)).noGravity = true;

				Dust.NewDustPerfect(Projectile.Center, DustID.Ash, Main.rand.NextVector2Circular(2f, 2f), 150, default, Main.rand.NextFloat(2f));
			}
		}
	}
}
