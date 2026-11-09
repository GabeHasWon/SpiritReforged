namespace SpiritReforged.Common.WorldGeneration.PointOfInterest;

#nullable enable

[Autoload(false)]
internal class CrossmodInterest(string name, Func<int[]> getPins, bool isMapped, Func<int, int, HashSet<Type>, bool>? mapperFunc,
	LocalizedText[]? getDialogue = null, Func<bool>? isAvailable = null) : InterestDefinition
{
	public override int[] PinTypes => GetPinTypes();
	public override bool MappedInterest => IsMapped;

	public override string Name => InternalName;

	private readonly string InternalName = name;
	private readonly Func<int[]> GetPinTypes = getPins;
	private readonly LocalizedText[]? OverrideDialogue = getDialogue;
	private readonly bool IsMapped = isMapped;

	public override bool IsAvailable() => isAvailable?.Invoke() ?? true;

	public override void SetStaticDefaults()
	{
		if (OverrideDialogue is null)
			base.SetStaticDefaults();
		else
			DiscoveryText = OverrideDialogue;
	}

	public override bool IsInterestAt(int x, int y, HashSet<Type> placedInterests) => mapperFunc?.Invoke(x, y, placedInterests) ?? false;
}
