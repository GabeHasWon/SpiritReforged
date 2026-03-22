using SpiritReforged.Common.Multiplayer;
using System.IO;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Misc.ChristmasTreeTops;

internal abstract class TreeTopper : ModItem
{
	public class SendTopperToTree(int x, int y, int type) : PacketData
	{
		private readonly int _type = type;
		private readonly int _x = x;
		private readonly int _y = y;

		public SendTopperToTree() : this(0, 0, 0) { }

		public override void OnSend(ModPacket modPacket)
		{
			modPacket.Write((ushort)_x);
			modPacket.Write((ushort)_y);
			modPacket.Write((ushort)_type);
		}

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			int x = reader.ReadUInt16();
			int y = reader.ReadUInt16();
			ushort type = reader.ReadUInt16();

			if (!ChristmasTreeInformationEntity.TryGetEntity(new Point16(x, y), out ChristmasTreeInformationEntity tree, out _))
				return;

			tree.Topper = (short)type;
			NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, tree.ID);
		}
	}

	public static Dictionary<int, Asset<Texture2D>> TopperTextures = [];

	public override void SetStaticDefaults() => TopperTextures.Add(Type, ModContent.Request<Texture2D>(Texture + "_Top"));
	public override void SetDefaults() => Item.CloneDefaults(ItemID.StarTopper1);

	public override bool? UseItem(Player player)
	{
		if (Main.myPlayer != player.whoAmI)
			return false;

		Point16 pos = Main.MouseWorld.ToTileCoordinates16();

		if (!ChristmasTreeInformationEntity.TryGetEntity(pos, out ChristmasTreeInformationEntity tree, out Point16 newPosition))
			return false;

		pos = newPosition;

		if (tree.Topper == Type)
			return false;

		ClearTileExistingDecor(pos);

		tree.Topper = (short)Type;

		if (Main.netMode == NetmodeID.MultiplayerClient)
			new SendTopperToTree(pos.X, pos.Y, Type).Send();

		return true;
	}

	private static void ClearTileExistingDecor(Point16 pos)
	{
		if (WorldGen.checkXmasTreeDrop(pos.X, pos.Y, 0) != -1)
		{
			WorldGen.dropXmasTree(pos.X, pos.Y, 0);
			WorldGen.setXmasTree(pos.X, pos.Y, 0, 0);
			int num7 = pos.X;
			int num8 = pos.Y;
			if (Main.tile[pos.X, pos.Y].TileFrameX < 10)
			{
				num7 -= Main.tile[pos.X, pos.Y].TileFrameX;
				num8 -= Main.tile[pos.X, pos.Y].TileFrameY;
			}

			NetMessage.SendTileSquare(-1, num7, num8);
		}
	}
}
