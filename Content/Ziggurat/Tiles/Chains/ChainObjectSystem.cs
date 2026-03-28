using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.WorldGeneration;
using System.Collections.ObjectModel;
using System.IO;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Ziggurat.Tiles.Chains;

public sealed class ChainObjectSystem : ModSystem
{
	internal class PlacementData : PacketData
	{
		private readonly Point16 _coords;
		private readonly byte _segments;
		private readonly ushort _tileType;

		public PlacementData() { }
		public PlacementData(Point16 coordinates, byte segments, ushort tileType)
		{
			_coords = coordinates;
			_segments = segments;
			_tileType = tileType;
		}

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			Point16 coords = reader.ReadPoint16();
			byte count = reader.ReadByte();
			ushort tileType = reader.ReadUInt16();

			if (Main.netMode == NetmodeID.Server) //Relay to other clients
				new PlacementData(coords, count, tileType).Send(ignoreClient: whoAmI);

			if (TileLoader.GetTile(tileType) is ChainLoop loop)
				AddObject(loop.CreateObject(coords, count));
		}

		public override void OnSend(ModPacket modPacket)
		{
			modPacket.WritePoint16(_coords);
			modPacket.Write(_segments);
			modPacket.Write(_tileType);
		}
	}

	internal class ModifyVelocityData : PacketData
	{
		private Point16 _coords;
		private Vector2 _newVelocity;

		public ModifyVelocityData()
		{
		}

		public ModifyVelocityData(Point16 coords, Vector2 newVelocity)
		{
			_coords = coords;
			_newVelocity = newVelocity;
		}

		public override void OnSend(ModPacket modPacket)
		{
			modPacket.WritePoint16(_coords);
			modPacket.WriteVector2(_newVelocity);
		}

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			_coords = reader.ReadPoint16();
			_newVelocity = reader.ReadVector2();

			ObjectByCoords[_coords].lastDelta = _newVelocity;

			if (Main.netMode == NetmodeID.Server)
				Send(-1, whoAmI);
		}
	}

	public static ReadOnlyCollection<ChainObject> Objects => new([.. ObjectByCoords.Values]);

	[WorldBound]
	private static readonly Dictionary<Point16, ChainObject> ObjectByCoords = [];

	public static bool AddObject(ChainObject value) => ObjectByCoords.TryAdd(value.anchor, value);
	public static bool RemoveObject(Point16 coordinate)
	{
		if (ObjectByCoords.TryGetValue(coordinate, out ChainObject value))
			value.OnKill();

		return ObjectByCoords.Remove(coordinate);
	}

	public override void PostUpdateProjectiles()
	{
		foreach (Point16 coords in ObjectByCoords.Keys)
		{
			ChainObject chain = ObjectByCoords[coords];

			if (Main.netMode == NetmodeID.Server || chain.OnScreen()) // Server has no screen
				chain.Update();
		}
	}

	public override void PostDrawTiles()
	{
		if (ObjectByCoords.Count != 0)
		{
			Main.spriteBatch.BeginDefault();

			foreach (Point16 coords in ObjectByCoords.Keys)
			{
				ChainObject chain = ObjectByCoords[coords];

				if (chain.OnScreen())
					chain.Draw(Main.spriteBatch);
			}

			Main.spriteBatch.End();
		}
	}

	public override void NetSend(BinaryWriter writer)
	{
		writer.Write((ushort)ObjectByCoords.Count);

		foreach (Point16 coords in ObjectByCoords.Keys)
		{
			ChainObject chain = ObjectByCoords[coords];

			writer.WritePoint16(chain.anchor);
			writer.Write(chain.segments);
			writer.Write(Framing.GetTileSafely(coords).TileType);
		}
	}

	public override void NetReceive(BinaryReader reader)
	{
		ushort count = reader.ReadUInt16();

		for (int i = 0; i < count; i++)
		{
			Point16 coords = reader.ReadPoint16();
			byte segments = reader.ReadByte();
			ushort tileType = reader.ReadUInt16();

			if (ObjectByCoords.ContainsKey(coords))
				return;

			if (TileLoader.GetTile(tileType) is ChainLoop loop)
			{
				ChainObject newChain = loop.CreateObject(coords, segments);
				AddObject(newChain);
			}
		}
	}

	public override void SaveWorldData(TagCompound tag)
	{
		List<TagCompound> list = [];
		TagCompound data = [];

		foreach (Point16 coords in ObjectByCoords.Keys)
		{
			ChainObject o = ObjectByCoords[coords];
			data["anchor"] = o.anchor;
			data["segments"] = o.segments;

			list.Add(data);
			data = [];
		}

		if (list.Count != 0)
			tag["objects"] = list;
	}

	public override void LoadWorldData(TagCompound tag)
	{
		var list = tag.GetList<TagCompound>("objects");
		foreach (var item in list)
		{
			Point16 coords = item.Get<Point16>("anchor");
			byte segments = item.GetByte("segments");
			int tileType = Framing.GetTileSafely(coords).TileType;

			if (TileLoader.GetTile(tileType) is ChainLoop loop)
				AddObject(loop.CreateObject(coords, segments));
		}
	}
}