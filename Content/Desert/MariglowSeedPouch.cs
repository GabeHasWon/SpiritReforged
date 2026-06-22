using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Desert.Tiles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert;

public class MariglowSeedPouch : ModItem
{
	public class MariglowSeed : ModProjectile
	{
		public override void SetStaticDefaults() => Main.projFrames[Type] = 3;

		public override void SetDefaults()
		{
			Projectile.Size = new(6);
			Projectile.aiStyle = -1;
			Projectile.timeLeft = 2000;
			Projectile.frame = Main.rand.Next(Main.projFrames[Type]); //Select a random frame
		}

		public override void AI()
		{
			Projectile.velocity.Y += 0.2f;
			Projectile.rotation += Projectile.velocity.X * 0.05f;
		}

		public override void OnKill(int timeLeft)
		{
			Point position = (Projectile.Center + new Vector2(0, 8)).ToTileCoordinates();

			if (Main.myPlayer == Projectile.owner)
			{
				if (WorldGen.IsTileReplacable(position.X, position.Y - 1))
					Placer.PlaceTile<Glowflower>(position.X, position.Y - 1).Send();
			}

			SoundEngine.PlaySound(SoundID.Grass with { Volume = 0.5f, Pitch = 0.8f }, Projectile.Center);

			for (int i = 0; i < 4; ++i)
			{
				Vector2 velocity = new(WorldGen.genRand.NextFloat(-1, 1) + Projectile.velocity.X, -Main.rand.NextFloat(-1, 1));
				Dust.NewDust(position.ToWorldCoordinates(), 2, 2, DustID.YellowStarDust, velocity.X * 0.2f, velocity.Y * 0.2f);
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Projectile.QuickDraw(drawColor: Projectile.GetAlpha(Color.White));
			return false;
		}

		public override bool? CanCutTiles() => false;
		public override bool? CanDamage() => false;
	}

	public override void SetStaticDefaults() => NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry(static (shop) => Main.LocalPlayer.ZoneDesert && shop.NpcType == NPCID.Merchant, 
		new NPCShop.Entry(ModContent.ItemType<MariglowSeedPouch>(), Condition.DownedEyeOfCthulhu)));

	public override void SetDefaults()
	{
		Item.width = Item.height = 26;
		Item.value = Item.sellPrice(0, 0, 0, 5);
		Item.rare = ItemRarityID.White;
		Item.shoot = ModContent.ProjectileType<MariglowSeed>();
		Item.UseSound = SoundID.Item1;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.shootSpeed = 8;
		Item.useTime = Item.useAnimation = 25;
		Item.useTurn = true;
		Item.autoReuse = true;
		Item.consumable = true;
		Item.maxStack = Item.CommonMaxStack;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		for (int i = 0; i < 6; ++i)
			Projectile.NewProjectile(source, position, velocity.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.5f, 1f), type, 0, knockback, player.whoAmI);	

		return false;
	}
}