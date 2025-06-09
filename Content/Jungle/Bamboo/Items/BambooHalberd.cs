using SpiritReforged.Common.ModCompat;

namespace SpiritReforged.Content.Jungle.Bamboo.Items;

public class BambooHalberd : ModItem
{
	public override void SetStaticDefaults() => ItemID.Sets.Spears[Type] = true;
	public override void SetDefaults()
	{
		Item.damage = 5;
		Item.knockBack = 2f;
		Item.width = Item.height = 24;
		Item.value = Item.sellPrice(silver: 4);
		Item.rare = ItemRarityID.White;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.useTime = Item.useAnimation = 20;
		Item.DamageType = DamageClass.Melee;
		Item.noMelee = true;
		Item.autoReuse = true;
		Item.noUseGraphic = true;
		Item.shoot = ModContent.ProjectileType<BambooHalberdProj>();
		Item.shootSpeed = 2f;
		Item.UseSound = SoundID.Item1;
	}

	public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback) => velocity = velocity.RotatedByRandom(.1f);
	public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.BambooBlock, 20).AddTile(TileID.WorkBenches).Register();
}

public class BambooHalberdProj : ModProjectile
{
	public int Counter { get => (int)Projectile.ai[0]; set => Projectile.ai[0] = value; }
	public int CounterMax => Main.player[Projectile.owner].itemTimeMax - 2;

	private readonly int lungeLength = 54;

	public override LocalizedText DisplayName => Language.GetText("Mods.SpiritReforged.Items.BambooHalberd.DisplayName");

	public override void SetDefaults()
	{
		Projectile.Size = new Vector2(10);
		Projectile.friendly = true;
		Projectile.penetrate = -1;
		Projectile.DamageType = DamageClass.Melee;
		Projectile.ignoreWater = true;
		Projectile.tileCollide = false;
		Projectile.ownerHitCheck = true;

		MoRHelper.SetSpearBonus(Projectile);
	}

	public override void AI()
	{
		Player owner = Main.player[Projectile.owner];
		Projectile.rotation = Projectile.velocity.ToRotation();

		if (!owner.frozen)
			Counter++;
		if (Counter < CounterMax && owner.active && !owner.dead)
			Projectile.timeLeft = 2;

		int halfTime = (int)(CounterMax * .5f);
		Vector2 TargetVector(float magnitude) => (Vector2.UnitX * magnitude).RotatedBy(Projectile.rotation);

		if (Counter > halfTime)
			Projectile.velocity = Vector2.Lerp(TargetVector(lungeLength), TargetVector(30), (float)Counter / CounterMax);
		else
			Projectile.velocity = Vector2.Lerp(TargetVector(10), TargetVector(lungeLength), (float)Counter / halfTime);

		owner.itemRotation = MathHelper.WrapAngle(owner.AngleTo(Projectile.Center) - owner.fullRotation - (owner.direction < 0 ? MathHelper.Pi : 0));
		owner.heldProj = Projectile.whoAmI;
		owner.ChangeDir(Projectile.direction);
		Projectile.Center = owner.MountedCenter + Projectile.velocity;

		if (Counter < halfTime)
		{
			var dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(20f), DustID.JunglePlants, null, 90, default, Scale: (float)Counter / halfTime * 1.5f);
			dust.noGravity = true;
			dust.velocity = Vector2.Normalize(Projectile.velocity).RotatedByRandom(1);
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D texture = TextureAssets.Projectile[Type].Value;
		SpriteEffects effects = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		var origin = new Vector2(effects == SpriteEffects.FlipHorizontally ? Projectile.width / 2 : texture.Width - Projectile.width / 2, texture.Height / 2);
		lightColor = Lighting.GetColor((Main.player[Projectile.owner].MountedCenter / 16).ToPoint());

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, effects, 0);

		return false;
	}
}