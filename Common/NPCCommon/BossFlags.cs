using SpiritReforged.Common.WorldGeneration;
using System.IO;
using System.Linq;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Common.NPCCommon;

internal class BossFlags : GlobalNPC
{
	internal class BossFlagSystem : ModSystem
	{
		public override void SaveWorldData(TagCompound tag) => tag.Add("ourBosses", DownedBossIds.Select(x => ModContent.GetModNPC(x).Name).ToArray());

		public override void LoadWorldData(TagCompound tag)
		{
			string[] bosses = tag.Get<string[]>("ourBosses");

			DownedBossIds.Clear();

			foreach (string boss in bosses)
				DownedBossIds.Add(SpiritReforgedMod.Instance.Find<ModNPC>(boss).Type);
		}

		public override void NetSend(BinaryWriter writer)
		{
			writer.Write((byte)DownedBossIds.Count);

			foreach (int boss in DownedBossIds)
				writer.Write((short)boss);
		}

		public override void NetReceive(BinaryReader reader)
		{
			int count = reader.ReadByte();

			DownedBossIds.Clear();

			for (int i = 0; i < count; ++i)
				DownedBossIds.Add(reader.ReadInt16());
		}
	}

	[WorldBound]
	public static HashSet<int> DownedBossIds = [];

	public static bool Downed(int type) => DownedBossIds.Contains(type);

	public override void OnKill(NPC npc)
	{
		if (npc.boss && npc.ModNPC is { } modNpc && modNpc.Mod is SpiritReforgedMod)
		{
			DownedBossIds.Add(npc.type);

			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.WorldData);
		}
	}
}
