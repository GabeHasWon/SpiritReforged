using System.Linq;
using Terraria.DataStructures;

namespace SpiritReforged.Common.WorldGeneration;

/// <summary>
/// Contains a few helper tools to use <see cref="StructureHelper.Generator"/>.
/// </summary>
internal static class StructureTools
{
	/// <summary>
	/// Places a structure according to an origin. An <paramref name="origin"/> of (0.5f, 0.5f) would be centered, 
	/// (0.5f, 1) would be centered on the X axis, and place from the bottom on the Y axis, and so on.
	/// </summary>
	/// <param name="structure">The path to the structure to place.</param>
	/// <param name="position">Position of the structure.</param>
	/// <param name="origin">Placement origin of the structure.</param>
	/// <param name="mod">Mod to associate the structure with. Defaults to the Spirit Reforged instance.</param>
	/// <param name="cullAbove">Whether to cull tiles directly above the spawned structure. This can be useful when spawning over trees or lare multitiles.</param>
	/// <returns>The adjusted position after accounting for the origin.</returns>
	public static Point16 PlaceByOrigin(string structure, Point16 position, Vector2 origin, Mod mod = null, bool cullAbove = false)
	{
		mod ??= ModContent.GetInstance<SpiritReforgedMod>();

		var dims = StructureHelper.API.Generator.GetStructureDimensions(structure, mod);
		position = (position.ToVector2() - dims.ToVector2() * origin).ToPoint16();

		if (cullAbove)
			CullLine(position, dims);

		StructureHelper.API.Generator.GenerateStructure(structure, position, mod);
		return position;
	}

	public static void AdjustPositionByOrigin(ref Point16 position, Point16 dimensions, Vector2 origin)
	{
		position = (position.ToVector2() - dimensions.ToVector2() * origin).ToPoint16();
	}

	private static void CullLine(Point16 position, Point16 dims)
	{
		for (int i = position.X; i < position.X + dims.X; ++i)
			WorldGen.KillTile(i, position.Y - 1);
	}

	/// <summary>
	/// Spawns a structure at position and size, excluding all biomes in <paramref name="invalidBiomes"/>.
	/// </summary>
	/// <param name="position">Position of the spawned structure.</param>
	/// <param name="size">Size of the structure to be placed.</param>
	/// <param name="structureName">Path to the structure to spawn.</param>
	/// <param name="invalidBiomes">Invalid places to place the structure.</param>
	/// <returns>Whether the structure was placed or not.</returns>
	public static bool SpawnConvertedStructure(Point16 position, Point16 size, string structureName, out QuickConversion.BiomeType biome, params QuickConversion.BiomeType[] invalidBiomes)
	{
		biome = QuickConversion.FindConversionBiome(position, size);

		if (invalidBiomes.Contains(biome))
			return false;

		var conditions = TileCondition.GetArea(position.X, position.Y, size.X, size.Y);
		PlaceByOrigin(structureName, position, new(0));
		QuickConversion.SimpleConvert(conditions, biome, biome != QuickConversion.BiomeType.Purity);
		return true;
	}

	internal static void ClearActuators(short x, short y, short width, short height)
	{
		for (int i = x; i <= x + width; ++i)
		{
			for (int j = y; j <= y + height; j++)
			{
				Tile tile = Main.tile[i, j];
				tile.HasActuator = false;
			}
		}
	}
}
