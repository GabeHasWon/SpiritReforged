using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Common.NPCCommon;

/// <summary>
/// Automatically applies the following things to the ID associated with the NPC:<br/>
/// Adds the ID to <see cref="NPCID.Sets.GoldCrittersCollection"/>;<br/>
/// Sets <see cref="NPC.rarity"/> = 3; in SetDefaults<br/>
/// Sets the NPC's <see cref="BestiaryEntry.UIInfoProvider"/> to a <see cref="GoldCritterUICollectionInfoProvider"/>;<br/>
/// And spawns <see cref="DustID.GoldCoin"/> dust randomly for the critter.
/// </summary>
public interface IGoldCritter
{
	public int[] NormalPersistentIDs => [];
}

public class GoldCritterNPC : GlobalNPC
{
	private static readonly Dictionary<int, int[]> NPCTypes = [];

	public override void SetStaticDefaults()
	{
		foreach (var npc in Mod.GetContent<ModNPC>())
		{
			if (npc is IGoldCritter g)
			{
				NPCTypes.Add(npc.Type, g.NormalPersistentIDs);
				NPCID.Sets.GoldCrittersCollection.Add(npc.Type);
			}
		}
	}

	public override void SetDefaults(NPC npc)
	{
		if (!Main.dedServ && NPCTypes.TryGetValue(npc.type, out _))
			npc.rarity = 3;
	}

	public override void SetBestiary(NPC npc, BestiaryDatabase database, BestiaryEntry bestiaryEntry)
	{
		if (NPCTypes.TryGetValue(npc.type, out int[] ids) && ids.Length > 0)
			bestiaryEntry.UIInfoProvider = new GoldCritterUICollectionInfoProvider(ids, ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[npc.type]);
	}

	public override void AI(NPC npc)
	{
		if (!Main.dedServ && NPCTypes.TryGetValue(npc.type, out _))
		{
			Lighting.AddLight((int)(npc.Center.X / 16f), (int)(npc.Center.Y / 16f), 0.1f, 0.1f, 0.1f);

			if (Main.rand.NextBool(30))
			{
				var dust = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.GoldCoin);
				dust.velocity *= 0f;
				dust.fadeIn += 0.5f;
			}
		}
	}
}