using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.PlayerCommon;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Granite.Armor;

[AutoloadEquip(EquipType.Body)]
[FromClassic("GraniteChest")]
public class GraniteBody : ModItem
{
	public override void Load() => DoubleTapPlayer.OnDoubleTap += DoubleTap;
	private static void DoubleTap(Player player, int keyDir)
	{
		if (keyDir == 0 && !EnergyPlunge.Stomping(player) && EnergyPlunge.CanStomp(player))
			EnergyPlunge.Begin(player);
	}

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
	public override void ArmorSetShadows(Player player) => player.armorEffectDrawShadow = true;
	public override bool IsArmorSet(Item head, Item body, Item legs) => (head.type == ModContent.ItemType<GraniteHead>() || head.type is ItemID.UltrabrightHelmet or ItemID.NightVisionHelmet) && body.type == Type && legs.type == ModContent.ItemType<GraniteLegs>();

	public override void UpdateArmorSet(Player player) //Normally set effects would go into the head armor class, but that's not possible here due to the alternative helmet functionality
	{
		string tapDir = Language.GetTextValue(Main.ReversedUpDownArmorSetBonuses ? "Key.UP" : "Key.DOWN");
		player.setBonus = Language.GetTextValue("Mods.SpiritReforged.SetBonuses.Granite", tapDir);

		if (EnergyPlunge.Stomping(player))
		{
			player.noKnockback = true;
			player.noFallDmg = true;
			player.maxFallSpeed = EnergyPlunge.FallSpeed;

			player.velocity.Y += 2.5f * player.gravDir;
		}
	}
}