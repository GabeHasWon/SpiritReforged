namespace SpiritReforged.Common.WorldGeneration.Noise;

internal class NoiseSystem : ModSystem
{
	private FastNoiseLite _noise;
	private FastNoiseLite _noiseStatic;

	public override void Load()
	{
		_noise = new FastNoiseLite(Main.rand.Next());

		_noiseStatic = new FastNoiseLite();
		_noiseStatic.SetFrequency(.1f);
	}

	/// <summary> 0.01 frequency perlin noise with a variable seed. </summary>
	public static float Perlin(float x, float y) => ModContent.GetInstance<NoiseSystem>()._noise.GetNoise(x, y);

	/// <summary> 0.1 frequency perlin noise with a non-variable seed. </summary>
	public static float PerlinStatic(float x, float y) => ModContent.GetInstance<NoiseSystem>()._noiseStatic.GetNoise(x, y);
}
