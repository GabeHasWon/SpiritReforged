using SpiritReforged.Common.WorldGeneration;
using System.IO;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.SaltFlats;

internal class SaltFlatsSystem : ModSystem
{
	[WorldBound]
	internal static int SurfaceHeight;

	public override void SaveWorldData(TagCompound tag) => tag.Add("height", SurfaceHeight);
	public override void LoadWorldData(TagCompound tag) => SurfaceHeight = tag.GetInt("height");

	public override void NetSend(BinaryWriter writer) => writer.Write((short)SurfaceHeight);
	public override void NetReceive(BinaryReader reader) => SurfaceHeight = reader.ReadInt16();
}
