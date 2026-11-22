using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.ModCompat.EcotoneMapper;
using System.Linq;
using System.Reflection;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.IO;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Core;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace SpiritReforged.Common.WorldGeneration.GenConfiguration;

#nullable enable

/// <summary>
/// Controls the basic necessary parameters used for creating a configurable member.
/// </summary>
public readonly record struct GenConfigParameters(object Min, object Max, object Step);

/// <summary>
/// Controls all parameters required for creating a configurable member, 
/// including many optional ones that mimic strong reference attributes (such as <see cref="ReverseMinMaxAttribute"/>).
/// </summary>
public readonly record struct ConfigInfo(GenConfigParameters Parameters, bool ReverseMinMax, bool Slider, bool IsDenominator, string? PriorityConfig);

public record LoadedConfig(object Default, string Name, GenConfigParameters Params, LocalizedText DisplayName, LocalizedText Tip, bool IsSlider, Func<object> Get, Action<object> Set, 
	bool ReverseMinMax, bool IsDenominator, string? PriorityConfig)
{
	public bool Modified = false;
}

public class GenConfigLoader : ModSystem
{
	public static readonly List<Mod> LoadingMods = [];
	public static readonly List<GenConfigPage> LoadedPages = [];
	public static readonly Dictionary<string, GenConfigPage> PagesByModAndName = [];
	public static readonly Dictionary<Type, GenConfigPage> PagesByType = [];

	/// <summary>
	/// Lookup table designed to be called in <see cref="Mod.Load"/> (or other Load methods) to add custom configurable values.<br/>
	/// The way the system works is it creates a wrapper around a member (property with get/set or writeable field) in order to automatically create a configurable element, alongside
	/// the info provided in the attached <see cref="ConfigInfo"/>.<para/>
	/// At minimum, all values in the returned list must be a valid, non-null <see cref="MemberInfo"/>, and a <see cref="ConfigInfo"/> with a non-null 
	/// <see cref="ConfigInfo.Parameters"/>'s <see cref="GenConfigParameters.Min"/> <see cref="GenConfigParameters.Max"/> and <see cref="GenConfigParameters.Step"/>, and the rest may be
	/// left default.<para/>
	/// This is indexed by page name (<see cref="IGenerationPage.Info"/>.Name) and mod name (<see cref="IGenerationPage.Mod"/>.Name), so something like SpiritReforged/Savanna.
	/// </summary>
	public static readonly Dictionary<string, Func<List<(MemberInfo member, ConfigInfo info)>>> CrossmodConfigurables = [];

	/// <summary>
	/// Gets the <see cref="GenConfigPage"/> associated with <paramref name="t"/>. Throws if invalid.
	/// </summary>
	public static GenConfigPage GetPage(Type t) => PagesByType[t];

	/// <summary>
	/// Gets the <see cref="GenConfigPage"/> associated with <typeparamref name="T"/>. Throws if invalid.
	/// </summary>
	public static GenConfigPage GetPage<T>() => GetPage(typeof(T));

	[WorldBound]
	internal static bool Configured = false;

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

		if (CrossMod.RussianLocalizable)
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
			var types = AssemblyManager.GetLoadableTypes(mod.Code);

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
		GenConfigPage configPage = new(page.Mod, page.Info, Language.GetOrRegister(key + "Name", () => pageName), 
			Language.GetOrRegister(key + "Description", () => ""), page.Info.Presets.Count);

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
		if (CrossmodConfigurables.TryGetValue(page.Mod.Name + "/" + page.Info.PageName, out var hook))
		{
			var crossModManualMembers = hook.Invoke();

			foreach (var member in crossModManualMembers)
			{
				ConfigInfo info = member.info;

				if (member.member is PropertyInfo prop)
					delay += () => InternalGenerateProp(page, configPage, prop, info);
				else if (member.member is FieldInfo field)
					delay += () => InternalGenerateField(page, configPage, field, info);
			}
		}

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
			bool hasReverse = field.GetCustomAttribute<ReverseMinMaxAttribute>() is { };
			bool isDenom = field.GetCustomAttribute<DenominatorAttribute>() is { };
			string? prioConfig = field.GetCustomAttribute<PriorityModifierAttribute>() is PriorityModifierAttribute prior ? prior.ParentName : null;
			InternalGenerateField(page, configPage, field, new ConfigInfo(GenerateParameters(attribute, field.FieldType), hasReverse, IsSlider(field), isDenom, prioConfig));
		}
	}

	private static void InternalGenerateField(IGenerationPage page, GenConfigPage configPage, FieldInfo field, ConfigInfo info)
	{
		var getDelegate = new Func<object>(() => field.GetValue(null)!);
		var setDelegate = new Action<object>((val) => field.SetValue(null, val));
		object def = getDelegate();

		GenerateLocalization(page, field.Name, out LocalizedText text, out LocalizedText tip);
		LoadedConfig config = new(def, field.Name, info.Parameters, text, tip, info.Slider, getDelegate, setDelegate, info.ReverseMinMax, info.IsDenominator, info.PriorityConfig);
		configPage.ConfigsByName.Add(field.Name, config);

		if (field.FieldType.IsEnum)
			GenerateEnumLocalization(page, field.FieldType);
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
			bool rev = prop.GetCustomAttribute<ReverseMinMaxAttribute>() is { };
			bool isDenom = prop.GetCustomAttribute<DenominatorAttribute>() is { };
			string? prioConfig = prop.GetCustomAttribute<PriorityModifierAttribute>() is PriorityModifierAttribute prior ? prior.ParentName : null;
			InternalGenerateProp(page, configPage, prop, new ConfigInfo(GenerateParameters(attribute, prop.GetGetMethod()!.ReturnType), rev, IsSlider(prop), isDenom, prioConfig));
		}
	}

	private static void InternalGenerateProp(IGenerationPage page, GenConfigPage configPage, PropertyInfo prop, ConfigInfo info)
	{
		MethodInfo getMethod = prop.GetGetMethod()!;
		MethodInfo setMethod = prop.GetSetMethod()!;

		object getDelegate() => getMethod.Invoke(null, null)!;
		void setDelegate(object input) => setMethod.Invoke(null, [input]);

		object def = getDelegate();

		GenerateLocalization(page, prop.Name, out LocalizedText text, out LocalizedText tip);
		LoadedConfig config = new(def, prop.Name, info.Parameters, text, tip, info.Slider, getDelegate, setDelegate, info.ReverseMinMax, info.IsDenominator, info.PriorityConfig);
		configPage.ConfigsByName.Add(prop.Name, config);

		if (getMethod.ReturnType.IsEnum)
			GenerateEnumLocalization(page, getMethod.ReturnType);
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
