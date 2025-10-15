using SpiritReforged.Common.Multiplayer;
using System.IO;
using Terraria.DataStructures;

namespace SpiritReforged.Common.TileCommon.PresetTiles;

/// <summary> Helps build tiles which use a tile entity of any sort. </summary>
public abstract class EntityTile<T> : ModTile where T : ModTileEntity
{
	/// <summary> The <b>template</b> instance of the associated tile entity. if instanced data is required, use <see cref="Entity"/> instead. </summary>
	protected T entity;

	public PlacementHook Hook => new(entity.Hook_AfterPlacement, -1, 0, false);
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
		if (!effectOnly && !fail && Entity(i, j) is T entity)
			entity.Kill(i, j);
	}
}

/// <summary> Syncs a tile entity by ID. </summary>
internal class TileEntityData : PacketData
{
	private readonly short _id;

	public TileEntityData() { }
	public TileEntityData(short tileEntityID) => _id = tileEntityID;

	public override void OnReceive(BinaryReader reader, int whoAmI)
	{
		short id = reader.ReadInt16();

		if (Main.netMode == NetmodeID.Server) //Relay to other clients
			new TileEntityData(id).Send(ignoreClient: whoAmI);

		if (TileEntity.ByID.TryGetValue(id, out var value))
			value.NetReceive(reader);
	}

	public override void OnSend(ModPacket modPacket)
	{
		modPacket.Write(_id);
		TileEntity.ByID[_id].NetSend(modPacket);
	}
}