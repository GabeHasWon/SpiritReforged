using Terraria.GameContent.Generation;

namespace SpiritReforged.Common.WorldGeneration.Ecotones;

/// <summary>
/// Defines a pass as one that places an ecotone. This is solely a marker for use in the manual ecotone selector.
/// </summary>
public class EcotonePass(string name, WorldGenLegacyMethod method, EcotoneBase ecotone) : PassLegacy(name, method)
{
	public readonly EcotoneBase Ecotone = ecotone;
}
