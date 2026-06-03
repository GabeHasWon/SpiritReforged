using System.Linq;
using System.Reflection;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ModLoader.Config;
using Terraria.UI;

namespace SpiritReforged.Common.WorldGeneration.GenConfiguration;

#nullable enable

public readonly record struct GenConfigParameters(object Min, object Max, object Step);

public record struct LoadedConfig(GenConfigParameters Parameters, bool Modified, LocalizedText DisplayName, bool IsSlider, Func<object> Get, Action<object> Set);

internal class GenConfigurationLoader : ModSystem
{
	public static List<Mod> LoadingMods = [];
	public static Dictionary<string, GenConfigPage> PagesByName = [];
	public static Dictionary<Type, GenConfigPage> PagesByType = [];

	public static GenConfigPage GetPage<T>() where T : IGenerationPage => PagesByType[typeof(T)];

	public override void Load() => On_UIWorldCreation.MakeBackAndCreatebuttons += AddConfigButton;

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
			Width = StyleDimension.FromPixels(32),
			Height = StyleDimension.FromPixels(32),
			Left = StyleDimension.FromPixels(0),
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

		foreach (Mod mod in LoadingMods)
		{
			var types = mod.Code.GetTypes();

			foreach (var type in types)
			{
				if (typeof(IGenerationPage).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
				{
					var page = (IGenerationPage)Activator.CreateInstance(type)!;
					string key = $"Mods.{page.Mod.Name}.GenConfigs.Pages.{page.PageName}.";
					var configPage = new GenConfigPage(page.PageName, Language.GetOrRegister(key + "Name", () => page.PageName), Language.GetOrRegister(key + "Description", () => ""));
					PagesByName.Add(page.PageName, configPage);
					PagesByType.Add(type, configPage);

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
								LocalizedText text = Language.GetOrRegister($"Mods.{page.Mod.Name}.GenConfigs.Members.{prop.Name}", () => prop.Name);
								bool isSlider = prop.GetCustomAttribute<SliderAttribute>() is not null;
								LoadedConfig config = new(GenerateParameters(attribute, getMethod.ReturnType), false, text, isSlider, getDelegate, setDelegate);
								configPage.Configs.Add(config);
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
								LocalizedText text = Language.GetOrRegister($"Mods.{page.Mod.Name}.GenConfigs.{field.Name}", () => field.Name);
								bool isSlider = field.GetCustomAttribute<SliderAttribute>() is not null;
								LoadedConfig config = new(GenerateParameters(attribute, field.FieldType), false, text, isSlider, getDelegate, setDelegate);
								configPage.Configs.Add(config);
							}
						};
					}
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
