using SpiritReforged.Common.EmoteCommon;
using SpiritReforged.Common.ItemCommon.Pins;
using SpiritReforged.Common.NPCCommon.Abstract;
using SpiritReforged.Common.NPCCommon.Interfaces;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Content.Savanna.Biome;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Forest.Cartography;

[AutoloadHead]
public class Enchanter : WorldNPC, ITravelNPC
{
	public override void Load() => AutoEmote.LoadFaceEmote(this, static () => NPC.AnyNPCs(ModContent.NPCType<Enchanter>()));

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		Main.npcFrameCount[Type] = 25;

		NPCID.Sets.ExtraFramesCount[Type] = 9;
		NPCID.Sets.AttackFrameCount[Type] = 4;
		NPCID.Sets.DangerDetectRange[Type] = 600;
		NPCID.Sets.AttackType[Type] = -1;
		NPCID.Sets.AttackTime[Type] = 20;
		NPCID.Sets.HatOffsetY[Type] = 2;

		NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers()
		{ Velocity = 1f });
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "Surface");

	public override string GetChat() => Language.GetTextValue("Mods.SpiritReforged.NPCs.Cartographer.Dialogue." + Main.rand.Next(5));

	public override List<string> SetNPCNameList()
	{
		List<string> names = [];

		for (int i = 0; i < 6; ++i)
			names.Add(Language.GetTextValue("Mods.SpiritReforged.NPCs.Cartographer.Names." + i));

		return names;
	}

	public override void SetChatButtons(ref string button, ref string button2)
	{
		button = Language.GetTextValue("LegacyInterface.28");
		button2 = "Enchant";
	}

	public override void OnChatButtonClicked(bool firstButton, ref string shopName)
	{
		if (firstButton)
		{
			shopName = "Shop";
		}
		else
		{
			
		}
	}

	public override void AddShops() => new NPCShop(Type).Register();

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (Main.dedServ)
			return;

		if (NPC.life <= 0)
		{
			for (int i = 1; i < 7; i++)
			{
				int goreType = Mod.Find<ModGore>(nameof(Cartographer) + i).Type;
				Gore.NewGore(NPC.GetSource_Death(), Main.rand.NextVector2FromRectangle(NPC.getRect()), NPC.velocity, goreType);
			}
		}

		for (int d = 0; d < 8; d++)
			Dust.NewDustPerfect(Main.rand.NextVector2FromRectangle(NPC.getRect()), DustID.Blood, Main.rand.NextVector2Unit() * 1.5f, 0, default, Main.rand.NextFloat(1f, 1.5f));
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo)
	{
		if (SpawnedToday || spawnInfo.Invasion || spawnInfo.Water)
			return 0; //Never spawn during an invasion, in water or if already spawned that day

		float multiplier = MathHelper.Lerp(1.75f, .5f, spawnInfo.Player.GetModPlayer<PinPlayer>().PinProgress) * (Main.hardMode ? .6f : 1f);

		if (spawnInfo.SpawnTileY > Main.worldSurface && spawnInfo.SpawnTileY < Main.UnderworldLayer && !spawnInfo.Player.ZoneEvil())
			return .00023f * multiplier; //Rarely spawn in caves above underworld height

		if ((spawnInfo.Player.InModBiome<SavannaBiome>() || spawnInfo.Player.ZoneDesert || spawnInfo.Player.ZoneJungle || OuterThirds(spawnInfo.SpawnTileX) && spawnInfo.Player.InZonePurity() && !spawnInfo.Player.ZoneSkyHeight) && Main.dayTime)
			return .0024f * multiplier; //Spawn most commonly in the Savanna, Desert, Jungle, and outer thirds of the Forest during the day

		return 0;

		static bool OuterThirds(int x) => x < Main.maxTilesX / 3 || x > Main.maxTilesX - Main.maxTilesY / 3;
	}

	public bool CanSpawnTraveler()
	{
		foreach (var p in Main.ActivePlayers)
			if (p.TryGetModPlayer(out PinPlayer pinPl) && pinPl.PinProgress != 0)
				return true;

		return false;
	}
}