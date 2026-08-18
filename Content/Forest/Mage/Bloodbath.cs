using SpiritReforged.Common.Visuals.Glowmasks;

namespace SpiritReforged.Content.Forest.Mage;

[AutoloadGlowmask("255,255,255")]
public class Bloodbath : ModItem
{
	public class BloodStream : ModProjectile
	{
		public ref float Counter => ref Projectile.ai[0];

		public override string Texture => AssetLoader.EmptyTexture;

		public override void SetDefaults()
		{
			Projectile.Size = new(16);
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.timeLeft = 40;
			Projectile.penetrate = 3;
			Projectile.extraUpdates = 1;
		}

		public override void AI()
		{
			if (++Counter > 30)
				Projectile.velocity.Y += 0.25f;

			Dust.NewDustPerfect(Projectile.Center, DustID.Blood, Projectile.velocity * 0.5f);
		}

		public override void OnKill(int timeLeft)
		{
		}

		public override bool PreDraw(ref Color lightColor)
		{
			return false;
		}
	}

	public override void SetStaticDefaults() { } //DROPS

	public override void SetDefaults()
	{
		Item.damage = 28;
		Item.mana = 10;
		Item.knockBack = 6.5f;
		Item.width = Item.height = 46;
		Item.useTime = Item.useAnimation = 34;
		Item.DamageType = DamageClass.Magic;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.Blue;
		Item.UseSound = SoundID.DD2_BookStaffCast with { Pitch = 0.3f };
		Item.shoot = ModContent.ProjectileType<BloodStream>();
		Item.shootSpeed = 14f;
		Item.autoReuse = true;
		Item.useTurn = true;
		Item.noUseGraphic = true;
		Item.noMelee = true;
	}

	public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
	{
		Vector2 offset = Vector2.Normalize(velocity) * 30;

		if (Collision.CanHit(position, 2, 2, position + velocity, 2, 2))
			position += offset;
	}
}