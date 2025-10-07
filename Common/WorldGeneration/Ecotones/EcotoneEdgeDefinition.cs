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
	private static Dictionary<string, EcotoneEdgeDefinition> _ecotonesByName = [];
	private static Dictionary<int, EcotoneEdgeDefinition> _ecotonesByValidTileIds = [];
	private static HashSet<int> _registeredIds = [];

	public static void AddEcotone(EcotoneEdgeDefinition def)
	{
		_ecotonesByName.Add(def.Name, def);

		foreach (int item in def.ValidIds)
		{
			_ecotonesByValidTileIds.Add(item, def);
			_registeredIds.Add(item);
		}
	}

	public static EcotoneEdgeDefinition GetEcotone(string name) => _ecotonesByName[name];
	public static EcotoneEdgeDefinition GetEcotoneByTile(int id) => _ecotonesByValidTileIds[id];
	public static bool TryGetEcotoneByTile(int id, out EcotoneEdgeDefinition def) => _ecotonesByValidTileIds.TryGetValue(id, out def);
	public static bool TileRegistered(int id) => _registeredIds.Contains(id);

	public void Load(Mod mod)
	{
		AddEcotone(new EcotoneEdgeDefinition(TileID.Dirt, "Forest", TileID.Grass, TileID.Dirt, TileID.ClayBlock));
		AddEcotone(new EcotoneEdgeDefinition(TileID.Adamantite, "Desert", TileID.Sand));
		AddEcotone(new EcotoneEdgeDefinition(TileID.CobaltBrick, "Ocean"));
		AddEcotone(new EcotoneEdgeDefinition(TileID.SnowBlock, "Snow", TileID.SnowBlock, TileID.IceBlock));
		AddEcotone(new EcotoneEdgeDefinition(TileID.ChlorophyteBrick, "Jungle", TileID.JungleGrass));
		AddEcotone(new EcotoneEdgeDefinition(TileID.DemoniteBrick, "Corruption", TileID.CorruptGrass, TileID.Ebonsand, TileID.Ebonstone, TileID.CorruptIce, TileID.CorruptJungleGrass));
		AddEcotone(new EcotoneEdgeDefinition(TileID.CrimtaneBrick, "Crimson", TileID.CrimsonGrass, TileID.Crimsand, TileID.Crimstone, TileID.FleshIce, TileID.CrimsonJungleGrass));
		AddEcotone(new EcotoneEdgeDefinition(TileID.CrimtaneBrick, "Hallow", TileID.HallowedGrass, TileID.Pearlsand, TileID.Pearlstone, TileID.HallowedIce));
	}

	public void Unload()
	{
		_ecotonesByName = null;
		_ecotonesByValidTileIds = null;
		_registeredIds = null;
	}
}
