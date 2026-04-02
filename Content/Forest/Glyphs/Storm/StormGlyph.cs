using SpiritReforged.Common.ItemCommon;

namespace SpiritReforged.Content.Forest.Glyphs.Storm;

public class StormGlyph : GlyphItem
{
	public sealed class StormItem : GlobalItem
	{
		/// <summary> Whether projectile velocity should be boosted on the local client only </summary>
		private static bool BoostedVelocity;

		public override bool? UseItem(Item item, Player player)
		{
			if (player.whoAmI == Main.myPlayer && player.ItemAnimationJustStarted && player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<StormGlyph>() && Main.rand.NextBool((int)MathHelper.Clamp(30 - item.useTime / 2, 2, 10)))
			{
				BoostedVelocity = true;

				Vector2 velocity = player.DirectionTo(Main.MouseWorld) * (item.shootSpeed > 1 ? item.shootSpeed * 1.5f : 12f);
				Projectile.NewProjectile(player.GetSource_ItemUse(item), player.Center, velocity, ModContent.ProjectileType<SlicingGust>(), item.damage, 12f, player.whoAmI);

				if (item.DamageType.CountsAsClass(DamageClass.Melee) && !item.noUseGraphic)
					Projectile.NewProjectile(player.GetSource_ItemUse(item), player.Center, velocity, ModContent.ProjectileType<ZephyrWave>(), 0, 0, player.whoAmI);
			}

			return null;
		}

		public override void ModifyShootStats(Item item, Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
		{
			if (BoostedVelocity)
			{
				velocity *= 1.5f;
				BoostedVelocity = false;
			}
		}
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.Green;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(new(142, 186, 231));
	}
}