using Terraria.ModLoader.IO;

namespace SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes.Ziggurat;

public partial class ZigguratBiome : Microbiome
{
	public static ZigguratBiome Instance { get; private set; }
	public Rectangle Area { get; private set; }

	public override void WorldLoad(TagCompound tag)
	{
		base.WorldLoad(tag);

		Area = new(Position.X - Width / 2, Position.Y - Height / 2, Width, Height);
		Instance = this;
	}
}