namespace SpiritReforged.Content.Vanilla.Leather.MarksmanArmor;

[AutoloadEquip(EquipType.Body)]
public class AncientMarksmanPlate : LeatherPlate
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();	
		ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<LeatherPlate>();
	}

	public override void AddRecipes()
	{
	}
}