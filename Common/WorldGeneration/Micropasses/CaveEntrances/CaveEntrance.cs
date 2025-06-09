namespace SpiritReforged.Common.WorldGeneration.Micropasses.CaveEntrances;

public enum CaveEntranceType : byte
{
	Vanilla = 0,
	Karst,
	Canyon,
	Count //Must always be last
}

internal abstract class CaveEntrance : ILoadable
{
	public static readonly Dictionary<CaveEntranceType, CaveEntrance> EntranceByType = [];

	/// <summary>
	/// The type of cave entrance this is associated with. This makes it easier to "register" this as a new entrance type, instead of manually adding it.
	/// </summary>
	public abstract CaveEntranceType Type { get; }

	public void Load(Mod mod) => EntranceByType.Add(Type, this);

	public void Unload() { }

	public abstract void Generate(int x, int y);

	/// <summary>
	/// Allows you to modify the opening created for the "mountain" in the "Mountain Caves" step.<br/>
	/// The X and Y are modifyable for ease of use; returning false will stop the original method from running entirely. This means x, y are ignored past this call.<br/>
	/// <paramref name="isOpening"/> is used for determining if this is from the <see cref="WorldGen.CaveOpenater(int, int)"/> detour or 
	/// the <see cref="WorldGen.Cavinator(int, int, int)"/> detour, as you may want different logic for both.<br/>
	/// For clarity; CaveOpenator creates a small opening to the cave (the short horizontal space), then the Cavinator makes the rest of the cave.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="isOpening"></param>
	/// <returns></returns>
	public abstract bool ModifyOpening(ref int x, ref int y, bool isOpening);
}
