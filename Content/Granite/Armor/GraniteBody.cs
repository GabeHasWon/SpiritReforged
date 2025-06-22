using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.NPCCommon;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Granite.Armor;

[AutoloadEquip(EquipType.Body)]
[FromClassic("GraniteChest")]
public class GraniteBody : ModItem
{
	public override void SetStaticDefaults()
	{
		NPCLootDatabase.AddLoot(new(NPCLootDatabase.MatchId(NPCID.GraniteGolem, NPCID.GraniteFlyer), ItemDropRule.Common(Type, 21)));

		ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<GraniteLegs>();
		ItemID.Sets.ShimmerTransformToItem[ItemID.GladiatorBreastplate] = ItemID.GladiatorLeggings;
	}

	public override void SetDefaults()
	{
		Item.width = 28;
		Item.height = 24;
		Item.value = Item.sellPrice(silver: 32);
		Item.rare = ItemRarityID.Green;
		Item.defense = 5;
	}

	public override void UpdateEquip(Player player) => Player.jumpSpeed += 1;
}