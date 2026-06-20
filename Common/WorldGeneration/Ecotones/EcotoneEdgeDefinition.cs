using SpiritReforged.Content.SaltFlats.Biome;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using SpiritReforged.Content.Savanna.Biome;
using SpiritReforged.Content.Savanna.Tiles;

namespace SpiritReforged.Common.WorldGeneration.Ecotones;

#nullable enable

/// <summary>
/// Stores a vanilla frame offset or modded biome icon.
/// </summary>
public readonly struct VariableIcon
{
	private readonly Asset<Texture2D> VanillaIcons = null!;

	private readonly Point? FrameOffset;
	private readonly Asset<Texture2D>? ModIcon;

	public VariableIcon(Point? vanillaFrameOffset = null, Asset<Texture2D>? modIcon = null)
	{
		VanillaIcons = Main.Assets.Request<Texture2D>("Images/UI/Bestiary/Icon_Tags_Shadow");

		FrameOffset = vanillaFrameOffset;
		ModIcon = modIcon;

		if (ModIcon is null && FrameOffset is null)
			throw new ArgumentException("One and only one of ModIcon or FrameOffset must not be null.");
	}

	public void Draw(Vector2 position)
	{
		if (FrameOffset is { } value)
			Main.spriteBatch.Draw(VanillaIcons.Value, position, new Rectangle(30 * value.X, 30 * value.Y, 28, 28), Color.White);
		else
			Main.spriteBatch.Draw(ModIcon!.Value, position, Color.White);
	}
}

/// <summary>
/// Defines an edge for the ecotone mapper to use. This allows ecotones to know what they are and what is to either side of them.
/// </summary>
public readonly struct EcotoneEdgeDefinition(int displayId, string name, LocalizedText text, Color color, VariableIcon icon, params int[] validIds)
{
	public readonly string Name = name;
	public readonly int[] ValidIds = validIds;
	public readonly int DisplayId = displayId;
	public readonly LocalizedText DisplayName = text;
	public readonly Color MappingColor = color;
	public readonly VariableIcon Icon = icon;
	
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
	public static void AddEdgeDefinition(Mod mod, int displayId, string name, LocalizedText? displayName, Color color, VariableIcon icon, params int[] validIds)
	{
		EcotoneEdgeDefinition def = new(displayId, name, displayName ?? Language.GetOrRegister($"Mods.{mod.Name}.EcotoneEdges.{name}", () => name), color, icon, validIds);
		InternalAddDefinition(def);
	}

	/// <summary>
	/// Adds a mod-only edge definition with an overrideable display name.
	/// </summary>
	public static void AddEdgeDefinition<TDisplay, T2, TBiome>(Mod mod, string name, LocalizedText? displayName, Color color) where TDisplay : ModTile where T2 : ModTile
		where TBiome : ModBiome
	{
		LocalizedText text = displayName ?? Language.GetOrRegister($"Mods.{mod.Name}.EcotoneEdges.{name}");
		VariableIcon icon = new(null, ModContent.Request<Texture2D>(ModContent.GetInstance<TBiome>().BestiaryIcon));
		EcotoneEdgeDefinition edge = new(ModContent.TileType<TDisplay>(), name, text, color, icon, ModContent.TileType<TDisplay>(), ModContent.TileType<T2>());
		InternalAddDefinition(edge);
	}

	/// <inheritdoc cref="AddEdgeDefinition{TDisplay, T2}(Mod, string, LocalizedText?)"/>
	public static void AddEdgeDefinition<TDisplay, T2, T3, TBiome>(Mod mod, string name, LocalizedText? displayName, Color color) 
		where TDisplay : ModTile where T2 : ModTile where T3 : ModTile where TBiome : ModBiome
	{
		LocalizedText text = displayName ?? Language.GetOrRegister($"Mods.{mod.Name}.EcotoneEdges.{name}");
		VariableIcon icon = new(null, ModContent.Request<Texture2D>(ModContent.GetInstance<TBiome>().BestiaryIcon));
		EcotoneEdgeDefinition edge = new(ModContent.TileType<TDisplay>(), name, text, color, icon, ModContent.TileType<TDisplay>(), ModContent.TileType<T2>(), ModContent.TileType<T3>());
		InternalAddDefinition(edge);
	}

	public static EcotoneEdgeDefinition GetEcotone(string name) => _edgesByName[name];
	public static EcotoneEdgeDefinition GetEcotoneByTile(int id) => _edgesByContainedTiles[id];
	public static bool TryGetEcotoneByTile(int id, out EcotoneEdgeDefinition def) => _edgesByContainedTiles.TryGetValue(id, out def);
	public static bool TileRegistered(int id) => _registeredIds.Contains(id);

	public override void PostSetupContent()
	{
		AddEdgeDefinition(Mod, TileID.Dirt, "Forest", null, Color.ForestGreen, new VariableIcon(new(0, 0)), TileID.Grass, TileID.Dirt, TileID.ClayBlock, 
			TileID.CorruptGrass, TileID.CrimsonGrass, TileID.HallowedGrass);
		AddEdgeDefinition(Mod, TileID.Adamantite, "Desert", null, Color.SandyBrown, new VariableIcon(new(4, 0)), TileID.Sand, TileID.Ebonsand, TileID.Crimsand);
		AddEdgeDefinition(Mod, TileID.CobaltBrick, "Ocean", null, Color.DeepSkyBlue, new VariableIcon(new(11, 1)));
		AddEdgeDefinition(Mod, TileID.SnowBlock, "Snow", null, Color.LightSkyBlue, new VariableIcon(new(5, 0)), TileID.SnowBlock, TileID.IceBlock, TileID.CorruptIce, TileID.FleshIce);
		AddEdgeDefinition(Mod, TileID.ChlorophyteBrick, "Jungle", null, Color.LimeGreen, new VariableIcon(new(6, 1)), TileID.JungleGrass, TileID.Mud, TileID.CorruptJungleGrass, 
			TileID.CrimsonJungleGrass);

		AddEdgeDefinition<SavannaGrass, LivingBaobab, LivingBaobabLeaf, SavannaBiome>(Mod, "Savanna", null, new Color(153, 111, 48));
		AddEdgeDefinition<SaltBlockDull, SaltBlockReflective, SaltBiome>(Mod, "SaltFlats", null, Color.White);

		// Legacy conversion entries - these now all count as their respective non-converted biome, i.e. Forest, Desert, Jungle
		// These are kept in case we have substantial changes, but I find it unlikely. - Gabe
		//AddEdgeDefinition(Mod, TileID.DemoniteBrick, "Corruption", null, Color.Purple, TileID.CorruptGrass, TileID.Ebonstone, TileID.CorruptIce, TileID.CorruptJungleGrass);
		//AddEdgeDefinition(Mod, TileID.CrimtaneBrick, "Crimson", null, Color.Red, TileID.CrimsonGrass, TileID.Crimstone, TileID.FleshIce, TileID.CrimsonJungleGrass);
		//AddEdgeDefinition(Mod, TileID.CrimtaneBrick, "Hallow", null, Color.Pink, TileID.HallowedGrass, TileID.Pearlsand, TileID.Pearlstone, TileID.HallowedIce);
	}

	public override void OnModUnload() 
	{
		_edgesByName = null!;
		_edgesByContainedTiles = null!;
		_registeredIds = null!;
	}
}
