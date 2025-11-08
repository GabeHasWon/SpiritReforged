using SpiritReforged.Common.ItemCommon.Backpacks;
using System.Diagnostics;
using Terraria.GameInput;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace SpiritReforged.Common.UI.BackpackInterface;

internal class BackpackUIControllerSupport : ILoadable
{
	/// <summary>
	/// Static ID for the backpack IDs.
	/// </summary>
	public const int BackpackIdStart = 16000;

	/// <summary>
	/// Static ID for the backpack <see cref="UILinkPage"/>.
	/// </summary>
	public const int BackpackPageIndex = 16000;

	/// <summary>
	/// Helper ID for the vanilla ammo <see cref="UILinkPage"/>.
	/// </summary>
	public const int AmmoPageIndex = 2;

	/// <summary>
	/// Inclusive lower bound for the ammo slots.
	/// </summary>
	public const int AmmoSlotMin = 54;

	/// <summary>
	/// Exclusive upper bound for the ammo slot.
	/// </summary>
	public const int AmmoSlotMax = 58;

	public void Load(Mod mod) => GeneratePage(0);

	/// <summary>
	/// Regenerates the list nodes if needed. Call this every time the backpack UI is updated.
	/// </summary>
	internal static void GeneratePage(int slotCount)
	{
		UILinkPage backpackPage;

		if (!UILinkPointNavigator.Pages.TryGetValue(BackpackPageIndex, out UILinkPage value))
		{
			backpackPage = new();

			backpackPage.OnSpecialInteracts += () => PlayerInput.BuildCommand(Lang.misc[56].Value, false, PlayerInput.ProfileGamepadUI.KeyStatus["Inventory"])
				+ PlayerInput.BuildCommand(Lang.misc[64].Value, true, PlayerInput.ProfileGamepadUI.KeyStatus["HotbarMinus"], PlayerInput.ProfileGamepadUI.KeyStatus["HotbarPlus"]);

			// Page to inventory
			backpackPage.PageOnLeft = 0;

			// Page to equipment/armor
			backpackPage.PageOnRight = 8;
			UILinkPointNavigator.RegisterPage(backpackPage, BackpackPageIndex, false);
		}
		else
			backpackPage = value;

		// Ammo page index points to itself if backpack isn't active
		UILinkPointNavigator.Pages[AmmoPageIndex].PageOnRight = slotCount != 0 ? BackpackPageIndex : AmmoPageIndex;

		for (int i = AmmoSlotMin; i < AmmoSlotMax; ++i)
		{
			int adj = i - AmmoSlotMin;
			UILinkPointNavigator.Points[i].Right = slotCount != 0 ? BackpackIdStart + adj : (adj + 1) * 10;
		}
		
		// Do nothing if there are no slots
		if (slotCount == 0)
			return;

		backpackPage.DefaultPoint = BackpackPageIndex;

		static string DetermineSpecialInteract() => ItemSlot.GetGamepadInstructions(Main.LocalPlayer.inventory, 2, UILinkPointNavigator.CurrentPoint);

		// Generate nodes
		for (int l = BackpackIdStart; l <= BackpackIdStart + slotCount; l++)
		{
			if (backpackPage.LinkMap.ContainsKey(l))
				continue;

			UILinkPoint link = new UILinkPoint(l, enabled: true, -3, -4, l - 1, l + 1);
			link.OnSpecialInteracts += () => DetermineBackpackSlotInstruction(Main.LocalPlayer, l - BackpackIdStart);
			backpackPage.LinkMap.Add(l, link);
		}

		// Add link points & map left edge to ammo slots
		for (int i = 0; i < Math.Min(4, slotCount); ++i)
			backpackPage.LinkMap[BackpackIdStart + i].Left = 54 + i;

		// Top/bottom map to ???
		backpackPage.LinkMap[BackpackIdStart].Up = -1;
		backpackPage.LinkMap[BackpackIdStart + Math.Min(3, slotCount)].Down = -2;

		// Dynamically point to either crafting or the shop depending on state
		backpackPage.UpdateEvent += delegate
		{
			bool noChestOrShop = Main.LocalPlayer.chest == -1 && Main.npcShop == 0;
			backpackPage.LinkMap[BackpackIdStart].Up = noChestOrShop ? 302 : 504;
			backpackPage.LinkMap[BackpackIdStart + Math.Min(3, slotCount)].Down = noChestOrShop ? 302 : 500;
		};

		foreach (KeyValuePair<int, UILinkPoint> points in backpackPage.LinkMap)
			UILinkPointNavigator.Points.TryAdd(points.Key, points.Value);
	}

	internal static string DetermineBackpackSlotInstruction(Player player, int slot)
	{
		BackpackPlayer plr = player.GetModPlayer<BackpackPlayer>();
		
		if (plr.backpack.ModItem is not BackpackItem backpack)
			return "";

		var items = backpack.items;

		if (slot >= items.Length)
			return "";

		if (Main.mouseItem.type > ItemID.None)
			return !items[slot].IsAir ? Lang.misc[66].Value : Lang.misc[65].Value;

		return !items[slot].IsAir ? Lang.misc[65].Value : "";
	}

	/// <summary>
	/// Adds the link positions for the backpack slots.
	/// </summary>
	/// <param name="children"></param>
	internal static void SetLinkNodes(IEnumerable<UIElement> children)
	{
		if (UILinkPointNavigator.CurrentPage != BackpackPageIndex)
			return;

		int id = BackpackIdStart;

		foreach (var child in children)
		{
			if (child is not PackInventorySlot packSlot)
				continue;

			Vector2 slotPos = packSlot.GetDimensions().Center();
			UILinkPointNavigator.SetPosition(id, slotPos + new Vector2(20f) * Main.inventoryScale);
			packSlot.Selected = UILinkPointNavigator.CurrentPoint == id;
			id++;
		}
	}

	public void Unload()
	{
		UILinkPointNavigator.Pages[AmmoPageIndex].PageOnRight = AmmoPageIndex;
		UILinkPointNavigator.Pages.Remove(BackpackPageIndex);
	}
}
