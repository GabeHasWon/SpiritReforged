using SpiritReforged.Common.Visuals;
using System.Linq;
using System.Reflection;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Chat;

namespace SpiritReforged.Common.WorldGeneration.GenConfiguration;

#nullable enable

internal class GenConfigUIState(Action returnAction) : UIState
{
	private static readonly Asset<Texture2D> Border = DrawHelpers.RequestLocal(typeof(GenConfigUIState), "PageBorder", false);

	private readonly Action ReturnAction = returnAction;

	bool updatePage = false;
	int pageNumber = 0;
	UIElement mainPanel = null!;
	UIPanel pagePanel = null!;
	UIText pageName = null!;
	UIText pageDescription = null!;

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);

		if (updatePage)
		{
			ResetPage(GenConfigLoader.LoadedPages[pageNumber]);
			updatePage = false;
		}
	}

	public override void OnInitialize() => ResetPage(GenConfigLoader.LoadedPages[pageNumber]);

	private void ResetPage(GenConfigPage page)
	{
		const int Padding = 12;

		RemoveAllChildren();

		mainPanel = page.PageInfo.PageBack is { } value ? new UIImage(value.Value) { Color = new Color(160, 160, 160) } : new UIPanel();
		mainPanel.Width = StyleDimension.FromPixels(800);
		mainPanel.Height = StyleDimension.FromPixels(500);
		mainPanel.Top = StyleDimension.FromPixels(20);
		mainPanel.HAlign = 0.5f;
		mainPanel.VAlign = 0.5f;
		mainPanel.SetPadding(Padding);
		Append(mainPanel);

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

		//pagePanel?.Remove();
		//pageName?.Remove();
		//pageDescription?.Remove();
		OpenPage(page);
	}

	private void OpenPage(GenConfigPage page)
	{
		pagePanel = new()
		{
			Width = StyleDimension.Fill,
			Height = StyleDimension.FromPixels(390),
			VAlign = 1
		};

		mainPanel.Append(pagePanel);

		mainPanel.Append(pageName = new UIText(page.DisplayName, 0.7f, true)
		{
			HAlign = 0.5f,
			Top = StyleDimension.FromPixels(8)
		});

		AppendNextPriorButtons(mainPanel, page);

		mainPanel.Append(pageDescription = new UIText(page.Tooltip, 0.45f, true)
		{
			HAlign = 0.5f,
			Top = StyleDimension.FromPixels(52),
			TextColor = new Color(240, 240, 240)
		});

		UIList configList = new()
		{
			Width = StyleDimension.FromPixelsAndPercent(-24, 1),
			Height = StyleDimension.Fill,
		};
		pagePanel.Append(configList);

		UIScrollbar bar = new()
		{
			Width = StyleDimension.FromPixels(20),
			Height = StyleDimension.Fill,
			HAlign = 1f
		};
		pagePanel.Append(bar);
		configList.SetScrollbar(bar);

		foreach (LoadedConfig config in page.ConfigsByName.Values)
		{
			UIPanel itemPanel = new()
			{
				Width = StyleDimension.Fill,
				Height = StyleDimension.FromPixels(60),
			};
			configList.Add(itemPanel);

			UIText text = new UIText(config.DisplayName)
			{
				Width = StyleDimension.FromPixels(2),
				Height = StyleDimension.FromPixels(2),
				VAlign = 0.5f,
				HAlign = 0,
			};

			text.OnUpdate += _ =>
			{
				text.SetText(config.DisplayName + ": " + config.Get().ToString());
				text.TextColor = config.Modified ? new Color(200, 255, 200) : Color.White;
			};

			itemPanel.Append(text);

			bool isNumber = config.Get() is int or short or long or float or double or ushort or uint or byte or sbyte;
			UIElement? slider = null;

			if (config.IsSlider)
				slider = AddSlider(itemPanel, config);
			else
				AddPlusMinus(itemPanel, config, text);

			UIButton<string> resetButton = new("Reset")
			{
				Width = StyleDimension.FromPixels(60),
				Height = StyleDimension.FromPixels(40),
				Left = StyleDimension.FromPixels(0),
				HAlign = 1f,
			};

			MethodInfo? info = slider?.GetType().GetMethod("Reset", BindingFlags.NonPublic | BindingFlags.Instance, [typeof(UIImageButton)]);
			FieldInfo? button = slider?.GetType().GetField("button");

			resetButton.OnLeftClick += (_, _) =>
			{
				config.Set(config.Default);
				config.Modified = false;

				if (info is not null && slider is not null && button is not null)
				{
					info.Invoke(slider, [button.GetValue(slider)]);
				}
			};

			itemPanel.Append(resetButton);
		}
	}

	private void AppendNextPriorButtons(UIElement backPanel, GenConfigPage page)
	{
		float width = ChatManager.GetStringSize(FontAssets.DeathText.Value, page.DisplayName.Value, new(0.7f)).X;
		GenConfigPage prior = GetPriorPage();
		UIButton<string> priorButton = new("Prior: " + prior.DisplayName.Value)
		{
			Width = StyleDimension.FromPixels(140),
			Height = StyleDimension.FromPixels(40),
			HAlign = 1f,
			Left = StyleDimension.FromPixelsAndPercent(-width / 2 - 20, -0.5f)
		};

		priorButton.OnLeftClick += (_, _) =>
		{
			pageNumber--;

			if (pageNumber < 0)
				pageNumber = GenConfigLoader.LoadedPages.Count - 1;

			updatePage = true;
		};

		backPanel.Append(priorButton);

		GenConfigPage next = GetPriorPage();
		UIButton<string> nextButton = new("Next: " + next.DisplayName.Value)
		{
			Width = StyleDimension.FromPixels(140),
			Height = StyleDimension.FromPixels(40),
			HAlign = 0f,
			Left = StyleDimension.FromPixelsAndPercent(width / 2 + 20, 0.5f)
		};

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

	private static UIElement? AddSlider(UIPanel itemPanel, LoadedConfig config)
	{
		dynamic def = config.Default;
		dynamic step = config.Params.Step;
		dynamic min = config.Params.Min;
		dynamic max = config.Params.Max;

		UIElement slider = config.Get() switch
		{
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
		slider.Left = StyleDimension.FromPixels(-44 - 80);
		slider.Top = StyleDimension.FromPixels(12);
		slider.Width = StyleDimension.FromPixels(200);
		slider.Height = StyleDimension.Fill;

		MethodInfo? valueField = slider.GetType()?.GetProperty("Value")?.GetGetMethod();
		FieldInfo? dragging = slider.GetType()?.GetField("_dragging", BindingFlags.NonPublic | BindingFlags.Instance);

		if (valueField is not null)
		{
			slider.OnUpdate += self =>
			{
				if (!config.Get().Equals(def))
					config.Modified = true;

				if (dragging?.GetValue(slider) is true)
					config.Set(valueField.Invoke(slider, [])!);
			};
		}
		else
			return null;

		itemPanel.Append(slider);

		itemPanel.Append(new UIText(config.Params.Max.ToString())
		{
			HAlign = 0f,
			VAlign = 0.5f,
			Left = StyleDimension.FromPixels(584),
			Width = StyleDimension.FromPixels(2),
			Height = StyleDimension.FromPixels(2),
			TextColor = Color.Gray
		});

		itemPanel.Append(new UIText(config.Params.Min.ToString())
		{
			HAlign = 1f,
			VAlign = 0.5f,
			Left = StyleDimension.FromPixels(-334),
			Width = StyleDimension.FromPixels(2),
			Height = StyleDimension.FromPixels(2),
			TextColor = Color.Gray
		});

		return slider;
	}

	private static void AddPlusMinus(UIPanel itemPanel, LoadedConfig config, UIText nameText)
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
			dynamic value = (dynamic)config.Get() + (dynamic)config.Params.Step;

			if (value > (dynamic)config.Params.Max)
				value = config.Params.Max;

			config.Set(value);
			config.Modified = true;
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
			dynamic value = (dynamic)config.Get() - (dynamic)config.Params.Step;

			if (value < (dynamic)config.Params.Min)
				value = config.Params.Min;

			config.Set(value);
			config.Modified = true;
		};

		itemPanel.Append(minus);

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
