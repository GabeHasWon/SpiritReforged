using SpiritReforged.Common.Subclasses.Greatshields;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.StonemasonShield;

internal class StonemasonGreatshield : GreatshieldItem
{
	public override void SetDefaults()
	{
		Item.DamageType = ModContent.GetInstance<GreatshieldClass>();
		Item.defense = 2;
		Item.useTime = 35;
		Item.useAnimation = 8;
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.useStyle = -1;
	}

	public override void ModifyLayerDrawing(ref DrawData data) => data.position.Y -= 4;
}
