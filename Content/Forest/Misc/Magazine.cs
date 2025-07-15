using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.Visuals;
using System.Linq;
using Terraria.Audio;

namespace SpiritReforged.Content.Forest.Misc;

[FromClassic("AssassinMagazine")]
public class Magazine : EquippableItem
{
	private static float SwapTime;
	private static int SwapItemType;

	public override void Load()
	{
		DoubleTapPlayer.OnDoubleTap += CycleAmmo;
		On_Main.DrawInterface_14_EntityHealthBars += static (orig, self) =>
		{
			SwapTime = Math.Max(SwapTime - 0.03f, 0);

			DrawIndicator();
			orig(self);
		};
	}

	private static void CycleAmmo(Player player, int keyDir)
	{
		if (keyDir == 1 && player.HeldItem.useAmmo > AmmoID.None && (player.HasEquip<Magazine>() || player.HasItem(ModContent.ItemType<Magazine>())))
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

				SoundEngine.PlaySound(SoundID.DD2_BallistaTowerShot with { Pitch = 1, Volume = 0.7f });
				SwapTo(ammoItems[0].type);
			}
		}
	}

	private static void SwapTo(int type)
	{
		SwapTime = 1;
		SwapItemType = type;
	}

	private static void DrawIndicator()
	{
		if (SwapTime == 0)
			return;

		var sb = Main.spriteBatch;
		var center = Main.LocalPlayer.Center - new Vector2((float)Math.Sin(Math.Max(SwapTime - 0.75f, 0) * 20) * 10, 50) - Main.screenPosition;
		float scale = 1 + Math.Max(SwapTime - 0.9f, 0) * 10;

		var texture = TextureAssets.Item[SwapItemType].Value;
		var outline = TextureColorCache.ColorSolid(texture, Color.White);

		DrawHelpers.DrawOutline(sb, outline, center, Color.White * SwapTime);
		sb.Draw(texture, center, null, Color.White * EaseFunction.EaseQuinticOut.Ease(SwapTime), 0, texture.Size() / 2, scale, default, 0);
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		foreach (TooltipLine line in tooltips)
		{
			if (line.Mod == "Terraria" && line.Name == "Tooltip0")
			{
				string down = !Main.ReversedUpDownArmorSetBonuses ? "UP" : "DOWN";
				line.Text = line.Text.Replace("{0}", down);

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