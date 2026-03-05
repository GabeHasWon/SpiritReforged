using Terraria.DataStructures;

namespace SpiritReforged.Common.NPCCommon;

internal class PersistentNPCSystem : ModSystem
{
	/// <returns> Whether this check was successful. Skips all later checks as a result. </returns>
	public delegate bool PlayerCheck(Player p);

	/// <summary> The range that NPCs are made persistent. </summary>
	public static int Range => NPC.sWidth;

	/// <summary> Contains NPC types who can persist off-screen without taking up space in the NPC array. Only approximate position and type are remembered. </summary>
	internal static readonly HashSet<int> PersistentTypes = [];

	private static readonly Dictionary<Point16, int> Persistent = [];
	private static byte CooldownCounter;

	public override void PreUpdateNPCs()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;

		HashSet<Point16> queued = [];
		foreach (var pt in Persistent.Keys)
		{
			if (ActiveRegion(pt))
			{
				NPC.NewNPCDirect(Entity.GetSource_NaturalSpawn(), pt.ToWorldCoordinates(), Persistent[pt]);
				queued.Add(pt);
			}
		}

		foreach (var pt in queued)
			Persistent.Remove(pt);

		static bool ActiveRegion(Point16 origin)
		{
			int range = Range / 16;
			bool playerInRange = false;

			Iterate(delegate (Player p)
			{
				playerInRange = new Vector2(origin.X, origin.Y).DistanceSQ(p.Center / 16) < range * range;
				return playerInRange;
			});

			return playerInRange;
		}
	}

	public override void OnWorldUnload() => Persistent.Clear();

	/// <summary> Kills <paramref name="npc"/> and adds them to the persistent lookup. </summary>
	public static void MakePersistent(NPC npc)
	{
		Persistent.TryAdd(npc.Center.ToTileCoordinates16(), npc.type);
		npc.active = false;
	}

	public static void Iterate(PlayerCheck check)
	{
		const int cooldownMax = 60; //How regularly this method is allowed to iterate over all players

		if ((CooldownCounter %= cooldownMax) != 0)
			return;

		foreach (var p in Main.ActivePlayers)
		{
			if (check?.Invoke(p) == true)
				return;
		}
	}
}

internal class PersistentGlobalNPC : GlobalNPC
{
	public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => PersistentNPCSystem.PersistentTypes.Contains(entity.type);

	public override void PostAI(NPC npc)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;

		int range = PersistentNPCSystem.Range + 2 * 16;
		bool playerInRange = true;

		PersistentNPCSystem.Iterate(delegate (Player p)
		{
			playerInRange = npc.DistanceSQ(p.Center) < range * range;
			return playerInRange;
		});

		if (!playerInRange)
		{
			PersistentNPCSystem.MakePersistent(npc);
			npc.netUpdate = true;
		}
	}
}