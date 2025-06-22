using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Granite.Armor;

[AutoloadEquip(EquipType.Head)]
[AutoloadGlowmask("255,255,255")]
[FromClassic("GraniteHelm")]
public class GraniteHead : ModItem
{
	/// <returns> Whether the set bonus related to this item is active on <paramref name="player"/>. </returns>
	public static bool SetActive(Player player) => player.active
		&& (player.armor[0].type == ModContent.ItemType<GraniteHead>() || player.armor[0].type == ItemID.UltrabrightHelmet || player.armor[0].type == ItemID.NightVisionHelmet)
		&& player.armor[1].type == ModContent.ItemType<GraniteBody>()
		&& player.armor[2].type == ModContent.ItemType<GraniteLegs>();

	public override void Load() => DoubleTapPlayer.OnDoubleTap += DoubleTap;
	private static void DoubleTap(Player player, int keyDir)
	{
		if (SetActive(player) && !EnergyPlunge.Stomping(player))
			EnergyPlunge.Begin(player);
	}

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
	public override void ArmorSetShadows(Player player) => player.armorEffectDrawShadow = true;
	public override bool IsArmorSet(Item head, Item body, Item legs) => (head.type == Type || head.type == ItemID.UltrabrightHelmet || head.type == ItemID.NightVisionHelmet) &&
																	body.type == ModContent.ItemType<GraniteBody>() && legs.type == ModContent.ItemType<GraniteLegs>();

	public override void UpdateArmorSet(Player player)
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