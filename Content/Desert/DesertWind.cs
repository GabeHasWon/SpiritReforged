using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Particles;

namespace SpiritReforged.Content.Desert;

[Autoload(Side = ModSide.Client)]
public class DesertWind : ILoadable
{
	public static readonly HashSet<int> SandTypes = [TileID.Sand, TileID.Ebonstone, TileID.Crimsand, TileID.Pearlsand];

	public void Load(Mod mod) => TileEvents.OnNearby += SpawnWind;
	public void Unload() { }

	private static void SpawnWind(int i, int j, int type, bool closer)
	{
		if (!closer || Main.gamePaused || !Main.LocalPlayer.ZoneDesert || j > Main.worldSurface || Math.Abs(Main.windSpeedCurrent) < 0.3f || !WorldGen.InWorld(i, j, 4))
			return;

		var tileAbove = Main.tile[i, j - 1];

		if (SandTypes.Contains(type) && !WorldGen.SolidTile(tileAbove) && tileAbove.LiquidAmount == 0 && tileAbove.WallType == WallID.None)
		{
			float odds = MathHelper.Lerp(1, 0.25f, Math.Abs(Main.windSpeedCurrent));
			if (Main.rand.NextBool((int)(2000 * odds)))
			{
				var position = new Vector2(i, j + 2) * 16;
				var velocity = new Vector2(Main.windSpeedCurrent * Main.rand.NextFloat(1, 3), 0.2f);
				int timeLeft = Main.rand.Next(250, 500);

				var color = TileMaterial.FindMaterial(type).Color;
				var hsl = Main.rgbToHsl(color);

				ParticleHandler.SpawnParticle(new DesertCloud(position, velocity, Main.hslToRgb(hsl with { X = hsl.X - 0.05f, Z = hsl.Z - 0.1f }) * 0.5f, 1f, EaseFunction.EaseCircularOut, timeLeft + 10)
				{
					TertiaryColor = Main.hslToRgb(hsl with { X = hsl.X - 0.05f, Y = 0.3f, Z = hsl.Z - 0.2f })
				});

				ParticleHandler.SpawnParticle(new DesertCloud(position, velocity, color * 0.5f, 0.7f, EaseFunction.EaseCircularOut, timeLeft)
				{
					TertiaryColor = Main.hslToRgb(hsl with { X = hsl.X - 0.1f, Z = 0.5f })
				});

				for (int x = 0; x < 2; x++)
				{
					ParticleHandler.SpawnParticle(new DesertCloud(position, velocity, color * 0.7f, 0.5f, EaseFunction.EaseCircularOut, timeLeft)
					{
						TertiaryColor = Main.hslToRgb(hsl with { X = hsl.X - 0.1f, Z = 0.5f })
					});
				}
			}

			if (Main.rand.NextBool((int)(800 * odds)))
			{
				var velocity = new Vector2(Math.Sign(Main.windSpeedCurrent) * Main.rand.NextFloat(2, 4), -0.2f);

				var dust = Dust.NewDustPerfect(new Vector2(i, j) * 16, DustID.Sand, velocity, 100);
				dust.noGravity = true;
				dust.scale = Main.rand.NextFloat(0.5f, 1f);
			}
		}
	}
}

public class DesertCloud : DissipatingImage
{
	private readonly EaseFunction _acceleration;
	private readonly Vector2 _initialVel;

	public DesertCloud(Vector2 position, Vector2 velocity, Color color, float scale, EaseFunction acceleration, int maxTime) : base(position, color, Main.rand.NextFloatDirection(), scale, 0.15f, GetCloudTexture(), new(0.33f, 0.33f), new(2, 1), maxTime)
	{
		Velocity = velocity;
		_initialVel = velocity;
		_acceleration = acceleration;

		DissolveAmount = 1;
		Pixellate = true;
		PixelDivisor = 3;
		Rotation = Main.rand.NextFloat(-0.1f, 0.1f);
	}

	private static Texture2D GetCloudTexture() => TextureAssets.Cloud[Main.rand.Next([0, 1, 2, 3, 14, 15, 16, 17])].Value;

	public override void Update()
	{
		base.Update();

		Velocity = (1 - _acceleration.Ease(Progress)) * _initialVel;

		var size = new Vector2(50, 25) * Scale;
		if (Collision.SolidCollision(Position - size / 2, (int)size.X, (int)size.Y))
			Velocity.Y -= 0.15f;
	}

	public override Color GetLightColor() => Lighting.GetColor((int)Position.X / 16, (int)(Position.Y - 400 * Scale) / 16);
	public override ParticleLayer DrawLayer => ParticleLayer.BelowWall;
}