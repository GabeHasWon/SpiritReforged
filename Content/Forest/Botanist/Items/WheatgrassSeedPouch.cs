using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Forest.Botanist.Tiles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Botanist.Items;

public class WheatgrassSeedPouch : ModItem
{
	public override void SetStaticDefaults() => NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry((shop) => shop.NpcType == NPCID.Merchant, 
		new NPCShop.Entry(ModContent.ItemType<WheatgrassSeedPouch>(), Condition.DownedEyeOfCthulhu)));

	public override void SetDefaults()
	{
		Item.width = Item.height = 26;
		Item.value = Item.sellPrice(0, 0, 0, 5);
		Item.rare = ItemRarityID.White;
		Item.shoot = ModContent.ProjectileType<WheatgrassSeed>();
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

internal class WheatgrassSeed : ModProjectile
{
	public override void SetStaticDefaults() => Main.projFrames[Type] = 3;

	public override void SetDefaults()
	{
		Projectile.Size = new(6);
		Projectile.aiStyle = -1;
		Projectile.timeLeft = 2000;
		Projectile.frame = -1;
	}

	public override void AI()
	{
		if (Projectile.frame == -1)
			Projectile.frame = Main.rand.Next(Main.projFrames[Type]); //Select a random frame on spawn

		Projectile.velocity.Y += 0.2f;
		Projectile.rotation += Projectile.velocity.X * 0.05f;
	}

	public override void OnKill(int timeLeft)
	{
		if (Main.myPlayer == Projectile.owner)
		{
			var position = (Projectile.Center + new Vector2(0, 8)).ToTileCoordinates();
			if (WorldGen.IsTileReplacable(position.X, position.Y - 1))
			{
				Placer.PlaceTile<Wheatgrass>(position.X, position.Y - 1).Send();

				for (int i = 0; i < 4; ++i)
					Dust.NewDust(position.ToWorldCoordinates(), 2, 2, DustID.Hay, WorldGen.genRand.NextFloat(-1, 1) + Projectile.velocity.X, -Main.rand.NextFloat(1, 4), Scale: 0.6f);
			}
		}

		SoundEngine.PlaySound(SoundID.Grass with { Volume = 0.5f, Pitch = 0.8f }, Projectile.Center);
	}

	public override bool? CanCutTiles() => false;
	public override bool? CanDamage() => false;
}