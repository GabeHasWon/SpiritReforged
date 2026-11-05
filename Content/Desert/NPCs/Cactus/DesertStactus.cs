using SpiritReforged.Common.ModCompat;
using Terraria.DataStructures;
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

	public override void OnSpawn(IEntitySource source) //Randomly transform into a Bomb Stactus with Fables enabled
	{
		if (Segment is SegmentType.Variant_A or SegmentType.Variant_B && CrossMod.Fables.Enabled && Main.rand.NextBool(8))
		{
			if (NPC.ModNPC is Stactus stactus)
			{
				int parentWhoAmI = stactus.Parent;
				Parameters parameters = stactus.parameters;

				NPC.Transform(ModContent.NPCType<BombStactus>());

				stactus.parameters = parameters;
				stactus.Parent = parentWhoAmI;
				NPC.netUpdate = true;
			}
		}
	}
}