using SpiritReforged.Common.ModCompat;
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

	public override float SpawnChance(NPCSpawnInfo spawnInfo)
	{
		bool onSand = spawnInfo.SpawnTileType == TileID.Sand;
		float spawnModifier = 0.8f;

		if (CrossMod.Spooky.CheckFind("DesertSand", out ModTile tarSand) && spawnInfo.SpawnTileType == tarSand.Type)
		{
			spawnModifier = 0.6f;
			onSand = true;
		}

		return (spawnInfo.PlayerInTown || !spawnInfo.Player.ZoneDesert || !onSand) ? 0 : SpawnCondition.OverworldDayDesert.Chance * spawnModifier;
	}
}