using SpiritReforged.Common.ItemCommon;
using System.Linq;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Forest.Glyphs;

public class EnchantedStamp : ModItem
{
	public sealed class StampToggle : BuilderToggle
	{
		public const int InactiveState = 1;

		public override int NumberOfStates => 2;
		public override string HoverTexture => Texture + "_Hover";

		public override bool Active() => Main.LocalPlayer.GetModPlayer<StampPlayer>().usedStamp;
		public override string DisplayValue() => Language.GetTextValue("Mods.SpiritReforged.Items.EnchantedStamp." + ((CurrentState == InactiveState) ? "Inactive" : "Active"));
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

	public sealed class StampGlobalItem : GlobalItem
	{
		public override void ApplyPrefix(Item item, int pre)
		{
			if (!Main.gameMenu && ModContent.GetInstance<StampToggle>().CurrentState != StampToggle.InactiveState && WorldGen.genRand.NextBool(5)) //Randomly replace prefixes with Glyph effects when active
			{
				GlyphItem[] array = Mod.GetContent<GlyphItem>().ToArray();
				GlyphItem glyphItem = array[WorldGen.genRand.Next(array.Length)];

				if (item.SetGlyph(new(glyphItem.Type), new GlyphItem.ApplyContext(Main.LocalPlayer)))
					GlyphItem.GlyphGlobalItem.StartAnimation(item);
			}
		}
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