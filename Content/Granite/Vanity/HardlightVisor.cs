using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Granite.Vanity;

[AutoloadEquip(EquipType.Head)]
[AutoloadGlowmask("100,100,100,100")]
public class HardlightVisor : ModItem
{
	private const string Common = "Mods.SpiritReforged.Items.HardlightVisor.";

	public override LocalizedText DisplayName => CrossMod.Redemption.Enabled ? Language.GetText(Common + "AltDisplayName") : base.DisplayName;
	public override LocalizedText Tooltip => CrossMod.Redemption.Enabled ? Language.GetText(Common + "AltTooltip") : base.Tooltip;

	public override void SetStaticDefaults()
	{
		ArmorIDs.Head.Sets.DrawFullHair[Item.headSlot] = true;
		NPCLootDatabase.AddLoot(new(NPCLootDatabase.MatchId(NPCID.GraniteFlyer), ItemDropRule.Common(Type, 50)));
		NPCLootDatabase.AddLoot(new(NPCLootDatabase.MatchId(NPCID.GraniteGolem), ItemDropRule.Common(Type, 50)));
	}

	public override void SetDefaults()
	{
		Item.width = 30;
		Item.height = 12;
		Item.value = Item.buyPrice(0, 0, 75, 0);
		Item.rare = ItemRarityID.White;
		Item.vanity = true;
	}
}