using System.Linq;
using Terraria.DataStructures;

namespace SpiritReforged.Common.NPCCommon.Abstract;

/// <summary> Facilititates making NPCs which replace other spawns of <see cref="TypesToReplace"/>. </summary>
internal interface ISubstitute
{
	/// <summary> The types of NPCs to be replaced. </summary>
	public int[] TypesToReplace { get; }

	/// <summary> Under what conditions this NPC should replace regular spawns. </summary>
	public bool CanSubstitute(Player player);
}

public class ReplaceGlobalNPC : GlobalNPC
{
	//Stores a modded NPC type and vanilla NPC types to replace, respectively
	private static readonly Dictionary<int, int[]> Types = [];

	public override void SetStaticDefaults()
	{
		foreach (ModNPC npc in Mod.GetContent<ModNPC>())
		{
			if (npc is ISubstitute replaceable)
				Types.Add(npc.Type, replaceable.TypesToReplace);
		}
	}

	public override void OnSpawn(NPC npc, IEntitySource source)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || source is not EntitySource_SpawnNPC)
			return;

		Player closest = Main.player[Player.FindClosest(npc.position, npc.width, npc.height)];
		var validT = Types.Where(x => x.Value.Contains(npc.type) && (NPCLoader.GetNPC(x.Key) as ISubstitute).CanSubstitute(closest)).ToArray();
		if (validT.Length != 0)
		{
			npc.Transform(validT[Main.rand.Next(validT.Length)].Key);
			npc.netUpdate = true;
		}
	}
}