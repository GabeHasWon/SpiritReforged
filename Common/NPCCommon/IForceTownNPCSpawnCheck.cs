namespace SpiritReforged.Common.NPCCommon;

/// <summary>
/// Defines an NPC as one that "brute forces" a spawn check; namely, this just ignores vanilla's requirement for a town NPC to have a map head slot.<br/>
/// Town NPCs with this interface will have a check seperate from vanilla's town NPCs, but will respect a priority NPC spawning before it.
/// </summary>
internal interface IForceTownNPCSpawnCheck
{
	internal class SkipHeadCheckForTownNPCSpawning : ILoadable
	{
		public void Load(Mod mod) => MonoModHooks.Add(typeof(NPCLoader).GetMethod(nameof(NPCLoader.CanTownNPCSpawn)), Detour_CanTownNPCSpawn);

		public static void Detour_CanTownNPCSpawn(Action<int> orig, int numTownNpcs)
		{
			orig(numTownNpcs);

			var npcs = SpiritReforgedMod.Instance.GetContent<ModNPC>();

			foreach (ModNPC modNPC in npcs)
			{
				NPC npc = modNPC.NPC;

				if (modNPC is IForceTownNPCSpawnCheck && npc.townNPC && !NPC.AnyNPCs(npc.type) && modNPC.CanTownNPCSpawn(numTownNpcs))
				{
					Main.townNPCCanSpawn[npc.type] = true;

					if (WorldGen.prioritizedTownNPCType == 0)
						WorldGen.prioritizedTownNPCType = npc.type;
				}
			}
		}

		public void Unload() { }
	}
}