using ReLogic.Graphics;
using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.UI.Elements;
using SpiritReforged.Common.Visuals;
using System.IO;
using System.Reflection;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.IO;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Chat;

namespace SpiritReforged.Common.WorldGeneration.GenConfiguration;

#nullable enable

// This is terrifying code. Good luck!
/// <summary>
/// UI for generation configuration; displays one <see cref="GenConfigPage"/> at a time.
/// </summary>
internal class GenConfigUIState(Action returnAction) : UIState
{
	private static readonly Asset<Texture2D> Border = DrawHelpers.RequestLocal(typeof(GenConfigUIState), "PageBorder", false);
	private static readonly Asset<Texture2D> ButtonBorder = DrawHelpers.RequestLocal(typeof(GenConfigUIState), "ButtonBorder", false);

	private static string PresetsPath => Path.Combine(Main.SavePath, "GenPresets");

	private static bool LoadedAllPresets = false;

	/// <summary>
	/// Used to return to the vanilla world UI when exiting.
	/// </summary>
	private readonly Action ReturnAction = returnAction;

	private static bool _applyingPreset = false;

	bool updatePage = false;
	int pageNumber = 0;
	int pageConfig = -1;
	Action<GenConfigPage, ConfigPreset>? onSelectPreset = null;
	Action? onReset = null;
	Action? onMax = null;
	Action? onMin = null;
	UIButton<string> presetButton = null!;
	UIElement mainPanel = null!;
	UIText warningText = null!;
	int warningTimer = 0;
	string hoverText = "";

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);

		warningText.TextColor = Color.Lerp(Color.OrangeRed, Color.Transparent, 1 - Math.Clamp(warningTimer / 120f, 0, 1));
		warningTimer--;

		if (updatePage)
		{
			ResetPage(GenConfigLoader.LoadedPages[pageNumber]);
			updatePage = false;
		}
	}

	public override void Draw(SpriteBatch spriteBatch)
	{
		base.Draw(spriteBatch);

		if (hoverText != string.Empty)
		{
			DynamicSpriteFont font = FontAssets.MouseText.Value;
			Vector2 size = ChatManager.GetStringSize(font, hoverText, Vector2.One);
			Vector2 position = Main.MouseScreen + new Vector2(0, 20);
			var backRectangle = new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y);
			backRectangle.Inflate(8, 8);

			if (backRectangle.Right > Main.screenWidth)
				backRectangle.X -= backRectangle.Right - Main.screenWidth;

			Utils.DrawInvBG(spriteBatch, backRectangle with { Height = backRectangle.Height - 4 }, new Color(63, 65, 151, 255));
			
			var textPosition = backRectangle.Location.ToVector2() + new Vector2(8);
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, hoverText, textPosition, Color.White, 0f, Vector2.Zero, Vector2.One);
		}

		hoverText = string.Empty;
	}

	public override void OnInitialize()
	{
		if (!LoadedAllPresets)
		{
			LoadedAllPresets = true;

			if (!AssurePresetsPathExists())
			{
				string[] files = Directory.GetFiles(PresetsPath, "*.txt");

				foreach (string loadPath in files)
				{
					TagCompound tag = TagIO.FromFile(loadPath);
					string name = loadPath[(loadPath.LastIndexOf('\\') + 1)..loadPath.LastIndexOf('.')];
					LoadFromTag(null, tag, name);
				}
			}
		}

		ResetPage(GenConfigLoader.LoadedPages[pageNumber]);
	}

	private void ResetPage(GenConfigPage page)
	{
		const int Padding = 12;

		RemoveAllChildren();

		pageConfig = -1;

		mainPanel = page.PageInfo.PageBack is { } value ? new UIImage(value.Value) { Color = new Color(160, 160, 160) } : new UIPanel();
		mainPanel.Width = StyleDimension.FromPixels(800);
		mainPanel.Height = StyleDimension.FromPixels(500);
		mainPanel.Top = StyleDimension.FromPixels(20);
		mainPanel.HAlign = 0.5f;
		mainPanel.VAlign = 0.5f;
		mainPanel.SetPadding(Padding);
		Append(mainPanel);

		warningText = new UIText("")
		{
			HAlign = 1f,
			VAlign = 1f,
			Top = StyleDimension.FromPixels(38),
		};

		mainPanel.Append(warningText);

		mainPanel.Append(new UIImage(Border)
		{
			Left = StyleDimension.FromPixels(-Padding),
			Top = StyleDimension.FromPixels(-Padding)
		});

		UIButton<string> backButton = new("x")
		{
			Width = StyleDimension.FromPixels(40),
			Height = StyleDimension.FromPixels(40),
		};

		backButton.OnLeftClick += (_, _) => ReturnAction();
		mainPanel.Append(backButton);
		OpenPage(page);
	}

	private void OpenPage(GenConfigPage page)
	{
		UIPanel pagePanel = new()
		{
			Width = StyleDimension.Fill,
			Height = StyleDimension.FromPixels(390),
			VAlign = 1
		};

		mainPanel.Append(pagePanel);

		UIText pageName = new(page.DisplayName, 0.7f, true)
		{
			HAlign = 0.5f,
			Top = StyleDimension.FromPixels(8)
		};
		mainPanel.Append(pageName);

		AppendNextPriorButtons(mainPanel, page);

		UIText pageDescription = new(page.Tooltip, 0.45f, true)
		{
			HAlign = 0.5f,
			Top = StyleDimension.FromPixels(52),
			TextColor = new Color(240, 240, 240)
		};
		mainPanel.Append(pageDescription);

		UIList configList = new()
		{
			Width = StyleDimension.FromPixelsAndPercent(-24, 1),
			Height = StyleDimension.FromPixelsAndPercent(-60, 1),
		};
		pagePanel.Append(configList);

		UIScrollbar bar = new()
		{
			Width = StyleDimension.FromPixels(20),
			Height = StyleDimension.FromPixelsAndPercent(-60, 1),
			HAlign = 1f
		};
		pagePanel.Append(bar);
		configList.SetScrollbar(bar);

		AddBottomButtons(page, pagePanel);

		foreach (LoadedConfig config in page.ConfigsByName.Values)
		{
			UIPanel itemPanel = new()
			{
				Width = StyleDimension.Fill,
				Height = StyleDimension.FromPixels(56),
			};

			itemPanel.OnUpdate += _ =>
			{
				if (itemPanel.ContainsPoint(Main.MouseScreen) && configList?.ContainsPoint(Main.MouseScreen) is true)
				{
					hoverText = config.Tip.Value;

					if (config.Get() is Enum en)
					{
						string baseKey = $"Mods.{page.Mod.Name}.GenConfigs.Enums.{en.GetType().Name}.{en}";
						string value = $"\n [c/AAAAAA:{GetEnumName(page, en, "DisplayName")}:] ";
						hoverText += value + GetEnumName(page, en, "Tooltip");
					}
				}
			};
			configList.Add(itemPanel);

			UIText text = new(config.DisplayName)
			{
				Width = StyleDimension.FromPixels(2),
				Height = StyleDimension.FromPixels(2),
				VAlign = 0.5f,
				HAlign = 0,
			};

			text.OnUpdate += _ =>
			{
				object valueBack = config.Get();
				string? value = valueBack.ToString();

				if (valueBack is float f)
					value = f.ToString("#0.##");
				else if (valueBack is double d)
					value = d.ToString("#0.####");
				else if (valueBack is decimal de)
					value = de.ToString("#0.######");

				if (valueBack is bool)
					text.SetText(config.DisplayName + ":");
				else if (valueBack is Enum en)
					text.SetText(config.DisplayName + $": [c/AAAAAA:{GetEnumName(page, en, "DisplayName")}]");
				else
				{
					string valueText = $": [c/AAAAAA:{value}]";

					if (config.IsDenominator)
						valueText = $": [c/9988FF:1 /] [c/AAAAAA:{value}]";

					text.SetText(config.DisplayName + valueText);
				}

				text.TextColor = config.Modified ? new Color(200, 255, 200) : Color.White;
			};

			itemPanel.Append(text);

			// Define this super early so we can get it for the below onEnter delegate
			UIElement? slider = null;
			object defaultValue = config.Get();

			if (defaultValue is not bool)
			{
				if (config.IsSlider)
					slider = AddSlider(page, itemPanel, config);
				else
					AddPlusMinus(page, itemPanel, config, text);
			}

			AddResetButton(config, itemPanel, slider);

			if (defaultValue is bool)
			{
				string tru = Language.GetTextValue("Mods.SpiritReforged.GenConfigs.UI.True");
				string fals = Language.GetTextValue("Mods.SpiritReforged.GenConfigs.UI.False");
				UIButton<string> boolButton = new(fals)
				{
					Width = StyleDimension.FromPixels(100),
					Height = StyleDimension.FromPixels(50),
				};

				boolButton.OnLeftClick += (_, _) =>
				{
					bool reversed = !(bool)config.Get();
					config.Set(reversed);
					boolButton.SetText(reversed ? tru : fals);
					ConfigModified(page, config);

					SoundEngine.PlaySound(SoundID.MenuTick);
				};

				boolButton.OnUpdate += _ => boolButton.Left = StyleDimension.FromPixels(ChatManager.GetStringSize(FontAssets.MouseText.Value, text.Text, Vector2.One).X + 8);

				onMax += () => boolButton.SetText(config.ReverseMinMax ? tru : fals);
				onMin += () => boolButton.SetText(!config.ReverseMinMax ? tru : fals);
				onReset += () => boolButton.SetText((bool)config.Get() ? tru : fals);

				itemPanel.Append(boolButton);
				AddHoverTicks(boolButton);
				continue;
			}

			if (defaultValue is not Enum)
				AddManualInput(config, itemPanel, text, slider, defaultValue);
		}
	}
	
	private static string GetEnumName(GenConfigPage page, Enum en, string postfix) 
		=> Language.GetTextValue($"Mods.{page.Mod.Name}.GenConfigs.Enums.{en.GetType().Name}.{en}." + postfix);

	private static void AddManualInput(LoadedConfig config, UIPanel itemPanel, UIText text, UIElement? slider, object defaultValue)
	{
		bool isNumber = defaultValue is int or short or long or float or double or ushort or uint or byte or sbyte;
		bool isInt = defaultValue is int or short or long or ushort or uint or byte or sbyte;
		InputType inputType = isNumber ? isInt ? InputType.Integer : InputType.Number : InputType.Text;

		UIEditableText input = new(inputType, "...", text =>
		{
			config.Modified = true;
			object obj = defaultValue switch
			{
#pragma warning disable IDE0004 // Unnecessary cast
				// I said this in some other garish code, but the boxing preserves the type for some reason - Gabe
				int => (object)int.Parse(text),
				double => (object)double.Parse(text),
				short => (object)short.Parse(text),
				float => (object)float.Parse(text),
				byte => (object)byte.Parse(text),
				ushort => (object)ushort.Parse(text),
				sbyte => (object)sbyte.Parse(text),
				long => (object)long.Parse(text),
#pragma warning disable IDE0004
				_ => throw new NotSupportedException("Man! I didn't add a switch for this! Do it (EnterText delegate) - gabe")
			};

			if (isNumber)
			{
				if ((dynamic)obj < (dynamic)config.Params.Min)
					obj = config.Params.Min;

				if ((dynamic)obj > (dynamic)config.Params.Max)
					obj = config.Params.Max;
			}

			config.Set(obj);

			MethodInfo? setToFactor = slider?.GetType()?.GetMethod("SetToFactor", BindingFlags.Public | BindingFlags.Instance);

			if (setToFactor is not null)
			{
				GenConfigParameters configParams = config.Params;
				float factor = GenericMath.InverseLerp((dynamic)configParams.Min, (dynamic)configParams.Max, (dynamic)obj);
				setToFactor.Invoke(slider, [factor]);
			}
		})
		{
			Width = StyleDimension.FromPixels(60),
			Height = StyleDimension.FromPixels(60),
			Left = StyleDimension.FromPixels(ChatManager.GetStringSize(FontAssets.MouseText.Value, text.Text, Vector2.One).X + 4),
			Top = StyleDimension.FromPixels(4)
		};

		input.OnUpdate += _ =>
		{
			string measureText = text.Text + (config.IsSlider ? " " : $" ({config.Params.Min}-{config.Params.Max})");
			input.Left = StyleDimension.FromPixels(ChatManager.GetStringSize(FontAssets.MouseText.Value, measureText, Vector2.One).X + 4);
		};

		itemPanel.Append(input);
	}

	private void AddResetButton(LoadedConfig config, UIPanel itemPanel, UIElement? slider)
	{
		UIButton<string> resetButton = new(Language.GetTextValue("Mods.SpiritReforged.GenConfigs.UI.Reset"))
		{
			Width = StyleDimension.FromPixels(60),
			Height = StyleDimension.FromPixels(40),
			Left = StyleDimension.FromPixels(0),
			HAlign = 1f,
		};

		MethodInfo? info = slider?.GetType().GetMethod("Reset", BindingFlags.NonPublic | BindingFlags.Instance);
		MethodInfo? setFactor = slider?.GetType()?.GetMethod("SetToFactor", BindingFlags.Public | BindingFlags.Instance);

		if (slider is not null)
		{
			if (info is not null)
				onReset += () => info.Invoke(slider, []);

			if (setFactor is not null)
			{
				onMin += () => setFactor.Invoke(slider, [(config.ReverseMinMax ? 1 : 0)]);
				onMax += () => setFactor.Invoke(slider, [(config.ReverseMinMax ? 0 : 1)]);
			}
		}

		resetButton.OnLeftClick += (_, _) =>
		{
			config.Set(config.Default);
			config.Modified = false;

			if (info is not null && slider is not null)
			{
				info.Invoke(slider, []);
			}
		};

		itemPanel.Append(resetButton);
		AddHoverTicks(resetButton);
	}

	private void AddBottomButtons(GenConfigPage page, UIPanel pagePanel)
	{
		UIButton<string> setMax = new(Language.GetTextValue("Mods.SpiritReforged.GenConfigs.UI.Max"))
		{
			Width = StyleDimension.FromPixels(80),
			Height = StyleDimension.FromPixels(50),
			HAlign = 0.5f,
			VAlign = 1f,
			Left = StyleDimension.FromPixels(168)
		};

		setMax.OnUpdate += _ =>
		{
			if (setMax.ContainsPoint(Main.MouseScreen))
				hoverText = Language.GetTextValue("Mods.SpiritReforged.GenConfigs.UI.MaxDescription");
		};

		setMax.OnLeftClick += (_, _) =>
		{
			foreach (LoadedConfig config in page.ConfigsByName.Values)
			{
				config.Set(config.ReverseMinMax ? config.Params.Min : config.Params.Max);
				ConfigModified(page, config);
			}

			onMax?.Invoke();
		};
		pagePanel.Append(setMax);
		AddHoverTicks(setMax);

		UIButton<string> setMin = new(Language.GetTextValue("Mods.SpiritReforged.GenConfigs.UI.Min"))
		{
			Width = StyleDimension.FromPixels(80),
			Height = StyleDimension.FromPixels(50),
			HAlign = 0.5f,
			VAlign = 1f,
			Left = StyleDimension.FromPixels(-168)
		};

		setMin.OnUpdate += _ =>
		{
			if (setMin.ContainsPoint(Main.MouseScreen))
				hoverText = Language.GetTextValue("Mods.SpiritReforged.GenConfigs.UI.MinDescription");
		};

		setMin.OnLeftClick += (_, _) =>
		{
			foreach (LoadedConfig config in page.ConfigsByName.Values)
			{
				config.Set(config.ReverseMinMax ? config.Params.Max : config.Params.Min);
				ConfigModified(page, config);
			}

			onMin?.Invoke();
		};
		pagePanel.Append(setMin);
		AddHoverTicks(setMin);

		presetButton = new(GetConfigPresetDisplay(page))
		{
			Width = StyleDimension.FromPixels(234),
			Height = StyleDimension.FromPixels(50),
			HAlign = 0.5f,
			VAlign = 1f,
		};

		presetButton.OnLeftClick += (_, _) =>
		{
			if (page.PageInfo.Presets is null or { Count: 0 })
				return;

			pageConfig++;

			if (pageConfig >= page.PageInfo.Presets.Count)
				pageConfig = 0;

			ApplyCurrentPreset(page);
		};

		presetButton.OnUpdate += _ =>
		{
			if (pageConfig != -1 && presetButton.ContainsPoint(Main.MouseScreen))
				hoverText = pageConfig >= page.BuiltInPresets ? Language.GetTextValue("Mods.SpiritReforged.GenConfigs.UI.CustomPresetTooltip") 
					: page.PresetLocalization[pageConfig].Tooltip.Value;

			if (pageConfig == -1)
				presetButton.SetText(GetConfigPresetDisplay(page));
		};

		pagePanel.Append(presetButton);
		AddHoverTicks(presetButton);

		UIButton<string> resetButton = new(Language.GetTextValue("Mods.SpiritReforged.GenConfigs.UI.ResetAll"))
		{
			Width = StyleDimension.FromPixels(80),
			Height = StyleDimension.FromPixels(50),
			HAlign = 0,
			VAlign = 1f,
		};

		resetButton.OnLeftClick += (_, _) =>
		{
			foreach (var config in page.ConfigsByName.Values)
			{
				config.Set(config.Default);
				config.Modified = false;
			}

			onReset?.Invoke();
			ResetPreset(page);

			SoundEngine.PlaySound(SoundID.MenuTick);
		};

		pagePanel.Append(resetButton);
		AddHoverTicks(resetButton);

		UIImageFramed saveButton = new(DrawHelpers.RequestLocal(GetType(), "NewButton", false), new Rectangle(0, 0, 44, 44))
		{
			Width = StyleDimension.FromPixels(44),
			Height = StyleDimension.FromPixels(44),
			HAlign = 1f,
			VAlign = 1
		};

		saveButton.OnLeftClick += (_, _) => SaveConfig(page);

		saveButton.OnUpdate += _ =>
		{
			bool canSave = !DefaultConfig(page);
			saveButton.Color = !canSave ? Color.Gray : Color.White;
			bool hover = canSave && saveButton.ContainsPoint(Main.MouseScreen);
			saveButton.SetFrame(new Rectangle(0, hover ? 46 : 0, 44, 44));

			if (hover)
				hoverText = Language.GetTextValue(DefaultConfig(page) ? "Mods.SpiritReforged.GenConfigs.UI.CantSave" : "Mods.SpiritReforged.GenConfigs.UI.Create");
		};

		pagePanel.Append(saveButton);

		UIImageFramed loadButton = new(DrawHelpers.RequestLocal(GetType(), "LoadButton", false), new Rectangle(0, 0, 44, 44))
		{
			Width = StyleDimension.FromPixels(44),
			Height = StyleDimension.FromPixels(44),
			HAlign = 1f,
			VAlign = 1,
			Left = StyleDimension.FromPixels(-48)
		};

		loadButton.OnLeftClick += (_, _) => LoadConfig(page);

		loadButton.OnUpdate += _ =>
		{
			bool hover = loadButton.ContainsPoint(Main.MouseScreen);
			loadButton.SetFrame(new Rectangle(0, hover ? 50 : 0, 44, 44));

			if (hover)
				hoverText = Language.GetTextValue("Mods.SpiritReforged.GenConfigs.UI.Load");
		};

		pagePanel.Append(loadButton);
	}

	private void ApplyCurrentPreset(GenConfigPage page)
	{
		_applyingPreset = true;

		ConfigPreset configPreset = page.PageInfo.Presets[pageConfig];
		configPreset.Apply(page);
		presetButton.SetText(GetConfigPresetDisplay(page));
		onSelectPreset?.Invoke(page, configPreset);

		_applyingPreset = false;
	}

	private void LoadConfig(GenConfigPage page)
	{
		AssurePresetsPathExists();
		var result = nativefiledialog.NFD_OpenDialog("txt", PresetsPath, out string loadPath);

		if (result == nativefiledialog.nfdresult_t.NFD_OKAY)
		{
			TagCompound tag = TagIO.FromFile(loadPath);
			string name = loadPath[(loadPath.LastIndexOf('\\') + 1)..loadPath.LastIndexOf('.')];

			if (!LoadFromTag(page, tag, name))
				return;

			pageConfig = page.PageInfo.Presets.Count - 1;
			ApplyCurrentPreset(page);
		}
	}

	private static bool AssurePresetsPathExists()
	{
		if (!Directory.Exists(PresetsPath))
		{
			Directory.CreateDirectory(PresetsPath);
			return true;
		}

		return false;
	}

	private bool LoadFromTag(GenConfigPage? page, TagCompound tag, string configName)
	{
		string name = tag.GetString("pageName");
		string[] paths = name.Split('/');

		// Get page if it's not passed in
		page ??= GenConfigLoader.PagesByModAndName[paths[0] + "/" + paths[1]];

		if (paths[0] != page.Mod.Name || paths[1] != page.PageInfo.PageName)
		{
			warningTimer = 300;
			string actualName = GenConfigLoader.PagesByModAndName[paths[0] + "/" + paths[1]].DisplayName.Value;
			warningText.SetText(Language.GetTextValue("Mods.SpiritReforged.GenConfigs.UI.FailedToLoad", actualName));
			warningText.Recalculate();
			return false; // Add notice
		}

		List<IndividualPreset> presets = [];
		TagCompound presetTag = tag.GetCompound("presets");

		foreach (var config in page.ConfigsByName.Values)
		{
			if (presetTag.TryGet(config.Name, out object val))
			{
				object value;

				if (config.Get() is Enum en)
					value = Enum.Parse(en.GetType(), val.ToString()!);
				else
					value = Convert.ChangeType(val, config.Get().GetType());

				presets.Add(new IndividualPreset(config.Name, value));
			}
		}

		ConfigPreset preset = new(configName, presets);
		page.PageInfo.Presets.Add(preset);
		return true;
	}

	private static void SaveConfig(GenConfigPage page)
	{
		if (DefaultConfig(page))
			return;

		AssurePresetsPathExists();
		var result = nativefiledialog.NFD_SaveDialog("txt", PresetsPath, out string savePath);

		if (result == nativefiledialog.nfdresult_t.NFD_OKAY)
		{
			TagCompound tag = CreateTag(page);
			TagIO.ToFile(tag, savePath.EndsWith(".txt") ? savePath : savePath + ".txt", true);
		}
	}

	private static TagCompound CreateTag(GenConfigPage page)
	{
		TagCompound tag = [];
		TagCompound presets = [];
		tag.Add("pageName", page.Mod.Name + "/" + page.PageInfo.PageName);

		foreach (LoadedConfig config in page.ConfigsByName.Values)
		{
			object value = config.Get();

			if (value is Enum en)
			{
				Type type = en.GetType().GetEnumUnderlyingType();

				// Ah. Hello again. This is bad. Oh well! - Gabe
				if (type == typeof(int))
					value = (object)Convert.ToInt32(en);
				else if (type == typeof(short))
					value = (object)Convert.ToInt16(en);
				else if (type == typeof(byte))
					value = (object)Convert.ToByte(en);
				else if (type == typeof(ushort))
					value = (object)Convert.ToUInt16(en);
				else if (type == typeof(sbyte))
					value = (object)Convert.ToSByte(en);
				else if (type == typeof(float))
					value = (object)Convert.ToSingle(en);
				else if (type == typeof(double))
					value = (object)Convert.ToDouble(en);
			}

			presets.Add(config.Name, value);
		}

		tag.Add("presets", presets);
		return tag;
	}

	public static bool DefaultConfig(GenConfigPage page)
	{
		foreach (LoadedConfig config in page.ConfigsByName.Values)
			if (config.Modified)
				return false;

		return true;
	}

	private string GetConfigPresetDisplay(GenConfigPage page)
	{
		const string Key = "Mods.SpiritReforged.GenConfigs.UI.";

		if (page.PageInfo.Presets is null or { Count: 0 })
			return Language.GetTextValue(Key + "NoPresets");

		if (pageConfig >= page.BuiltInPresets)
			return "[i:75] [c/AAAAFF:" + page.PageInfo.Presets[pageConfig].Name + "]";

		string noneText = Language.GetTextValue(Key + "None") + $" ({Language.GetTextValue(Key + "Total", page.PageInfo.Presets.Count)})";
		return Language.GetTextValue(Key + "Preset") + " " + (pageConfig == -1 ? noneText : page.PresetLocalization[pageConfig].Name.Value);
	}

	private void AppendNextPriorButtons(UIElement backPanel, GenConfigPage page)
	{
		float width = ChatManager.GetStringSize(FontAssets.DeathText.Value, page.DisplayName.Value, new(0.7f)).X;
		GenConfigPage prior = GetPriorPage();
		string priorText = Language.GetTextValue("Mods.SpiritReforged.GenConfigs.UI.Prior");
		UIElement priorButton = prior.PageInfo.PageButton is null ? new UIButton<string>(priorText + " " + prior.DisplayName.Value) : new UIImage(prior.PageInfo.PageButton)
		{
			Width = StyleDimension.FromPixels(140),
			Height = StyleDimension.FromPixels(40),
			HAlign = 1f,
			Left = StyleDimension.FromPixelsAndPercent(-width / 2 - 20, -0.5f)
		};

		if (priorButton is UIImage priorImage)
		{
			priorImage.OnUpdate += _ => priorImage.Color = priorImage.ContainsPoint(Main.MouseScreen) ? Color.Gray : Color.White;
			priorButton.Append(new UIImage(ButtonBorder));

			string buttonText = priorText + " " + prior.DisplayName.Value;
			float textWidth = ChatManager.GetStringSize(FontAssets.ItemStack.Value, buttonText, Vector2.One).X;
			UIText text = new(buttonText, Math.Min(1, 114 / textWidth))
			{
				Width = StyleDimension.FromPixels(3),
				Height = StyleDimension.FromPixels(6),
				HAlign = 0.5f,
				VAlign = 0.5f,
				DynamicallyScaleDownToWidth = true, // This doesn't work for some reason?
			};

			priorButton.Append(text);
		}

		priorButton.OnLeftClick += (_, _) =>
		{
			pageNumber--;

			if (pageNumber < 0)
				pageNumber = GenConfigLoader.LoadedPages.Count - 1;

			updatePage = true;
		};

		backPanel.Append(priorButton);

		GenConfigPage next = GetNextPage();
		string nextText = Language.GetTextValue("Mods.SpiritReforged.GenConfigs.UI.Next");
		UIElement nextButton = next.PageInfo.PageButton is null ? new UIButton<string>(nextText + " " + next.DisplayName.Value) : new UIImage(next.PageInfo.PageButton)
		{
			Width = StyleDimension.FromPixels(140),
			Height = StyleDimension.FromPixels(40),
			HAlign = 0f,
			Left = StyleDimension.FromPixelsAndPercent(width / 2 + 20, 0.5f)
		};

		if (nextButton is UIImage nextImage)
		{
			nextImage.OnUpdate += _ => nextImage.Color = nextImage.ContainsPoint(Main.MouseScreen) ? Color.Gray : Color.White;
			nextButton.Append(new UIImage(ButtonBorder));

			string buttonText = nextText + " " + next.DisplayName.Value;
			float textWidth = ChatManager.GetStringSize(FontAssets.ItemStack.Value, buttonText, Vector2.One).X;
			UIText text = new(buttonText, Math.Min(1, 114 / textWidth))
			{
				Width = StyleDimension.Fill,
				Height = StyleDimension.FromPixels(0),
				HAlign = 0.5f,
				VAlign = 0.5f,
				DynamicallyScaleDownToWidth = true
			};

			nextButton.Append(text);
		}

		nextButton.OnLeftClick += (_, _) =>
		{
			pageNumber++;

			if (pageNumber >= GenConfigLoader.LoadedPages.Count)
				pageNumber = 0;

			updatePage = true;
		};

		backPanel.Append(nextButton);
	}

	private GenConfigPage GetPriorPage()
	{
		int current = pageNumber - 1;

		if (current < 0)
			current = GenConfigLoader.LoadedPages.Count - 1;

		return GenConfigLoader.LoadedPages[current];
	}

	private GenConfigPage GetNextPage()
	{
		int current = pageNumber + 1;

		if (current >= GenConfigLoader.LoadedPages.Count)
			current = 0;

		return GenConfigLoader.LoadedPages[current];
	}

	private UIElement? AddSlider(GenConfigPage page, UIPanel itemPanel, LoadedConfig config)
	{
		dynamic def = config.Default;
		dynamic step = config.Params.Step;
		dynamic min = config.Params.Min;
		dynamic max = config.Params.Max;

		UIElement slider = config.Get() switch
		{
			Enum => new UISlider<int>((int)def, (int)1, (int)min, (int)max, Color.CornflowerBlue),
			int => new UISlider<int>(def, step, min, max, Color.CornflowerBlue),
			double => new UISlider<double>(def, step, min, max, Color.CornflowerBlue),
			short => new UISlider<short>(def, step, min, max, Color.CornflowerBlue),
			byte => new UISlider<byte>(def, step, min, max, Color.CornflowerBlue),
			float => new UISlider<float>(def, step, min, max, Color.CornflowerBlue),
			ushort => new UISlider<ushort>(def, step, min, max, Color.CornflowerBlue),
			uint => new UISlider<uint>(def, step, min, max, Color.CornflowerBlue),
			_ => throw new NotSupportedException("I didn't write a type case for this. Write one!")
		};

		slider.HAlign = 1f;
		slider.Left = StyleDimension.FromPixels(-44 - 70);
		slider.Top = StyleDimension.FromPixels(12);
		slider.Width = StyleDimension.FromPixels(200);
		slider.Height = StyleDimension.Fill;

		if (def is Enum)
			slider.Left = StyleDimension.FromPixels(-180);

		MethodInfo? valueField = slider.GetType()?.GetProperty("Value")?.GetGetMethod();
		FieldInfo? dragging = slider.GetType()?.GetField("_dragging", BindingFlags.NonPublic | BindingFlags.Instance);
		MethodInfo? setToFactor = slider.GetType()?.GetMethod("SetToFactor", BindingFlags.Public | BindingFlags.Instance);

		if (setToFactor is not null)
		{
			onSelectPreset += (page, preset) =>
			{
				foreach (var indiv in preset.Presets) 
				{
					if (config.Name == indiv.Name)
					{
						GenConfigParameters configParams = config.Params;
						dynamic minimum = (dynamic)configParams.Min;
						dynamic maximum = (dynamic)configParams.Max;
						dynamic value = (dynamic)indiv.Value;

						if (minimum is Enum)
						{
							minimum = (long)minimum;
							maximum = (long)minimum;
							value = (long)minimum;
						}

						float factor = GenericMath.InverseLerp(minimum, maximum, value);
						setToFactor.Invoke(slider, [factor]);
						break;
					}
				}
			};

			dynamic current = (dynamic)config.Get();

			if (current is Enum)
				setToFactor.Invoke(slider, [GenericMath.InverseLerp((int)(dynamic)config.Params.Min, (int)(dynamic)config.Params.Max, (int)current)]);
			else
				setToFactor.Invoke(slider, [GenericMath.InverseLerp((dynamic)config.Params.Min, (dynamic)config.Params.Max, current)]);
		}

		if (valueField is not null)
		{
			slider.OnUpdate += self =>
			{
				if (dragging?.GetValue(slider) is true)
				{
					object newValue = valueField.Invoke(slider, [])!;

					if (config.Get() is Enum val)
					{
						var enumValue = Enum.Parse(val.GetType(), ((dynamic)newValue).ToString());
						config.Set(enumValue);
					}
					else
						config.Set(newValue);

					ConfigModified(page, config);
				}
			};
		}
		else
			return null;

		itemPanel.Append(slider);

		slider.Append(new UIText(config.Params.Max is Enum enMax ? GetEnumName(page, enMax, "DisplayName") : config.Params.Max.ToString())
		{
			HAlign = 0f,
			VAlign = 0,
			Left = StyleDimension.FromPixelsAndPercent(8, 1),
			Top = StyleDimension.FromPixels(-2),
			Width = StyleDimension.FromPixels(ChatManager.GetStringSize(FontAssets.ItemStack.Value, config.Params.Max.ToString(), Vector2.One).X),
			Height = StyleDimension.FromPixels(2),
			TextColor = Color.Gray
		});

		slider.Append(new UIText(config.Params.Min is Enum enMin ? GetEnumName(page, enMin, "DisplayName") : config.Params.Min.ToString())
		{
			HAlign = 1f,
			VAlign = 0,
			Left = StyleDimension.FromPixelsAndPercent(-8, -1),
			Top = StyleDimension.FromPixels(-2),
			Width = StyleDimension.FromPixels(2),
			Height = StyleDimension.FromPixels(2),
			TextColor = Color.Gray
		});

		return slider;
	}

	private void ConfigModified(GenConfigPage page, LoadedConfig config)
	{
		config.Modified = true;

		if (!_applyingPreset && pageConfig != -1)
			ResetPreset(page);
	}

	private void ResetPreset(GenConfigPage page)
	{
		pageConfig = -1;
		presetButton.SetText(GetConfigPresetDisplay(page));
	}

	private void AddPlusMinus(GenConfigPage page, UIPanel itemPanel, LoadedConfig config, UIText nameText)
	{
		UIButton<string> plus = new("+")
		{
			Width = StyleDimension.FromPixels(40),
			Height = StyleDimension.FromPixels(40),
			Left = StyleDimension.FromPixels(-64),
			HAlign = 1f,
		};

		plus.OnLeftClick += (_, _) =>
		{
			dynamic curValue = (dynamic)config.Get();
			dynamic value;

			if (curValue is Enum)
			{
				GetEnumValueArray(curValue, out Array values, out int index);

				index++;

				if (index >= values.Length)
					index = 0;

				value = values.GetValue(index)!;
			}
			else
				value = curValue + (dynamic)config.Params.Step;

			if (value > (dynamic)config.Params.Max)
				value = config.Params.Max;

			config.Set(value);
			ConfigModified(page, config);
		};

		itemPanel.Append(plus);

		UIButton<string> minus = new("-")
		{
			Width = StyleDimension.FromPixels(40),
			Height = StyleDimension.FromPixels(40),
			HAlign = 1f,
			Left = StyleDimension.FromPixels(-108)
		};

		minus.OnLeftClick += (_, _) =>
		{
			dynamic curValue = (dynamic)config.Get();
			dynamic value;

			if (curValue is Enum)
			{
				GetEnumValueArray(curValue, out Array values, out int index);

				index--;

				if (index < 0)
					index = values.Length - 1;

				value = values.GetValue(index)!;
			}
			else
				value = (dynamic)config.Get() - (dynamic)config.Params.Step;

			if (value < (dynamic)config.Params.Min)
				value = config.Params.Min;

			config.Set(value);
			ConfigModified(page, config);

		};

		itemPanel.Append(minus);

		if (config.Get() is not Enum)
		{
			UIText minMax = new($"({config.Params.Min}-{config.Params.Max})", 0.8f)
			{
				TextColor = new Color(180, 180, 180),
				Left = StyleDimension.FromPixels(ChatManager.GetStringSize(FontAssets.MouseText.Value, nameText.Text, Vector2.One).X + 6),
				VAlign = 0.5f
			};

			minMax.OnUpdate += (self) => self.Left = StyleDimension.FromPixels(ChatManager.GetStringSize(FontAssets.MouseText.Value, nameText.Text, Vector2.One).X + 6);
			itemPanel.Append(minMax);
		}
	}

	/// <summary>
	/// Retrieves the array of enums and the index of the given <paramref name="curValue"/> in the array, so it can be incremented/decremented.
	/// </summary>
	private static void GetEnumValueArray(dynamic curValue, out Array values, out int index)
	{
		values = Enum.GetValues(curValue.GetType());
		index = 0;

		for (int i = 0; i < values.Length; ++i)
		{
			if (values.GetValue(i)!.Equals(curValue))
			{
				index = i;
				break;
			}
		}
	}

	public static void AddHoverTicks(UIElement element, bool hasOut = true)
	{
		element.OnMouseOver += (_, _) => SoundEngine.PlaySound(SoundID.MenuTick);

		if (hasOut)
			element.OnMouseOut += (_, _) => SoundEngine.PlaySound(SoundID.MenuTick);
	}
}
