using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.TileCommon;
using System.IO;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Forest.Misc.ChristmasTreeTops;

internal class ChristmasTreeInformationEntity : ModTileEntity
{
	/// <summary>
	/// Used to place a <see cref="ChristmasTreeInformationEntity"/> on servers as the standard system doesn't seem to work.
	/// </summary>
	internal class ChristmasEntityData : PacketData
	{
		private readonly short _x;
		private readonly short _y;

		public ChristmasEntityData() { }
		public ChristmasEntityData(int x, int y)
		{
			_x = (short)x;
			_y = (short)y;
		}

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			int x = reader.ReadInt16();
			int y = reader.ReadInt16();
			int id = ModContent.GetInstance<ChristmasTreeInformationEntity>().Place(x, y);

			NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, id);
		}

		public override void OnSend(ModPacket modPacket)
		{
			modPacket.Write(_x);
			modPacket.Write(_y);
		}
	}

	public short Topper = -1;

	internal static bool TryGetEntity(Point16 pos, out ChristmasTreeInformationEntity tree, out Point16 newPosition)
	{
		tree = null;
		newPosition = pos;

		Tile tile = Main.tile[pos];
		int x = pos.X;
		int y = pos.Y;

		if (tile.TileFrameX < 10)
		{
			x -= tile.TileFrameX;
			y -= tile.TileFrameY;
		}

		x += 2; // Above code mimics vanilla top-left anchor, but this TE is offset by 2 tiles right so it draws over the tree properly

		// Check if position is valid
		if (ByPosition.TryGetValue(new Point16(x, y), out TileEntity treeEntityTryTwo) && treeEntityTryTwo is ChristmasTreeInformationEntity treeTETwo)
		{
			tree = treeTETwo;
			newPosition = new Point16(x, y);
			return true;
		}
		else if (ValidCheck(x, y))
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				new ChristmasEntityData(x, y).Send();
				return false;
			}

			ModContent.GetInstance<ChristmasTreeInformationEntity>().Place(x, y);
			tree = ByPosition[new Point16(x, y)] as ChristmasTreeInformationEntity;
			newPosition = new Point16(x, y);
			return true;
		}
		else
			return false;
	}

	public override bool IsTileValidForEntity(int x, int y) => ValidCheck(x, y);

	internal static bool ValidCheck(int x, int y)
	{
		Tile tile = Main.tile[x, y];
		return tile.HasTile && tile.TileType == TileID.ChristmasTree && tile.TileFrameX == 2 && tile.TileFrameY == 0;
	}

	public override void SaveData(TagCompound tag) => tag.Add("topper", Topper);
	public override void LoadData(TagCompound tag) => Topper = tag.GetShort("topper");

	public override void NetSend(BinaryWriter writer) => writer.Write(Topper);
	public override void NetReceive(BinaryReader reader) => Topper = reader.ReadInt16();
}

internal class ChristmasTreeFunctionality : GlobalTile
{
	/// <summary>
	/// Used to update <see cref="ChristmasTreeInformationEntity"/> from Client -> Server.
	/// </summary>
	internal class ChristmasTreePacketData : PacketData
	{
		private readonly short _x;
		private readonly short _y;
		private readonly int _obj;

		public ChristmasTreePacketData() { }
		public ChristmasTreePacketData(int x, int y, int obj)
		{
			_x = (short)x;
			_y = (short)y;
			_obj = obj;
		}

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			int x = reader.ReadInt16();
			int y = reader.ReadInt16();
			int obj = reader.ReadInt32();

			UpdateTreeTopper(ref x, ref y, obj);
		}

		public override void OnSend(ModPacket modPacket)
		{
			modPacket.Write(_x);
			modPacket.Write(_y);
			modPacket.Write(_obj);
		}
	}

	public override void Load() => On_WorldGen.dropXmasTree += RemoveCustomItemFromTree;

	private void RemoveCustomItemFromTree(On_WorldGen.orig_dropXmasTree orig, int x, int y, int obj)
	{
		orig(x, y, obj);

		UpdateTreeTopper(ref x, ref y, obj);
	}

	private static void UpdateTreeTopper(ref int x, ref int y, int obj)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			new ChristmasTreePacketData(x, y, obj).Send();
			return;
		}

		if (!ChristmasTreeInformationEntity.TryGetEntity(new Point16(x, y), out ChristmasTreeInformationEntity tree, out Point16 newPosition))
			return;

		x = newPosition.X;
		y = newPosition.Y;

		if (obj == 0 && tree.Topper != -1)
		{
			Item.NewItem(new EntitySource_TileBreak(x, y), new Vector2(x, y) * 16, tree.Topper);
			tree.Topper = -1;
			NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, tree.ID);
		}
	}

	public override void PostDraw(int i, int j, int type, SpriteBatch spriteBatch)
	{
		if (!ChristmasTreeInformationEntity.ValidCheck(i, j) || !TileEntity.ByPosition.TryGetValue(new Point16(i, j), out TileEntity te) || te is not ChristmasTreeInformationEntity tree)
			return;

		if (tree.Topper != -1)
		{
			Texture2D tex = TreeTopper.TopperTextures[tree.Topper].Value;
			spriteBatch.Draw(tex, TileExtensions.DrawPosition(i - 1, j, new Vector2(-5, 0)), Color.White);
		}
	}

	public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (!ChristmasTreeInformationEntity.ValidCheck(i, j) || !TileEntity.ByPosition.TryGetValue(new Point16(i, j), out TileEntity te) || te is not ChristmasTreeInformationEntity tree)
			return;

		if (!noItem)
		{
			if (tree.Topper != -1)
				Item.NewItem(new EntitySource_TileBreak(i, j), new Vector2(i, j) * 16, tree.Topper);
		}

		ModContent.GetInstance<ChristmasTreeInformationEntity>().Kill(i, j);
	}
}