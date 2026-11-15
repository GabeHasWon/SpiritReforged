using SpiritReforged.Common.Multiplayer;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Terraria.DataStructures;
using Terraria.Map;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Common.WorldGeneration.PointOfInterest;

/// <summary> Common identifier for interest types. </summary> 
/// ALWAYS append new types immediately before Count to ensure proper loading
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
	Spooky_NoseCult,
	Spooky_KrampusWorkshop,
	Spooky_RottenDepths,
	Spooky_FetidFarms,

	/// <summary>
	/// Defines a custom interest type which has to be gleaned from external data. This works for both in-house Reforged content and external crossmod content.<br/>
	/// Per this, DO NOT add more interest types! 
	/// </summary>
	Custom,

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
					modPacket.Write((byte)interest.legacyType);
					modPacket.Write(interest.discovered);
				}
			}
		}
	}

	/// <summary> Relays select point of interest data for client and server use. </summary>
	internal class SyncPoIData : PacketData
	{
		private readonly Point16 _position;
		private readonly string _type;
		private readonly bool _discovered;

		public SyncPoIData() { }
		public SyncPoIData(Point16 position, InterestType type, bool value)
		{
			_position = position;
			_type = "SpiritReforged/" + type.ToString();
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

	public readonly record struct UnloadedInterest(Point16 Position, string DefinitionName, bool Discovered);

	public class Interest(InterestDefinition definition)
	{
		public readonly InterestType legacyType = InterestType.Custom;

		public bool discovered;
		public InterestDefinition definition = definition;

		/// <summary>
		/// Legacy overload for using the enum.
		/// </summary>
		internal Interest(InterestType type) : this(InterestDefinition.EnumToDefinition[type]) => legacyType = type;
	}

	/// <summary> Finds whether <see cref="InterestByPosition"/> has any elements that are not discovered. </summary>
	public static bool AnyInterests => InterestByPosition.Any(x => !x.Value.discovered);

	/// <summary> A collection of interest data added during worldgen, keyed by tile position. </summary>
	[WorldBound]
	public static readonly Dictionary<Point16, Interest> InterestByPosition = [];

	/// <summary> Collections of interests that were unable to be loaded properly. </summary>
	[WorldBound]
	internal static readonly List<UnloadedInterest> UnloadedInterests = [];

	public override void Load()
	{
		Debug.Assert(InterestType.Custom == InterestType.Count - 1, "No new interest types should be added. Use the new system instead.");
		On_WorldMap.UpdateLighting += UpdateInterestLighting;
	}

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
				["type"] = interest.definition.FullName,
				["discovered"] = interest.discovered
			});
		}

		foreach (UnloadedInterest interest in UnloadedInterests)
		{
			list.Add(new()
			{
				["position"] = interest.Position,
				["type"] = interest.DefinitionName,
				["discovered"] = interest.Discovered
			});
		}

		if (list.Count != 0)
			tag["interests"] = list;
	}

	public override void LoadWorldData(TagCompound tag)
	{
		// Populate points using the original legacy method if necessary
		if (tag.TryGet("typesCount", out int _))
			LegacyLoad_TypedLoad(tag);
		else if (tag.TryGet("pointsOfInterest", out List<TagCompound> list)) // Populate using list-legacy load
			LegacyLoad_EnumType(list);
		else // Use modern crossmod-compatible list mode
		{
			var interests = tag.GetList<TagCompound>("interests");

			foreach (TagCompound item in interests)
			{
				Point16 position = item.Get<Point16>("position");
				string type = item.GetString("type");
				bool discovered = item.GetBool("discovered");

				if (ModContent.TryFind<InterestDefinition>(type, out var def))
					InterestByPosition.Add(position, new(def) { discovered = discovered });
				else
					UnloadedInterests.Add(new UnloadedInterest(position, type, discovered));
			}
		}
	}

	private static void LegacyLoad_EnumType(List<TagCompound> list)
	{
		foreach (TagCompound item in list)
		{
			Point16 position = item.Get<Point16>("position");
			var type = (InterestType)item.GetByte("type");
			bool discovered = item.GetBool("discovered");

			InterestByPosition.Add(position, new(type) { discovered = discovered });
		}
	}

	private static void LegacyLoad_TypedLoad(TagCompound tag)
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