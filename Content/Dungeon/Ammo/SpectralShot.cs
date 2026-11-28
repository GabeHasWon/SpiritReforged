using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Subclasses.Greatshields;
using SpiritReforged.Common.Subclasses.Shotguns;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Forest.Ammo;
using SpiritReforged.Content.Particles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Dungeon.Ammo;

public class SpectralShot : ShotgunAmmoItem
{
	static List<Projectile> Behavior(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 direction, int shotCount, float spreadAmount, float speed, int damage, float knockback)
	{
		var spawnedProjectiles = new List<Projectile>();

		for (int i = 0; i < shotCount; i++)
		{
			Vector2 spreadDir = direction;

			if (spreadAmount > 0f && i != 0) // no spread on first shot
				spreadDir = direction.RotatedByRandom(spreadAmount);

			var p = Projectile.NewProjectileDirect(source, position, spreadDir * speed * Main.rand.NextFloat(0.75f, 1.5f), ModContent.ProjectileType<SpectralShotProjectile>(), damage, knockback, player.whoAmI);
			spawnedProjectiles.Add(p);
		}

		return spawnedProjectiles;
	}

	public SpectralShot() : base(Behavior, 4, .8f, 16.5f) { }

	public override void SafeSetDefaults()
	{
		Item.damage = 15;
		Item.value = Item.sellPrice(silver: 1);
		Item.rare = ItemRarityID.Lime;
	}

	public override void AddRecipes()
	{
		CreateRecipe(75).
			AddIngredient<Shot>(75).
			AddIngredient(ItemID.Ectoplasm).
			AddTile(TileID.MythrilAnvil).
			Register();
	}
}

public class SpectralShotProjectile : ModProjectile
{
	public const int MAX_TIMELEFT = 85;
	public const int HOMING_TIME = 45;

	public override string Texture => AssetLoader.EmptyTexture;

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailingMode[Type] = 0;
		ProjectileID.Sets.TrailCacheLength[Type] = 7;
	}

	public override void SetDefaults()
	{
		Projectile.friendly = true;
		Projectile.DamageType = ModContent.GetInstance<ShotgunClass>();
		Projectile.Size = new(6);
		Projectile.timeLeft = MAX_TIMELEFT;
		Projectile.scale = Main.rand.NextFloat(.45f, 1f);
	}

	public override void AI()
	{
		if (++Projectile.ai[0] <= HOMING_TIME)
		{
			HomingAI();

			return;
		}

		Projectile.velocity *= 0.975f;
		Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
	}

	void HomingAI()
	{
		Projectile.TryGetOwner(out var player);

		Vector2 mousePosition = Vector2.Zero;
		if (Main.myPlayer == player.whoAmI)
			mousePosition = Main.MouseWorld;

		float progress = Projectile.ai[0] / HOMING_TIME;

		float homingSpeed = MathHelper.Lerp(17f, 2f, progress);

		Projectile.velocity = (Projectile.velocity * homingSpeed + (mousePosition - Projectile.Center).SafeNormalize(Vector2.UnitX) * 12f) / (homingSpeed + 1);

		Projectile.rotation += Projectile.velocity.Length() * 0.01f;

		if (Main.rand.NextBool(3))
			ParticleRenderers.OverSolids.Add(new SmokeCloud(Projectile.Center + Main.rand.NextVector2Circular(15, 15), Vector2.Zero, Color.Lerp(Color.LightSeaGreen, Color.LightSkyBlue, Main.rand.NextFloat()) * 0.35f, Main.rand.NextFloat(0.05f, 0.09f), EaseFunction.EaseQuadOut, Main.rand.Next(30, 60))
			{
				Pixellate = true,
				PixelDivisor = 2
			});

		if (Main.rand.NextBool(5))
		{
			Vector2 velocity = Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.5f);
			float scale = Main.rand.NextFloat(0.66f);
			ParticleRenderers.UnderProjectiles.Add(new GlowParticle(Projectile.Center, velocity, Color.Cyan * 0.5f, scale, 40));
			ParticleRenderers.UnderProjectiles.Add(new GlowParticle(Projectile.Center, velocity, Color.LightCyan, scale, 30));
		}

		if (Projectile.ai[0] == HOMING_TIME)
		{
			Projectile.extraUpdates++;
			Projectile.tileCollide = false;

			Projectile.velocity = Projectile.velocity.RotatedByRandom(0.1f) * Main.rand.NextFloat(1.75f, 2.65f);
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		var bloom = AssetLoader.LoadedTextures["Bloom"].Value;
		var star = AssetLoader.LoadedTextures["Star"].Value;

		float progress = 1f - Projectile.timeLeft / (float)MAX_TIMELEFT;

		if (progress < 0.5f)
		{
			float fade = 1f - progress / 0.5f;

			Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, Color.LightCyan.Additive() * 0.2f * fade, Projectile.rotation, bloom.Size() / 2f, Projectile.scale * 0.33f, 0f, 0f);
			Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, Color.Turquoise.Additive() * 0.2f, Projectile.rotation, star.Size() / 2f, Projectile.scale * 0.2f * fade, 0f, 0f);
			Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, Color.White.Additive() * 0.2f, Projectile.rotation, star.Size() / 2f, Projectile.scale * 0.2f * fade, 0f, 0f);
		}

		if (Projectile.ai[0] >= HOMING_TIME)
		{
			Main.instance.LoadProjectile(873); //Ensure these textures are loaded before drawing
			Main.instance.LoadProjectile(ProjectileID.FallingStar);

			float time = Projectile.timeLeft / (float)(MAX_TIMELEFT - HOMING_TIME);
			float fadeIn = 1f;
			if (time > 0.5f)
				fadeIn = 1f - (time - 0.5f) / 0.5f;

			int trailLength = Projectile.oldPos.Length;

			for (int i = 0; i < trailLength; i++)
			{
				var texture = TextureAssets.Projectile[873].Value;

				float lerp = 1f - i / (float)(trailLength - 1);
				var color = (Color.Lerp(Color.Turquoise, Color.LightCyan, lerp) with { A = 0 }) * lerp;
				var position = Projectile.oldPos[i] - Main.screenPosition;
				var scale = new Vector2(time, 1f) * Projectile.scale;

				if (i == 0)
				{
					color = Color.LightCyan with { A = 0 };
					texture = TextureAssets.Extra[ExtrasID.FallingStar].Value;
					scale = new Vector2(MathHelper.Max(time, .25f), 1f) * Projectile.scale * 0.5f;
				}

				Main.EntitySpriteDraw(texture, position, null, color * fadeIn, Projectile.velocity.ToRotation() + MathHelper.PiOver2, texture.Size() / 2, scale, SpriteEffects.None);
			}
		}

		return false;
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{

	}
}
