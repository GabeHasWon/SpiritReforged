using System.Linq;
using Terraria.DataStructures;

namespace SpiritReforged.Common.NPCCommon;

internal class SlimeItemDatabase : GlobalNPC
{
	/// <summary>
	/// Defines an item that could be put into a slime.
	/// </summary>
	/// <param name="condition"></param>
	/// <param name="chance"></param>
	/// <param name="item"></param>
	public readonly record struct ConditionalItem(Func<NPC, bool> Condition, float Chance, int Item);

	private static readonly List<ConditionalItem> LootToAdd = [];

	public static Func<NPC, bool> MatchId(params int[] types) => (npc) => types.Contains(npc.netID);
	public static Func<NPC, bool> MatchId(int type) => (npc) => type == npc.netID;

	public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.type is NPCID.BlueSlime; // All other slimes are netID variants

	/// <summary>
	/// Adds the given item to the database. This should only be run in <see cref="ModType.SetStaticDefaults"/>.
	/// </summary>
	/// <param name="loot">Loot to add.</param>
	public static void AddLoot(ConditionalItem loot) => LootToAdd.Add(loot);

	public override void OnSpawn(NPC npc, IEntitySource source)
	{
		foreach (var loot in LootToAdd)
		{
			if (loot.Condition(npc) && Main.rand.NextFloat() < loot.Chance)
			{
				npc.ai[1] = loot.Item;
				npc.netUpdate = true;
				return;
			}
		}
	}
}
