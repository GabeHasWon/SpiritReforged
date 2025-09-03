using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.Visuals;
using Terraria.Audio;
using Terraria.GameContent.UI;
using Terraria.UI;

namespace SpiritReforged.Content.Forest.Misc;

[FromClassic("AssassinMagazine")]
public class SleightOfHand : EquippableItem
{
	private class Indicator : ILoadable
	{
		public const int Duration = 50;

		public float Progress => (float)_timeLeft / Duration;

		private readonly int _itemType;
		private readonly Color _primaryColor;
		private readonly Vector2 _offset;

		private int _timeLeft;

		#region handling
		private static readonly Asset<Texture2D> PopupTexture = DrawHelpers.RequestLocal(typeof(SleightOfHand), "SleightOfHand_Popup", false);
		public static readonly List<Indicator> Indicators = [];

		public void Load(Mod mod) => On_Main.DrawInterface_14_EntityHealthBars += static (orig, self) =>
		{
			for (int i = Indicators.Count - 1; i >= 0; i--)
			{
				Indicators[i].Draw(Main.spriteBatch);

				if (!Main.gamePaused && --Indicators[i]._timeLeft <= 0)
					Indicators.RemoveAt(i);
			}

			orig(self);
		};

		public void Unload() { }
		#endregion

		public Indicator() { }
		public Indicator(int itemType, Color color, Vector2 offset = default) : base()
		{
			_itemType = itemType;
			_primaryColor = color;
			_offset = offset;
			_timeLeft = Duration;
		}

		private void Draw(SpriteBatch sb)
		{
			Main.instance.LoadItem(_itemType);

			var center = Main.LocalPlayer.Center - new Vector2(0, 50 - Progress * 14) - Main.screenPosition + _offset;
			float rotation = EaseFunction.EaseCubicIn.Ease(Progress) - 0.2f;
			var squashScale = new Vector2(EaseFunction.EaseQuinticOut.Ease(Progress), 1) * (1 + Math.Max(Progress - 0.9f, 0) * 10);

			var back = PopupTexture.Value;
			var item = TextureAssets.Item[_itemType].Value;

			var currentWhite = Color.White;
			var currentSource = item.Frame();
			ItemSlot.DrawItem_GetColorAndScale(new Item(_itemType), 1, ref currentWhite, 20, ref currentSource, out _, out float itemScale);

			DrawHelpers.DrawOutline(sb, back, center, Color.White, (offset) => sb.Draw(back, center + offset, null, _primaryColor.Additive() * Progress, rotation, back.Size() / 2, squashScale, default, 0));

			sb.Draw(back, center, null, Color.Lerp(Color.Gray, Color.White, Progress) * EaseFunction.EaseCubicOut.Ease(Progress), rotation, back.Size() / 2, squashScale, default, 0);
			sb.Draw(item, center, null, Color.White * EaseFunction.EaseCubicOut.Ease(Progress), rotation, item.Size() / 2, squashScale * itemScale, default, 0);

			Texture2D pulse = TextureAssets.Extra[98].Value;
			Texture2D wave = TextureAssets.GlowMask[239].Value;

			sb.Draw(wave, center, null, _primaryColor.Additive() * EaseFunction.EaseCircularIn.Ease(Progress) * 0.5f, MathHelper.PiOver2, wave.Size() / 2, 1f - Progress, default, 0);

			Vector2 pulseScale = new(0.2f, 1 + (1f - Progress));
			sb.Draw(pulse, center, null, _primaryColor.Additive() * EaseFunction.EaseQuinticIn.Ease(Progress), MathHelper.PiOver2, pulse.Size() / 2, pulseScale * 2, default, 0);
			sb.Draw(pulse, center, null, Color.White.Additive() * EaseFunction.EaseQuinticIn.Ease(Progress), MathHelper.PiOver2, pulse.Size() / 2, pulseScale * 1.5f, default, 0);
		}
	}

	public override void Load() => DoubleTapPlayer.OnDoubleTap += CycleAmmo;
	private static void CycleAmmo(Player player, int keyDir)
	{
		if (keyDir != 1 || player.HeldItem.useAmmo == AmmoID.None || !player.HasEquip<SleightOfHand>() && !player.HasItem(ModContent.ItemType<SleightOfHand>()))
			return;

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

			Indicator.Indicators.Clear(); //Remove old indicators
			Indicator.Indicators.Add(new(ammoItems[0].type, ItemRarity.GetColor(ammoItems[0].rare)));
		}
	}

	public override void SetStaticDefaults() => NPCShopHelper.AddEntry(new NPCShopHelper.ConditionalEntry((shop) => shop.NpcType == NPCID.ArmsDealer, new NPCShop.Entry(Type)));
	public override void SetDefaults()
	{
		Item.Size = new(32);
		Item.value = Item.buyPrice(0, 5, 0, 0);
		Item.rare = ItemRarityID.Green;
		Item.accessory = true;
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
}