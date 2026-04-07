using SpiritReforged.Common.ItemCommon;

namespace SpiritReforged.Content.Forest.Glyphs.Shock;

public class ShockGlyph : GlyphItem
{
	public sealed class ShockPlayer : ModPlayer
	{
		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (hit.Crit && Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<ShockGlyph>())
				Projectile.NewProjectile(Player.GetSource_OnHit(target), target.Center, Vector2.Zero, ModContent.ProjectileType<LightningBlast>(), (int)(damageDone * 0.35f), 0);
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