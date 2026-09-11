using SpiritReforged.Common.ItemCommon;
using System.Linq;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Glyphs;

public class CelestialStamp : ModItem
{
	public sealed class CelestialStampToggle : BuilderToggle
	{
		public const int InactiveState = 1;

		public override int NumberOfStates => 2;
		public override string HoverTexture => Texture + "_Hover";

		public override bool Active() => Main.LocalPlayer.TryGetModPlayer(out CelestialStampPlayer mPlayer) && mPlayer.usedCelestialStamp;
		public override string DisplayValue() => Language.GetTextValue("Mods.SpiritReforged.Items.EnchantedStamp." + (CurrentState == InactiveState ? "Inactive" : "Active"));
		public override bool Draw(SpriteBatch spriteBatch, ref BuilderToggleDrawParams drawParams)
		{
			if (CurrentState == InactiveState)
				drawParams.Color *= 0.5f;

			return true;
		}
	}

	public sealed class CelestialStampPlayer : ModPlayer
	{
		public bool usedCelestialStamp;

		public override void SaveData(TagCompound tag) => tag[nameof(usedCelestialStamp)] = usedCelestialStamp;
		public override void LoadData(TagCompound tag) => usedCelestialStamp = tag.GetBool(nameof(usedCelestialStamp));
	}

	public override void Load() => ItemEvents.OnPrefix += ReplacePrefixes;

	private static bool ReplacePrefixes(Item item, int prefix)
	{
		if (prefix == -1 && !Main.gameMenu) //Is a naturally-occuring reforge
		{
			CelestialStampToggle stampToggle = ModContent.GetInstance<CelestialStampToggle>();
			if (stampToggle.Active() && stampToggle.CurrentState != CelestialStampToggle.InactiveState && Main.rand.NextBool(5)) //Randomly replace prefixes with Glyph effects when active
			{
				GlyphItem[] array = ModContent.GetContent<GlyphItem>().ToArray();
				GlyphItem glyphItem = array[Main.rand.Next(array.Length)];

				if (item.SetGlyph(new(glyphItem.Type), new GlyphItem.ApplyContext(Main.LocalPlayer)) && item.TryGetGlobalItem(out GlyphItem.GlyphGlobalItem glyphGlobalItem))
					glyphGlobalItem.StartAnimation();
			}
		}

		return true;
	}

	public override void SetDefaults()
	{
		Item.CloneDefaults(ItemID.TorchGodsFavor);
		Item.value = Item.buyPrice(platinum: 1);
		Item.rare = ItemRarityID.Red;
	}

	public override bool? UseItem(Player player)
	{
		if (player.ItemAnimationJustStarted)
		{
			CelestialStampPlayer stampPlayer = player.GetModPlayer<CelestialStampPlayer>();
			bool didUseStamp = stampPlayer.usedCelestialStamp;
			stampPlayer.usedCelestialStamp = true;

			return !didUseStamp;
		}

		return false;
	}
}