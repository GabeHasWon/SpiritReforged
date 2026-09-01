using SpiritReforged.Common.ItemCommon.Abstract;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items;

public class AdornedBow : BaseGreatbowItem
{
	public readonly record struct PrismaticPalette
	{
		public readonly Color[] Colors;

		public PrismaticPalette() => Colors = GetPrismaticColors();

		public void FadeColors(Color[] source, float progress)
		{
			Colors[0] = Color.Lerp(source[0], source[1], progress);
			Colors[1] = Color.Lerp(source[1], source[2], progress);
			Colors[2] = Color.Lerp(source[2], source[0], progress);
		}

		public static Color[] GetPrismaticColors()
		{
			var colors = new Color[3];

			colors[0] = new Color(255, 0, 70 + 25 * Main.rand.Next(5)); // Magenta to Purple
			colors[1] = new Color(0, 255, 255 - 25 * Main.rand.Next(5)); // Cyan to Green
			colors[2] = new Color(255, 255 - 25 * Main.rand.Next(5), 0); // Yellow to Orange

			return colors;
		}
	}

	internal override float ChargeScaling => 1.75f;
	internal override float PerfectShotScaling => 1.33f;

	internal override void SafeSetDefaults()
	{
		Item.damage = 25;
		Item.Size = new Vector2(48, 52);
		Item.rare = ItemRarityID.Green;
		Item.value = Item.sellPrice(gold: 2);
	}
}