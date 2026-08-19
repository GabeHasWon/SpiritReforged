namespace SpiritReforged.Content.Forest.Mage;

public class Ferroflood : ModItem
{
	public class Ferrofluid : ModProjectile
	{
		public ref float Counter => ref Projectile.ai[0];

		public override string Texture => AssetLoader.EmptyTexture;

		private NPC _target;

		public override void SetDefaults()
		{
			Projectile.Size = new(16);
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = 3;
			Projectile.extraUpdates = 1;
		}

		public override void AI()
		{
			if (_target == null || !_target.active)
			{
				const int max_distance = 50;
				bool foundTarget = false;

				foreach (NPC npc in Main.ActiveNPCs)
				{
					if (npc.CanBeChasedBy() && npc.DistanceSQ(Projectile.Center) < max_distance * max_distance)
					{
						_target = npc;
						foundTarget = true;

						break;
					}
				}

				if (!foundTarget)
				{
					Projectile.velocity *= 0.98f;
				}
			}
			else
			{
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(_target.Center) * 3, 0.02f);
			}
		}

		public override void OnKill(int timeLeft)
		{
		}

		public override bool PreDraw(ref Color lightColor)
		{
			return false;
		}
	}

	public override void SetStaticDefaults() => Item.staff[Type] = true;

	public override void SetDefaults()
	{
		Item.damage = 9;
		Item.mana = 3;
		Item.knockBack = 6.5f;
		Item.width = Item.height = 46;
		Item.useTime = Item.useAnimation = 8;
		Item.DamageType = DamageClass.Magic;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.Blue;
		Item.UseSound = SoundID.DD2_BookStaffCast with { Pitch = 0.3f };
		Item.shoot = ModContent.ProjectileType<Ferrofluid>();
		Item.shootSpeed = 7f;
		Item.autoReuse = true;
		Item.noMelee = true;
	}

	public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
	{
		Vector2 offset = Vector2.Normalize(velocity) * 30;

		if (Collision.CanHit(position, 2, 2, position + velocity, 2, 2))
			position += offset;
	}
}