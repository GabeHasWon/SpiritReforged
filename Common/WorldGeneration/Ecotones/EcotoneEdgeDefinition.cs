using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using SpiritReforged.Content.Savanna.Tiles;

namespace SpiritReforged.Common.WorldGeneration.Ecotones;

#nullable enable

/// <summary>
/// Defines an edge for the ecotone mapper to use. This allows ecotones to know what they are and what is to either side of them.
/// </summary>
public readonly struct EcotoneEdgeDefinition(int displayId, string name, LocalizedText text, Color color, params int[] validIds)
{
	public readonly string Name = name;
	public readonly int[] ValidIds = validIds;
	public readonly int DisplayId = displayId;
	public readonly LocalizedText DisplayName = text;
	public readonly Color MappingColor = color;
	
	public override string ToString() => Name + $"(Display: {DisplayId})";
}

public class EcotoneEdgeDefinitions : ModSystem
{
	private static Dictionary<string, EcotoneEdgeDefinition> _edgesByName = [];
	private static Dictionary<int, EcotoneEdgeDefinition> _edgesByContainedTiles = [];
	private static HashSet<int> _registeredIds = [];

	private static void InternalAddDefinition(EcotoneEdgeDefinition def)
	{
		_edgesByName.Add(def.Name, def);

		foreach (int item in def.ValidIds)
		{
			_edgesByContainedTiles.Add(item, def);
			_registeredIds.Add(item);
		}
	}

	/// <summary>
	/// Adds a vanilla-only edge definition with an overrideable display name.
	/// </summary>
	public static void AddEdgeDefinition(Mod mod, int displayId, string name, LocalizedText? displayName, Color color, params int[] validIds)
	{
		EcotoneEdgeDefinition def = new(displayId, name, displayName ?? Language.GetOrRegister($"Mods.{mod.Name}.EcotoneEdges.{name}", () => name), color, validIds);
		InternalAddDefinition(def);
	}

	/// <summary>
	/// Adds a mod-only edge definition with an overrideable display name.
	/// </summary>
	public static void AddEdgeDefinition<TDisplay, T2>(Mod mod, string name, LocalizedText? displayName, Color color) where TDisplay : ModTile where T2 : ModTile
	{
		LocalizedText text = displayName ?? Language.GetOrRegister($"Mods.{mod.Name}.EcotoneEdges.{name}");
		EcotoneEdgeDefinition edge = new(ModContent.TileType<TDisplay>(), name, text, color, ModContent.TileType<TDisplay>(), ModContent.TileType<T2>());
		InternalAddDefinition(edge);
	}

	/// <inheritdoc cref="AddEdgeDefinition{TDisplay, T2}(Mod, string, LocalizedText?)"/>
	public static void AddEdgeDefinition<TDisplay, T2, T3>(Mod mod, string name, LocalizedText? displayName, Color color) where TDisplay : ModTile where T2 : ModTile where T3 : ModTile
	{
		LocalizedText text = displayName ?? Language.GetOrRegister($"Mods.{mod.Name}.EcotoneEdges.{name}");
		EcotoneEdgeDefinition edge = new(ModContent.TileType<TDisplay>(), name, text, color, ModContent.TileType<TDisplay>(), ModContent.TileType<T2>(), ModContent.TileType<T3>());
		InternalAddDefinition(edge);
	}

	public static EcotoneEdgeDefinition GetEcotone(string name) => _edgesByName[name];
	public static EcotoneEdgeDefinition GetEcotoneByTile(int id) => _edgesByContainedTiles[id];
	public static bool TryGetEcotoneByTile(int id, out EcotoneEdgeDefinition def) => _edgesByContainedTiles.TryGetValue(id, out def);
	public static bool TileRegistered(int id) => _registeredIds.Contains(id);

	public override void PostSetupContent()
	{
		AddEdgeDefinition(Mod, TileID.Dirt, "Forest", null, Color.ForestGreen, TileID.Grass, TileID.Dirt, TileID.ClayBlock);
		AddEdgeDefinition(Mod, TileID.Adamantite, "Desert", null, Color.SandyBrown, TileID.Sand, TileID.Ebonsand, TileID.Crimsand);
		AddEdgeDefinition(Mod, TileID.CobaltBrick, "Ocean", null, Color.DeepSkyBlue);
		AddEdgeDefinition(Mod, TileID.SnowBlock, "Snow", null, Color.LightSkyBlue, TileID.SnowBlock, TileID.IceBlock);
		AddEdgeDefinition(Mod, TileID.ChlorophyteBrick, "Jungle", null, Color.LimeGreen, TileID.JungleGrass, TileID.Mud);
		AddEdgeDefinition(Mod, TileID.DemoniteBrick, "Corruption", null, Color.Purple, TileID.CorruptGrass, TileID.Ebonstone, TileID.CorruptIce, TileID.CorruptJungleGrass);
		AddEdgeDefinition(Mod, TileID.CrimtaneBrick, "Crimson", null, Color.Red, TileID.CrimsonGrass, TileID.Crimstone, TileID.FleshIce, TileID.CrimsonJungleGrass);
		AddEdgeDefinition(Mod, TileID.CrimtaneBrick, "Hallow", null, Color.Pink, TileID.HallowedGrass, TileID.Pearlsand, TileID.Pearlstone, TileID.HallowedIce);
		AddEdgeDefinition<SavannaGrass, LivingBaobab, LivingBaobabLeaf>(Mod, "Savanna", null, Color.Orange);
		AddEdgeDefinition<SaltBlockDull, SaltBlockReflective>(Mod, "Salt Flats", null, Color.White);
	}

	public override void OnModUnload() 
	{
		_edgesByName = null!;
		_edgesByContainedTiles = null!;
		_registeredIds = null!;
	}
}
