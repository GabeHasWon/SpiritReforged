using SpiritReforged.Common.ProjectileCommon.Abstract;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Crossbows;

public class Bolter : ModItem
{
	public class BolterHeld : Crossbow.CrossbowHeld
	{
		public override LocalizedText DisplayName => ModContent.GetInstance<Bolter>().DisplayName;

		public override string Texture => ModContent.GetInstance<Bolter>().Texture;

		public override IConfiguration SetConfiguration() => new Crossbow.CrossbowConfiguration(null, 30, 10, 40);
	}

	public override void SetDefaults()
    {
		Item.DefaultToBow(30, 10, true);
		Item.UseSound = SoundID.DD2_BallistaTowerShot with { Pitch = 0.5f };
		Item.useAmmo = ModContent.ItemType<Bolt>();
		Item.damage = 10;
		Item.knockBack = 4.5f;
		Item.noUseGraphic = true;
		Item.rare = ItemRarityID.Blue;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
		SwungProjectile.Spawn(position, velocity, ModContent.ProjectileType<BolterHeld>(), damage, knockback, player, 0, source);
		return true;
    }
}