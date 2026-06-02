using System.Linq;
using System.Reflection;

namespace SpiritReforged.Common.WorldGeneration.GenConfiguration;

#nullable enable

internal class GenConfigurationLoader : ModSystem
{
	public record struct LoadedConfig(GenConfigParameters Parameters, bool Modified, Func<object> Get, Action<object> Set);

	public static List<Mod> LoadingMods = [];
	public static Dictionary<string, GenConfigPage> PagesByName = [];
	public static Dictionary<Type, GenConfigPage> PagesByType = [];

	public static GenConfigPage GetPage<T>() where T : IGenerationPage => PagesByType[typeof(T)];

	public override void PostSetupContent()
	{
		LoadingMods.Add(SpiritReforgedMod.Instance);

		Action? delay = null;

		foreach (Mod mod in LoadingMods)
		{
			var types = mod.Code.GetTypes();

			foreach (var type in types)
			{
				if (typeof(IGenerationPage).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
				{
					var page = (IGenerationPage)Activator.CreateInstance(type)!;
					var configPage = new GenConfigPage(page.PageName);
					PagesByName.Add(page.PageName, configPage);
					PagesByType.Add(type, configPage);
				}

				var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

				foreach (var prop in props)
				{
					delay += () =>
					{
						if (prop.GetCustomAttributes().FirstOrDefault(x => x is GenConfigurableAttribute) is GenConfigurableAttribute attribute)
						{
							MethodInfo getMethod = prop.GetGetMethod()!;
							var getDelegate = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), getMethod);
							var setDelegate = (Action<object>)Delegate.CreateDelegate(typeof(Action<object>), prop.GetSetMethod()!);
							LoadedConfig config = new(GenerateParameters(attribute, getMethod.ReturnType), false, getDelegate, setDelegate);
						}
					};
				}

				var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

				foreach (var field in fields)
				{
					delay += () =>
					{
						if (field.GetCustomAttributes().FirstOrDefault(x => x is GenConfigurableAttribute) is GenConfigurableAttribute attribute)
						{
							var getDelegate = new Func<object>(() => field.GetValue(null)!);
							var setDelegate = new Action<object>((val) => field.SetValue(null, val));
							LoadedConfig config = new(GenerateParameters(attribute, field.FieldType), false, getDelegate, setDelegate);
						}
					};
				}
			}
		}

		delay?.Invoke();
	}

	private static GenConfigParameters GenerateParameters(GenConfigurableAttribute attribute, Type type)
	{
		object step = attribute.Step!;

		if (step is null)
		{
			object? instance = Activator.CreateInstance(type);

			if (instance is not null)
			{
#pragma warning disable IDE0004 // Unnecessary cast

				// Weird code used to preserve type.
				// This may just be paranoia. There may be better ways to do this.
				// Too bad! - Gabe
				step = instance switch
				{
					int => 1,
					short => (short)1,
					long => (long)1,
					float => (float)1,
					double => (double)1,
					ushort => (ushort)1,
					uint => (uint)1,
					ulong => (ulong)1,
					byte => (byte)1,
					sbyte => (sbyte)1,
					_ => throw new NotSupportedException($"Type {type.Name} not supported.")
				};
#pragma warning restore IDE0004 // Unnecessary cast

			}
			else
				throw new NotSupportedException($"Type {type.Name} not supported.");
		}

		return new GenConfigParameters(attribute.Min, attribute.Max, step);
	}
}
