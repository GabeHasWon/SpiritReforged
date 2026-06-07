using System.Linq;
using System.Reflection;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace SpiritReforged.Common.WorldGeneration.GenConfiguration;

#nullable enable

public readonly record struct GenConfigParameters(object Min, object Max, object Step);

public record LoadedConfig(object Default, string Name, GenConfigParameters Params, LocalizedText DisplayName, LocalizedText Tip, bool IsSlider, Func<object> Get, Action<object> Set, bool ReverseMinMax)
{
	public bool Modified = false;
}

internal class GenConfigLoader : ModSystem
{
	public static List<Mod> LoadingMods = [];
	public static List<GenConfigPage> LoadedPages = [];
	public static Dictionary<string, GenConfigPage> PagesByName = [];
	public static Dictionary<Type, GenConfigPage> PagesByType = [];

	public static GenConfigPage GetPage(Type t) => PagesByType[t];
	public static GenConfigPage GetPage<T>() => GetPage(typeof(T));

	[WorldBound]
	public static bool Configured = false;

	public override void Load()
	{
		On_UIWorldCreation.MakeBackAndCreatebuttons += AddConfigButton;
		On_AWorldListItem.GetIconElement += AddMappingIcon;
	}

	private UIElement AddMappingIcon(On_AWorldListItem.orig_GetIconElement orig, AWorldListItem self)
	{
		UIElement element = orig(self);

		if (HasConfiguredMarker(self))
		{
			element.Append(new UIImage(ModContent.Request<Texture2D>("SpiritReforged/Common/WorldGeneration/GenConfiguration/ConfigIcon")) 
			{ 
				VAlign = 1f, 
				Height = StyleDimension.FromPixels(20),
				Left = StyleDimension.FromPixels(-2)
			});
		}

		return element;
	}

	internal static bool HasConfiguredMarker(AWorldListItem self) => self.Data.TryGetHeaderData<GenConfigLoader>(out TagCompound tag) && tag.ContainsKey("configured");

	public override void SaveWorldHeader(TagCompound tag)
	{
		if (Configured)
			tag.Add("configured", true);
	}

	public override void PreWorldGen()
	{
		Configured = false;

		foreach (GenConfigPage page in LoadedPages)
		{
			foreach (LoadedConfig config in page.ConfigsByName.Values)
			{
				if (config.Modified)
				{
					Configured = true;
					return;
				}	
			}
		}
	}

	private void AddConfigButton(On_UIWorldCreation.orig_MakeBackAndCreatebuttons orig, UIWorldCreation self, UIElement outerContainer)
	{
		orig(self, outerContainer);

		UIPanel panel = new()
		{
			HAlign = 0.5f,
			VAlign = 0.5f,
			Left = StyleDimension.FromPixels(-274),
			Top = StyleDimension.FromPixels(-168),
			Width = StyleDimension.FromPixels(40),
			Height = StyleDimension.FromPixels(40),
			PaddingLeft = 4,
			PaddingTop = 4,
			BackgroundColor = new Color(33, 43, 79) * 0.8f
		};

		self.Append(panel);

		UIImageButton button = new(ModContent.Request<Texture2D>("SpiritReforged/Common/WorldGeneration/GenConfiguration/ConfigButton"))
		{
			Width = StyleDimension.FromPixels(30),
			Height = StyleDimension.FromPixels(30),
			Left = StyleDimension.FromPixels(1),
			Top = StyleDimension.FromPixels(1),
			OverrideSamplerState = SamplerState.PointClamp
		};

		button.OnLeftClick += (_, _) =>
		{
			UIState state = Main.MenuUI.CurrentState;
			Main.MenuUI.SetState(new GenConfigUIState(() => Main.MenuUI.SetState(state)));
		};
		panel.Append(button);
	}

	public override void PostSetupContent()
	{
		LoadingMods.Add(SpiritReforgedMod.Instance);

		Action? delay = null;
		List<IGenerationPage> delayedPages = [];

		foreach (Mod mod in LoadingMods)
		{
			var types = mod.Code.GetTypes();

			foreach (var type in types)
			{
				if (typeof(IGenerationPage).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
				{
					var page = (IGenerationPage)Activator.CreateInstance(type)!;

					if (page.Info.CopiedPage is not null)
					{
						delayedPages.Add(page);
						continue;
					}

					GenConfigPage configPage = CreatePage(type, page);
					GetConfigs(ref delay, type, page, configPage);
				}
			}
		}

		foreach (IGenerationPage page in delayedPages)
		{
			GenConfigPage configPage = PagesByName[page.Info.CopiedPage!.Info.PageName];
			GetConfigs(ref delay, page.GetType(), page, configPage);
			PagesByType.Add(page.GetType(), configPage);
		}

		delay?.Invoke();
	}

	private static GenConfigPage CreatePage(Type type, IGenerationPage page)
	{
		string pageName = page.Info.PageName;
		string key = $"Mods.{page.Mod.Name}.GenConfigs.Pages.{pageName}.";
		GenConfigPage configPage = new(page.Info, Language.GetOrRegister(key + "Name", () => pageName), Language.GetOrRegister(key + "Description", () => ""));

		if (PagesByName.TryAdd(pageName, configPage))
		{
			PagesByType.Add(type, configPage);
			LoadedPages.Add(configPage);

			if (page.Info.Presets is not null)
			{
				foreach (ConfigPreset preset in page.Info.Presets)
				{
					LocalizedText presetName = Language.GetOrRegister(key + "Presets." + preset.Name + ".Name", () => preset.Name);
					LocalizedText presetTip = Language.GetOrRegister(key + "Presets." + preset.Name + ".Tooltip", () => preset.Name);
					configPage.PresetLocalization.Add((presetName, presetTip));
				}
			}
		}
		else
			configPage = PagesByName[pageName];

		return configPage;
	}

	private static void GetConfigs(ref Action? delay, Type type, IGenerationPage page, GenConfigPage configPage)
	{
		var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

		foreach (var prop in props)
			delay += () => GeneratePropConfig(page, configPage, prop);

		var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

		foreach (var field in fields)
			delay += () => GenerateFieldConfig(page, configPage, field);
	}

	private static void GenerateFieldConfig(IGenerationPage page, GenConfigPage configPage, FieldInfo field)
	{
		if (field.GetCustomAttributes().FirstOrDefault(x => x is GenConfigurableAttribute) is GenConfigurableAttribute attribute)
		{
			var getDelegate = new Func<object>(() => field.GetValue(null)!);
			var setDelegate = new Action<object>((val) => field.SetValue(null, val));
			object def = getDelegate();

			GenerateLocalization(page, field.Name, out LocalizedText text, out LocalizedText tip);
			bool hasReverse = field.GetCustomAttribute<ReverseMinMaxAttribute>() is { };
			LoadedConfig config = new(def, field.Name, GenerateParameters(attribute, field.FieldType), text, tip, IsSlider(field), getDelegate, setDelegate, hasReverse);
			configPage.ConfigsByName.Add(field.Name, config);
		}
	}

	private static void GeneratePropConfig(IGenerationPage page, GenConfigPage configPage, PropertyInfo prop)
	{
		if (prop.GetCustomAttributes().FirstOrDefault(x => x is GenConfigurableAttribute) is GenConfigurableAttribute attribute)
		{
			MethodInfo getMethod = prop.GetGetMethod()!;
			var getDelegate = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), getMethod);
			var setDelegate = (Action<object>)Delegate.CreateDelegate(typeof(Action<object>), prop.GetSetMethod()!);
			object def = getDelegate();

			GenerateLocalization(page, prop.Name, out LocalizedText text, out LocalizedText tip);
			bool hasReverse = prop.GetCustomAttribute<ReverseMinMaxAttribute>() is { };
			LoadedConfig config = new(def, prop.Name, GenerateParameters(attribute, getMethod.ReturnType), text, tip, IsSlider(prop), getDelegate, setDelegate, hasReverse);
			configPage.ConfigsByName.Add(prop.Name, config);
		}
	}

	private static bool IsSlider(MemberInfo member) => member.GetCustomAttribute<SliderAttribute>() is not null;

	private static void GenerateLocalization(IGenerationPage page, string name, out LocalizedText text, out LocalizedText tip)
	{
		text = Language.GetOrRegister($"Mods.{page.Mod.Name}.GenConfigs.Members.{name}.DisplayName", () => name);
		tip = Language.GetOrRegister($"Mods.{page.Mod.Name}.GenConfigs.Members.{name}.Tooltip", () => name);
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
				// Weird code used to preserve type. The object cast forces the data type in the first (chronological) cast to be boxed, properly preserving it.
				// This may just be paranoia. There may be better ways to do this.
				// Too bad! - Gabe
				step = instance switch
				{
					int => (object)(int)1,
					short => (object)(short)1,
					long => (object)(long)1,
					float => (object)(float)1,
					double => (object)(double)1,
					ushort => (object)(ushort)1,
					uint => (object)(uint)1,
					ulong => (object)(ulong)1,
					byte => (object)(byte)1,
					sbyte => (object)(sbyte)1,
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
