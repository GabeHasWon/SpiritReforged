using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Crossbows;

public class Starbalest : ModItem, ReloadPlayer.IPerfectReload
{
	public class StarburstProjectile : GlobalProjectile
	{
		public override bool InstancePerEntity => true;

		public bool starburst;

		public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => lateInstantiation && entity.arrow;

		public override void AI(Projectile projectile) => base.AI(projectile);

		public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone) => base.OnHitNPC(projectile, target, hit, damageDone);

		public override void OnKill(Projectile projectile, int timeLeft) => base.OnKill(projectile, timeLeft);

		private void Explode(Projectile projectile)
		{

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
		PerfectReload = false;

		return true;
    }

	public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<Crossbow>()).AddIngredient(ItemID.MeteoriteBar, 10).AddIngredient(ItemID.FallenStar, 5).AddTile(TileID.Anvils).Register();
}