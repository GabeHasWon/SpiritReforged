using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Multiplayer;
using System.IO;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Common.TileCommon.PresetTiles;

/// <summary> A tile entity who can store a single item, saved on unload. See <see cref="SingleSlotData"/> for syncing. </summary>
public abstract class SingleSlotEntity : ModTileEntity
{
	public Item item = new();

	/// <summary> Called on the local client when right-clicking a tile. </summary>
	/// <returns> Whether an interaction has occured. </returns>
	public virtual bool OnInteract(Player player)
	{
		bool success = CanAddItem(player.HeldItem);

		if (!item.IsAir)
		{
			ItemMethods.NewItemSynced(player.GetSource_TileInteraction(Position.X, Position.Y), item, Position.ToVector2() * 16, true);
			item.TurnToAir();

			success = true;
		}

		if (success)
		{
			if (CanAddItem(player.HeldItem))
			{
				item = ItemLoader.TransferWithLimit(player.inventory[player.selectedItem], 1);

				if (player.selectedItem == 58)
					Main.mouseItem = player.inventory[player.selectedItem].Clone(); //Consume mouseItem like vanilla does
			}

			if (!item.IsAir)
				player.PlayDroppedItemAnimation(20);

			player.releaseUseItem = false;
			player.mouseInterface = true;

			if (Main.netMode == NetmodeID.MultiplayerClient)
				new SingleSlotData((short)ID, item).Send();
		}

		return success;
	}

	/// <summary> Removes <see cref="item"/> and automatically syncs it. </summary>
	public void RemoveItem()
	{
		item.TurnToAir();

		if (Main.netMode != NetmodeID.SinglePlayer)
			new SingleSlotData((short)ID, item).Send();
	}

	public virtual bool CanAddItem(Item item) => true;

	public override int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternate)
	{
		TileExtensions.GetTopLeft(ref i, ref j);
		var d = TileObjectData.GetTileData(Main.tile[i, j]);

		var size = (d is null) ? new Point(1, 1) : new Point(d.Width, d.Height);

		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			NetMessage.SendTileSquare(Main.myPlayer, i, j, size.X, size.Y);
			NetMessage.SendData(MessageID.TileEntityPlacement, number: i, number2: j, number3: Type);
			return -1;
		}

		return Place(i, j);
	}

	public override void Update()
	{
		if (!IsTileValidForEntity(Position.X, Position.Y))
			Kill(Position.X, Position.Y);
	}

	public override void OnNetPlace() => NetMessage.SendData(MessageID.TileEntitySharing, number: ID, number2: Position.X, number3: Position.Y);
	public override void NetSend(BinaryWriter writer) => ItemIO.Send(item, writer);
	public override void NetReceive(BinaryReader reader) => item = ItemIO.Receive(reader);

	public override void SaveData(TagCompound tag)
	{
		if (!item.IsAir)
			tag[nameof(item)] = item;
	}

	public override void LoadData(TagCompound tag) => item = tag.Get<Item>(nameof(item));
}

/// <summary> Sends <see cref="SingleSlotEntity.item"/> by tile entity ID. </summary>
internal class SingleSlotData : PacketData
{
	private readonly short _id;
	private readonly Item _item;

	public SingleSlotData() { }
	public SingleSlotData(short tileEntityID, Item item)
	{
		_id = tileEntityID;
		_item = item;
	}

	public override void OnReceive(BinaryReader reader, int whoAmI)
	{
		short index = reader.ReadInt16();
		Item item = ItemIO.Receive(reader);

		if (Main.netMode == NetmodeID.Server) //Relay to other clients
			new SingleSlotData(index, item).Send(ignoreClient: whoAmI);

		if (TileEntity.ByID[index] is SingleSlotEntity slot)
			slot.item = item;
	}

	public override void OnSend(ModPacket modPacket)
	{
		modPacket.Write(_id);
		ItemIO.Send(_item, modPacket);
	}
}

/// <summary> Helper tile to be used in conjunction with <see cref="SingleSlotEntity"/>. </summary>
public abstract class SingleSlotTile<T> : ModTile where T : SingleSlotEntity
{
	/// <summary> The <b>template</b> instance of the associated tile entity. if instanced data is required, use <see cref="Entity"/> instead. </summary>
	protected SingleSlotEntity entity;

	public int ItemType => (this is IAutoloadTileItem) ? this.AutoItem().type : ItemID.None;

	public override void SetStaticDefaults() => entity = ModContent.GetInstance<T>();

	/// <returns> Whether the multitile at the given position has a tile entity. </returns>
	public T Entity(int i, int j)
	{
		if (Main.tile[i, j].TileType != Type)
			return null;

		TileExtensions.GetTopLeft(ref i, ref j);
		int id = ModContent.GetInstance<T>().Find(i, j);

		return (id == -1) ? null : (T)TileEntity.ByID[id];
	}

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (effectOnly)
			return;

		if (Entity(i, j) is T slot && !slot.item.IsAir)
		{
			fail = true;

			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				TileExtensions.GetTopLeft(ref i, ref j);

				var pos = new Vector2(i, j).ToWorldCoordinates();

				Item.NewItem(new EntitySource_TileBreak(i, j), pos, slot.item);
				slot.RemoveItem();
			}
		}
	}

	public override bool RightClick(int i, int j)
	{
		if (Entity(i, j) is T entity)
			entity.OnInteract(Main.LocalPlayer);

		return true;
	}

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = (Entity(i, j) is not T entity || entity.item.IsAir) ? ItemType : entity.item.type;
	}
}