using SpiritReforged.Common.ItemCommon.Backpacks;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.UI.Misc;
using SpiritReforged.Common.UI.System;
using System.IO;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace SpiritReforged.Common.UI.BackpackInterface;

internal class BackpackUIState : AutoUIState
{
	internal class BackpackPickupPacket(bool enabled, short player = -1) : PacketData
	{
		readonly bool Enabled = enabled;
		readonly short Player = player;

		public BackpackPickupPacket() : this(false, -1)
		{
		}

		public override void OnSend(ModPacket modPacket)
		{
			if (Player != -1)
				modPacket.Write((byte)Player);

			modPacket.Write(Enabled);
		}

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			int player = Main.dedServ ? whoAmI : reader.ReadByte();
			bool enabled = reader.ReadBoolean();

			if (Main.dedServ)
				new BackpackPickupPacket(enabled, (short)whoAmI).Send();

			Main.player[player].GetModPlayer<BackpackPlayer>().packPickup = enabled;
		}
	}

	internal static bool HasPotionSlotMod { get; private set; }

	private BackpackUISlot _functionalSlot;
	private BackpackUISlot _vanitySlot;
	private BackpackUISlot _dyeSlot;
	private UIImageFramed _pickupToggle;

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

	private void MakePickupIcon(Vector2 position)
	{
		_pickupToggle?.Remove();

		_pickupToggle = new UIImageFramed(ModContent.Request<Texture2D>("SpiritReforged/Common/UI/BackpackInterface/BackpackPickupIcon"), new(0, 0, 20, 24))
		{
			Width = StyleDimension.FromPixels(20),
			Height = StyleDimension.FromPixels(24),
			Left = new StyleDimension((int)position.X, 0),
			Top = new StyleDimension((int)position.Y, 0),
		};

		_pickupToggle.OnUpdate += _ =>
		{
			bool hover = _pickupToggle.ContainsPoint(Main.MouseScreen);

			if (hover)
			{
				Main.LocalPlayer.mouseInterface = true;
				Main.LocalPlayer.cursorItemIconEnabled = false;
				Main.LocalPlayer.cursorItemIconID = -1;
			}

			Rectangle frame = new(Main.LocalPlayer.GetModPlayer<BackpackPlayer>().packPickup ? 0 : 22, hover ? 26 : 0, 20, 24);
			_pickupToggle.SetFrame(frame);
		};

		_pickupToggle.OnLeftClick += (_, _) =>
		{
			ref bool pickup = ref Main.LocalPlayer.GetModPlayer<BackpackPlayer>().packPickup;
			pickup = !pickup;

			if (Main.netMode == NetmodeID.MultiplayerClient)
				new BackpackPickupPacket(pickup).Send();
		};

		Append(_pickupToggle);
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

	private void SetVariablePositions()
	{
		var baseY = new StyleDimension(UIHelper.GetMapHeight() + 174, 0);
		_functionalSlot.Top = _vanitySlot.Top = _dyeSlot.Top = baseY;
	}

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

			float baseY = 86;
			float textScale = 0.725f;

			if (Language.ActiveCulture.Name == "ru-RU")
			{
				textScale = 0.475f;
				baseY = 90.5f;

				//Adjust for custom Russian font
				if (CrossMod.RussianTranslate.Enabled)
				{
					textScale = 0.525f;
					baseY = 89;
				}
			}

			Append(new UIText(Language.GetTextValue("Mods.SpiritReforged.SlotContexts.Backpack"), textScale, false)
			{
				Left = new StyleDimension(baseX, 0),
				Top = new StyleDimension(baseY, 0),
				Width = StyleDimension.FromPixels(32),
				Height = StyleDimension.FromPixels(32),
				TextColor = Color.White * 0.95f,
				ShadowColor = Color.Transparent
			});

			var mPlayer = Main.LocalPlayer.GetModPlayer<BackpackPlayer>();
			var backpack = mPlayer.backpack.ModItem as BackpackItem;
			var items = backpack.Items;

			for (int i = 0; i < items.Length + 1; ++i) //Add backpack storage slots
			{
				Vector2 position = new(baseX + xOff * spacing, 105 + yOff * spacing);

				if (i == items.Length)
					MakePickupIcon(position);
				else
					Append(backpack.SetupSlot(i, position));

				if (++yOff >= 4)
				{
					xOff++;
					yOff = 0;
				}
			}
		}
	}

	protected override void DrawChildren(SpriteBatch spriteBatch)
	{
		foreach (UIElement element in Elements)
		{
			if (element == _pickupToggle)
			{
				element.Draw(spriteBatch);

				if (element.ContainsPoint(Main.MouseScreen))
				{
					string key = Main.LocalPlayer.GetModPlayer<BackpackPlayer>().packPickup ? "BackpackPickupEnabled" : "BackpackPickupDisabled";
					UICommon.TooltipMouseText(Language.GetTextValue("Mods.SpiritReforged." + key));
				}
			}
			else
				element.Draw(spriteBatch);
		}
	}
}