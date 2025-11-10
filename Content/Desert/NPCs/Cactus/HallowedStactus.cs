using Terraria.GameContent.Bestiary;
using Terraria.ModLoader.Utilities;

namespace SpiritReforged.Content.Desert.NPCs.Cactus;

public class HallowedStactus : Stactus
{
	public override void SetDefaults()
	{
		base.SetDefaults();
		parameters = new(DustID.HallowedPlants, 100, ["Juvenile", "Corsage", "FullBloom", "PricklyPears"]);
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "HallowDesert");

	public override void DoDeathEffects()
	{
		base.DoDeathEffects();

		if (Segment == SegmentType.Head)
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>(Name + "Head3").Type);
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo) => (!spawnInfo.Player.ZoneDesert || !spawnInfo.Player.ZoneHallow || spawnInfo.SpawnTileType != TileID.Pearlsand) ? 0 : SpawnCondition.OverworldDayDesert.Chance * 0.8f;
}