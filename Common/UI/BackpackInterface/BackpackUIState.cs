using SpiritReforged.Common.ItemCommon.Backpacks;
using SpiritReforged.Common.UI.Misc;
using SpiritReforged.Common.UI.System;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace SpiritReforged.Common.UI.BackpackInterface;

internal class BackpackUIState : AutoUIState
{
	internal static bool HasPotionSlotMod { get; private set; }

	private BackpackUISlot _functionalSlot;
	private BackpackUISlot _vanitySlot;
	private BackpackUISlot _dyeSlot;

	private Item _lastBackpack;
	private int _lastAdjustY;

	public override void OnInitialize()
	{
		HasPotionSlotMod = ModLoader.HasMod("PotionSlots");
		Width = Height = StyleDimension.Fill;

		_functionalSlot = new BackpackUISlot(false);
		_functionalSlot.Left = new StyleDimension(-186, 1);
		Append(_functionalSlot);

		_vanitySlot = new BackpackUISlot(true);
		_vanitySlot.Left = new StyleDimension(_functionalSlot.Left.Pixels - 48, 1);
		Append(_vanitySlot);

		_dyeSlot = new BackpackUISlot(false, true);
		_dyeSlot.Left = new StyleDimension(_vanitySlot.Left.Pixels - 48, 1);
		Append(_dyeSlot);

		SetVariablePositions();

		On_Main.DrawInventory += TryOpenUI;
	}

	private static void TryOpenUI(On_Main.orig_DrawInventory orig, Main self)
	{
		orig(self);

		if (Main.playerInventory)
			UISystem.SetActive<BackpackUIState>();
	}

	public override void Update(GameTime gameTime)
	{
		if (!Main.playerInventory)
		{
			_lastBackpack = null; //Force the storage list to reload when the UI closes
			SetStorageSlots(true);

			UISystem.SetInactive<BackpackUIState>(); //Close the UI
			return;
		}

		if (Main.LocalPlayer.GetModPlayer<BackpackPlayer>().backpack.ModItem is BackpackItem bp)
		{
			if (_lastBackpack != bp.Item)
				SetStorageSlots(false);

			_lastBackpack = bp.Item;
		}
		else
		{
			if (_lastBackpack != null)
				SetStorageSlots(true);

			_lastBackpack = null;
		}

		int value = UIHelper.GetMapHeight();
		if (value != _lastAdjustY)
			SetVariablePositions();

		_lastAdjustY = value;

		base.Update(gameTime);
	}

	private void SetVariablePositions() => _functionalSlot.Top = _vanitySlot.Top = _dyeSlot.Top = new StyleDimension(UIHelper.GetMapHeight() + 174, 0);

	/// <summary> Adds or removes backpack slots with items according to the currently equipped backpack.<para/>
	/// This is a snapshot, and must be called again if the <see cref="BackpackPlayer.backpack"/> instance has changed.<br/>
	/// In most cases, this is handled automatically by the UI state, but not always. </summary>
	/// <param name="clear"> Whether to remove the backpack storage slots. </param>
	internal void SetStorageSlots(bool clear)
	{
		List<UIElement> removals = [];

		foreach (var item in Children)
			if (item is BasicItemSlot or UIText)
				removals.Add(item);

		foreach (var item in removals)
			RemoveChild(item);

		if (!clear)
		{
			const float spacing = 33.5f;
			int baseX = HasPotionSlotMod ? 609 : 571;

			int xOff = 0, yOff = 0;

			Append(new UIText(Language.GetTextValue("Mods.SpiritReforged.SlotContexts.Backpack"), 0.725f, false)
			{
				Left = new StyleDimension(baseX, 0),
				Top = new StyleDimension(86, 0),
				Width = StyleDimension.FromPixels(32),
				Height = StyleDimension.FromPixels(32),
				TextColor = Color.White * 0.95f,
				ShadowColor = Color.Transparent
			});

			var mPlayer = Main.LocalPlayer.GetModPlayer<BackpackPlayer>();
			var items = (mPlayer.backpack.ModItem as BackpackItem).items;

			for (int i = 0; i < items.Length; ++i) //Add backpack storage slots
			{
				var newSlot = new PackInventorySlot(items, i)
				{
					Left = new StyleDimension(baseX + xOff * spacing, 0),
					Top = new StyleDimension(105 + yOff * spacing, 0),
					Width = StyleDimension.FromPixels(32),
					Height = StyleDimension.FromPixels(32)
				};

				if (++yOff >= 4)
				{
					xOff++;
					yOff = 0;
				}

				Append(newSlot);
			}
		}
	}
}
