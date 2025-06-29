namespace SpiritReforged.Content.SaltFlats.Biome;

public class SaltWaterStyle : ModWaterStyle
{
	public override int ChooseWaterfallStyle() => ModContent.GetInstance<SaltWaterfallStyle>().Slot;
	public override int GetSplashDust() => DustID.Water;
	public override int GetDropletGore() => GoreID.WaterDrip;
	public override Color BiomeHairColor() => Color.White;
}