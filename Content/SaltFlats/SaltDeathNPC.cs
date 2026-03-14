using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using SpiritReforged.Content.Ziggurat.NPCs;
using Terraria.DataStructures;

namespace SpiritReforged.Content.SaltFlats;

public class SaltDeathNPC : GlobalNPC
{
	public override bool InstancePerEntity => true;

	private bool salted = false;

	public override bool AppliesToEntity(NPC npc, bool lateInstantiation) => npc.type is NPCID.Grubby or NPCID.Sluggy or NPCID.Snail or NPCID.GlowingSnail 
		|| npc.type == ModContent.NPCType<Grub>() || npc.type == ModContent.NPCType<TinyGrub>();

	public override bool PreAI(NPC npc)
	{
		Point16 pos = (npc.Bottom + new Vector2(0, 4)).ToTileCoordinates16();
		Tile tile = Main.tile[pos];

		if (tile.HasTileType(ModContent.TileType<SaltBlockDull>()) || tile.HasTileType(ModContent.TileType<SaltBlockReflective>()))
		{
			npc.GetGlobalNPC<SaltDeathNPC>().salted = true;
		}

		return true;
	}

	public override void UpdateLifeRegen(NPC npc, ref int damage)
	{
		if (salted)
		{
			int power = npc.CountsAsACritter ? 2 : 8;
			npc.lifeRegen -= power;
			damage = Math.Max(1, damage);
		}
	}
}