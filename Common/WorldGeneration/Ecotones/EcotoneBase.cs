using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Ecotones;

/// <summary>
/// Defines the base of an ecotone. This is mostly generation data, alongside metadata for the ecotone selection screen.
/// </summary>
public abstract class EcotoneBase : ILoadable
{
	/// <summary>
	/// Simple wrapper for defining and storing a mod icon texture.
	/// </summary>
	public readonly struct EcotoneIcon(string path)
	{
		public readonly Asset<Texture2D> Icon = ModContent.Request<Texture2D>(path);

		public static EcotoneIcon FromBiome<T>() where T : ModBiome => new(ModContent.GetInstance<T>().BestiaryIcon);
	}

	public readonly record struct ManualPlacementInfo(bool Singular);

	public static readonly List<EcotoneBase> Ecotones = [];

	public EcotoneIcon Icon { get; private set; }

	public void Load(Mod mod)
	{
		Ecotones.Add(this);
		Load();
		Icon = GetIcon();
	}

	protected virtual void Load() { }
	public void Unload() { }
	public abstract void AddTasks(List<GenPass> tasks, List<EcotoneSurfaceMapping.EcotoneEntry> entries);
	protected abstract EcotoneIcon GetIcon();
}
