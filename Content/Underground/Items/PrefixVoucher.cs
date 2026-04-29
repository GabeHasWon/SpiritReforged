using Humanizer;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using System.Collections.ObjectModel;
using System.IO;
using Terraria.GameContent.UI;
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
				item.ResetPrefix();
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

		public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
		{
			if (Main.mouseItem.ModItem is PrefixVoucher voucher && item.CanApplyPrefix(voucher.prefix))
			{
				Texture2D texture = TextureAssets.Item[item.type].Value;
				//spriteBatch.Draw(texture, position, null, Color.White.Additive() * (float)Math.Sin(Main.timeForVisualEffects / 20f), 0, texture.Size() / 2, scale, 0, 0);

				DrawHelpers.DrawOutline(default, default, default, default, (offset) =>
					spriteBatch.Draw(TextureColorCache.ColorSolid(texture, Color.White), position + offset * scale, null, voucher._info.Color.Additive() * 0.25f, 0, texture.Size() / 2, scale, 0, 0));
			}

			return true;
		}
	}

	public readonly record struct ExtendedPrefixInfo(Color Color, int Rarity, string PrefixText, Rectangle TooltipSource);

	/// <summary> Item types to sample for prefix rarity color. </summary>
	private static readonly int[] _sampleTypes = [ItemID.CopperBroadsword, ItemID.WoodenBow, ItemID.WandofSparking, ItemID.BabyBirdStaff, ItemID.Aglet];

	public int prefix;
	private ExtendedPrefixInfo _info;

	/// <summary> <see cref="prefix"/> must be valid before calling. </summary>
	public ExtendedPrefixInfo FindInfo()
	{
		Color color = Color.White;
		int rare = ItemRarityID.White;

		for (int i = 0; i < _sampleTypes.Length; i++)
		{
			Item item = new(_sampleTypes[i]);

			if (item.Prefix(prefix))
			{
				rare = item.rare - item.OriginalRarity;
				color = ItemRarity.GetColor(rare);

				break;
			}
		}

		//Find source
		var font = FontAssets.MouseText.Value;
		string[] lines = Tooltip.Value.Split('\n');

		if (prefix > 0 && prefix < Lang.prefix.Length)
		{
			string text = (PrefixLoader.GetPrefix(prefix) is ModPrefix modPrefix ? modPrefix.DisplayName : Lang.prefix[prefix]).Value;

			Vector2 lineTwoSize = font.MeasureString(lines[1].Split(' ')[0]);
			Vector2 prefixSize = font.MeasureString(text);

			Rectangle source = new((int)lineTwoSize.X, 0, (int)prefixSize.X, (int)prefixSize.Y);

			return _info = new(color, rare, text, source);
		}

		return _info = new(color, rare, string.Empty, Rectangle.Empty);
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.Green;
		Item.maxStack = 1;

		prefix = Main.rand.Next(PrefixLoader.PrefixCount);
		FindInfo();
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		if (tooltips.FindIndex(static x => x.Name == "Tooltip1") is int index && index < 0)
			return;

		Color color = _info.Color * (Main.mouseTextColor / 255f);
		tooltips[index].Text = tooltips[index].Text.FormatWith(string.Format("{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B), _info.PrefixText);
	}

	public override void PostDrawTooltip(ReadOnlyCollection<DrawableTooltipLine> lines)
	{
		foreach (DrawableTooltipLine line in lines)
		{
			if (line.Name != "Tooltip1")
				continue;

			Texture2D texture = AssetLoader.LoadedTextures["Star"].Value;
			Rectangle area = new(line.X + _info.TooltipSource.X + 8, line.Y + _info.TooltipSource.Y, _info.TooltipSource.Width, _info.TooltipSource.Height);

			Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
			Main.EntitySpriteDraw(bloom, area.Center(), null, _info.Color.Additive() * 0.25f, 0, bloom.Size() / 2, new Vector2(1f / bloom.Width * area.Width * 1.5f, 1f / bloom.Height * area.Height), default);

			DrawStar(new(area.X, area.Y), _info.Color.Additive(), 12);
			DrawStar(new(area.X + area.Width * 0.75f, area.Y + area.Height * 0.8f), _info.Color.Additive(), 30);
			DrawStar(new(area.Right, area.Y + area.Height * 0.2f), _info.Color.Additive(), 20);
		}

		static void DrawStar(Vector2 position, Color color, float duration)
		{
			double time = Main.timeForVisualEffects;
			float opacity = (float)Math.Sin(time / duration);

			Texture2D texture = AssetLoader.LoadedTextures["Star"].Value;
			Main.spriteBatch.Draw(texture, position, null, color * opacity, (float)Main.timeForVisualEffects * 0.02f, texture.Size() / 2, 0.1f, 0, 0);
			Main.spriteBatch.Draw(texture, position, null, Color.White.Additive() * opacity * 0.5f, (float)Main.timeForVisualEffects * 0.02f, texture.Size() / 2, 0.08f, 0, 0);
		}
	}

	public override void NetSend(BinaryWriter writer) => writer.Write(prefix);

	public override void NetReceive(BinaryReader reader)
	{
		prefix = reader.ReadInt32();
		FindInfo();
	}

	public override void SaveData(TagCompound tag) => tag[nameof(prefix)] = prefix;

	public override void LoadData(TagCompound tag)
	{
		prefix = tag.GetInt(nameof(prefix));
		FindInfo();
	}
}