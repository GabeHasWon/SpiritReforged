using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Content.Forest.Botanist.Items;
using Terraria.Audio;

namespace SpiritReforged.Content.Ocean.Items.Rum;

public class ExplosiveRum : ModItem
{
	public override void SetStaticDefaults()
	{
		NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry((shop) => shop.NpcType == NPCID.DD2Bartender,
		new NPCShop.Entry(ModContent.ItemType<ExplosiveRum>(), Condition.InBeach)));

		MoRHelper.AddElement(Item, MoRHelper.Explosive);
		MoRHelper.AddElement(Item, MoRHelper.Fire, true);
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 24;
		Item.useAnimation = Item.useTime = 30;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.UseSound = SoundID.Item1;
		Item.DamageType = DamageClass.Ranged;
		Item.maxStack = Item.CommonMaxStack;
		Item.shoot = ModContent.ProjectileType<ExplosiveRumProj>();
		Item.noUseGraphic = true;
		Item.noMelee = true;
		Item.consumable = true;
		Item.autoReuse = true;
		Item.consumable = true;
		Item.shootSpeed = 10.5f;
		Item.damage = 13;
		Item.knockBack = 1.5f;
		Item.value = Item.sellPrice(0, 0, 0, 50);
		Item.rare = ItemRarityID.Blue;
	}
}

public class ExplosiveRumProj : ModProjectile
{
	public override LocalizedText DisplayName => ModContent.GetInstance<ExplosiveRum>().DisplayName;
	public static readonly SoundStyle Boom = new("SpiritReforged/Assets/SFX/Item/Rumboom");

	public override void SetDefaults()
	{
		Projectile.CloneDefaults(ProjectileID.Shuriken);
		Projectile.width = Projectile.height = 20;
		Projectile.DamageType = DamageClass.Ranged;
		Projectile.penetrate = 1;
	}

	public override void AI()
	{
		var dust = Dust.NewDustPerfect(Projectile.Center + 15 * (Projectile.rotation - 1.57f).ToRotationVector2(), 6);
		dust.noGravity = true;
		dust.scale = Main.rand.NextFloat(0.6f, 0.9f);
		dust.fadeIn = .75f;
	}

	public override void OnKill(int timeLeft)
	{
		const int numSplit = 12;

		if (!Main.dedServ)
		{
			SoundEngine.PlaySound(Boom, Projectile.Center);
			SoundEngine.PlaySound(SoundID.Item27, Projectile.Center);

			for (int i = 1; i < 4; ++i)
				Gore.NewGore(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero, Mod.Find<ModGore>("Rum_" + i).Type);

			for (int i = 0; i < 10; i++)
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Glass, 0, 0, 0, Color.SeaGreen);
		}

		if (Projectile.owner == Main.myPlayer)
		{
			Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<RumExplosion>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
			int damage = (int)(Projectile.damage * .75f);
			int fire = ModContent.ProjectileType<RumFire>();

			for (int i = 0; i < 2; i++)
			{
				Vector2 direction = new((i == 0) ? -1 : 1, 0);
				Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, direction, fire, damage, Projectile.knockBack, Projectile.owner, numSplit);
			}
		}
	}

	public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac) => !(fallThrough = false);
}