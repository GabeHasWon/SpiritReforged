using SpiritReforged.Common.WorldGeneration;
using System.Linq;
using Terraria.Chat;

namespace SpiritReforged.Common.NPCCommon;

internal interface ITravelNPC
{
	public bool CanSpawnTraveler();
}

internal class TravelNPC : GlobalNPC
{
	public override bool CheckActive(NPC npc)
	{
		if (TravelFlags.TravelerSpawned)
			return npc.ModNPC is not ITravelNPC;

		return true;
	}
}

internal class TravelFlags : ModSystem
{
	[WorldBound]
	public static bool TravelerSpawned;
	private static readonly Dictionary<int, Func<bool>> vDatabase = [];

	/// <summary><inheritdoc cref="ModSystem.PostUpdateTime"/><para/>
	/// Spawns a random <see cref="ITravelNPC"/> NPC where <see cref="ITravelNPC.CanSpawnTraveler"/> is true, and handles despawning like the traveling merchant. </summary>
	public override void PostUpdateTime()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;

		if (TravelerSpawned)
			TryDespawn();
		else
			TrySpawn();
	}

	private static void TrySpawn()
	{
		if (Main.eclipse || !Main.dayTime || Main.invasionType > 0 && Main.invasionDelay == 0 && Main.invasionSize > 0)
			return;

		if (!Main.IsFastForwardingTime() && Main.dayTime && Main.time < 27000.0 && Main.rand.NextDouble() < Main.dayRate / 108000.0)
		{
			if (vDatabase.Count == 0)
				PopulateDatabase();

			int[] visitors = vDatabase.Where(x => x.Value.Invoke()).Select(x => x.Key).ToArray();
			if (visitors.Length != 0)
			{
				int type = visitors[Main.rand.Next(visitors.Length)];
				if (NPC.AnyNPCs(type))
					return; //Don't spawn a duplicate

				var position = GetSpawnPosition();
				var npc = NPC.NewNPCDirect(Entity.GetSource_TownSpawn(), position, type);

				if (Main.netMode == NetmodeID.SinglePlayer)
					Main.NewText(Language.GetTextValue("Announcement.HasArrived", npc.FullName), 50, 125);
				else if (Main.netMode == NetmodeID.Server)
					ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Announcement.HasArrived", npc.GetFullNetName()), new Color(50, 125, 255));

				TravelerSpawned = true;
			}
		}

		static Vector2 GetSpawnPosition()
		{
			List<NPC> townNPCs = [];
			foreach (var npc in Main.ActiveNPCs)
			{
				if (npc.townNPC && !npc.homeless && npc.type != NPCID.OldMan)
					townNPCs.Add(npc);
			}

			if (townNPCs.Count == 0)
				return new Vector2(Main.spawnTileX, Main.spawnTileY - 1).ToWorldCoordinates();

			return townNPCs[Main.rand.Next(townNPCs.Count)].Center;
		}
	}

	private static void TryDespawn()
	{
		if (!Main.dayTime || Main.time > 48600.0)
		{
			foreach (var npc in Main.ActiveNPCs)
			{
				if (vDatabase.ContainsKey(npc.type) && !WorldGen.PlayerLOS((int)(npc.Center.X / 16), (int)(npc.Center.Y / 16)))
				{
					DespawnNPC(npc);
					TravelerSpawned = false;

					break;
				}
			}
		}

		static void DespawnNPC(NPC npc)
		{
			if (Main.netMode == NetmodeID.SinglePlayer)
				Main.NewText(Lang.misc[35].Format(npc.FullName), 50, 125);
			else if (Main.netMode == NetmodeID.Server)
				ChatHelper.BroadcastChatMessage(NetworkText.FromKey(Lang.misc[35].Key, npc.GetFullNetName()), new Color(50, 125, 255));

			npc.active = false;
			npc.netSkip = -1;
			npc.life = 0;

			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
		}
	}

	private static void PopulateDatabase()
	{
		foreach (var npc in SpiritReforgedMod.Instance.GetContent<ModNPC>())
		{
			if (npc is ITravelNPC v)
				vDatabase.Add(npc.Type, v.CanSpawnTraveler);
		}
	}
}