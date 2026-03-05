using SpiritReforged.Common.Misc;

namespace SpiritReforged.Common.ModCompat.Classic;

/// <summary> Indicates that Classic items contained in <see cref="SpiritClassic.ClassicItemToReforged"/> are shimmerable into Reforged versions. </summary>
internal class ObsoleteItem : GlobalItem
{
	private const string Shimmerable = "Shimmerable";

	public override bool IsLoadingEnabled(Mod mod) => CrossMod.Classic.Enabled;

	public override bool AppliesToEntity(Item entity, bool lateInstantiation) => SpiritClassic.ClassicItemToReforged.ContainsKey(entity.type);
	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		string text = Language.GetTextValue("Mods.SpiritReforged.Items.CommonTooltips.Shimmerable");
		tooltips.Add(new TooltipLine(Mod, Shimmerable, text));
	}

	public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
	{
		const int frameCount = 5;

		if (line.Mod == Mod.Name && line.Name == Shimmerable)
		{
			Main.instance.LoadNPC(NPCID.Shimmerfly);
			var icon = TextureAssets.Npc[NPCID.Shimmerfly].Value;

			for (int i = 0; i < 4; i++)
			{
				int frameY = (int)((float)Main.timeForVisualEffects / 5f % frameCount);
				var source = icon.Frame(4, frameCount, i, frameY, 0, -2);
				var position = new Vector2(line.X, line.Y) + new Vector2(10, 10 + (float)Math.Sin(Main.timeForVisualEffects / 80f) * 2f);
				var color = (i < 2) ? Color.White : Main.hslToRgb((float)Main.timeForVisualEffects / 120f % 1f, 1, .5f).Additive();

				if (i == 3)
					color *= .5f;

				Main.spriteBatch.Draw(icon, position, source, color, 0, source.Size() / 2, 1, default, 0);
			}

			line.X += 16;
			Utils.DrawBorderString(Main.spriteBatch, line.Text.Replace("{0}", string.Empty), new Vector2(line.X, line.Y), Main.MouseTextColorReal.Additive(50));
			return false;
		}

		return true;
	}
}