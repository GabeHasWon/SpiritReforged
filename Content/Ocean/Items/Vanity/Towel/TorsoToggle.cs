using Terraria.Audio;
using Terraria.GameInput;
using Terraria.UI;

namespace SpiritReforged.Content.Ocean.Items.Vanity.Towel;

/// <summary> Shirt toggle hack for <see cref="BeachTowel"/>. </summary>
public class TorsoToggle : ILoadable
{
	private static Asset<Texture2D> Toggle;
	private static bool HoveringOverToggle;

	public void Load(Mod mod)
	{
		if (!Main.dedServ)
		{
			string texture = ModContent.GetInstance<BeachTowel>().Texture;
			Toggle = ModContent.Request<Texture2D>(texture.Replace("BeachTowel", "TorsoToggle"));
		}

		On_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += DrawBodyToggle;
		On_ItemSlot.OverrideLeftClick += StopLeftClick;
	}

	/// <summary> Prevents the player from picking up this item when toggle is pressed. </summary>
	private static bool StopLeftClick(On_ItemSlot.orig_OverrideLeftClick orig, Item[] inv, int context, int slot)
	{
		if (IsToggleable(context, inv[slot]) && HoveringOverToggle)
			return true; //Skips orig

		return orig(inv, context, slot);
	}

	/// <summary> Draws the toggle button and handles client logic. </summary>
	private static void DrawBodyToggle(On_ItemSlot.orig_Draw_SpriteBatch_ItemArray_int_int_Vector2_Color orig, SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, Color lightColor)
	{
		orig(spriteBatch, inv, context, slot, position, lightColor);

		if (IsToggleable(context, inv[slot]))
			DrawToggle(spriteBatch, position);
	}

	private static void DrawToggle(SpriteBatch spriteBatch, Vector2 position)
	{
		var mPlayer = Main.LocalPlayer.GetModPlayer<BeachTowelPlayer>();
		var visTexture = Toggle.Value;
		var source = visTexture.Frame(2, 1, mPlayer.bodyEquip ? 1 : 0, 0, -2);
		var point = new Point((int)position.X + 42, (int)position.Y + 4);
		var area = new Rectangle(point.X - source.Width / 2, point.Y - source.Height / 2, source.Width, source.Height);

		spriteBatch.Draw(visTexture, area.Center(), source, Color.White * .8f, 0, source.Size() / 2, 1, SpriteEffects.None, 0);

		if (area.Contains(Main.MouseScreen.ToPoint()) && !PlayerInput.IgnoreMouseInterface)
		{
			Main.HoverItem = new Item();
			Main.hoverItemName = Lang.inter[59].Value;

			Main.LocalPlayer.mouseInterface = true;
			HoveringOverToggle = true;

			if (Main.mouseLeft && Main.mouseLeftRelease)
			{
				mPlayer.bodyEquip = !mPlayer.bodyEquip;
				SoundEngine.PlaySound(SoundID.MenuTick);

				if (Main.netMode == NetmodeID.MultiplayerClient)
					new TowelVisibilityData(mPlayer.bodyEquip, (byte)Main.myPlayer).Send();
			}

			Main.HoverItem = new Item();
			Main.hoverItemName = Lang.inter[mPlayer.bodyEquip ? 60 : 59].Value; // visible/hidden
		}
		else
			HoveringOverToggle = false;
	}

	private static bool IsToggleable(int context, Item item) => context == ItemSlot.Context.EquipAccessoryVanity && item.type == ModContent.ItemType<BeachTowel>();

	public void Unload() { }
}