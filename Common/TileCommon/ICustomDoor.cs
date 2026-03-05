namespace SpiritReforged.Common.TileCommon;

/// <summary> Supports doors with custom dimensions and provides access to <see cref="OnDoorInteraction"/>. </summary>
public interface ICustomDoor
{
	//Multi-tile thick doors will automatically close when opened with AutoDoor from the right
	//GameContent.DoorOpeningHelper provides an inaccessible solution using the private interface, DoorAutoHandler
	public sealed class CustomDoorLoader : ILoadable
	{
		public void Load(Mod mod)
		{
			On_WorldGen.OpenDoor += OpenCustomDoor;
			On_WorldGen.CloseDoor += CloseCustomDoor;
		}

		private static bool OpenCustomDoor(On_WorldGen.orig_OpenDoor orig, int i, int j, int direction)
		{
			Tile tile = Main.tile[i, j];

			if (TileLoader.GetTile(tile.TileType) is ICustomDoor customDoor)
			{
				customDoor.OnDoorInteraction(i, j, true);
				return InteractWithCustomDoor(tile, i, j); //Skip orig
			}

			return orig(i, j, direction);
		}

		private static bool CloseCustomDoor(On_WorldGen.orig_CloseDoor orig, int i, int j, bool forced)
		{
			Tile tile = Main.tile[i, j];

			if (TileLoader.GetTile(tile.TileType) is ICustomDoor customDoor)
			{
				customDoor.OnDoorInteraction(i, j, false);
				return InteractWithCustomDoor(tile, i, j); //Skip orig
			}

			return orig(i, j, forced);
		}

		/// <summary> Transforms the provided door tile into its alternate type. </summary>
		public static bool InteractWithCustomDoor(Tile tile, int i, int j)
		{
			TileExtensions.GetTopLeft(ref i, ref j);
			var data = TileObjectData.GetTileData(tile);
			int type = (TileID.Sets.OpenDoorID[tile.TileType] is int openDoorType && openDoorType != -1) ? openDoorType : TileID.Sets.CloseDoorID[tile.TileType];

			if (type == -1 || data == null)
				return false;

			Point size = new(data.Width, data.Height);
			for (int x = 0; x < size.X; x++)
			{
				for (int y = 0; y < size.Y; y++)
				{
					Main.tile[i + x, j + y].TileType = (ushort)type;

					if (Main.netMode != NetmodeID.MultiplayerClient && Wiring.running)
						Wiring.SkipWire(i + x, j + y);
				}
			}

			for (int x = 0; x < size.X; x++)
			{
				for (int y = 0; y < size.Y; y++)
					WorldGen.TileFrame(i + x, j + y);
			}

			return true;
		}

		public void Unload() { }
	}

	/// <summary> Called whenever this door is opened or closed. </summary>
	/// <param name="i"> The X coordinate. </param>
	/// <param name="j"> The Y coordinate. </param>
	/// <param name="open"> Whether the door was opened. </param>
	public void OnDoorInteraction(int i, int j, bool open) { }
}