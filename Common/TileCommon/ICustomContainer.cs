using Terraria.GameContent.Drawing;

namespace SpiritReforged.Common.TileCommon;

public interface ICustomContainer
{
	public sealed class CustomContainerLoader : ILoadable
	{
		public void Load(Mod mod)
		{
			On_Player.IsInInteractionRangeToMultiTileHitbox += ModifyInteractionRange;
			On_TileDrawing.CacheSpecialDraws_Part2 += PreventContainerAnimation;
		}

		private static bool ModifyInteractionRange(On_Player.orig_IsInInteractionRangeToMultiTileHitbox orig, Player self, int chestPointX, int chestPointY)
		{
			bool value = orig(self, chestPointX, chestPointY);
			Tile tile = Main.tile[chestPointX, chestPointY];

			if (TileLoader.GetTile(tile.TileType) is ModTile modTile && modTile is ICustomContainer)
			{
				Rectangle dimensions = GetDimensions(chestPointX, chestPointY);
				dimensions.Inflate(Player.tileRangeX * 32, Player.tileRangeY * 32);

				if (dimensions.Intersects(self.Hitbox))
					value = true;
			}

			return value;
		}

		/// <summary> </summary>
		/// <param name="i"> The top left X coordinate. </param>
		/// <param name="j"> The top left Y coordinate. </param>
		/// <returns></returns>
		private static Rectangle GetDimensions(int i, int j)
		{
			if (TileObjectData.GetTileData(Main.tile[i, j]) is TileObjectData objectData)
				return new(i * 16, j * 16, objectData.Width * 16, objectData.Height * 16);

			return new(i * 16, j * 16, 2 * 16, 2 * 16);
		}

		private static void PreventContainerAnimation(On_TileDrawing.orig_CacheSpecialDraws_Part2 orig, TileDrawing self, int tileX, int tileY, Terraria.DataStructures.TileDrawInfo drawData, bool skipDraw)
		{
			if (TileLoader.GetTile(Main.tile[tileX, tileY].TileType) is ModTile modTile && modTile is ICustomContainer)
				return; //Skip orig

			orig(self, tileX, tileY, drawData, skipDraw);
		}

		public void Unload() { }
	}
}