using System.Linq;
using static Terraria.Utilities.NPCUtils;

namespace SpiritReforged.Common.NPCCommon;

internal static class AdvancedTargetingHelper
{
	internal static readonly Dictionary<int, int[]> TargetLookup = [];

	public static bool IsTarget(this NPC npc)
	{
		if (TargetLookup.TryGetValue(npc.type, out int[] targets))
			return targets.Contains(npc.type);

		return false;
	}

	/// <summary> Used to set up specific NPC targets for this NPC. </summary>
	public static void SetNPCTargets(this NPC npc, params int[] targetTypes)
	{
		NPCID.Sets.TakesDamageFromHostilesWithoutBeingFriendly[npc.type] = true;
		NPCID.Sets.UsesNewTargetting[npc.type] = true;

		TargetLookup.Add(npc.type, targetTypes);
	}

	/// <summary> Used to find an NPC or player target. Use <see cref="NPC.GetTargetData"/> to get the resulting target-specific data. </summary>
	/// <param name="npc"> This NPC. </param>
	/// <param name="flags"> Which target types should be searched for. </param>
	/// <param name="playerFilter"> Delegate to determine whether the given player is a valid target. </param>
	/// <param name="npcFilter"> Delegate to determine whether the given NPC is a valid target. Automatically filters by types provided in <see cref="SetNPCTargets"/>. </param>
	/// <returns> The results of this search. </returns>
	public static TargetSearchResults FindTarget(this NPC npc, TargetSearchFlag flags = TargetSearchFlag.All, SearchFilter<Player> playerFilter = null, SearchFilter<NPC> npcFilter = null)
	{
		var result = SearchForTarget(npc, flags, playerFilter, npcFilter ?? NPCsByDistanceAndType(npc));

		if (result.FoundTarget)
		{
			npc.target = result.NearestTargetIndex;
			npc.targetRect = result.NearestTargetHitbox;

			if (npc.ShouldFaceTarget(ref result))
				npc.FaceTarget();
		}

		return result;
	}

	public static SearchFilter<NPC> NPCsByDistanceAndType(NPC thisNPC, int distance = 999) => delegate (NPC target)
	{
		if (TargetLookup.TryGetValue(thisNPC.type, out int[] targets))
			return targets.Contains(target.type) && target.Distance(thisNPC.Center) <= distance;

		return target.Distance(thisNPC.Center) <= distance;
	};
}
