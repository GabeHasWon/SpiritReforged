using ILLogger;
using MonoMod.Cil;
using SpiritReforged.Common.NPCCommon;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Aether.Items;

internal class Ledger : ModItem
{
	public override void Load() => IL_Main.MouseText_DrawItemTooltip += HackyForceShop;

	private void HackyForceShop(ILContext il)
	{
		ILCursor c = new(il);

		if (!c.TryGotoNext(MoveType.After, x => x.MatchCall<Main>("get_" + nameof(Main.npcShop))))
		{
			SpiritReforgedMod.Instance.LogIL("Ledger Shop Workaround", "Method 'Main.get_npcShop' not found.");
			return;
		}

		c.EmitDelegate((int npcShop) => // This is only used to trick the game into showing the price.
		{
			if (!Main.gameMenu && Main.LocalPlayer.GetModPlayer<LedgerPlayer>().Enabled)
				return 1;

			return npcShop;
		});
	}

	public override void SetStaticDefaults()
	{
		NPCLootDatabase.AddLoot(new NPCLootDatabase.ConditionalLoot(NPCLootDatabase.MatchId(NPCID.Demon), ItemDropRule.Common(Type, 50)));
		NPCLootDatabase.AddLoot(new NPCLootDatabase.ConditionalLoot(NPCLootDatabase.MatchId(NPCID.VoodooDemon), ItemDropRule.Common(Type, 20)));
	}

	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.Radar);
		Item.Size = new(30, 32);
	}

	public override void UpdateInfoAccessory(Player player) => player.GetModPlayer<LedgerPlayer>().Enabled = true;
}

internal class LedgerPlayer : ModPlayer
{
	public bool Enabled = false;

	public override void ResetInfoAccessories() => Enabled = false;

	public override void RefreshInfoAccessoriesFromTeamPlayers(Player otherPlayer)
	{
		if (otherPlayer.GetModPlayer<LedgerPlayer>().Enabled)
			Enabled = true;
	}
}

public class LedgerInfoDisplay : InfoDisplay
{
	public static LocalizedText ShowingPriceText { get; private set; }

	public override string HoverTexture => Texture + "Hover";

	public override void SetStaticDefaults() => ShowingPriceText = this.GetLocalization("ShowingShimmer");
	public override bool Active() => Main.LocalPlayer.GetModPlayer<LedgerPlayer>().Enabled;
	public override string DisplayValue(ref Color displayColor, ref Color displayShadowColor) => ShowingPriceText.Value;
}