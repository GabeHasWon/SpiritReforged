namespace SpiritReforged.Common.WorldGeneration.Ecotones;

public readonly struct EcotoneEdgeDefinition(int displayId, string name, params int[] validIds)
{
	public readonly string Name = name;
	public readonly int[] ValidIds = validIds;
	public readonly int DisplayId = displayId;

	public override string ToString() => Name + $"(Display: {DisplayId})";
}

public class EcotoneEdgeDefinitions : ILoadable
{
	private static Dictionary<string, EcotoneEdgeDefinition> _edgesByName = [];
	private static Dictionary<int, EcotoneEdgeDefinition> _edgesByContainedTiles = [];
	private static HashSet<int> _registeredIds = [];

	public static void AddEdgeDefinition(EcotoneEdgeDefinition def)
	{
		_edgesByName.Add(def.Name, def);

		foreach (int item in def.ValidIds)
		{
			_edgesByContainedTiles.Add(item, def);
			_registeredIds.Add(item);
		}
	}

	public static EcotoneEdgeDefinition GetEcotone(string name) => _edgesByName[name];
	public static EcotoneEdgeDefinition GetEcotoneByTile(int id) => _edgesByContainedTiles[id];
	public static bool TryGetEcotoneByTile(int id, out EcotoneEdgeDefinition def) => _edgesByContainedTiles.TryGetValue(id, out def);
	public static bool TileRegistered(int id) => _registeredIds.Contains(id);

	public void Load(Mod mod)
	{
		AddEdgeDefinition(new EcotoneEdgeDefinition(TileID.Dirt, "Forest", TileID.Grass, TileID.Dirt, TileID.ClayBlock));
		AddEdgeDefinition(new EcotoneEdgeDefinition(TileID.Adamantite, "Desert", TileID.Sand));
		AddEdgeDefinition(new EcotoneEdgeDefinition(TileID.CobaltBrick, "Ocean"));
		AddEdgeDefinition(new EcotoneEdgeDefinition(TileID.SnowBlock, "Snow", TileID.SnowBlock, TileID.IceBlock));
		AddEdgeDefinition(new EcotoneEdgeDefinition(TileID.ChlorophyteBrick, "Jungle", TileID.JungleGrass));
		AddEdgeDefinition(new EcotoneEdgeDefinition(TileID.DemoniteBrick, "Corruption", TileID.CorruptGrass, TileID.Ebonsand, TileID.Ebonstone, TileID.CorruptIce, TileID.CorruptJungleGrass));
		AddEdgeDefinition(new EcotoneEdgeDefinition(TileID.CrimtaneBrick, "Crimson", TileID.CrimsonGrass, TileID.Crimsand, TileID.Crimstone, TileID.FleshIce, TileID.CrimsonJungleGrass));
		AddEdgeDefinition(new EcotoneEdgeDefinition(TileID.CrimtaneBrick, "Hallow", TileID.HallowedGrass, TileID.Pearlsand, TileID.Pearlstone, TileID.HallowedIce));
	}

	public void Unload()
	{
		_edgesByName = null;
		_edgesByContainedTiles = null;
		_registeredIds = null;
	}
}
