using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.NPCCommon;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Forest.JinxBow;

public class JinxBow : MinionAccessory
{
	public override MinionAccessoryData Data => new(ModContent.ProjectileType<JinxBowMinion>(), 15);

	public override void StaticDefaults() => NPCLootDatabase.AddLoot(new(NPCLootDatabase.MatchId(NPCID.GoblinArcher), ItemDropRule.Common(Type, 50)));

	public override void Defaults()
	{
		Item.width = 20;
		Item.height = 40;
		Item.value = Item.buyPrice(0, 3, 0, 0);
		Item.rare = ItemRarityID.Blue;
	}
}