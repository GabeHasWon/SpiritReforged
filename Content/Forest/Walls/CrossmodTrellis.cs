namespace SpiritReforged.Content.Forest.Walls;

#nullable enable

internal class CrossmodTrellis : ModSystem
{
	public readonly record struct CustomVine((int, int)[] ItemToStyle);

	public static readonly Dictionary<int, (int style, int type)> CustomVinesByItem = [];
	public static readonly Dictionary<int, CustomVine> CustomVinesById = [];

	private static Action? _delayedActions;

	public static int Id { get; private set; } = 0;

	public static int Recieve(object[] parameters)
	{
		if (parameters.Length != 4)
			throw new ArgumentException("TrellisVine Mod.Call takes exactly 4 parameters - (Mod mod, Func<(int, int)[]> itemStylePairFunc, string name, string path).");

		if (parameters[0] is not Mod mod)
			throw new ArgumentException("TrellisVine parameter 0 should be a Mod (mod).");

		if (parameters[1] is not Func<(int, int)[]> itemStylePairs)
			throw new ArgumentException("TrellisVine parameter 1 should be an Func<(int, int)[]> (itemStylePairs).");

		if (parameters[2] is not string name)
			throw new ArgumentException("TrellisVine parameter 2 should be a string (name).");

		if (parameters[3] is not string path)
			throw new ArgumentException("TrellisVine parameter 3 should be a string (path).");

		return InternalRecieve(mod, itemStylePairs, name, path);
	}

	private static int InternalRecieve(Mod mod, Func<(int, int)[]> itemStylePairs, string name, string path)
	{
		mod.AddContent(new CustomTrellisVine(Id, name, path));
		int type = mod.Find<ModTile>(name).Type;

		_delayedActions += () =>
		{
			var vine = new CustomVine(itemStylePairs());
			CustomVinesById.Add(Id, vine);

			foreach ((int id, int style) in vine.ItemToStyle)
				CustomVinesByItem.Add(id, (style, type));
		};

		Id++;
		return type;
	}

	public override void PostSetupContent() => _delayedActions?.Invoke();
}
