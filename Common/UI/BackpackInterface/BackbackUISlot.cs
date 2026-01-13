using SpiritReforged.Common.ItemCommon.Backpacks;
using System.Linq;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.UI;

namespace SpiritReforged.Common.UI.BackpackInterface;

public class BackpackUISlot : UIElement
{
	public const int Context = ItemSlot.Context.ChestItem;
	public const float Scale = 0.85f;

	private static Asset<Texture2D> icon;

	private readonly bool _isVanity;
	private readonly bool _isDye;

	public BackpackUISlot(bool isVanity, bool isDye = false)
	{
		_isVanity = isVanity;
		_isDye = isDye;

		Width = Height = new StyleDimension(52 * Scale, 0f);
	}

	public override void OnInitialize() => icon = ModContent.Request<Texture2D>("SpiritReforged/Common/UI/BackpackInterface/BackpackIcon");

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		var mPlayer = Main.LocalPlayer.GetModPlayer<BackpackPlayer>();
		var item = _isDye ? mPlayer.packDye : (_isVanity ? mPlayer.vanityBackpack : mPlayer.backpack); //Bind the player's backpack item

		if (Main.EquipPage != 2 || item is null)
			return;

		base.DrawSelf(spriteBatch);

		float oldScale = Main.inventoryScale;
		Main.inventoryScale = Scale;

		ItemSlot.Draw(spriteBatch, ref item, Context, GetDimensions().ToRectangle().TopLeft());

		if (item.IsAir) //Draw slot icons when empty
		{
			Texture2D texture;
			Rectangle source;

			if (_isVanity || _isDye)
			{
				texture = TextureAssets.Extra[ExtrasID.EquipIcons].Value;
				source = texture.Frame(3, 6, _isDye ? 1 : 2, 0, -2, -2);
			}
			else
			{
				texture = icon.Value;
				source = texture.Frame();
			}

			spriteBatch.Draw(texture, GetDimensions().Center(), source, Color.White * .35f, 0, source.Size() / 2, Main.inventoryScale, SpriteEffects.None, 0);
		}

		if (!DrawVisibility(spriteBatch))
			HandleItemSlotLogic(ref item);

		Main.inventoryScale = oldScale;

		if (_isDye)
			mPlayer.packDye = item;
		else if (_isVanity) //Release the results
			mPlayer.vanityBackpack = item;
		else
			mPlayer.backpack = item;
	}

	private void HandleItemSlotLogic(ref Item item)
	{
		if (!IsMouseHovering)
			return;

		Main.LocalPlayer.mouseInterface = true;
		ItemSlot.OverrideHover(ref item, Context);
		ItemSlot.MouseHover(ref item, Context);

		if (Main.mouseLeft && Main.mouseLeftRelease && CanClickItem(item, _isVanity, _isDye))
		{
			ItemSlot.LeftClick(ref item, _isDye ? ItemSlot.Context.EquipMiscDye : ItemSlot.Context.InventoryItem); //Don't use Context because it causes issues in multiplayer due to syncing
			ItemSlot.RightClick(ref item, Context);
		}

		if (item.IsAir)
		{
			Main.mouseText = true;
			Main.hoverItemName = _isDye ? Lang.inter[57].Value : Language.GetTextValue("Mods.SpiritReforged.SlotContexts.Backpack");
		}
	}

	/// <param name="currentItem"> The item currently in the slot. </param>
	/// <param name="vanity"> Whether this slot is a vanity slot. </param>
	internal static bool CanClickItem(Item currentItem, bool vanity = false, bool isDye = false)
	{
		var plr = Main.LocalPlayer;

		if (isDye)
		{
			return plr.HeldItem.dye > 0 || Main.mouseItem.IsAir;
		}
		else if (vanity)
		{
			if (currentItem.IsAir)
				return plr.HeldItem.ModItem is BackpackItem vanityPack && !vanityPack.items.Any(x => !x.IsAir);
		}
		else if (!currentItem.IsAir && currentItem.ModItem is BackpackItem backpack && backpack.items.Any(x => !x.IsAir))
		{
			if (currentItem.TryGetGlobalItem(out BackpackGlobal anim))
				anim.StartAnimation();

			return false;
		}

		return plr.HeldItem.ModItem is BackpackItem || plr.HeldItem.IsAir || Main.mouseItem.IsAir;
	}

	/// <summary> Draws the toggleable visibility icon associated with this equip slot. </summary>
	/// <param name="spriteBatch"> The SpriteBatch to draw on. </param>
	/// <returns> Whether an interaction has occured. </returns>
	private bool DrawVisibility(SpriteBatch spriteBatch)
	{
		if (_isVanity || _isDye)
			return false;

		var mPlayer = Main.LocalPlayer.GetModPlayer<BackpackPlayer>();
		var visTexture = (mPlayer.packVisible ? TextureAssets.InventoryTickOn : TextureAssets.InventoryTickOff).Value;

		var point = new Point((int)(GetDimensions().X + 34), (int)GetDimensions().Y - 2);
		var area = new Rectangle(point.X, point.Y, visTexture.Width, visTexture.Height);
		
		spriteBatch.Draw(visTexture, area.Center(), null, Color.White * .8f, 0, visTexture.Size() / 2, 1, SpriteEffects.None, 0);

		if (area.Contains(Main.MouseScreen.ToPoint()) && !PlayerInput.IgnoreMouseInterface)
		{
			Main.HoverItem = new Item();
			Main.hoverItemName = Lang.inter[59].Value;

			Main.LocalPlayer.mouseInterface = true;
			if (Main.mouseLeft && Main.mouseLeftRelease)
			{
				mPlayer.packVisible = !mPlayer.packVisible;
				SoundEngine.PlaySound(SoundID.MenuTick);

				if (Main.netMode == NetmodeID.MultiplayerClient)
					new BackpackPlayerData(mPlayer.packVisible, (byte)Main.myPlayer).Send();
			}

			Main.HoverItem = new Item();
			Main.hoverItemName = Lang.inter[mPlayer.packVisible ? 59 : 60].Value;

			return true;
		}

		return false;
	}
}
