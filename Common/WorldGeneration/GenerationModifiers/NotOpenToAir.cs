using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.GenerationModifiers;

/// <summary>
/// Continues generation if the given tile is not exposed to air.
/// </summary>
public class NotOpenToAir() : GenAction
{
	public override bool Apply(Point origin, int x, int y, params object[] args)
	{
		if (WorldGen.TileIsExposedToAir(x, y))
			return Fail();

		return UnitApply(origin, x, y, args);
	}
}
