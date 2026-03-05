namespace SpiritReforged.Content.Vanilla.Leather.MarksmanArmor;

[AutoloadEquip(EquipType.Legs)]
public class AncientMarksmanLegs : LeatherLegs
{
	public override void SetStaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<LeatherLegs>();

	public override void AddRecipes()
	{
	}
}