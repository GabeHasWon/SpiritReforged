namespace SpiritReforged.Common.WorldGeneration.PointOfInterest;

/// <summary>
/// Defines an "interest" or "discovery" - a naturally placed tile of importance, usually rare and/or low-occurance.<br/>
/// This does a few things:<para/>
/// 1. Allows this to define what counts as this interest (such as matching a tile type),<br/>
/// 2. Marks it as a discovery for the Points of Interest position,<br/>
/// 3. Allows the Cartographer to point it out as a point of interest,<br/>
/// 4. Allows custom dialogue to be said by the Cartographer when pointed out,<br/>
/// 5. Allows any interest position to be retrieved at any time, and whether it was discovered or not<para/>
/// Cross-mod interests persist even if their mod(s) are disabled, and all listed functionality is handled automatically.
/// </summary>
public abstract class InterestDefinition : ModType
{
	internal static Dictionary<InterestType, InterestDefinition> EnumToDefinition = [];

	/// <summary>
	/// The legacy enumeration value this uses. Should be Custom for all but Reforged's existing in-house interests.
	/// </summary>
	internal virtual InterestType LegacyInterestType => InterestType.Custom;

	/// <summary>
	/// If true, this instance assigns if its enabled through <see cref="IsInterestAt(int, int, HashSet{Type})"/>.<br/>
	/// Disable if you use your own logic (such as placing an interest in another generation step).
	/// </summary>
	internal virtual bool MappedInterest => true;

	public Dictionary<string, InterestDefinition> DefinitionsByModName = [];

	protected sealed override void Register()
	{
		ModTypeLookup<InterestDefinition>.Register(this);

		if (LegacyInterestType != InterestType.Custom)
			EnumToDefinition.Add(LegacyInterestType, this);
	}

	public virtual bool IsInterestAt(int x, int y, HashSet<Type> placedInterests) => false;
}
