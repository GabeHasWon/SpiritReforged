using System.IO;
using System.Linq;
using System.Reflection;
using Terraria.ModLoader.Core;

namespace SpiritReforged.Common.Multiplayer;

/// <summary> Denotes a public static method than can be synced using <see cref="MultiplayerLoader.Send"/>. </summary>
/// <param name="Relay"> Whether the method will automatically be called back on other clients. Only has an effect if <see cref="MultiplayerLoader.Send"/> is called on a multiplayer client. </param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
internal class NetSyncedAttribute(bool Relay = false) : Attribute
{
	public readonly bool RelayToClients = Relay;
}

internal partial class MultiplayerLoader : ILoadable
{
	public static readonly Dictionary<byte, PacketData> PacketTypes = [];
	private static readonly Dictionary<byte, MethodInfo> NetMethods = [];

	/// <summary> A list of write behaviours for respective types. Must contain types equal to <see cref="ReadSigns"/>. </summary>
	private static readonly Dictionary<Type, Action<ModPacket, object>> WriteSigns = [];
	/// <summary> A list of read behaviours for respective types. Must contain types equal to <see cref="WriteSigns"/>. </summary>
	private static readonly Dictionary<Type, Func<BinaryReader, object>> ReadSigns = [];

	/// <summary> Loads all data definitions into static lookups. Must be ordered consistently between clients. </summary>
	public void Load(Mod mod)
	{
		byte count = 0;

		#region sign registration
		//Add supported signs for ModPacket
		WriteSigns.Add(typeof(bool), static (modPacket, argument) => modPacket.Write((bool)argument));
		WriteSigns.Add(typeof(int), static (modPacket, argument) => modPacket.Write((int)argument));
		WriteSigns.Add(typeof(float), static (modPacket, argument) => modPacket.Write((float)argument));
		WriteSigns.Add(typeof(Vector2), static (modPacket, argument) => modPacket.WriteVector2((Vector2)argument));
		WriteSigns.Add(typeof(Player), static (modPacket, argument) => modPacket.Write((ushort)((Player)argument).whoAmI));
		WriteSigns.Add(typeof(NPC), static (modPacket, argument) => modPacket.Write((ushort)((NPC)argument).whoAmI));

		//Add supported signs for BinaryReader
		ReadSigns.Add(typeof(bool), static (binaryReader) => binaryReader.ReadBoolean());
		ReadSigns.Add(typeof(int), static (binaryReader) => binaryReader.ReadInt32());
		ReadSigns.Add(typeof(float), static (binaryReader) => binaryReader.ReadSingle());
		ReadSigns.Add(typeof(Vector2), static (binaryReader) => binaryReader.ReadVector2());
		ReadSigns.Add(typeof(Player), static (binaryReader) => Main.player[binaryReader.ReadUInt16()]);
		ReadSigns.Add(typeof(NPC), static (binaryReader) => Main.npc[binaryReader.ReadUInt16()]);
		#endregion

		foreach (Type type in AssemblyManager.GetLoadableTypes(mod.Code))
		{
			if (!type.IsAbstract && type.IsSubclassOf(typeof(PacketData))) //Register PacketData
				PacketTypes.Add(count++, (PacketData)Activator.CreateInstance(type));

			AddNetMethods(ref count, type.GetMethods(BindingFlags.Static| BindingFlags.Public)); //Register net methods
		}

		static void AddNetMethods(ref byte count, MethodInfo[] methods)
		{
			foreach (MethodInfo methodInfo in methods)
			{
				if (methodInfo.GetCustomAttribute(typeof(NetSyncedAttribute)) is NetSyncedAttribute netSyncedAttribute)
					NetMethods.Add(count++, methodInfo);
			}
		}
	}

	public void Unload() { }

	/// <summary> Sends a method labeled with <see cref="NetSyncedAttribute"/> with the provided <paramref name="parameters"/>.<para/>
	/// Ensure all data types passed as parameters are registered in <see cref="Load"/> to ensure accurate reading and writing.<br/>
	/// Additionally, ensure the labeled method uses a completely unique name for identification. </summary>
	public static void Send(string methodName, int toClient = -1, int ignoreClient = -1, params object[] parameters)
	{
		byte id = NetMethods.Where(x => x.Value.Name == methodName).First().Key;
		ModPacket packet = SpiritReforgedMod.Instance.GetPacket();
		packet.Write(id);

		foreach (object parameter in parameters)
		{
			if (WriteSigns.TryGetValue(parameter.GetType(), out var writeAction))
				writeAction.Invoke(packet, parameter);
			else
				SpiritReforgedMod.Instance.Logger.Warn($"[Synchronization] Send failed! No registered sign for type {parameter.GetType()}");
		}

		packet.Send(toClient, ignoreClient);
	}

	public static void HandlePacket(BinaryReader reader, int whoAmI)
	{
		byte id = reader.ReadByte();
		if (PacketTypes.TryGetValue(id, out PacketData data)) //Read PacketData
		{
			if (data.Log)
				SpiritReforgedMod.Instance.Logger.Debug("[Synchronization] Reading incoming: " + data.GetType().Name);

			data.OnReceive(reader, whoAmI);
		}
		else if (NetMethods.TryGetValue(id, out MethodInfo info)) //Read a net method
		{
			ParameterInfo[] parameters = info.GetParameters();
			object[] parameterObjects = Enumerable.Repeat<object>(null, parameters.Length).ToArray();

			for (int index = 0; index < parameters.Length; index++)
				parameterObjects[index] = ReadSigns.TryGetValue(parameters[index].ParameterType, out var readAction) ? readAction.Invoke(reader) : reader.Read();

			SpiritReforgedMod.Instance.Logger.Debug("[Synchronization] Reading incoming method: " + info.Name);
			info.Invoke(null, parameterObjects);

			if (Main.dedServ && info.GetCustomAttribute(typeof(NetSyncedAttribute)) is NetSyncedAttribute netSyncedAttribute && netSyncedAttribute.RelayToClients)
				Send(info.Name, -1, whoAmI, parameterObjects); //Relay to clients
		}
		else
		{
			SpiritReforgedMod.Instance.Logger.Debug("[Synchronization] Invalid data id: " + id);
		}
	}
}