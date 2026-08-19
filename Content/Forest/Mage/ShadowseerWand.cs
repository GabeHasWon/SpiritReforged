using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Mage;

[AutoloadGlowmask("255,255,255")]
public class ShadowseerWand : ModItem
{
	public sealed class ShadowseerWandHeld : ModProjectile
	{
		public int ChargeTime
		{
			get => (int)Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}

		public ref float Counter => ref Projectile.ai[1];

		public override LocalizedText DisplayName => ModContent.GetInstance<ShadowseerWand>().DisplayName;

		public override string Texture => ModContent.GetInstance<ShadowseerWand>().Texture;

		private bool _released;

		public override void SetDefaults() => base.SetDefaults();

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (owner.channel && !_released)
			{
				if (++Counter == ChargeTime)
					SoundEngine.PlaySound(SoundID.MaxMana, Projectile.Center);
			}
			else
			{
				if (!_released && Projectile.owner == Main.myPlayer)
				{
					Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<ShadowBall>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
				} //Just released

				_released = true;
			}
		}

		public override bool? CanDamage() => false;

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Vector2 position = Projectile.Center - Main.screenPosition;
			Vector2 origin = new(0, texture.Height);

			Main.EntitySpriteDraw(texture, position, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, 0);

			return false;
		}
	}

	public sealed class ShadowBall : ModProjectile
	{
		public override void SetDefaults() => base.SetDefaults();

		public override void AI() => base.AI();

		public override bool PreDraw(ref Color lightColor) => base.PreDraw(ref lightColor);
	}

	public override void SetStaticDefaults() => Item.staff[Type] = true;

	public override void SetDefaults()
	{
		Item.damage = 35;
		Item.mana = 20;
		Item.knockBack = 6.5f;
		Item.width = Item.height = 46;
		Item.useTime = Item.useAnimation = 25;
		Item.DamageType = DamageClass.Magic;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.Orange;
		Item.UseSound = SoundID.DD2_BookStaffCast with { Pitch = 0.3f };
		Item.shoot = ProjectileID.WoodenArrowFriendly;
		Item.shootSpeed = 14f;
		Item.autoReuse = true;
		Item.noUseGraphic = true;
		Item.noMelee = true;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		float chargeTime = Item.useTime * player.GetTotalAttackSpeed(DamageClass.Magic);
		Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, chargeTime); //Spawn held projectile

		return false;
	}

	public override void AddRecipes()
	{
		CreateRecipe().AddIngredient(ItemID.ShadowScale, 8).AddIngredient(ItemID.BlackLens).AddTile(TileID.Anvils).Register();
		CreateRecipe().AddIngredient(ItemID.TissueSample, 8).AddIngredient(ItemID.BlackLens).AddTile(TileID.Anvils).Register();
	}
}