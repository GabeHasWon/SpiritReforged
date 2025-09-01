using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.Visuals;
using Terraria.Audio;
using Terraria.UI;

namespace SpiritReforged.Content.Forest.Misc;

[FromClassic("AssassinMagazine")]
public class SleightOfHand : EquippableItem
{
	private static readonly Asset<Texture2D> PopupTexture = DrawHelpers.RequestLocal(typeof(SleightOfHand), "SleightOfHand_Popup", false);

	private static float SwapTime;
	private static int SwapItemType;

	public override void Load()
	{
		DoubleTapPlayer.OnDoubleTap += CycleAmmo;
		On_Main.DrawInterface_14_EntityHealthBars += static (orig, self) =>
		{
			if (!Main.gamePaused)
				SwapTime = Math.Max(SwapTime - 0.03f, 0);

			DrawIndicator();
			orig(self);
		};
	}

	private static void CycleAmmo(Player player, int keyDir)
	{
		if (keyDir == 1 && player.HeldItem.useAmmo > AmmoID.None && (player.HasEquip<SleightOfHand>() || player.HasItem(ModContent.ItemType<SleightOfHand>())))
		{
			var ammoItems = new List<Item>();
			var ammoPos = new List<int>();

			for (int i = Main.InventoryAmmoSlotsStart; i < Main.InventoryAmmoSlotsStart + Main.InventoryAmmoSlotsCount; i++)
			{
				if (!player.inventory[i].IsAir && player.inventory[i].ammo == player.HeldItem.useAmmo)
				{
					ammoItems.Add(player.inventory[i]);
					ammoPos.Add(i);
				}
			}

			if (ammoItems.Count > 1)
			{
				//Shift the top item to the bottom
				var temp = ammoItems[0];
				ammoItems.RemoveAt(0);
				ammoItems.Add(temp);

				//Move the items around accordingly and trigger sync messages
				for (int i = 0; i < ammoItems.Count; i++)
				{
					player.inventory[ammoPos[i]] = ammoItems[i];
					if (Main.netMode == NetmodeID.MultiplayerClient)
						NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, ammoPos[i]);
				}

				//Play sounds
				SoundEngine.PlaySound(SoundID.DD2_BallistaTowerShot with { Pitch = 1, Volume = 0.4f });
				SoundEngine.PlaySound(SoundID.DD2_DarkMageHealImpact with { Pitch = 1, Volume = 0.5f });

				SwapTo(ammoItems[0].type);
			}
		}

		static void SwapTo(int type)
		{
			SwapTime = 1;
			SwapItemType = type;
		}
	}

	private static void DrawIndicator()
	{
		if (SwapTime == 0)
			return;

		Main.instance.LoadItem(SwapItemType);

		var sb = Main.spriteBatch;
		var center = Main.LocalPlayer.Center - new Vector2(0, 50 - SwapTime * 8) - Main.screenPosition;
		float rotation = (EaseFunction.EaseCubicIn.Ease(SwapTime) - 0.2f) * 2;

		var back = PopupTexture.Value;
		var texture = TextureAssets.Item[SwapItemType].Value;
		var outline = TextureColorCache.ColorSolid(texture, Color.White);

		var currentWhite = Color.White;
		var currentSource = texture.Frame();
		ItemSlot.DrawItem_GetColorAndScale(new Item(SwapItemType), 1, ref currentWhite, 20, ref currentSource, out _, out float itemScale);

		var squashScale = new Vector2(EaseFunction.EaseQuinticOut.Ease(SwapTime), 1) * (1 + Math.Max(SwapTime - 0.9f, 0) * 10);

		DrawHelpers.DrawOutline(sb, back, center, Color.White, (offset) => sb.Draw(back, center + offset, null, Color.Red.Additive() * SwapTime, rotation, back.Size() / 2, squashScale, default, 0));

		sb.Draw(back, center, null, Color.White * EaseFunction.EaseCubicOut.Ease(SwapTime), rotation, back.Size() / 2, squashScale, default, 0);
		sb.Draw(texture, center, null, Color.White * EaseFunction.EaseCubicOut.Ease(SwapTime), rotation, texture.Size() / 2, squashScale * itemScale, default, 0);

		Texture2D pulse = TextureAssets.Extra[98].Value;
		Vector2 pulseScale = new(0.2f, 1 + (1f - SwapTime));
		sb.Draw(pulse, center, null, Color.Red.Additive() * EaseFunction.EaseQuinticIn.Ease(SwapTime), MathHelper.PiOver2, pulse.Size() / 2, pulseScale * 2, default, 0);
		sb.Draw(pulse, center, null, Color.White.Additive() * EaseFunction.EaseQuinticIn.Ease(SwapTime), MathHelper.PiOver2, pulse.Size() / 2, pulseScale * 1.5f, default, 0);
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		foreach (TooltipLine line in tooltips)
		{
			if (line.Mod == "Terraria" && line.Name == "Tooltip0")
			{
				string up = Language.GetTextValue(Main.ReversedUpDownArmorSetBonuses ? "Key.DOWN" : "Key.UP");
				line.Text = line.Text.Replace("{0}", up);

				return;
			}
		}
	}

	public override void SetDefaults()
	{
		Item.Size = new(32);
		Item.value = Item.buyPrice(0, 5, 0, 0);
		Item.rare = ItemRarityID.Green;
		Item.accessory = true;
	}
}