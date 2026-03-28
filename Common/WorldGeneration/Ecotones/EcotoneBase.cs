using Terraria.WorldBuilding;

namespace SpiritReforged.Common.WorldGeneration.Ecotones;

public abstract class EcotoneBase : ILoadable
{
	public static readonly List<EcotoneBase> Ecotones = [];

	public void Load(Mod mod)
	{
		Ecotones.Add(this);
		Load();
	}

	protected virtual void Load() { }
	public void Unload() { }
	public abstract void AddTasks(List<GenPass> tasks, List<EcotoneSurfaceMapping.EcotoneEntry> entries);
}
