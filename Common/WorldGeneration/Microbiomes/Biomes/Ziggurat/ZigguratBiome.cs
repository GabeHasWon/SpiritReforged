using System.IO;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes.Ziggurat;

public partial class ZigguratBiome : Microbiome
{
	public static ZigguratBiome Instance { get; private set; }
	public Rectangle Area => new(Position.X - Width / 2, Position.Y - Height / 2, Width, Height);

	public override void WorldLoad(TagCompound tag)
	{
		base.WorldLoad(tag);
		Instance ??= this;
	}

	public override void NetReceive(BinaryReader reader)
	{
		base.NetReceive(reader);
		Instance ??= this;
	}
}