using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.ProjectileCommon;
using System.IO;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Desert.Tiles.Chains;

public class ChainEntity : ModTileEntity
{
	internal class PlacementData : PacketData
	{
		private readonly ushort _x;
		private readonly ushort _y;

		private readonly short _type;
		private readonly byte _segments;

		public PlacementData() { }
		public PlacementData(ushort x, ushort y, byte segments)
		{
			_x = x;
			_y = y;
			_segments = segments;
		}

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			ushort x = reader.ReadUInt16();
			ushort y = reader.ReadUInt16();
			byte segments = reader.ReadByte();

			if (Main.netMode == NetmodeID.Server) //Relay to other clients
				new PlacementData(x, y, segments).Send(ignoreClient: whoAmI);

			OnPlace(x, y, segments);
		}

		public override void OnSend(ModPacket modPacket)
		{
			modPacket.Write(_x);
			modPacket.Write(_y);

			modPacket.Write(_type);
			modPacket.Write(_segments);
		}
	}

	//Only accessible by the server and singleplayer client
	public ChainLoop.PhysicsChain Chain { get; private set; }
	public byte segments;

	public void CreatePhysicsChain()
	{
		if (Chain == null && TileLoader.GetTile(Framing.GetTileSafely(Position).TileType) is ChainLoop l)
		{
			int type = l.ChainType;
			Vector2 worldCoords = Position.ToWorldCoordinates();

			Projectile projectile = PreNewProjectile.New(new EntitySource_TileEntity(this), worldCoords + new Vector2(2), Vector2.Zero, type, preSpawnAction: (projectile) =>
			{
				if (projectile.ModProjectile is ChainLoop.PhysicsChain c)
					c.anchor = Position;
			});

			Chain = projectile.ModProjectile as ChainLoop.PhysicsChain;
		}
	}

	public override bool IsTileValidForEntity(int x, int y) => IsValidForChain(x, y);
	public override int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternate)
	{
		byte segments = (byte)(ChainLoop.GetSegmentCount() + 1);
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			new PlacementData((ushort)i, (ushort)j, segments).Send();
			return -1;
		}

		return OnPlace(i, j, segments);
	}

	public override void Update() => CreatePhysicsChain();
	public override void OnNetPlace() => NetMessage.SendData(MessageID.TileEntitySharing, number: ID, number2: Position.X, number3: Position.Y);

	public override void NetSend(BinaryWriter writer) => writer.Write(segments);
	public override void NetReceive(BinaryReader reader) => segments = reader.ReadByte();

	public override void SaveData(TagCompound tag) => tag[nameof(segments)] = segments;
	public override void LoadData(TagCompound tag) => segments = tag.GetByte(nameof(segments));

	public static int OnPlace(int x, int y, byte segments)
	{
		int id = ModContent.GetInstance<ChainEntity>().Place(x, y);
		var placedChain = ByID[id] as ChainEntity;

		placedChain.segments = segments;
		return id;
	}

	public static bool IsValidForChain(int i, int j)
	{
		Tile tile = Framing.GetTileSafely(i, j);
		return tile.HasTile && TileLoader.GetTile(tile.TileType) is ChainLoop;
	}
}