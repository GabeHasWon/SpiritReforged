using SpiritReforged.Content.Savanna.Biome;
using SpiritReforged.Content.Savanna.DustStorm;

namespace SpiritReforged.Common.NPCCommon;

public class VanillaNPCDialogue : GlobalNPC
{
	public override void GetChat(NPC npc, ref string chat)
	{
		Player player = Main.LocalPlayer;

		//Arms Dealer
		if (npc.type == NPCID.ArmsDealer)
		{
			if (player.InModBiome<SavannaBiome>())
			{
				if (Main.rand.NextBool(3)) 
					chat = Language.GetTextValue("Mods.SpiritReforged.NPCs.VanillaDialogue.ArmsDealer.Savanna1");
			}
		}

		//Stylist
		if (npc.type == NPCID.Stylist)
		{
			if (player.InModBiome<SavannaBiome>())
			{
				if (Main.rand.NextBool(3))
					chat = Language.GetTextValue("Mods.SpiritReforged.NPCs.VanillaDialogue.Stylist.Savanna1");

				if (player.GetModPlayer<DustStormPlayer>().ZoneDustStorm)
				{
					if (Main.rand.NextBool(2))
						chat = Language.GetTextValue("Mods.SpiritReforged.NPCs.VanillaDialogue.Stylist.Duststorm1");
				}
			}
		}
	}
}
