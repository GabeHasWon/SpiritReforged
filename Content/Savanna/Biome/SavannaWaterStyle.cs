namespace SpiritReforged.Content.Savanna.Biome;

public class SavannaWaterStyle : ModWaterStyle
{
	public override int ChooseWaterfallStyle() => ModContent.GetInstance<SavannaWaterfallStyle>().Slot;
	public override int GetSplashDust() => DustID.Water;
	public override int GetDropletGore() => GoreID.WaterDrip;
	public override Color BiomeHairColor() => new(185, 175, 40);
}