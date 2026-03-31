using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.UI;
using SpiritReforged.Common.Visuals;
using System.IO;
using Terraria.Audio;
using Terraria.UI;

namespace SpiritReforged.Content.Ocean.Items.Vanity;

[AutoloadEquip(EquipType.HandsOn)]
public class BeachTowel : ModItem
{
	private sealed class ShirtButton : ISlotButton
	{
		public static readonly Asset<Texture2D> Texture = DrawHelpers.RequestLocal<ShirtButton>("BeachTowelToggle", false);

		public void Draw(SpriteBatch spriteBatch, Vector2 center, Item item, bool hoveredOver)
		{
			var modPlayer = Main.LocalPlayer.GetModPlayer<BeachTowelPlayer>();
			Texture2D texture = Texture.Value;
			Rectangle source = texture.Frame(2, 1, modPlayer.bodyEquip ? 1 : 0, 0, -2);

			spriteBatch.Draw(texture, center, source, Color.White * .8f, 0, source.Size() / 2, 1, SpriteEffects.None, 0);

			if (hoveredOver)
			{
				if (Main.mouseLeft && Main.mouseLeftRelease)
				{
					modPlayer.bodyEquip = !modPlayer.bodyEquip;
					SoundEngine.PlaySound(SoundID.MenuTick);

					if (Main.netMode == NetmodeID.MultiplayerClient)
						new TowelVisibilityData(modPlayer.bodyEquip, (byte)Main.myPlayer).Send();
				}

				Main.HoverItem = new Item();
				Main.hoverItemName = Lang.inter[modPlayer.bodyEquip ? 60 : 59].Value; // visible/hidden
			}
		}

		public bool IsActive(int context, out Rectangle bounds)
		{
			bounds = new(42, 4, 14, 14);
			return context is ItemSlot.Context.EquipAccessoryVanity;
		}
	}

	internal static int Slot { get; private set; }

	public override void Load()
	{
		EquipLoader.AddEquipTexture(Mod, Texture + "_Body", EquipType.Body, name: nameof(BeachTowel));
		On_Player.PlayerFrame += PostPlayerFrame;
	}

	private static void PostPlayerFrame(On_Player.orig_PlayerFrame orig, Player self)
	{
		orig(self);

		var mod = SpiritReforgedMod.Instance;

		if (self.handon == Slot && self.GetModPlayer<BeachTowelPlayer>().bodyEquip)
			self.body = EquipLoader.GetEquipSlot(mod, nameof(BeachTowel), EquipType.Body);
	}

	public override void SetStaticDefaults()
	{
		Slot = EquipLoader.GetEquipSlot(Mod, nameof(BeachTowel), EquipType.HandsOn);
		ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<BikiniTop>();

		SlotButtonLoader.RegisterButton(Type, new ShirtButton());
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 26;
		Item.value = Item.buyPrice(0, 2, 0, 0);
		Item.rare = ItemRarityID.White;
		Item.accessory = true;
		Item.vanity = true;
	}
}

internal class BeachTowelPlayer : ModPlayer
{
	/// <summary> Whether the player has opted to be shirtless. </summary>
	public bool bodyEquip;

	public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) => new TowelVisibilityData(bodyEquip, (byte)Player.whoAmI).Send();
}

/// <summary> Syncs <see cref="BeachTowel"/> shirt visibility when updated from the local client. </summary>
internal class TowelVisibilityData : PacketData
{
	private readonly bool _visibility;
	private readonly byte _playerIndex;

	public TowelVisibilityData() { }
	public TowelVisibilityData(bool value, byte playerIndex)
	{
		_visibility = value;
		_playerIndex = playerIndex;
	}

	public override void OnReceive(BinaryReader reader, int whoAmI)
	{
		bool visibility = reader.ReadBoolean();
		byte player = reader.ReadByte();

		if (Main.netMode == NetmodeID.Server)
			new TowelVisibilityData(visibility, player).Send(ignoreClient: whoAmI);

		Main.player[player].GetModPlayer<BeachTowelPlayer>().bodyEquip = visibility;
	}

	public override void OnSend(ModPacket modPacket)
	{
		modPacket.Write(_visibility);
		modPacket.Write(_playerIndex);
	}
}