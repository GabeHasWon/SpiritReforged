﻿using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Stargrass.Items;

public class StarPowder : ModItem
{
	public override void SetStaticDefaults()
	{
		ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.PurificationPowder;
		Item.ResearchUnlockCount = 99;
	}

	public override void SetDefaults()
	{
		Item.width = 26;
		Item.height = 28;
		Item.rare = ItemRarityID.White;
		Item.maxStack = Item.CommonMaxStack;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.useTime = 15;
		Item.useAnimation = 15;
		Item.noMelee = true;
		Item.consumable = true;
		Item.autoReuse = false;
		Item.UseSound = SoundID.Item1;
		Item.shoot = ModContent.ProjectileType<StarPowderProj>();
		Item.shootSpeed = 6f;
	}

	public override void AddRecipes() => CreateRecipe(5).AddIngredient(ItemID.FallenStar, 1).Register();
}

internal class StarPowderProj : ModProjectile
{
	public override string Texture => base.Texture[..^"Proj".Length];

	public override void SetDefaults() => Projectile.CloneDefaults(ProjectileID.PurificationPowder);

	public override bool? CanCutTiles() => false;

	public override void OnSpawn(IEntitySource source)
	{
		for (int i = 0; i < 20; i++)
		{
			var rectDims = new Vector2(50, 50);
			Vector2 position = new Vector2(Projectile.Center.X - rectDims.X / 2, Projectile.Center.Y - rectDims.Y / 2) + Projectile.velocity * 2;
			Vector2 velocity = (new Vector2(Projectile.velocity.X, Projectile.velocity.Y) * Main.rand.NextFloat(0.8f, 1.2f)).RotatedByRandom(1f);
			var dust = Dust.NewDustDirect(position, (int)rectDims.X, (int)rectDims.Y, Main.rand.NextBool(2) ? DustID.BlueTorch : DustID.PurificationPowder,
				velocity.X, velocity.Y, 0, default, Main.rand.NextFloat(0.7f, 1.1f));
			dust.noGravity = true;
			dust.fadeIn = 1.1f;
			if (dust.type == DustID.PurificationPowder && Main.rand.NextBool(2))
				dust.color = Color.Goldenrod;
		}
	}

	public override void AI()
	{
		Point pos = Projectile.position.ToTileCoordinates();
		Point end = Projectile.BottomRight.ToTileCoordinates();

		for (int i = pos.X; i < end.X; ++i)
		{
			for (int j = pos.Y; j < end.Y; ++j)
				StargrassConversion.Convert(i, j);
		}
	}
}