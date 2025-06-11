using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Content.Forest.Cartography;
using SpiritReforged.Content.Savanna.Biome;
using SpiritReforged.Content.Savanna.DustStorm;
using SpiritReforged.Content.Underground.Items.BigBombs;

namespace SpiritReforged.Common.NPCCommon;

public class VanillaNPCDialogue : GlobalNPC
{
	public override void GetChat(NPC npc, ref string chat)
	{
		Player player = Main.LocalPlayer;

		if (npc.type == NPCID.ArmsDealer)
		{
			if (player.InModBiome<SavannaBiome>())
			{
				if (Main.rand.NextBool(3)) 
					chat = Language.GetTextValue("Mods.SpiritReforged.NPCs.VanillaDialogue.ArmsDealer.Savanna1");
			}
		}

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

		if (npc.type == NPCID.Nurse)
		{
			if (player.InModBiome<SavannaBiome>())
			{
				if (Main.rand.NextBool(3))
					chat = Language.GetTextValue("Mods.SpiritReforged.NPCs.VanillaDialogue.Nurse.Savanna");

				if (player.GetModPlayer<DustStormPlayer>().ZoneDustStorm)
				{
					if (Main.rand.NextBool(2))
						chat = Language.GetTextValue("Mods.SpiritReforged.NPCs.VanillaDialogue.Nurse.Duststorm");
				}
			}
		}

		if (npc.type == NPCID.TravellingMerchant)
		{
			int cartographer = NPC.FindFirstNPC(ModContent.NPCType<Cartographer>());
			if (cartographer >= 0)
			{
				if (Main.rand.NextBool(3))
					chat = Language.GetTextValue("Mods.SpiritReforged.NPCs.VanillaDialogue.TravellingMerchant.Cartographer2", Main.npc[cartographer].GivenName);
			}
			else
			{
				if (Main.rand.NextBool(3))
					chat = Language.GetTextValue("Mods.SpiritReforged.NPCs.VanillaDialogue.TravellingMerchant.Cartographer1");
			}

			if (Main.rand.NextBool(4))
				chat = Language.GetTextValue("Mods.SpiritReforged.NPCs.VanillaDialogue.TravellingMerchant.Discoveries");
		}

		if (npc.type == NPCID.Guide)
		{
			if (Main.rand.NextBool(6))
				chat = Language.GetTextValue("Mods.SpiritReforged.NPCs.VanillaDialogue.Guide.WorldNPC");
			
			if (player.InModBiome<SavannaBiome>())
			{
				if (Main.rand.NextBool(3))
					chat = Language.GetTextValue("Mods.SpiritReforged.NPCs.VanillaDialogue.Guide.Savanna");
			}
			else
			{
				if (Main.rand.NextBool(6))
					chat = Language.GetTextValue("Mods.SpiritReforged.NPCs.VanillaDialogue.Guide.Ecotone");
			}
		}

		if (npc.type == NPCID.Demolitionist)
		{
			if (player.HasItem(ModContent.ItemType<BoomShroom>()) || player.HasEquip<BoomShroom>())
			{
				if (Main.rand.NextBool(4))
					chat = Language.GetTextValue("Mods.SpiritReforged.NPCs.VanillaDialogue.Demolitionist.HasBoomshroom");
			}
		}
	}
}
