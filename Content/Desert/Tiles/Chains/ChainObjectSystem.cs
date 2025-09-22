using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.WorldGeneration;
using System.Collections.ObjectModel;
using System.IO;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Desert.Tiles.Chains;

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
				AddObject(loop.Find(coords, count));
		}

		public override void OnSend(ModPacket modPacket)
		{
			modPacket.WritePoint16(_coords);
			modPacket.Write(_segments);
			modPacket.Write(_tileType);
		}
	}

	public static ReadOnlyCollection<ChainObject> Objects => new([.. ObjectByCoords.Values]);

	[WorldBound]
	private static readonly Dictionary<Point16, ChainObject> ObjectByCoords = [];

	public static void AddObject(ChainObject value) => ObjectByCoords.Add(value.anchor, value);
	public static bool RemoveObject(Point16 coordinate)
	{
		if (ObjectByCoords.TryGetValue(coordinate, out ChainObject value))
			value.OnKill();

		return ObjectByCoords.Remove(coordinate);
	}

	public override void PostUpdateProjectiles()
	{
		foreach (Point16 coords in ObjectByCoords.Keys)
			ObjectByCoords[coords].Update();
	}

	public override void PostDrawTiles()
	{
		if (ObjectByCoords.Count != 0)
		{
			Main.spriteBatch.BeginDefault();

			foreach (Point16 coords in ObjectByCoords.Keys)
				ObjectByCoords[coords].Draw(Main.spriteBatch);

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
		ObjectByCoords.Clear();
		ushort count = reader.ReadUInt16();

		for (int i = 0; i < count; i++)
		{
			Point16 coords = reader.ReadPoint16();
			byte segments = reader.ReadByte();
			ushort tileType = reader.ReadUInt16();

			if (TileLoader.GetTile(tileType) is ChainLoop loop)
				AddObject(loop.Find(coords, segments));
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
				AddObject(loop.Find(coords, segments));
		}
	}
}