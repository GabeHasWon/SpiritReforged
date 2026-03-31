using SpiritReforged.Common.WorldGeneration;
using System.IO;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Common.Misc;

public class WorldSystem : ModSystem
{
	public static event Action JustTurnedDay;

	[WorldBound]
	private static readonly Dictionary<string, bool> WorldFlags = [];

	private bool _wasDayTime;

	public static void SetWorldFlag(string name, bool value)
	{
		if (!WorldFlags.TryAdd(name, value))
			WorldFlags[name] = value;
	}

	public static bool CheckWorldFlag(string name) => WorldFlags.TryGetValue(name, out bool value) && value;

	public override void PostUpdateEverything()
	{
		if (Main.dayTime && !_wasDayTime)
			JustTurnedDay?.Invoke();

		_wasDayTime = Main.dayTime;
	}

	public override void NetSend(BinaryWriter writer)
	{
		writer.Write((byte)WorldFlags.Count);

		foreach (string name in WorldFlags.Keys)
		{
			writer.Write(name);
			writer.Write(WorldFlags[name]);
		}
	}

	public override void NetReceive(BinaryReader reader)
	{
		byte count = reader.ReadByte();

		for (int i = 0; i < count; i++)
		{
			string name = reader.ReadString();
			SetWorldFlag(name, reader.ReadBoolean());
		}
	}

	public override void SaveWorldData(TagCompound tag)
	{
		TagCompound flagCompound = [];

		foreach (string name in WorldFlags.Keys)
			flagCompound[name] = WorldFlags[name];

		tag["worldFlags"] = flagCompound;
	}

	public override void LoadWorldData(TagCompound tag)
	{
		TagCompound flagCompound = tag.GetCompound("worldFlags");

		foreach (var item in flagCompound)
		{
			if (flagCompound.TryGet(item.Key, out bool value))
				SetWorldFlag(item.Key, value);
		}
	}
}