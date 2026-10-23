using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Crossbows;

public class Starbalest : ModItem, ReloadPlayer.IPerfectReload
{
	public class StarburstProjectile : GlobalProjectile
	{
		public override bool InstancePerEntity => true;

		public bool starburst;

		public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => BoltCache.IsCrossbowBolt[entity.type];

		public override void AI(Projectile projectile)
		{
			if (starburst && Main.rand.NextBool())
				ParticleRenderers.UnderProjectiles.Add(new EmberParticle(projectile.Center + Main.rand.NextVector2Circular(4, 4), projectile.velocity * Main.rand.NextFloat(0f, 0.2f), Color.Lerp(Color.Yellow, Color.BlueViolet, Main.rand.NextFloat()), Main.rand.NextFloat(0.1f, 0.5f), 20, 3));
		}

		public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (starburst)
				Explode(projectile);
		}

		public override void OnKill(Projectile projectile, int timeLeft)
		{
			if (starburst)
				Explode(projectile);
		}

		private static void Explode(Projectile projectile)
		{
			SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion with { PitchVariance = 0.3f }, projectile.Center);
		}
	}

	public class StarbalestHeld : Crossbow.CrossbowHeld
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<Starbalest>().DisplayName;

		public override string Texture => DrawHelpers.RequestLocal<StarbalestHeld>("StarbalestHeld");

		public override IConfiguration SetConfiguration() => new Crossbow.CrossbowConfiguration(null, 30, 10, 40);

		public override bool PreDraw(ref Color lightColor)
		{
			if (!ReloadAnimation)
			{
				Texture2D texture = AssetLoader.LoadedTextures["Star"].Value;
				float quoteant = 1f - Math.Min(Counter / 10f, 1);

				Main.EntitySpriteDraw(texture, GetEndPosition(-4) - Main.screenPosition, null, Color.Yellow.Additive(), 0, texture.Size() / 2, Projectile.scale * 0.3f * quoteant, 0);
				Main.EntitySpriteDraw(texture, GetEndPosition(-4) - Main.screenPosition, null, Color.White.Additive(), 0, texture.Size() / 2, Projectile.scale * 0.2f * quoteant, 0);
			}

			return base.PreDraw(ref lightColor);
		}
	}

	public bool PerfectReload { get; set; }

	public override void SetDefaults()
    {
		Item.DefaultToBow(50, 10, true);
		Item.UseSound = SoundID.DD2_BallistaTowerShot with { Pitch = 0.5f };
		Item.useAmmo = ModContent.ItemType<Bolt>();
		Item.damage = 10;
		Item.knockBack = 4.5f;
		Item.noUseGraphic = true;
		Item.rare = ItemRarityID.Blue;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
		SwungProjectile.Spawn(position, velocity, ModContent.ProjectileType<StarbalestHeld>(), damage, knockback, player, 0, source);
		PreNewProjectile.New(source, position, velocity, type, damage, knockback, player.whoAmI, preSpawnAction: (projectile) =>
		{
			if (PerfectReload && projectile.TryGetGlobalProjectile(out StarburstProjectile global))
				global.starburst = true;
		});

		PerfectReload = false;
		return false;
    }

	public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<Crossbow>()).AddIngredient(ItemID.MeteoriteBar, 10).AddIngredient(ItemID.FallenStar, 5).AddTile(TileID.Anvils).Register();
}