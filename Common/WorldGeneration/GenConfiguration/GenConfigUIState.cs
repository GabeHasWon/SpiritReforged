using System.Linq;
using System.Numerics;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace SpiritReforged.Common.WorldGeneration.GenConfiguration;

internal class GenConfigUIState(Action returnAction) : UIState
{
	private readonly Action ReturnAction = returnAction;

	UIPanel pagePanel = null;
	UIText pageName = null;

	public override void OnInitialize()
	{
		UIPanel panel = new()
		{
			Width = StyleDimension.FromPixels(500),
			Height = StyleDimension.FromPixels(500),
			HAlign = 0.5f,
			VAlign = 0.5f,
		};
		Append(panel);

		UIButton<string> backButton = new("x")
		{
			Width = StyleDimension.FromPixels(40),
			Height = StyleDimension.FromPixels(40),
		};

		backButton.OnLeftClick += (_, _) => ReturnAction();
		panel.Append(backButton);

		pagePanel?.Remove();
		pageName?.Remove();
		GenConfigPage page = GenConfigurationLoader.PagesByType.Values.First();
		OpenPage(panel, page);
	}

	private void OpenPage(UIPanel backPanel, GenConfigPage page)
	{
		pagePanel = new()
		{
			Width = StyleDimension.Fill,
			Height = StyleDimension.FromPixels(420),
			VAlign = 1
		};

		backPanel.Append(pagePanel);

		backPanel.Append(pageName = new UIText(page.Name, 0.6f, true)
		{
			HAlign = 0.5f, 
			Top = StyleDimension.FromPixels(8)
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

		foreach (LoadedConfig config in page.Configs)
		{
			UIPanel itemPanel = new()
			{
				Width = StyleDimension.Fill,
				Height = StyleDimension.FromPixels(48),
			};
			configList.Add(itemPanel);

			UIText text = new UIText(config.DisplayName)
			{
				Width = StyleDimension.FromPixels(2),
				Height = StyleDimension.Fill,
				HAlign = 0,
			};
			text.OnUpdate += _ => text.SetText(config.DisplayName + ": " + config.Get().ToString());
			itemPanel.Append(text);

			if (config.IsSlider)
			{

			}
			else
			{
				bool isNumber = config.Get() is int or short or long or float or double or ushort or uint or byte or sbyte;

				if (isNumber)
					AddPlusMinus(itemPanel, config);
			}
		}
	}

	private static void AddPlusMinus(UIPanel itemPanel, LoadedConfig config)
	{
		UIButton<string> plus = new("+")
		{
			Width = StyleDimension.FromPixels(40),
			Height = StyleDimension.FromPixels(40),
			HAlign = 1f,
		};

		plus.OnLeftClick += (_, _) =>
		{
			dynamic value = (dynamic)config.Get() + 1;

			if (value > (dynamic)config.Parameters.Max)
				value = config.Parameters.Max;

			config.Set(value);
		};

		itemPanel.Append(plus);

		UIButton<string> minus = new("-")
		{
			Width = StyleDimension.FromPixels(40),
			Height = StyleDimension.FromPixels(40),
			HAlign = 1f,
			Left = StyleDimension.FromPixels(-44)
		};

		minus.OnLeftClick += (_, _) =>
		{
			dynamic value = (dynamic)config.Get() - 1;

			if (value < (dynamic)config.Parameters.Min)
				value = config.Parameters.Min;

			config.Set(value);
		};

		itemPanel.Append(minus);
	}
}
