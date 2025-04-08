﻿using Terraria.GameContent.ItemDropRules;
using Terraria.UI;

namespace SpiritReforged.Common.UI.PotCatalogue;

public class CatalogueItemInfo(DropRateInfo info) : CatalogueInfo
{
	private DropRateInfo _dropRateInfo = info;

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		base.DrawSelf(spriteBatch);

		var source = GetDimensions().ToRectangle();
		CatalogueUI.DrawPanel(spriteBatch, source, Color.Black, Color.SlateBlue, 0);

		int type = _dropRateInfo.itemId;
		ItemSlot.DrawItemIcon(new Item(type), 31, spriteBatch, source.Left() + new Vector2(14, 0), 1f, 24f, Color.White);

		Utils.DrawBorderString(spriteBatch, GetFullInfo(), source.Right() - new Vector2(10, 0), Main.MouseTextColorReal, .8f, 1, .5f);

		if (IsMouseHovering)
		{
			Main.HoverItem = new Item(type);
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
			dropRate = Utils.PrettifyPercentDisplay(_dropRateInfo.dropRate, format);

		return stackRange + dropRate;
	}
}