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

/// <summary> Provides a draw method for <see cref="ModTileEntity"/>s. </summary>
public interface IEntityDraw
{
	public sealed class EntityDrawLoader : ILoadable
	{
		public void Load(Mod mod) => DrawOrderSystem.DrawTilesNonSolid += DrawEntities;

		private static void DrawEntities()
		{
			foreach (int id in TileEntity.ByID.Keys)
			{
				if (TileEntity.ByID[id] is IEntityDraw draw)
					draw.Draw(Main.spriteBatch);
			}
		}

		public void Unload() { }
	}

	public void Draw(SpriteBatch spriteBatch);
}