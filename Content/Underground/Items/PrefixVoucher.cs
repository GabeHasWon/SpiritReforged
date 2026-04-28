using Humanizer;
using System.IO;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Underground.Items;

public class PrefixVoucher : ModItem
{
	public sealed class PrefixVoucherItem : GlobalItem
	{
		/// <summary> Prevents item consumption for the local client only. </summary>
		private static bool StopItemConsumption;

		public override bool CanRightClick(Item item) => Main.mouseItem.ModItem is PrefixVoucher voucher && item.CanApplyPrefix(voucher.prefix);

		public override void RightClick(Item item, Player player)
		{
			if (Main.mouseItem.ModItem is PrefixVoucher voucher && item.CanApplyPrefix(voucher.prefix))
			{
				item.Prefix(voucher.prefix);

				if (--Main.mouseItem.stack <= 0)
					Main.mouseItem.TurnToAir(); //Consume the voucher on hand

				StopItemConsumption = true;
			}
		}

		public override bool ConsumeItem(Item item, Player player)
		{
			bool value = StopItemConsumption;
			StopItemConsumption = false;
			return !value;
		}
	}

	public static readonly Color Color = Color.LightGreen;
	public int prefix;

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.Green;
		Item.maxStack = Item.CommonMaxStack;
		prefix = Main.rand.Next(PrefixLoader.PrefixCount);
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		if (tooltips.FindIndex(static x => x.Name == "Tooltip1") is int index && index < 0)
			return;

		Color color = Color * (Main.mouseTextColor / 255f);
		tooltips[index].Text = tooltips[index].Text.FormatWith(string.Format("{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B), Lang.prefix[prefix]);
	}

	public override void NetSend(BinaryWriter writer) => writer.Write(prefix);

	public override void NetReceive(BinaryReader reader) => prefix = reader.ReadInt32();

	public override void SaveData(TagCompound tag) => tag[nameof(prefix)] = prefix;

	public override void LoadData(TagCompound tag) => prefix = tag.GetInt(nameof(prefix));
}