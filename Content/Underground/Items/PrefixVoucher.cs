using Humanizer;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Savanna.Items.Vanity;
using System.IO;
using System.Reflection;
using Terraria.GameContent.UI;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Underground.Items;

public class PrefixVoucher : ModItem
{
	public sealed class PrefixVoucherItem : GlobalItem
	{
		/// <summary> Prevents item consumption for the local client only. </summary>
		private static bool StopItemConsumption;

		public override bool CanRightClick(Item item) => Main.mouseItem.ModItem is PrefixVoucher voucher && item.prefix != voucher.prefix && item.CanApplyPrefix(voucher.prefix);

		public override void RightClick(Item item, Player player)
		{
			if (Main.mouseItem.ModItem is PrefixVoucher voucher && item.prefix != voucher.prefix && item.CanApplyPrefix(voucher.prefix))
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

				DrawHelpers.DrawOutline(default, default, default, default, (offset) =>
					spriteBatch.Draw(TextureColorCache.ColorSolid(texture, Color.White), position + offset * scale, frame, voucher._info.Color.Additive() * 0.25f, 0, frame.Size() / 2, scale, 0, 0));
			}

			return true;
		}
	}

	private readonly record struct ExtendedPrefixInfo(Color Color, int Rarity, string PrefixText, Rectangle TooltipSource, bool Accessory);

	/// <summary> Item types to sample for prefix rarity color. </summary>
	private static readonly int[] _sampleTypes = [ItemID.CopperBroadsword, ItemID.WoodenBow, ItemID.WandofSparking, ItemID.BabyBirdStaff, ItemID.Aglet];

	private static FieldInfo _isLoadingInfo = null;

	public int prefix;

	private Item _tooltipPrefixItem;
	private ExtendedPrefixInfo _info;

	public override void Load() => _isLoadingInfo = typeof(ModLoader).GetField("isLoading", BindingFlags.Static | BindingFlags.NonPublic);

	/// <summary> <see cref="prefix"/> must be valid before calling. </summary>
	private ExtendedPrefixInfo FindInfo()
	{
		Color color = Color.White;
		int rare = ItemRarityID.White;
		bool accessory = false;

		for (int i = 0; i < _sampleTypes.Length; i++)
		{
			Item item = new(_sampleTypes[i]);
			if (item.Prefix(prefix) && item.prefix != 0)
			{
				rare = item.rare - item.OriginalRarity;
				color = ItemRarity.GetColor(rare);
				accessory = item.accessory;

				break;
			}
		}

		//Find source
		var font = FontAssets.MouseText.Value;
		string[] lines = Tooltip.Value.Split('\n');

		if (prefix > 0 && prefix < Lang.prefix.Length)
		{
			string text = (PrefixLoader.GetPrefix(prefix) is ModPrefix modPrefix ? modPrefix.DisplayName : Lang.prefix[prefix]).Value;

			//Apply a name override
			Item.ClearNameOverride();
			Item.SetNameOverride(Item.Name.FormatWith(text));

			// Split string,
			string[] strings = lines[1].Split(' ');
			int index = -1;

			for (int i = 0; i < strings.Length; i++)
			{
				string s = strings[i];

				if (s.Contains("{1}"))
				{
					index = i;
					break;
				}
			}

			// Find the replacement index, if any, or set to default (second word)
			if (index == -1)
				index = 1;

			// Default offset to the height of the font - maybe this has issues with resource packs?
			Vector2 offset = new(0, 29);

			for (int i = 0; i < index; i++)
			{
				string s = strings[i];
				offset.X += font.MeasureString(s + " ").X;
			}

			// Adjust string offset and set source
			Vector2 prefixNameSize = font.MeasureString(text);
			Rectangle source = new((int)offset.X, 0, (int)prefixNameSize.X, (int)offset.Y);

			return _info = new(color, rare, text, source, accessory);

			// New code from QM kept for posterity, or in case we need to re-adjust
			////Apply a name override
			//Item.ClearNameOverride();
			//Item.SetNameOverride(Item.Name.FormatWith(text));

			//string appliesLine = lines[1].Remove(lines[1].IndexOf(' ') + 1); //Return the tooltip line before any whitespace
			//Vector2 firstMeasure = font.MeasureString(appliesLine);
			//Vector2 secondMeasure = font.MeasureString(text);

			//Rectangle source = new((int)firstMeasure.X, 0, (int)secondMeasure.X, (int)secondMeasure.Y);
			//return _info = new(color, rare, text, source, accessory);
		}

		//Apply a default name override
		Item.ClearNameOverride();
		Item.SetNameOverride(Item.Name.Remove(0, 4)); //Remove format characters

		return _info = new(color, rare, Language.GetTextValue("Achievements.NoCategory"), Rectangle.Empty, accessory); //Display "None"
	}

	public void RecalculatePrefixInfo() => FindInfo(); // Publicly accessible portal for FindInfo

	private static Item GetPrefixableItem(int prefix)
	{
		Item item = new(ItemID.WoodenSword);

		if (_isLoadingInfo.GetValue(null) is true) // Stops an infinite loop in load
			return item;

		if (Main.dedServ && prefix == 0)
			return item;

		int attempts = 0;

		while (item.prefix == 0 || item.rare < item.OriginalRarity) // Has no prefix or is a negative prefix
		{
			item = new(_sampleTypes[Main.rand.Next(_sampleTypes.Length)]);
			item.Prefix(prefix);

			attempts++;

			if (attempts > 100)
			{
				attempts = 0;
				prefix = -2;
			}
		}

		return item;
	}

	public static int RollRandomPrefix(out int sampleItemType)
	{
		Item item = GetPrefixableItem(-2);
		sampleItemType = item.type;

		return item.prefix;
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.Green;
		Item.maxStack = 1;

		if (_isLoadingInfo.GetValue(null) is true) // Don't load data in menu for the template instance
			return;

		prefix = RollRandomPrefix(out int itemType);
		_tooltipPrefixItem = new(itemType, 1, prefix);

		if (!Main.dedServ)
			FindInfo();
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		for (int index = 0; index < tooltips.Count; index++)
		{
			TooltipLine line = tooltips[index];
			if (line.Name == "Tooltip0")
			{
				string accessories = Language.GetTextValue("Mods.SpiritReforged.Items.PrefixVoucher.AccessoryTooltip");
				string weapons = Language.GetTextValue("Mods.SpiritReforged.Items.PrefixVoucher.WeaponTooltip");

				line.Text = line.Text.FormatWith(_info.Accessory ? accessories : weapons);
			}
			else if (line.Name == "Tooltip1")
			{
				Color color = _info.Color * (Main.mouseTextColor / 255f);
				Item tooltipItem = _tooltipPrefixItem ?? new(ItemID.WoodenSword);
				int tooltipCount = (30 + tooltipItem.ToolTip?.Lines).GetValueOrDefault();

				string[] tooltipNames = new string[tooltipCount];
				string[] tooltipLines = new string[tooltipCount];
				bool[] prefixLine = new bool[tooltipCount];
				bool[] badPrefixLine = new bool[tooltipCount];

				int numLines = 1;
				float knockBack = tooltipItem.knockBack;
				int yoyoLogo = 0;
				int researchLine = 0;

				Main.MouseText_DrawItemTooltip_GetLinesInfo(tooltipItem, ref yoyoLogo, ref researchLine, knockBack, ref numLines, tooltipLines, prefixLine, badPrefixLine, tooltipNames, out int _);
				line.Text = line.Text.FormatWith(string.Format("{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B), _info.PrefixText); //Add the name of the prefix

				for (int i = 0; i < tooltipLines.Length; i++) //Add prefix stat information
				{
					if (i < prefixLine.Length && prefixLine[i])
						line.Text += "\n" + tooltipLines[i];
				}
			}
		}
	}

	public override void PostDrawTooltipLine(DrawableTooltipLine line)
	{
		if (line.Name != "Tooltip1")
			return;

		Rectangle area =  new(line.X + _info.TooltipSource.X, line.Y + _info.TooltipSource.Y, _info.TooltipSource.Width, _info.TooltipSource.Height);
		Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
		Main.EntitySpriteDraw(bloom, area.Center(), null, _info.Color.Additive() * 0.25f, 0, bloom.Size() / 2, new Vector2(1f / bloom.Width * area.Width * 1.5f, 1f / bloom.Height * area.Height), default);

		DrawStar(new(area.X, area.Y), _info.Color.Additive(), 12);
		DrawStar(new(area.X + area.Width * 0.75f, area.Y + area.Height * 0.8f), _info.Color.Additive(), 30);
		DrawStar(new(area.Right, area.Y + area.Height * 0.2f), _info.Color.Additive(), 20);

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
		_tooltipPrefixItem = GetPrefixableItem(prefix);

		if (!Main.dedServ)
			FindInfo();
	}

	public override void SaveData(TagCompound tag) => tag[nameof(prefix)] = prefix;

	public override void LoadData(TagCompound tag)
	{
		prefix = tag.GetInt(nameof(prefix));
		_tooltipPrefixItem = GetPrefixableItem(prefix);

		if (!Main.dedServ)
			FindInfo();
	}
}