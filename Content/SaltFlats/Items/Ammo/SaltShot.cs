using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Subclasses.Greatshields;
using SpiritReforged.Common.Subclasses.Shotguns;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Forest.Ammo;
using SpiritReforged.Content.Ocean.Items.JellyMinion;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.SaltFlats.Items.Ammo;
public class SaltShot : ShotgunAmmoItem
{
	static void Behavior(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 direction, int shotCount, float spreadAmount, float speed, int damage, float knockback)
	{
		for (int i = 0; i < shotCount; i++)
		{
			Vector2 spreadDir = direction;

			if (spreadAmount > 0f && i != 0) // no spread on first shot
				spreadDir = direction.RotatedByRandom(spreadAmount);

			Projectile.NewProjectile(source, position, spreadDir * speed * Main.rand.NextFloat(0.75f, 1.5f), ModContent.ProjectileType<SaltShotProjectile>(), damage, knockback, player.whoAmI);

			for (int x = 0; x < 2; x++)
			{
				ParticleHandler.SpawnParticle(new SmokeCloud(position, direction.RotatedByRandom(0.2f) * Main.rand.NextFloat(speed * 0.5f), Color.LightPink * 0.2f, Main.rand.NextFloat(0.01f, 0.04f), EaseFunction.EaseCubicOut, 60)
				{
					Pixellate = true,
					PixelDivisor = 2,
				});

				ParticleHandler.SpawnParticle(new SmokeCloud(position, direction.RotatedByRandom(0.25f) * Main.rand.NextFloat(speed * 0.65f), Color.WhiteSmoke * 0.1f, Main.rand.NextFloat(0.02f, 0.06f), EaseFunction.EaseCubicOut, 85)
				{
					Pixellate = true,
					PixelDivisor = 3,
				});

				Dust.NewDustPerfect(position, DustID.WhiteTorch, direction.RotatedByRandom(spreadAmount * 1.25f) * Main.rand.NextFloat(speed, speed * 2f), 0, default, Main.rand.NextFloat(2f)).noGravity = true;
			}
		}
	}

	public SaltShot() : base(Behavior, 4, .55f, 18.5f) { }

	public override void SafeSetDefaults()
	{
		Item.rare = ItemRarityID.Blue;
		Item.damage = 6;
		Item.knockBack = 1f;
	}

	public override void AddRecipes()
	{
		CreateRecipe(50).
			AddIngredient<Shot>(50).
			AddRecipeGroup("Salt", 5).
			AddTile(TileID.Anvils).
			Register();
	}
}

public class SaltShotProjectile : ModProjectile
{
	public static readonly Asset<Texture2D> BaseTexture = DrawHelpers.RequestLocal<SaltShotProjectile>("SaltShotProjectile", false);

	private static readonly List<int> eyeEnemies = [
		NPCID.EyeofCthulhu,
		NPCID.EyeballFlyingFish,
		NPCID.Eyezor,
		NPCID.WanderingEye,
		NPCID.Retinazer,
		NPCID.Spazmatism,
		NPCID.Creeper,
		NPCID.MoonLordFreeEye,
		NPCID.MoonLordHand,
		NPCID.MoonLordHead,
		NPCID.ServantofCthulhu,
		NPCID.Drippler,
		NPCID.WallofFleshEye,
	];

	public const int MAX_TIMELEFT = 240;
	public const int TIME_TILL_GRAVITY = 30; // how many frames before gravity kicks in, and the fire effects fade off
	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailingMode[Type] = 0;
		ProjectileID.Sets.TrailCacheLength[Type] = 6;
	}

	public override void SetDefaults()
	{
		Projectile.friendly = true;
		Projectile.DamageType = ModContent.GetInstance<ShotgunClass>();
		Projectile.penetrate = 2;
		Projectile.Size = new(4);
		Projectile.timeLeft = MAX_TIMELEFT;
		Projectile.scale = Main.rand.NextFloat(.5f, 1.1f);
		Projectile.frame = Main.rand.Next(4);
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 20;
		Projectile.extraUpdates = 1;
	}

	public override void AI()
	{
		Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

		if (Projectile.timeLeft < MAX_TIMELEFT - TIME_TILL_GRAVITY)
		{
			Projectile.velocity *= 0.995f;
			Projectile.velocity.Y += 0.1f;

			if (Projectile.velocity.Y > 0)
				Projectile.velocity.Y *= 1.05f;

			if (Projectile.velocity.Y > 16f)
				Projectile.velocity.Y = 16f;
		}
		else
			Projectile.velocity *= 0.96f;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		var tex = BaseTexture.Value;
		var bloom = AssetLoader.LoadedTextures["Bloom"].Value;

		var frame = tex.Frame(1, 4, 0, Projectile.frame);

		const int bloomFade = 70;
		int bloomTime = MAX_TIMELEFT - bloomFade;

		if (Projectile.timeLeft > MAX_TIMELEFT - bloomFade)
		{
			float fadeOut = (Projectile.timeLeft - bloomTime) / (float)bloomFade;

			Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, Color.MediumPurple.Additive() * 0.3f * fadeOut, Projectile.rotation, bloom.Size() / 2f, Projectile.scale * 0.135f, 0f, 0f);
			Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, Color.White.Additive() * 0.25f * fadeOut, Projectile.rotation, bloom.Size() / 2f, Projectile.scale * 0.11f, 0f, 0f);
		}

		Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, frame, lightColor * 0.55f, Projectile.rotation, frame.Size() / 2f, Projectile.scale, 0f, 0f);

		int fadeTime = MAX_TIMELEFT - TIME_TILL_GRAVITY;

		if (Projectile.timeLeft > fadeTime)
		{
			Main.instance.LoadProjectile(873); //Ensure these textures are loaded before drawing
			Main.instance.LoadProjectile(ProjectileID.FallingStar);

			float time = MathHelper.Min((Projectile.timeLeft - fadeTime) / (float)TIME_TILL_GRAVITY, 1f);
			float fadeIn = 1f;
			if (time > 0.75f)
				fadeIn = 1f - (time - 0.75f) / 0.25f;

			int trailLength = Projectile.oldPos.Length;

			Color baseColor = Color.White;

			for (int i = 0; i < trailLength; i++)
			{
				var texture = TextureAssets.Projectile[873].Value;

				float lerp = 1f - i / (float)(trailLength - 1);
				var color = (Color.Lerp(Color.Purple, baseColor, lerp) with { A = 0 }) * lerp;
				var position = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
				var scale = new Vector2(time, 1f) * Projectile.scale * 1.05f;

				if (i == 0)
				{
					color = Color.White with { A = 0 };
					texture = TextureAssets.Extra[ExtrasID.FallingStar].Value;
					scale = new Vector2(MathHelper.Max(time, .25f), 1f) * Projectile.scale * .4f;
				}

				Main.EntitySpriteDraw(texture, position - Projectile.velocity * 0.5f, null, color * fadeIn, Projectile.rotation, texture.Size() / 2, scale * fadeIn, SpriteEffects.None);
			}
		}

		return false;
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		if (Projectile.timeLeft > MAX_TIMELEFT - TIME_TILL_GRAVITY)
			Projectile.timeLeft = MAX_TIMELEFT - TIME_TILL_GRAVITY;

		Projectile.velocity.X = oldVelocity.X * 0.75f;
		Projectile.velocity.Y = -oldVelocity.Y * 0.4f;
		Projectile.damage = (int)(Projectile.damage * 0.75f);

		Projectile.penetrate--;

		SpawnDusts();

		return false;
	}

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		if (eyeEnemies.Contains(target.type) || target.BannerID() == NPCID.DemonEye)
			modifiers.FinalDamage *= 2f;
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (Projectile.timeLeft > MAX_TIMELEFT - TIME_TILL_GRAVITY)
			Projectile.timeLeft = MAX_TIMELEFT - TIME_TILL_GRAVITY;

		SoundEngine.PlaySound(SoundID.Tink with { Pitch = 0.25f, Volume = 0.5f }, target.Center);

		Projectile.velocity *= -0.35f;
		Projectile.velocity.Y -= 2;

		SpawnDusts();

		if (eyeEnemies.Contains(target.type) || target.BannerID() == NPCID.DemonEye)
			for (int i = 0; i < 3; i++)
				Dust.NewDustPerfect(Projectile.Center, DustID.Blood, Main.rand.NextVector2Circular(2.5f, 2.5f), 100, default, 0.9f).noGravity = true;
	}

	public override void OnKill(int timeLeft)
	{
		SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Tile/SaltMine" + Main.rand.Next(1, 4)) with { Pitch = 0.3f, Volume = 0.33f }, Projectile.Center);
	}

	void SpawnDusts()
	{
		for (int i = 0; i < 2; i++)
			Dust.NewDustPerfect(Projectile.Center, DustID.Pearlsand, Main.rand.NextVector2Circular(0.5f, 0.5f), 200, default, 0.65f).noGravity = true;

		ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Center, Main.rand.NextVector2Circular(0.25f, 0.25f), Color.LightCyan * 0.33f, Projectile.scale * 0.03f, EaseFunction.EaseCubicOut, 30)
		{
			Pixellate = true,
			PixelDivisor = 2,
		});
	}
}
