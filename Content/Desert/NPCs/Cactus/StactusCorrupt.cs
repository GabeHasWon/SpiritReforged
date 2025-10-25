using System.IO;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ModLoader.Utilities;

namespace SpiritReforged.Content.Desert.NPCs.Cactus;

[AutoloadBanner]
public class StactusCorrupt : Stactus
{
	public override void OnSpawn(IEntitySource source) { }

	public override void SetDefaults()
	{
		base.SetDefaults();
		Params = new(DustID.CorruptPlants, 100);
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "CorruptDesert");

	public override float SpawnChance(NPCSpawnInfo spawnInfo) => (!spawnInfo.Player.ZoneDesert || !spawnInfo.Player.ZoneCorrupt || spawnInfo.SpawnTileType != TileID.Ebonsand) ? 0 : SpawnCondition.OverworldDayDesert.Chance * 0.8f;

	public override void SendExtraAI(BinaryWriter writer) => writer.Write(Falling);
	public override void ReceiveExtraAI(BinaryReader reader) => Falling = reader.ReadBoolean();
}