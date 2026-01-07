using Terraria.DataStructures;

namespace SpiritReforged.Common.TileCommon;

/// <summary> Provides an update method called on all clients and the server for <see cref="ModTileEntity"/>s. </summary>
public interface IEntityUpdate
{
	public sealed class EntityUpdateSystem : ModSystem
	{
		public override void PostUpdateProjectiles()
		{
			foreach (int id in TileEntity.ByID.Keys)
			{
				if (TileEntity.ByID[id] is IEntityUpdate update)
					update.GlobalUpdate();
			}
		}
	}

	/// <summary> A general update method called on all clients and the server. </summary>
	public void GlobalUpdate();
}