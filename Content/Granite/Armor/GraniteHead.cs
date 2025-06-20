using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.Visuals.Glowmasks;

namespace SpiritReforged.Content.Granite.Armor;

[AutoloadEquip(EquipType.Head)]
[FromClassic("GraniteHelm")]
[AutoloadGlowmask("255,255,255")]
public class GraniteHead : ModItem
{
	/// <returns> Whether the set bonus related to this item is active on <paramref name="player"/>. </returns>
	public static bool SetActive(Player player) => player.active
		&& player.armor[0].type == ModContent.ItemType<GraniteHead>()
		&& player.armor[1].type == ModContent.ItemType<GraniteBody>()
		&& player.armor[2].type == ModContent.ItemType<GraniteLegs>();

	public override void Load() => DoubleTapPlayer.OnDoubleTap += DoubleTap;
	private static void DoubleTap(Player player, int keyDir)
	{
		if (SetActive(player))
			EnergyPlunge.Begin(player);
	}

	public override void SetDefaults()
	{
		Item.width = 28;
		Item.height = 24;
		Item.value = 1100;
		Item.rare = ItemRarityID.Green;
		Item.defense = 9;
	}

	public override void UpdateEquip(Player player) => Player.jumpSpeed += 1;
	public override void ArmorSetShadows(Player player) => player.armorEffectDrawShadow = true;
	public override bool IsArmorSet(Item head, Item body, Item legs) => body.type == ModContent.ItemType<GraniteBody>() && legs.type == ModContent.ItemType<GraniteLegs>();

	public override void UpdateArmorSet(Player player)
	{
		string tapDir = Language.GetTextValue(Main.ReversedUpDownArmorSetBonuses ? "Key.UP" : "Key.DOWN");
		player.setBonus = Language.GetTextValue("Mods.SpiritReforged.SetBonuses.Granite", tapDir);

		if (EnergyPlunge.Stomping(player))
		{
			player.noFallDmg = true;
			//player.gravity = 999f;
			player.maxFallSpeed = 999f;
			player.immuneNoBlink = true;

			player.velocity.Y += 2f;
		}
	}
}