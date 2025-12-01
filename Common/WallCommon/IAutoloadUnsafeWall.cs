using SpiritReforged.Content.Desert.Walls;
using System.Linq;
using Terraria.ModLoader.Core;

namespace SpiritReforged.Common.WallCommon;

/// <summary>
/// Automatically generates a clone of a given <see cref="ModWall"/> with the only difference being it's unsafe.
/// </summary>
internal interface IAutoloadUnsafeWall
{
	// These are already defined on ModWalls and shortens the autoloading code a bit.
	public string Name { get; }
	public string Texture { get; }
}

public class AutoloadedUnsafeWall(string name, string texture) : ModWall
{
	public override string Texture => InternalTexture;
	public override string Name => InternalName + "Unsafe";

	private ModWall AssociatedWall => ModContent.GetInstance<SpiritReforgedMod>().Find<ModWall>(InternalName);

	private readonly string InternalName = name;
	private readonly string InternalTexture = texture;

	public override void SetStaticDefaults()
	{
		AssociatedWall.SetStaticDefaults();
		Main.wallHouse[Type] = false;
		Main.wallBlend[Type] = AssociatedWall.Type;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => AssociatedWall.NumDust(i, j, fail, ref num);
}

public class UnsafeWallLoader : ModSystem
{
	public override void Load()
	{
		var types = AssemblyManager.GetLoadableTypes(Mod.Code).Where(x => typeof(IAutoloadUnsafeWall).IsAssignableFrom(x) && !x.IsAbstract && !x.IsInterface);

		foreach (var item in types)
		{
			if (!typeof(ModWall).IsAssignableFrom(item))
				throw new InvalidOperationException($"{item.Name} should be a ModWall, or should have IAutoloadUnsafeWall removed!");

			var instance = Activator.CreateInstance(item) as IAutoloadUnsafeWall;
			Mod.AddContent(new AutoloadedUnsafeWall(instance.Name, instance.Texture));
		}
	}
}

internal static class AutoloadedWallExtensions
{
	public static int GetUnsafe(this IAutoloadUnsafeWall wall, Mod mod) => mod.Find<ModWall>(wall.Name + "Unsafe").Type;

	/// <summary> Attempts to find the autoloaded unsafe wall associated with the given Type. Throws an exception on failure. </summary>
	public static int UnsafeWallType<T>() where T : ModWall
	{
		var mod = SpiritReforgedMod.Instance;
		return mod.Find<ModWall>(ModContent.GetInstance<T>().Name + "Unsafe").Type;
	}
}