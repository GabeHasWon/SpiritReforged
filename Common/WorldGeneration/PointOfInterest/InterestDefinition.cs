using SpiritReforged.Content.Forest.Cartography.Pins;

namespace SpiritReforged.Common.WorldGeneration.PointOfInterest;

/// <summary>
/// Defines an "interest" or "discovery" - a naturally placed tile of importance, usually rare and/or low-occurance.<br/>
/// This does a few things:<para/>
/// 1. Allows this to define what counts as this interest (such as matching a tile type),<br/>
/// 2. Marks it as a discovery for the Points of Interest position,<br/>
/// 3. Allows the Cartographer to point it out as a point of interest,<br/>
/// 4. Allows custom dialogue to be said by the Cartographer when pointed out,<br/>
/// 5. Allows any interest position to be retrieved at any time, and whether it was discovered or not,
/// 6. and what type of pin is used to mark this interest<para/>
/// Cross-mod interests persist even if their mod(s) are disabled, and all listed functionality is handled automatically.
/// </summary>
public abstract class InterestDefinition : ModType, ILocalizedModType
{
	internal static Dictionary<InterestType, InterestDefinition> EnumToDefinition = [];

	/// <summary>
	/// The legacy enumeration value this uses. Should be Custom for all but Reforged's existing in-house interests.
	/// </summary>
	internal virtual InterestType LegacyInterestType => InterestType.Custom;

	/// <summary>
	/// If true, this instance assigns if its enabled through <see cref="IsInterestAt(int, int, HashSet{string})"/>.<br/>
	/// Disable if you use your own logic (such as placing an interest in another generation step).
	/// </summary>
	public virtual bool MappedInterest => true;

	/// <summary>
	/// Which pins are used to pin this interest. Defaults to the "Curiosity" pin (a question mark pin).
	/// </summary>
	public virtual int[] PinTypes => [ModContent.ItemType<PinCuriosity>()];

	public LocalizedText[] DiscoveryText { get; protected set; }

	public string LocalizationCategory => $"Mods.{Mod.Name}.Interests.{Name}";

	public Dictionary<string, InterestDefinition> DefinitionsByModName = [];

	protected sealed override void Register()
	{
		ModTypeLookup<InterestDefinition>.Register(this);

		if (LegacyInterestType != InterestType.Custom)
			EnumToDefinition.Add(LegacyInterestType, this);

		SetStaticDefaults();
	}

	/// <summary>
	/// Registers the dialogue for this interest. Override this to set <see cref="DiscoveryText"/> to something else; otherwise, autoloads 3 variants by default.
	/// </summary>
	public override void SetStaticDefaults() => DiscoveryText = [Language.GetOrRegister(LocalizationCategory + ".0"), Language.GetOrRegister(LocalizationCategory + ".1"), 
		Language.GetOrRegister(LocalizationCategory + ".2")];

	/// <summary>
	/// Whether to mark the position as this interest. Return true to place this interest at the location.
	/// </summary>
	public virtual bool IsInterestAt(int x, int y, HashSet<string> placedInterests) => false;

	/// <summary>
	/// If this interest is obtainable in this world.
	/// </summary>
	public virtual bool IsAvailable() => true;
}
