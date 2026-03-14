using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Common.NPCCommon;

/// <summary> Automatically applies various elements of gold critters to this <see cref="ModNPC"/>, such as: <para/>
/// Adding the type to <see cref="NPCID.Sets.GoldCrittersCollection"/><para/>
/// Setting <see cref="NPC.rarity"/> to 3<para/>
/// Using <see cref="GoldCritterUICollectionInfoProvider"/> for this NPC's <see cref="BestiaryEntry.UIInfoProvider"/><para/>
/// Spawning <see cref="DustID.GoldCoin"/> dust randomly and emitting light<para/>
/// Spawning <see cref="DustID.GoldCritter_LessOutline"/> on death. </summary>
public interface IGoldCritter
{
	public int[] NormalPersistentIDs => [];
}

public class GoldCritterNPC : GlobalNPC
{
	public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.ModNPC is IGoldCritter;

	public override void SetDefaults(NPC npc) => npc.rarity = 3;

	public override void SetBestiary(NPC npc, BestiaryDatabase database, BestiaryEntry bestiaryEntry)
	{
		if (npc.ModNPC is IGoldCritter c)
			bestiaryEntry.UIInfoProvider = new GoldCritterUICollectionInfoProvider(c.NormalPersistentIDs, ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[npc.type]);
	}

	public override void AI(NPC npc)
	{
		if (!Main.dedServ)
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

	public override void OnKill(NPC npc)
	{
		for (int i = 0; i < 8; i++)
			Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.GoldCritter_LessOutline, npc.velocity.X * 0.3f, npc.velocity.Y * 0.3f);
	}
}