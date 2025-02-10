﻿using Terraria.UI;
using Terraria.GameContent.UI.Elements;

namespace SpiritReforged.Common.UI.Misc;

/// <summary> Behaves like <see cref="UIItemSlot"/> but with improved ease of use. </summary>
internal class BasicItemSlot : UIElement
{
	public float Scale { get; private set; }

	private readonly Item[] _items;
	private readonly int _index;
	private readonly int _context;

	public BasicItemSlot(Item item, int context = ItemSlot.Context.ChestItem, float scale = .85f)
	{
		_items = [item];
		_index = 0;
		_context = context;
		Scale = scale;

		Width = Height = new StyleDimension(52 * Scale, 0f);
	}

	public BasicItemSlot(Item[] items, int index, int context = ItemSlot.Context.ChestItem, float scale = .85f)
	{
		_items = items;
		_index = index;
		_context = context;
		Scale = scale;

		Width = Height = new StyleDimension(52 * Scale, 0f);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		base.DrawSelf(spriteBatch);

		float oldScale = Main.inventoryScale;
		Main.inventoryScale = Scale;

		ItemSlot.Draw(spriteBatch, ref _items[_index], _context, GetDimensions().ToRectangle().TopLeft());

		Main.inventoryScale = oldScale;

		if (IsMouseHovering)
			SlotLogic(ref _items[_index]);
	}

	protected virtual void SlotLogic(ref Item item)
	{
		Main.LocalPlayer.mouseInterface = true;
		ItemSlot.Handle(ref item, ItemSlot.Context.InventoryItem); //Don't use _context because it may cause issues in multiplayer due to syncing
	}
}
