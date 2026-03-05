using System.Collections.ObjectModel;
using System.Linq;
using Terraria.UI;

namespace SpiritReforged.Common.ItemCommon.Backpacks;

internal partial class BackpackGlobal : GlobalItem
{
	private static Asset<Texture2D> bagIcon, slotFrame;

	public override void Load()
	{
		bagIcon = Mod.Assets.Request<Texture2D>("Common/ItemCommon/Backpacks/BagIcon");
		slotFrame = Mod.Assets.Request<Texture2D>("Common/ItemCommon/Backpacks/SlotFrame");
	}

	public override void PostDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
	{
		if (!item.newAndShiny)
			return;

		var items = (item.ModItem as BackpackItem).items;
		if (items.Any(x => !x.IsAir))
		{
			var source = bagIcon.Frame(1, 2, 0, 0);
			spriteBatch.Draw(bagIcon.Value, position - frame.Size() / 2, source, Color.White, 0, source.Size() * .45f, scale * Main.mouseTextColor / 255f, default, 0);
		}
	}

	public override bool PreDrawTooltip(Item item, ReadOnlyCollection<TooltipLine> lines, ref int x, ref int y)
	{
		const int paddingSize = 28;

		if (item.tooltipContext != ItemSlot.Context.InventoryItem)
			return true;

		var items = (item.ModItem as BackpackItem).items;
		if (items.Any(x => !x.IsAir)) //Draw the backpack contents in the inventory, if any
		{
			int length = items.Length;
			int limit = 0;
			var position = new Vector2(x - 14, y + 5);

			foreach (var line in lines)
			{
				position.Y += FontAssets.MouseText.Value.MeasureString(line.Text).Y; //Position vertically
				limit = Math.Max((int)FontAssets.MouseText.Value.MeasureString(line.Text).X, limit);
			}

			limit /= paddingSize;

			if (Main.SettingsEnabled_OpaqueBoxBehindTooltips)
				Utils.DrawInvBG(Main.spriteBatch, new Rectangle((int)position.X, (int)position.Y, paddingSize * Math.Min(length, limit) + 34, paddingSize * ((length - 1) / limit + 1) + 6), new Color(23, 25, 81, 255) * 0.925f);
			
			var source = bagIcon.Frame(1, 2, 0, 1);

			Main.spriteBatch.Draw(bagIcon.Value, position + new Vector2(17), source, Color.White, 0, source.Size() / 2, .8f, default, 0);

			for (int i = 0; i < length; i++)
			{
				var newPosition = position + new Vector2(paddingSize * (i % limit), paddingSize * (i / limit)) + new Vector2(paddingSize + 17, 17);

				Main.spriteBatch.Draw(slotFrame.Value, newPosition, null, Color.White * .5f, 0, slotFrame.Size() / 2, 1, default, 0);
				ItemSlot.DrawItemIcon(items[i], ItemSlot.Context.ChestItem, Main.spriteBatch, newPosition, .75f, 24, Color.White);
			}
		}

		return true;
	}
}
