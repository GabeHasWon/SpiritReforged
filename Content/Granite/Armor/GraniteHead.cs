using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Granite.Armor;

[AutoloadEquip(EquipType.Head)]
[AutoloadGlowmask("255,255,255")]
[FromClassic("GraniteHelm")]
public class GraniteHead : ModItem
{
	public override void SetStaticDefaults()
	{
		NPCLootDatabase.AddLoot(new(NPCLootDatabase.MatchId(NPCID.GraniteGolem, NPCID.GraniteFlyer), ItemDropRule.Common(Type, 21)));

		ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<GraniteBody>();
		ItemID.Sets.ShimmerTransformToItem[ItemID.GladiatorHelmet] = ItemID.GladiatorBreastplate; //Shimmer transformation for gladiator armor
	}

	public override void SetDefaults()
	{
		Item.width = 28;
		Item.height = 24;
		Item.value = Item.sellPrice(silver: 30);
		Item.rare = ItemRarityID.Green;
		Item.defense = 5;
	}

	public override void UpdateEquip(Player player) => Player.jumpSpeed += 1;
}