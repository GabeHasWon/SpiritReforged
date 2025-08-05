using ILLogger;
using MonoMod.Cil;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.PlayerCommon;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Aether.Items;

public class Ledger : InfoItem
{
	public override void Load()
	{
		AutoloadInfoDisplay();
		IL_Main.MouseText_DrawItemTooltip += HackyForceShop;
	}

	private static void HackyForceShop(ILContext il)
	{
		ILCursor c = new(il);

		if (!c.TryGotoNext(MoveType.After, x => x.MatchCall<Main>("get_" + nameof(Main.npcShop))))
		{
			SpiritReforgedMod.Instance.LogIL("Ledger Shop Workaround", "Method 'Main.get_npcShop' not found.");
			return;
		}

		c.EmitDelegate((int npcShop) => // This is only used to trick the game into showing the price.
		{
			if (!Main.gameMenu && Main.LocalPlayer.HasInfoItem<Ledger>())
				return 1;

			return npcShop;
		});
	}

	public override void SetStaticDefaults()
	{
		NPCLootDatabase.AddLoot(new NPCLootDatabase.ConditionalLoot(NPCLootDatabase.MatchId(NPCID.Demon), ItemDropRule.Common(Type, 50)));
		NPCLootDatabase.AddLoot(new NPCLootDatabase.ConditionalLoot(NPCLootDatabase.MatchId(NPCID.VoodooDemon), ItemDropRule.Common(Type, 20)));
	}
}