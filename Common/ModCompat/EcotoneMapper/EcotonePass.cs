using SpiritReforged.Common.WorldGeneration.Ecotones;
using Terraria.GameContent.Generation;

namespace SpiritReforged.Common.ModCompat.EcotoneMapper;

/// <summary>
/// Defines a pass as one that places an ecotone. This is solely a marker for use in the manual ecotone selector.
/// </summary>
internal class EcotonePass(string name, WorldGenLegacyMethod method, EcotoneBase ecotone) : PassLegacy(name, method)
{
	public readonly EcotoneBase Ecotone = ecotone;
}
