using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Granite.Armor;

[AutoloadEquip(EquipType.Legs)]
[AutoloadGlowmask("255,255,255")]
public class GraniteLegs : ModItem
{
	public override void SetStaticDefaults() => NPCLootDatabase.AddLoot(new(NPCLootDatabase.MatchId(NPCID.GraniteGolem, NPCID.GraniteFlyer), ItemDropRule.Common(Type, 21)));
	public override void SetDefaults()
	{
		Item.width = 28;
		Item.height = 24;
		Item.value = Item.sellPrice(silver: 22);
		Item.rare = ItemRarityID.Green;
		Item.defense = 4;
	}

	public override void UpdateEquip(Player player) => Player.jumpSpeed += 1;
}