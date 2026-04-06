using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;

namespace SpiritReforged.Content.Forest.Glyphs;

public class MoonlightGlyph : GlyphItem
{
	public sealed class MoonlightPlayer : ModPlayer
	{
		public override void ModifyWeaponDamage(Item item, ref StatModifier damage)
		{
			if (item.GetGlyph().ItemType == ModContent.ItemType<MoonlightGlyph>())
			{
				float strength = 1f - Player.statMana / (float)Player.statManaMax2;
				damage *= 1 + strength * 0.25f; //Deal 25% more damage at zero mana
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<MoonlightGlyph>() && Player.statMana < Player.statLifeMax2)
			{
				int manaIncrease = (int)Math.Max(damageDone / 20f, 1);

				Player.statMana = Math.Min(Player.statMana + manaIncrease, Player.statManaMax2);
				Player.ManaEffect(manaIncrease); //Leeching
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

	public override bool CanApplyGlyph(Item item) => base.CanApplyGlyph(item) && !item.DamageType.CountsAsClass(DamageClass.Magic);

	public override void ApplyGlyph(Item item, IApplicationContext context)
	{
		item.DamageType = ModContent.GetInstance<HybridDamageClass>().Clone()
			.AddSubClass(new(item.DamageType, 0.8f))
			.AddSubClass(new(DamageClass.Magic, 0.2f));

		base.ApplyGlyph(item, context);
	}
}