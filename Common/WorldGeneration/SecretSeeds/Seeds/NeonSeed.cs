using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Common.WorldGeneration.SecretSeeds.Seeds;

internal class NeonSeed : SecretSeed
{
	public override string[] Keys => ["moss", "glowing moss", "krypton", "xenon", "argon", "neon", "helium", "radon", "oganesson"];
	public override string Icon => DrawHelpers.RequestLocal(GetType(), ModContent.GetInstance<SavannaSeed>().Name + "_Icon"); //DEBUG
}