using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.PlayerCommon;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Mage;

public class Bookmark : EquippableItem
{
	private sealed class CraneModPlayer : ModPlayer
	{
		private int _craneRate = 40;

		public override void OnConsumeMana(Item item, int manaConsumed)
		{
			if (Player.HasEquip<Bookmark>())
				_craneRate -= manaConsumed;
		}

		public override bool Shoot(Item item, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			const int max_crane_rate = 40;
			if (_craneRate <= 0)
			{
				_craneRate = Main.rand.Next(max_crane_rate - 10, max_crane_rate + 10);
				Projectile.NewProjectile(source, position, velocity, ProjectileID.PaperAirplaneB, damage, knockback, Player.whoAmI);
			}

			return true;
		}
	}

	public override void SetStaticDefaults() => Main.RegisterItemAnimation(Item.type, new DrawGrid(2, 1));

	public override void SetDefaults()
	{
		Item.rare = ItemRarityID.Green;
		Item.accessory = true;
	}
}