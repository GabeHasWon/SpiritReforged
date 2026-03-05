using SpiritReforged.Common.Multiplayer;
using System.IO;
using System.Linq;
using Terraria.DataStructures;
using Terraria.Map;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Common.WorldGeneration.PointOfInterest;

/// <summary> Common identifier for interest types. </summary> //ALWAYS append new types immediately before Count to ensure proper loading
public enum InterestType : byte
{
	FloatingIsland,
	EnchantedSword,
	ButterflyShrine,
	Shimmer,
	Savanna,
	Hive,
	Curiosity,
	BloodAltar, //Thorium Mod exclusive
	WulfrumBunker, //Fables Mod exclusive
	SaltFlat,
	Ziggurat,
	Count
}

/// <summary> Handles marking any points of interest, such as the Shimmer and Hives.<br/>
/// Currently in use by the Cartographer's mapping system. </summary>
public class PointOfInterestSystem : ModSystem
{
	/// <summary> Instantly requests all points of interest upon joining a server. </summary>
	internal class PoIPlayer : ModPlayer
	{
		public override void OnEnterWorld()
		{
			if (Main.netMode != NetmodeID.SinglePlayer)
				new RequestPoIData().Send();
		}
	}

	/// <summary> Requests <b>ALL</b> point of interest data for client use. </summary>
	internal class RequestPoIData : PacketData
	{
		public RequestPoIData() { }

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			if (Main.netMode == NetmodeID.Server)
			{
				new RequestPoIData().Send(whoAmI);
			}
			else
			{
				byte count = reader.ReadByte();

				for (int c = 0; c < count; c++)
				{
					Point16 position = reader.ReadPoint16();
					var type = (InterestType)reader.ReadByte();
					bool discovered = reader.ReadBoolean();

					InterestByPosition.Add(position, new(type) { discovered = discovered });
				}
			}
		}

		public override void OnSend(ModPacket modPacket)
		{
			if (Main.netMode == NetmodeID.Server)
			{
				modPacket.Write((byte)InterestByPosition.Count);

				foreach (Point16 position in InterestByPosition.Keys)
				{
					Interest interest = InterestByPosition[position];

					modPacket.WritePoint16(position);
					modPacket.Write((byte)interest.type);
					modPacket.Write(interest.discovered);
				}
			}
		}
	}

	/// <summary> Relays select point of interest data for client and server use. </summary>
	internal class SyncPoIData : PacketData
	{
		private readonly Point16 _position;
		private readonly byte _type;
		private readonly bool _discovered;

		public SyncPoIData() { }
		public SyncPoIData(Point16 position, InterestType type, bool value)
		{
			_position = position;
			_type = (byte)type;
			_discovered = value;
		}

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			Point16 position = reader.ReadPoint16();
			var type = (InterestType)reader.ReadByte();
			bool discovered = reader.ReadBoolean();

			if (Main.netMode == NetmodeID.Server)
				new SyncPoIData(position, type, discovered).Send(ignoreClient: whoAmI); //Relay to other clients

			InterestByPosition[position] = new(type) { discovered = discovered };
		}

		public override void OnSend(ModPacket modPacket)
		{
			modPacket.WritePoint16(_position);
			modPacket.Write(_type);
			modPacket.Write(_discovered);
		}
	}

	public class Interest(InterestType type)
	{
		public readonly InterestType type = type;
		public bool discovered;
	}

	/// <summary> Finds whether <see cref="InterestByPosition"/> has any elements that are not discovered. </summary>
	public static bool AnyInterests => InterestByPosition.Any(x => !x.Value.discovered);

	/// <summary> A collection of interest data added during worldgen, keyed by tile position. </summary>
	[WorldBound]
	public static readonly Dictionary<Point16, Interest> InterestByPosition = [];

	public override void Load() => On_WorldMap.UpdateLighting += UpdateInterestLighting;

	/// <summary> Causes points of interest to be discovered when illuminated. </summary>
	private static bool UpdateInterestLighting(On_WorldMap.orig_UpdateLighting orig, WorldMap self, int x, int y, byte light)
	{
		bool value = orig(self, x, y, light);
		if (value && InterestByPosition.TryGetValue(new(x, y), out Interest interest) && !interest.discovered)
		{
			interest.discovered = true;

			//if (Main.netMode != NetmodeID.SinglePlayer)
			//	new SyncPoIData(new(x, y), interest.type, true).Send(); //Should this be synced?
		}

		return value;
	}

	public override void SaveWorldData(TagCompound tag)
	{
		List<TagCompound> list = [];
		foreach (Point16 position in InterestByPosition.Keys)
		{
			Interest interest = InterestByPosition[position];

			list.Add(new()
			{
				["position"] = position,
				["type"] = (byte)interest.type,
				["discovered"] = interest.discovered
			});
		}

		if (list.Count != 0)
			tag["pointsOfInterest"] = list;
	}

	public override void LoadWorldData(TagCompound tag)
	{
		if (tag.TryGet("typesCount", out int _))
		{
			Dictionary<InterestType, HashSet<Point16>> byPosition = [];
			Dictionary<InterestType, HashSet<Point16>> worldGen_ByPosition = [];

			ReadPointsLegacy(tag, byPosition, string.Empty);
			ReadPointsLegacy(tag, worldGen_ByPosition, "WorldGen");

			foreach (InterestType type in worldGen_ByPosition.Keys)
			{
				foreach (Point16 position in worldGen_ByPosition[type])
				{
					bool notDiscovered = byPosition.TryGetValue(type, out HashSet<Point16> legacyPositions) && legacyPositions.Contains(position);
					InterestByPosition.Add(position, new(type) { discovered = !notDiscovered });
				}
			}

			return;
		} //Populate points using the legacy method if necessary
		else
		{
			var list = tag.GetList<TagCompound>("pointsOfInterest");
			foreach (TagCompound item in list)
			{
				Point16 position = item.Get<Point16>("position");
				var type = (InterestType)item.GetByte("type");
				bool discovered = item.GetBool("discovered");

				InterestByPosition.Add(position, new(type) { discovered = discovered });
			}
		}
	}

	private static void ReadPointsLegacy(TagCompound tag, Dictionary<InterestType, HashSet<Point16>> dictionary, string keyPrefix)
	{
		int count = tag.GetInt("typesCount" + keyPrefix);

		for (int i = 0; i < count; ++i)
		{
			var type = (InterestType)tag.GetByte("type" + keyPrefix + i);
			HashSet<Point16> points = new(tag.Get<Point16[]>("points" + keyPrefix + i));

			dictionary.Add(type, points);
		}
	}
}