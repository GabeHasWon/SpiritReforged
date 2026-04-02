using Humanizer;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.Visuals;
using System.IO;
using Terraria.Audio;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Forest.Glyphs;

[FromClassic("Glyph")]
public class BlankGlyph : ModItem
{
	public override void SetStaticDefaults() => Item.ResearchUnlockCount = 5;

	public override void SetDefaults()
	{
		Item.width = 28;
		Item.height = 28;
		Item.value = 0;
		Item.rare = ItemRarityID.Quest;
		Item.maxStack = Item.CommonMaxStack;
	}
}

public class GlyphGlobalItem : GlobalItem
{
	public readonly record struct GlyphType
	{
		private static readonly Dictionary<string, int> NameToType = [];

		public readonly bool Empty => Name == null || ItemType < ItemID.Count || ItemLoader.GetItem(ItemType) is not GlyphItem;

		/// <summary> The item type associated with this Glyph. </summary>
		public readonly int ItemType
		{
			get
			{
				if (Name != null)
				{
					if (NameToType.TryGetValue(Name, out int itemType))
					{
						return itemType;
					}
					else if (SpiritReforgedMod.Instance.TryFind(Name, out ModItem modItem))
					{
						NameToType.Add(Name, modItem.Type);
						return modItem.Type;
					}
				}

				return -1;
			}
		}

		public readonly string Name;

		public GlyphType(string Name) => this.Name = Name;

		public GlyphType(int ItemType) => Name = ItemLoader.GetItem(ItemType).Name;
	}

	public override bool InstancePerEntity => true;

	/// <summary> Prevents item consumption for the local client only. </summary>
	private static bool StopItemConsumption;
	public GlyphType glyph;

	public override bool CanRightClick(Item item) => Main.mouseItem.ModItem is GlyphItem glyphItem && glyphItem.CanBeApplied(item);

	public override void RightClick(Item item, Player player)
	{
		if (Main.mouseItem.ModItem is GlyphItem glyphItem && glyphItem.CanBeApplied(item))
		{
			glyphItem.OnApply(item);

			if (--Main.mouseItem.stack <= 0)
				Main.mouseItem.TurnToAir(); //Consume a glyph on hand

			StopItemConsumption = true;
		}
	}

	public override bool ConsumeItem(Item item, Player player)
	{
		bool value = StopItemConsumption;
		StopItemConsumption = false;
		return !value;
	}

	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		if (!glyph.Empty && ItemLoader.GetItem(glyph.ItemType) is GlyphItem glyphItem)
		{
			tooltips.AddRange(new List<TooltipLine>()
				{
					new(Mod, "GlyphEffect", glyphItem.Effect.Value) { OverrideColor = glyphItem.settings.Color },
					new(Mod, "GlyphTooltip", glyphItem.Tooltip.Value)
				}
			);
		}
	}

	public override void PostDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
	{
		const int slotDimensions = 52;

		if (!glyph.Empty && ItemLoader.GetItem(glyph.ItemType) is GlyphItem glyphItem)
		{
			Texture2D texture = glyphItem.IconTexture.Value;
			float iconScale = Main.inventoryScale;
			Vector2 iconPosition = position + (new Vector2(slotDimensions / 2, slotDimensions / 2) - texture.Size() / 2 - new Vector2(4)) * iconScale;

			DrawHelpers.DrawOutline(spriteBatch, texture, iconPosition, Color.White, (offset) =>
				spriteBatch.Draw(texture, iconPosition + offset, null, Color.White.Additive() * ((1f + (float)Math.Sin(Main.timeForVisualEffects / 30f)) * 0.1f), 0, texture.Size() / 2, iconScale, 0, 0));

			spriteBatch.Draw(texture, iconPosition, null, Color.White, 0, texture.Size() / 2, iconScale, 0, 0);
		}
	}

	public override void NetSend(Item item, BinaryWriter writer) => writer.Write(glyph.ItemType);

	public override void NetReceive(Item item, BinaryReader reader)
	{
		if (ItemLoader.GetItem(reader.ReadInt32()) is ModItem modItem)
			glyph = new(modItem.Name);
	}

	public override void SaveData(Item item, TagCompound tag) => tag[nameof(glyph)] = glyph.Name;

	public override void LoadData(Item item, TagCompound tag)
	{
		if (tag.GetString(nameof(glyph)) is string name && name != default)
			glyph = new(name);
	}
}

public abstract class GlyphItem : ModItem
{
	public readonly record struct GlyphSettings(Color Color);

	public LocalizedText Effect => this.GetLocalization("Effect");
	public static LocalizedText Gain => Language.GetText(glyphLocalization + "Gain");
	public static LocalizedText Target => Language.GetText(glyphLocalization + "Target");
	public static LocalizedText Enchant => Language.GetText(glyphLocalization + "RightClick");

	public Asset<Texture2D> IconTexture
	{
		get
		{
			if (IconByItemType.TryGetValue(Type, out Asset<Texture2D> texture))
			{
				return texture;
			}
			else
			{
				IconByItemType.Add(Type, ModContent.Request<Texture2D>(Texture + "_Icon"));
				return IconByItemType[Type];
			}
		}
	}

	private const string glyphLocalization = "Mods.SpiritReforged.Items.BlankGlyph.";
	private static readonly Dictionary<int, Asset<Texture2D>> IconByItemType = [];

	public static readonly SoundStyle EnchantSound = new("SpiritReforged/Assets/SFX/Item/GlyphAttach");
	public GlyphSettings settings;

	public override void SetStaticDefaults() => Item.ResearchUnlockCount = 5;

	public virtual bool CanBeApplied(Item item) => item.damage >= 0 && item.TryGetGlobalItem(out GlyphGlobalItem glyphItem) && glyphItem.glyph.ItemType != Type;

	public virtual void OnApply(Item item)
	{
		item.GetGlobalItem<GlyphGlobalItem>().glyph = new(Type);
		SoundEngine.PlaySound(EnchantSound);

		item.ClearNameOverride();
		item.SetNameOverride($"{Effect} " + item.Name);
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		if (tooltips.FindIndex(static x => x.Name == "Tooltip0") is int index && index < 0)
			return;

		Color color = settings.Color * (Main.mouseTextColor / 255f);
		tooltips.Insert(index, new(Mod, "GlyphTooltip", Gain.Value.FormatWith(string.Format("{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B), Effect))
		{
			OverrideColor = new Color(120, 190, 120)
		});

		tooltips.Insert(index, new(Mod, "GlyphHint", (Item.shopCustomPrice.HasValue ? Target : Enchant).Value)
		{
			OverrideColor = new Color(120, 190, 120)
		});
	}
}