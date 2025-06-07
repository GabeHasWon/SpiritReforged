using Terraria.Graphics.Shaders;

namespace SpiritReforged.Content.Vanilla.Leather.MarksmanArmor;

[AutoloadEquip(EquipType.Head)]
public class AncientMarksmanHood : LeatherHood
{
	public override bool IsArmorSet(Item head, Item body, Item legs)
		=> (head.type, body.type, legs.type) == (Type, ModContent.ItemType<AncientMarksmanPlate>(), ModContent.ItemType<AncientMarksmanLegs>());
	public override void AddRecipes()
	{
	}
}