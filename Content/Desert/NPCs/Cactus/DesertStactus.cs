using Terraria.ModLoader.Utilities;

namespace SpiritReforged.Content.Desert.NPCs.Cactus;

[AutoloadBanner]
public class DesertStactus : Stactus
{
	public override void SetDefaults()
	{
		base.SetDefaults();
		parameters = new(DustID.OasisCactus, 120, ["Juvenile", "FlowerCrown", "Blossom", "Bouquet", "Garland", "PricklyPears"]);
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo) => (spawnInfo.PlayerInTown || !spawnInfo.Player.ZoneDesert || spawnInfo.SpawnTileType != TileID.Sand) ? 0 : SpawnCondition.OverworldDayDesert.Chance * 0.8f;
}