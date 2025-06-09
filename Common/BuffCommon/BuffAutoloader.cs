using System.Linq;
using System.Reflection;
using Terraria.ModLoader.Core;

namespace SpiritReforged.Common.BuffCommon;

internal class BuffAutoloader : ILoadable
{
	/// <summary> The <see cref="Type"/> that autoloaded this buff to <see cref="ModBuff.Type"/>.
	/// <br/><see cref="GetAutoloadedBuffType"/> should be used for convenience. </summary>
	internal static readonly Dictionary<Type, int> SourceToType = [];

	/// <summary> Gets the type of autoloaded buff associated with <typeparam name="T"/>. Will throw exceptions on failure. </summary>
	public static int GetAutoloadedBuffType<T>() where T : ModType => SourceToType[typeof(T)];

	public void Load(Mod mod)
	{
		var content = AssemblyManager.GetLoadableTypes(mod.Code).Where(x => x.GetCustomAttribute<AutoloadBuffAttribute>() is not null);

		foreach (var t in AssemblyManager.GetLoadableTypes(mod.Code))
		{
			if (t.GetCustomAttribute<AutoloadBuffAttribute>() is AutoloadBuffAttribute attr)
				attr.AddContent(t, mod);
		}
	}

	public void Unload() { }
}

internal class AutoloadedBuff(string fullName) : ModBuff
{
	public const string Suffix = "_Buff";

	private readonly string _name = fullName.Split('.')[^1] + Suffix;
	private readonly string _texture = fullName.Replace('.', '/') + Suffix;

	public string SourceName => Name[..^Suffix.Length];

	public override string Texture => _texture;
	public override string Name => _name;
}

[AttributeUsage(AttributeTargets.Class)]
internal class AutoloadBuffAttribute : Attribute
{
	public virtual void AddContent(Type type, Mod mod)
	{
		var buff = new AutoloadedBuff(type.FullName);
		mod.AddContent(buff);

		BuffAutoloader.SourceToType.Add(type, buff.Type);
	}
}