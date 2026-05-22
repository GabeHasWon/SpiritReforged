using System.Linq;
using Terraria.GameInput;
using Terraria.UI;

namespace SpiritReforged.Common.UI;

public interface ISlotButton
{
	public void Draw(SpriteBatch spriteBatch, Vector2 center, Item item, bool hoveredOver);
	public bool IsActive(int context, out Rectangle bounds);
}

public sealed class SlotButtonLoader : ILoadable
{
	private static readonly Dictionary<int, ISlotButton[]> ButtonsByItemType = [];
	private static bool HoveringOverToggle;

	public static void RegisterButton(int itemType, ISlotButton value)
	{
		if (ButtonsByItemType.TryGetValue(itemType, out ISlotButton[] buttons))
		{
			ButtonsByItemType[itemType] = buttons.Concat([value]).ToArray();
		}
		else
		{
			ButtonsByItemType.Add(itemType, [value]);
		}
	}

	public void Load(Mod mod)
	{
		On_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += DrawButtonsOver;
		On_ItemSlot.OverrideLeftClick += StopLeftClick;
	}

	/// <summary> Prevents the player from picking up this item when a toggle is clicked. </summary>
	private static bool StopLeftClick(On_ItemSlot.orig_OverrideLeftClick orig, Item[] inv, int context, int slot)
	{
		if (HoveringOverToggle && ButtonsByItemType.ContainsKey(inv[slot].type))
			return true; //Skips orig

		return orig(inv, context, slot);
	}

	private static void DrawButtonsOver(On_ItemSlot.orig_Draw_SpriteBatch_ItemArray_int_int_Vector2_Color orig, SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, Color lightColor)
	{
		orig(spriteBatch, inv, context, slot, position, lightColor);

		if (ButtonsByItemType.TryGetValue(inv[slot].type, out ISlotButton[] buttons))
			DrawButtons(spriteBatch, position, context, inv[slot], buttons);
	}

	private static void DrawButtons(SpriteBatch spriteBatch, Vector2 position, int context, Item item, ISlotButton[] buttons)
	{
		if (PlayerInput.IgnoreMouseInterface)
		{
			HoveringOverToggle = false;
			return;
		}

		foreach (var button in buttons)
		{
			if (!button.IsActive(context, out Rectangle bounds))
			{
				HoveringOverToggle = false;
				continue;
			}

			Rectangle hitbox = new((int)position.X + bounds.X - bounds.Width / 2, (int)position.Y + bounds.Y - bounds.Height / 2, bounds.Width, bounds.Height);
			if (hitbox.Contains(Main.MouseScreen.ToPoint()))
			{
				Main.LocalPlayer.mouseInterface = true;
				HoveringOverToggle = true;
			}
			else
			{
				HoveringOverToggle = false;
			}

			button.Draw(spriteBatch, hitbox.Center(), item, HoveringOverToggle);
		}
	}

	public void Unload() { }
}