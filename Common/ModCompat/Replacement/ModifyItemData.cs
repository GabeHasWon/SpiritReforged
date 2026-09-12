using SpiritReforged.Common.Misc;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Common.ModCompat.Replacement;

/// <summary> Indicates that items contained in <see cref="ReplacementSystem.ItemToReplacement"/> are shimmerable into Reforged versions. </summary>
internal class ModifyItemData : GlobalItem
{
	private const string Shimmerable = "Shimmerable";

	public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
	{
		foreach (IItemDropRule rule in itemLoot.Get())
		{
			ChangeCommonRule(rule);
			if (rule is OneFromRulesRule oneFromRules)
			{
				foreach (IItemDropRule subRule in oneFromRules.options)
					ChangeCommonRule(subRule);
			}
		}

		static void ChangeCommonRule(IItemDropRule rule)
		{
			if (rule is CommonDrop drop)
			{
				if (ReplacementSystem.ItemToReplacement.TryGetValue(drop.itemId, out int reforged))
					drop.itemId = reforged;
			}
		}
	}

	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		if (ReplacementSystem.ItemToReplacement.ContainsKey(item.type))
		{
			string text = Language.GetTextValue("Mods.SpiritReforged.Items.CommonTooltips.Shimmerable");
			tooltips.Add(new TooltipLine(Mod, Shimmerable, text));
		}
	}

	public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
	{
		const int frame_count = 5;

		if (line.Mod == Mod.Name && line.Name == Shimmerable)
		{
			Main.instance.LoadNPC(NPCID.Shimmerfly);
			var icon = TextureAssets.Npc[NPCID.Shimmerfly].Value;

			for (int i = 0; i < 4; i++)
			{
				int frameY = (int)((float)Main.timeForVisualEffects / 5f % frame_count);
				var source = icon.Frame(4, frame_count, i, frameY, 0, -2);
				var position = new Vector2(line.X, line.Y) + new Vector2(10, 10 + (float)Math.Sin(Main.timeForVisualEffects / 80f) * 2f);
				var color = (i < 2) ? Color.White : Main.hslToRgb((float)Main.timeForVisualEffects / 120f % 1f, 1, .5f).Additive();

				if (i == 3)
					color *= 0.5f;

				Main.spriteBatch.Draw(icon, position, source, color, 0, source.Size() / 2, 1, default, 0);
			}

			line.X += 16;
			Utils.DrawBorderString(Main.spriteBatch, line.Text.Replace("{0}", string.Empty), new Vector2(line.X, line.Y), Main.MouseTextColorReal.Additive(50));
			return false;
		}

		return true;
	}
}