using System.IO;
using Terraria.DataStructures;

namespace SpiritReforged.Common.Multiplayer;

internal static class SyncHelper
{
	public static void WritePoint16(this BinaryWriter writer, Point16 point)
	{
		writer.Write(point.X);
		writer.Write(point.Y);
	}

	public static Point16 ReadPoint16(this BinaryReader reader) => new(reader.ReadInt16(), reader.ReadInt16());
}
