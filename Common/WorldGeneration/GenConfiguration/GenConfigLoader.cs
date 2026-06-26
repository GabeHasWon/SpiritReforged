using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.ModCompat.EcotoneMapper;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.IO;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace SpiritReforged.Common.WorldGeneration.GenConfiguration;

#nullable enable

public readonly record struct GenConfigParameters(object Min, object Max, object Step);

public record LoadedConfig(object Default, string Name, GenConfigParameters Params, LocalizedText DisplayName, LocalizedText Tip, bool IsSlider, Func<object> Get, Action<object> Set, 
	bool ReverseMinMax, bool IsDenominator, string? PriorityConfig)
{
	public bool Modified = false;
}

internal class GenConfigLoader : ModSystem
{
	public static List<Mod> LoadingMods = [];
	public static List<GenConfigPage> LoadedPages = [];
	public static Dictionary<string, GenConfigPage> PagesByModAndName = [];
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

		if (HasConfiguredMarker(self.Data))
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

	public override void OnWorldLoad()
	{
		if (Main.ActiveWorldFileData is { } data && HasConfiguredMarker(data))
			Configured = true;
	}

	internal static bool HasConfiguredMarker(WorldFileData data) => data.TryGetHeaderData<GenConfigLoader>(out TagCompound tag) && tag.ContainsKey("configured");

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

		int leftOffset = -274;

		if (CrossMod.RussianTranslate.Enabled)
		{
			leftOffset = -334;
		}

		UIPanel panel = new()
		{
			HAlign = 0.5f,
			VAlign = 0.5f,
			Left = StyleDimension.FromPixels(leftOffset),
			Top = StyleDimension.FromPixels(-218),
			Width = StyleDimension.FromPixels(40),
			Height = StyleDimension.FromPixels(40),
			PaddingLeft = 4,
			PaddingTop = 4,
			BackgroundColor = new Color(33, 43, 79) * 0.8f
		};

		self.Append(panel);

		UIImageFramed button = new(ModContent.Request<Texture2D>("SpiritReforged/Common/WorldGeneration/GenConfiguration/ConfigButton"), new Rectangle(0, 0, 32, 32))
		{
			Width = StyleDimension.FromPixels(36),
			Height = StyleDimension.FromPixels(36),
			OverrideSamplerState = SamplerState.PointClamp
		};

		button.OnLeftClick += (_, _) =>
		{
			UIState state = Main.MenuUI.CurrentState;
			Main.MenuUI.SetState(new GenConfigUIState(() => Main.MenuUI.SetState(state)));
			SoundEngine.PlaySound(SoundID.MenuOpen);
		};

		button.OnUpdate += (_) =>
		{
			button.SetFrame(new Rectangle(0, button.ContainsPoint(Main.MouseScreen) ? 34 : 0, 32, 32));
			AddHoverDescription(button, self);
		};

		button.OnMouseOut += (_, _) => RemoveDescription(self);
		GenConfigUIState.AddHoverTicks(button, false);
		panel.Append(button);
	}

	private static void AddHoverDescription(UIElement button, UIWorldCreation self)
	{
		bool hover = button.ContainsPoint(Main.MouseScreen);
		UIText description = EcotoneMapperHooks.GetDescriptionText(self);

		if (hover)
		{
			const string Key = "Mods.SpiritReforged.GenConfigs.UI.";
			description?.SetText(Language.GetTextValue(Key + "HoverDescription"));
		}
	}

	private static void RemoveDescription(UIWorldCreation self)
	{
		UIText description = EcotoneMapperHooks.GetDescriptionText(self); // Resets description text which is set below
		description?.SetText(Language.GetText("UI.WorldDescriptionDefault"));
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
			GenConfigPage configPage = PagesByModAndName[page.Info.CopiedPage!.Mod.Name + "/" + page.Info.CopiedPage!.Info.PageName];
			GetConfigs(ref delay, page.GetType(), page, configPage);
			PagesByType.Add(page.GetType(), configPage);
		}

		delay?.Invoke();
	}

	private static GenConfigPage CreatePage(Type type, IGenerationPage page)
	{
		string pageName = page.Info.PageName;
		string key = $"Mods.{page.Mod.Name}.GenConfigs.Pages.{pageName}.";
		GenConfigPage configPage = new(page.Mod, page.Info, Language.GetOrRegister(key + "Name", () => pageName), Language.GetOrRegister(key + "Description", () => ""), page.Info.Presets.Count);

		if (PagesByModAndName.TryAdd(configPage.FullName, configPage))
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
			configPage = PagesByModAndName[pageName];

		return configPage;
	}

	private static void GetConfigs(ref Action? delay, Type type, IGenerationPage page, GenConfigPage configPage)
	{
		MemberInfo[] members = [.. type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static), 
			.. type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)];

		foreach (var member in members)
		{
			if (member is PropertyInfo prop)
				delay += () => GeneratePropConfig(page, configPage, prop);
			else if (member is FieldInfo field)
				delay += () => GenerateFieldConfig(page, configPage, field);
		}
	}

	/// <summary>
	/// Orders the array according to <see cref="PriorityModifierAttribute"/>.
	/// </summary>
	internal static PriorityQueue<LoadedConfig, double> PrioritizeConfigs(IEnumerable<LoadedConfig> configs)
	{
		PriorityQueue<LoadedConfig, double> orderedConfigs = new();
		Dictionary<string, List<LoadedConfig>> delayedConfigs = [];
		int weight = 0;

		foreach (LoadedConfig info in configs)
		{
			if (info.PriorityConfig is { } prior)
			{
				delayedConfigs.TryAdd(prior, []);
				delayedConfigs[prior].Add(info);
			}
			else
				orderedConfigs.Enqueue(info, weight);

			weight++;
		}

		int inset = 0;
		Action delays = null!;

		foreach (var (config, prio) in orderedConfigs.UnorderedItems)
		{
			if (delayedConfigs.TryGetValue(config.Name, out List<LoadedConfig>? delayed) && delayed is not null)
			{
				double curPrio = prio + 0.01f;

				foreach (LoadedConfig delayedConfig in delayed)
				{
					double delegatePrio = curPrio;
					delays += () => orderedConfigs.Enqueue(delayedConfig, delegatePrio);
					curPrio += 0.01f;
				}
			}

			inset++;
		}

		delays?.Invoke();

		return orderedConfigs;
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
			bool isDenom = field.GetCustomAttribute<DenominatorAttribute>() is { };
			string? prioConfig = field.GetCustomAttribute<PriorityModifierAttribute>() is PriorityModifierAttribute prior ? prior.ParentName : null;
			LoadedConfig config = new(def, field.Name, GenerateParameters(attribute, field.FieldType), text, tip, IsSlider(field), getDelegate, setDelegate, hasReverse, isDenom, prioConfig);
			configPage.ConfigsByName.Add(field.Name, config);

			if (field.FieldType.IsEnum)
				GenerateEnumLocalization(page, field.FieldType);
		}
	}

	private static void GenerateEnumLocalization(IGenerationPage page, Type type)
	{
		string[] names = Enum.GetNames(type);
		string key = $"Mods.{page.Mod.Name}.GenConfigs.Enums.";

		foreach (string name in names)
		{
			Language.GetOrRegister(key + type.Name + "." + name + ".DisplayName", () => name);
			Language.GetOrRegister(key + type.Name + "." + name + ".Tooltip", () => name);
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
			bool rev = prop.GetCustomAttribute<ReverseMinMaxAttribute>() is { };
			bool isDenom = prop.GetCustomAttribute<DenominatorAttribute>() is { };
			string? prioConfig = prop.GetCustomAttribute<PriorityModifierAttribute>() is PriorityModifierAttribute prior ? prior.ParentName : null;
			LoadedConfig config = new(def, prop.Name, GenerateParameters(attribute, getMethod.ReturnType), text, tip, IsSlider(prop), getDelegate, setDelegate, rev, isDenom, prioConfig);
			configPage.ConfigsByName.Add(prop.Name, config);

			if (getMethod.ReturnType.IsEnum)
				GenerateEnumLocalization(page, getMethod.ReturnType);
		}
	}

	private static bool IsSlider(MemberInfo member) => member.GetCustomAttribute<SliderAttribute>() is not null;

	private static void GenerateLocalization(IGenerationPage page, string name, out LocalizedText text, out LocalizedText tip)
	{
		string pageName = page.Info.CopiedPage is { } copy ? copy.Info.PageName : page.Info.PageName;

		text = Language.GetOrRegister($"Mods.{page.Mod.Name}.GenConfigs.Pages.{pageName}.Members.{name}.DisplayName", () => name);
		tip = Language.GetOrRegister($"Mods.{page.Mod.Name}.GenConfigs.Pages.{pageName}.Members.{name}.Tooltip", () => name);
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
					int or GenRange => (object)(int)1,
					short => (object)(short)1,
					long => (object)(long)1,
					float or GenRangeF => (object)(float)1,
					double => (object)(double)1,
					ushort => (object)(ushort)1,
					uint => (object)(uint)1,
					ulong => (object)(ulong)1,
					byte => (object)(byte)1,
					sbyte => (object)(sbyte)1,
					Enum => (object)(int)1,
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
