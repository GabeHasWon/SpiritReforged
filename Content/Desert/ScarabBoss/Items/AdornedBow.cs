using SpiritReforged.Content.Desert.ScarabBoss.Items.Projectiles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items;

public class AdornedBow : ModItem
{
	public readonly record struct PrismaticPalette
	{
		public readonly Color[] Colors;

		public PrismaticPalette() => Colors = GetPrismaticColors();

		public void FadeColors(Color[] source, float progress)
		{
			Colors[0] = Color.Lerp(source[0], source[1], progress);
			Colors[1] = Color.Lerp(source[1], source[2], progress);
			Colors[2] = Color.Lerp(source[2], source[0], progress);
		}

		public static Color[] GetPrismaticColors()
		{
			var colors = new Color[3];

			colors[0] = new Color(255, 0, 70 + 25 * Main.rand.Next(5)); // Magenta to Purple
			colors[1] = new Color(0, 255, 255 - 25 * Main.rand.Next(5)); // Cyan to Green
			colors[2] = new Color(255, 255 - 25 * Main.rand.Next(5), 0); // Yellow to Orange

			return colors;
		}
	}

	public override void SetDefaults()
	{
		Item.damage = 25;
		Item.Size = new Vector2(48, 52);
		Item.useTime = Item.useAnimation = 60;
		Item.knockBack = 1f;
		Item.noMelee = true;
		Item.channel = true;
		Item.noUseGraphic = true;
		Item.DamageType = DamageClass.Ranged;
		Item.useTurn = false;
		Item.autoReuse = true;
		Item.rare = ItemRarityID.Green;
		Item.value = Item.sellPrice(gold: 2);
		Item.useStyle = ItemUseStyleID.Swing;
		Item.shoot = ModContent.ProjectileType<AdornedBowHeld>();
		Item.shootSpeed = 8;
		Item.useAmmo = AmmoID.Arrow;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		int useTime = (int)(Item.useTime / player.GetTotalAttackSpeed(DamageClass.Ranged));
		Projectile.NewProjectileDirect(source, position, Vector2.Zero, Item.shoot, damage, knockback, player.whoAmI, 0, useTime, type);
		return false;
	}

	public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;
}