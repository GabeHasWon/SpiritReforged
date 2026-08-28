using Terraria.GameContent.ItemDropRules;
using Terraria.UI;

namespace SpiritReforged.Common.UI.PotCatalogue;

public class CatalogueItemInfo(DropRateInfo info) : CatalogueInfo
{
	private DropRateInfo _dropRateInfo = info;
	private readonly Dictionary<int, Item> _itemCache = [];

	private Item GetItem(int type)
	{
		if (_itemCache.TryGetValue(type, out Item value))
		{
			return value;
		}
		else
		{
			Item item = new(type);
			_itemCache.Add(type, item);

			return item;
		}
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		const int condition_spacing = 8;

		base.DrawSelf(spriteBatch);

		var source = GetDimensions().ToRectangle();
		CatalogueUI.DrawPanel(spriteBatch, source, Color.Black, Color.SlateBlue, 0);

		int type = _dropRateInfo.itemId;
		ItemSlot.DrawItemIcon(GetItem(type), 31, spriteBatch, source.Left() + new Vector2(14, 0), 1f, 24f, Color.White);

		bool hasConditions = _dropRateInfo.conditions is not null;
		if (hasConditions) //Get condition info
		{
			string fullCondition = string.Empty;
			foreach (var c in _dropRateInfo.conditions)
				fullCondition += c.GetConditionDescription() + ", ";

			string trimmedCondition = fullCondition.Remove(fullCondition.Length - 2, 2);
			if (trimmedCondition != string.Empty)
			{
				Utils.DrawBorderString(spriteBatch, $"({trimmedCondition})", source.Right() + new Vector2(-10, condition_spacing), Main.MouseTextColorReal * 0.6f, 0.7f, 1, 0.5f, 50);
			}
			else
			{
				hasConditions = false;
			}
		}

		Utils.DrawBorderString(spriteBatch, GetFullInfo(), source.Right() - new Vector2(10, hasConditions ? condition_spacing : 0), Main.MouseTextColorReal, .8f, 1, .5f);

		if (IsMouseHovering)
		{
			Main.HoverItem = GetItem(type);
			Main.hoverItemName = "icon";
		}
	}

	private string GetFullInfo()
	{
		string stackRange = string.Empty;

		if (_dropRateInfo.stackMin != _dropRateInfo.stackMax)
			stackRange = $"({_dropRateInfo.stackMin}-{_dropRateInfo.stackMax}) ";
		else if (_dropRateInfo.stackMin != 1)
			stackRange = $"({_dropRateInfo.stackMin}) ";

		string dropRate = "100%";
		string format = (_dropRateInfo.dropRate < 0.001) ? "P4" : "P";

		if (_dropRateInfo.dropRate != 1f)
		{
			dropRate = _dropRateInfo.dropRate.ToString(format, Language.ActiveCulture.CultureInfo);

			//In russian, the percent sign is spaced exclusively in official documents
			if (Language.ActiveCulture.Name == "ru-RU")
				dropRate = dropRate.Replace(" %", "%");
		}

		return stackRange + dropRate;
	}
}