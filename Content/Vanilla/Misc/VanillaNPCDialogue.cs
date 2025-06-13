using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Content.Forest.Cartography;
using SpiritReforged.Content.Savanna.Biome;
using SpiritReforged.Content.Savanna.DustStorm;
using SpiritReforged.Content.Underground.Items.BigBombs;

namespace SpiritReforged.Content.Vanilla.Misc;

public class VanillaNPCDialogue : GlobalNPC
{
	public const string CommonPath = "Mods.SpiritReforged.NPCs.VanillaDialogue.";

	public override void GetChat(NPC npc, ref string chat)
	{
		Player player = Main.LocalPlayer;

		SetChat(ref chat, npc.type == NPCID.ArmsDealer && player.InModBiome<SavannaBiome>() && Main.rand.NextBool(3), Language.GetTextValue(CommonPath + "ArmsDealer.Savanna1"));

		if (npc.type == NPCID.Stylist && player.InModBiome<SavannaBiome>())
		{
			SetChat(ref chat, Main.rand.NextBool(3), Language.GetTextValue(CommonPath + "Stylist.Savanna1"));
			SetChat(ref chat, player.GetModPlayer<DustStormPlayer>().ZoneDustStorm && Main.rand.NextBool(2), Language.GetTextValue(CommonPath + "Stylist.Duststorm1"));
		}

		if (npc.type == NPCID.Nurse && player.InModBiome<SavannaBiome>())
		{
			SetChat(ref chat, Main.rand.NextBool(3), Language.GetTextValue(CommonPath + "Nurse.Savanna"));
			SetChat(ref chat, player.GetModPlayer<DustStormPlayer>().ZoneDustStorm && Main.rand.NextBool(2), Language.GetTextValue(CommonPath + "Nurse.Duststorm"));
		}

		if (npc.type == NPCID.PartyGirl && player.InModBiome<SavannaBiome>())
		{
			SetChat(ref chat, Main.rand.NextBool(3), Language.GetTextValue(CommonPath + "PartyGirl.Savanna"));
			SetChat(ref chat, player.GetModPlayer<DustStormPlayer>().ZoneDustStorm && Main.rand.NextBool(2), Language.GetTextValue(CommonPath + "PartyGirl.Duststorm"));
		}

		if (npc.type == NPCID.Golfer && player.InModBiome<SavannaBiome>())
		{
			SetChat(ref chat, Main.rand.NextBool(3), Language.GetTextValue(CommonPath + "Golfer.Savanna"));
			SetChat(ref chat, player.GetModPlayer<DustStormPlayer>().ZoneDustStorm && Main.rand.NextBool(2), Language.GetTextValue(CommonPath + "Golfer.Duststorm"));
		}

		if (npc.type == NPCID.BestiaryGirl && player.InModBiome<SavannaBiome>())
		{
			SetChat(ref chat, Main.rand.NextBool(3), Language.GetTextValue(CommonPath + "Zoologist.Savanna1"));
			SetChat(ref chat, player.statLife < player.statLifeMax && Main.rand.NextBool(2), Language.GetTextValue(CommonPath + "Zoologist.Savanna2"));
		}

		if (npc.type == NPCID.TravellingMerchant)
		{
			int cartographer = NPC.FindFirstNPC(ModContent.NPCType<Cartographer>());

			if (cartographer > 0)
				SetChat(ref chat, Main.rand.NextBool(3), Language.GetTextValue(CommonPath + "TravellingMerchant.Cartographer2", Main.npc[cartographer].GivenName));
			else
				SetChat(ref chat, Main.rand.NextBool(3), Language.GetTextValue(CommonPath + "TravellingMerchant.Cartographer1"));

			SetChat(ref chat, Main.rand.NextBool(4), Language.GetTextValue(CommonPath + "TravellingMerchant.Discoveries"));
		}

		if (npc.type == NPCID.Guide)
		{
			SetChat(ref chat, Main.rand.NextBool(6), Language.GetTextValue(CommonPath + "Guide.WorldNPC"));
			
			if (player.InModBiome<SavannaBiome>())
				SetChat(ref chat, Main.rand.NextBool(3), Language.GetTextValue(CommonPath + "Guide.Savanna"));
			else
				SetChat(ref chat, Main.rand.NextBool(6), Language.GetTextValue(CommonPath + "Guide.Ecotone"));

			SetChat(ref chat, player.ZoneBeach && Main.rand.NextBool(3), Language.GetTextValue(CommonPath + "Guide.Kelp"));
		}

		bool hasBoomShroom = player.HasItem(ModContent.ItemType<BoomShroom>()) || player.HasEquip<BoomShroom>() && Main.rand.NextBool(4);
		SetChat(ref chat, npc.type == NPCID.Demolitionist && hasBoomShroom, Language.GetTextValue(CommonPath + "Demolitionist.HasBoomshroom"));
	}

	private static void SetChat(ref string chat, bool condition, string message)
	{
		if (condition)
			chat = message;
	}
}