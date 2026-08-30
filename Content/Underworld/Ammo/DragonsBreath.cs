using Microsoft.Xna.Framework.Graphics;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Subclasses.Greatshields;
using SpiritReforged.Common.Subclasses.Shotguns;
using SpiritReforged.Content.Forest.Ammo;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underworld.Ammo;
public class DragonsBreath : ShotgunAmmoItem
{
	public override void SetStaticDefaults() => NPCShopHelper.AddEntry(NPCShopHelper.ConditionalEntry.FromNPC(NPCID.ArmsDealer, new NPCShop.Entry(Type)));

	static void Behavior(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 direction, int shotCount, float spreadAmount, float speed, int damage, float knockback)
	{
		for (int i = 0; i < shotCount; i++)
		{
			Vector2 spreadDir = direction;

			if (spreadAmount > 0f && i != 0) // no spread on first shot
				spreadDir = direction.RotatedByRandom(spreadAmount);

			Projectile.NewProjectile(source, position, spreadDir * speed * Main.rand.NextFloat(0.75f, 1.5f), ModContent.ProjectileType<DragonsBreathProjectile>(), damage, knockback, player.whoAmI);

			for (int x = 0; x < 6; x++)
				Dust.NewDustPerfect(position, DustID.Torch, direction.RotatedByRandom(spreadAmount * 1.25f) * Main.rand.NextFloat(speed, speed * 2f), 0, default, Main.rand.NextFloat(1.5f)).noGravity = true;

			Dust.NewDustPerfect(position + direction * speed, DustID.Smoke, direction.RotatedByRandom(0.4f) * Main.rand.NextFloat(3f), 240, default, Main.rand.NextFloat(3f, 6f));

			for (int x = 0; x < 2; x++)
			{
				ParticleHandler.SpawnParticle(new SmokeCloud(position, direction.RotatedByRandom(0.2f) * Main.rand.NextFloat(speed * 0.5f), Color.DarkGray * 0.25f, Main.rand.NextFloat(0.02f, 0.09f), EaseFunction.EaseCubicOut, 90)
				{
					Pixellate = true,
					PixelDivisor = 2,
				});

				ParticleHandler.SpawnParticle(new SmokeCloud(position, direction.RotatedByRandom(0.25f) * Main.rand.NextFloat(speed * 0.65f), Color.Black * 0.15f, Main.rand.NextFloat(0.03f, 0.12f), EaseFunction.EaseCubicOut, 40)
				{
					Pixellate = true,
					PixelDivisor = 3,
				});

				Vector2 velo = direction.RotatedByRandom(0.5f) * Main.rand.NextFloat(speed / 2);

				ParticleHandler.SpawnParticle(new BloomParticle(position, velo, Color.DarkOrange, 0.2f, 30, 1, (particle) => particle.Velocity *= 0.935f));

				ParticleHandler.SpawnParticle(new GlowParticle(position, velo, Color.Orange, 0.3f, 30, 1, (particle) => particle.Velocity *= 0.935f));
			}
		}
	}

	public DragonsBreath() : base(Behavior, 7, .7f, 15f) { }

	public override void SafeSetDefaults()
	{
		Item.damage = 9;
		Item.rare = ItemRarityID.Orange;
		Item.value = Item.sellPrice(silver: 1);
	}

	public override void AddRecipes()
	{
		CreateRecipe(50).
			AddIngredient<Shot>(50).
			AddIngredient(ItemID.HellstoneBar).
			AddTile(TileID.Anvils).
			Register();
	}
}

public class DragonsBreathProjectile : ModProjectile
{
	public const int MAX_TIMELEFT = 35;

	bool _hitTile;

	public override string Texture => AssetLoader.EmptyTexture;

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailingMode[Type] = 0;
		ProjectileID.Sets.TrailCacheLength[Type] = 8;
	}

	public override void SetDefaults()
	{
		Projectile.friendly = true;
		Projectile.DamageType = ModContent.GetInstance<ShotgunClass>();
		Projectile.extraUpdates = 1;
		Projectile.Size = new(6);
		Projectile.timeLeft = MAX_TIMELEFT;
		Projectile.penetrate = 5;
		Projectile.scale = Main.rand.NextFloat(0.15f, 1f);
		Projectile.tileCollide = false;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 20;
	}

	public override void AI()
	{
		Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

		if (Main.rand.NextBool(3))
			Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(16f, 16f), DustID.Torch, -Projectile.velocity * Main.rand.NextFloat(0.2f), 0, default, Main.rand.NextFloat(2.25f)).noGravity = true;

		if (Main.rand.NextBool(15))
		{
			Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(16f, 16f);
			Vector2 velocity = Projectile.velocity.RotatedByRandom(0.2f) * Main.rand.NextFloat(0.4f, 1f);

			ParticleHandler.SpawnParticle(new SparkParticle(pos, velocity, Color.DarkOrange, 1f, 30,
				(particle) => particle.Velocity *= 0.95f, tileCollide: false));
		}

		if (!_hitTile)
		{
			Tile tile = Framing.GetTileSafely((int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16);
			if (tile.HasTile && tile.BlockType == BlockType.Solid && Main.tileSolid[tile.TileType] && !TileID.Sets.Platforms[tile.TileType])
			{
				for (int i = 0; i < 2; i++)
				{
					Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(32f, 32f);
					Vector2 velocity = Projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.75f, 1f);

					ParticleHandler.SpawnParticle(new SparkParticle(pos, velocity, Color.Lerp(Color.DarkOrange, Color.OrangeRed, Main.rand.NextFloat()), 1.35f, Main.rand.Next(30, 60), SparkUpdate, tileCollide: false));
				}

				for (int i = 0; i < 6; i++)
				{
					Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(32f, 32f);
					Vector2 velocity = Projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.75f, 1f);

					ParticleHandler.SpawnParticle(new SparkParticle(pos, velocity, Color.Lerp(Color.DarkOrange, Color.OrangeRed, Main.rand.NextFloat()), 1f, Main.rand.Next(45, 75), (p) => p.Velocity *= 0.95f, tileCollide: false));
				}

				for (int i = 0; i < 6; i++)
				{
					Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(32f, 32f);
					Vector2 velocity = Main.rand.NextVector2Circular(12f, 12f);

					ParticleHandler.SpawnParticle(new SparkParticle(pos, velocity, Color.Lerp(Color.DarkOrange, Color.OrangeRed, Main.rand.NextFloat()), 0.5f, Main.rand.Next(80, 150), SparkUpdate, tileCollide: false));
				}

				static void SparkUpdate(Particle p)
				{
					p.Velocity *= 0.95f;
					p.Velocity.Y += 0.5f;
				}

				Projectile.penetrate -= 2;
				Projectile.damage /= 2;
				Projectile.velocity *= 0.66f;
				_hitTile = true;
			}
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Main.instance.LoadProjectile(873); //Ensure these textures are loaded before drawing
		Main.instance.LoadProjectile(ProjectileID.FallingStar);

		var starAura = TextureAssets.Extra[ExtrasID.FallingStar].Value;
		var glowLine = TextureAssets.Projectile[873].Value;

		float time = MathHelper.Min(Projectile.timeLeft / (float)MAX_TIMELEFT, 1f);
		float fadeIn = 1f;
		if (time > 0.5f)
			fadeIn = 1f - (time - 0.5f) / 0.5f;

		int trailLength = Projectile.oldPos.Length;

		for (int i = 0; i < trailLength; i++)
		{
			float lerp = i / (float)trailLength;

			Vector2 drawPos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;

			Color color = Color.Lerp(new(255, 200, 100), new(255, 20, 0), lerp).Additive();

			Vector2 scale = new Vector2(Projectile.scale * time, Projectile.scale) * fadeIn * (1f - lerp);

			Main.spriteBatch.Draw(glowLine, drawPos, null, color * 0.5f, Projectile.rotation, glowLine.Size() / 2f, new Vector2(scale.X * 1.5f, scale.Y * 2.2f), 0f, 0f);

			Main.spriteBatch.Draw(starAura, drawPos, null, color, Projectile.rotation, starAura.Size() / 2f, scale, 0f, 0f);
		}

		return false;
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		float strength = Projectile.penetrate / 5f;

		Projectile.damage = (int)(Projectile.damage * 0.66f);
		if (Projectile.damage < 3)
			Projectile.damage = 3;

		Projectile.velocity *= 0.9f;

		target.AddBuff(BuffID.OnFire, 180);

		// create an insanely violent spray of sparks and fire on hit

		for (int i = 0; i < Math.Max(1, (int)(3 * strength)); i++)
		{
			Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(32f, 32f);
			Vector2 velocity = Projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.75f, 1f) * strength;

			ParticleHandler.SpawnParticle(new SparkParticle(pos, velocity, Color.Lerp(Color.DarkOrange, Color.OrangeRed, Main.rand.NextFloat()), 1.35f, Main.rand.Next(30, 60), SparkUpdate));
		}
		
		for (int i = 0; i < Math.Max(1, (int)(3 * strength)); i++)
		{
			Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(32f, 32f);
			Vector2 velocity = Projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.75f, 1f) * strength;

			ParticleHandler.SpawnParticle(new SparkParticle(pos, velocity, Color.Lerp(Color.DarkOrange, Color.OrangeRed, Main.rand.NextFloat()), 1f, Main.rand.Next(45, 75), (p) => p.Velocity *= 0.95f, tileCollide: false));
		}

		for (int i = 0; i < Math.Max(1, (int)(18 * strength)); i++)
		{
			Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(32f, 32f);
			Vector2 velocity = Main.rand.NextVector2Circular(15f, 15f) * strength;

			ParticleHandler.SpawnParticle(new SparkParticle(pos, velocity, Color.Lerp(Color.DarkOrange, Color.OrangeRed, Main.rand.NextFloat()), 0.5f, Main.rand.Next(80, 150), SparkUpdate));
		}

		static void SparkUpdate(Particle p)
		{
			p.Velocity *= 0.95f;
			p.Velocity.Y += 0.5f;
		}
	}
}

