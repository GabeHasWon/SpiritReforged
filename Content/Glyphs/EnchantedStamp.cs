using SpiritReforged.Common.ItemCommon;
using System.Linq;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Glyphs;

public class EnchantedStamp : ModItem
{
	public sealed class StampToggle : BuilderToggle
	{
		public const int InactiveState = 1;

		public override int NumberOfStates => 2;
		public override string HoverTexture => Texture + "_Hover";

		public override bool Active() => Main.LocalPlayer.GetModPlayer<StampPlayer>().usedStamp;
		public override string DisplayValue() => Language.GetTextValue("Mods.SpiritReforged.Items.EnchantedStamp." + (CurrentState == InactiveState ? "Inactive" : "Active"));
		public override bool Draw(SpriteBatch spriteBatch, ref BuilderToggleDrawParams drawParams)
		{
			if (CurrentState == InactiveState)
				drawParams.Color *= 0.5f;

			return true;
		}
	}

	public sealed class StampPlayer : ModPlayer
	{
		public bool usedStamp;

		public override void SaveData(TagCompound tag) => tag[nameof(usedStamp)] = usedStamp;
		public override void LoadData(TagCompound tag) => usedStamp = tag.GetBool(nameof(usedStamp));
	}

	public override void Load() => ItemEvents.OnPrefix += ReplacePrefixes;

	private static bool ReplacePrefixes(Item item, int prefix)
	{
		if (prefix is -1 or -2 && !Main.gameMenu) //Is a naturally-occuring or goblin reforge
		{
			StampToggle stampToggle = ModContent.GetInstance<StampToggle>();
			if (stampToggle.Active() && stampToggle.CurrentState != StampToggle.InactiveState && Main.rand.NextBool(5)) //Randomly replace prefixes with Glyph effects when active
			{
				GlyphItem[] array = ModContent.GetContent<GlyphItem>().ToArray();
				GlyphItem glyphItem = array[Main.rand.Next(array.Length)];

				if (item.SetGlyph(new(glyphItem.Type), new GlyphItem.ApplyContext(Main.LocalPlayer)) && item.TryGetGlobalItem(out GlyphItem.GlyphGlobalItem glyphGlobalItem))
					glyphGlobalItem.StartAnimation();

				return false;
			}
		}

		return true;
	}

	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.TorchGodsFavor);
		Item.value = Item.buyPrice(gold: 50);
		Item.rare = ItemRarityID.Orange;
	}

	public override bool? UseItem(Player player)
	{
		if (player.ItemAnimationJustStarted)
		{
			StampPlayer stampPlayer = player.GetModPlayer<StampPlayer>();
			bool didUseStamp = stampPlayer.usedStamp;
			stampPlayer.usedStamp = true;

			return !didUseStamp;
		}

		return false;
	}
}