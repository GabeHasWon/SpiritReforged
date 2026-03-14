using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Savanna.DustStorm;
using Terraria.GameContent.Events;

namespace SpiritReforged.Content.Desert.Biome;

[Autoload(Side = ModSide.Client)]
public class DesertWind : ILoadable
{
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
			_opacity = EaseFunction.EaseCircularOut.Ease(Progress);
			_scaleMod = MathHelper.Lerp(1, FinalScaleMod, Progress);

			Velocity = (1 - _acceleration.Ease(Progress)) * _initialVel;
			Velocity.Y -= 0.1f;

			var size = new Vector2(50, 25) * Scale;
			if (Collision.SolidCollision(Position - size / 2, (int)size.X, (int)size.Y))
				Velocity.Y -= 0.15f;
		}

		public override Color GetLightColor() => Lighting.GetColor(Position.ToTileCoordinates()) * _opacity;
		public override ParticleLayer DrawLayer => ParticleLayer.BelowProjectile;
	}

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
			Color color = TileMaterial.FindMaterial(type).Color;
			float odds = MathHelper.Lerp(1, 0.25f, Math.Abs(Main.windSpeedCurrent));

			if (Main.rand.NextBool((int)(100 * odds)))
			{
				var position = new Vector2(i, j) * 16;
				var velocity = GetVelocity();
				int timeLeft = Main.rand.Next(250, 500);

				ParticleHandler.SpawnParticle(new DesertCloud(position, velocity, color * 0.5f, 0.3f, EaseFunction.EaseCircularOut, timeLeft + 10)
				{
					SecondaryColor = Color.Lerp(color, Color.Black, 0.2f) * 0.5f,
					TertiaryColor = color * 0.2f
				});
				ParticleHandler.SpawnParticle(new DesertCloud(position, velocity * 0.5f, color * 0.25f, 0.25f, EaseFunction.EaseCircularOut, timeLeft + 10)
				{
					SecondaryColor = Color.Lerp(color, Color.Black, 0.2f) * 0.25f,
					TertiaryColor = color * 0.1f
				});
			}

			if (Main.rand.NextBool((int)(20 * odds)))
			{
				var dust = Dust.NewDustPerfect(new Vector2(i, j) * 16, ModContent.DustType<SavannaSand>(), GetVelocity() - Vector2.UnitY, 200, color);
				dust.noGravity = true;
				dust.customData = Main.rand.NextFloat(0.005f, 0.011f);
				dust.scale = Main.rand.NextFloat(0.5f, 1.1f);
			}
		}
	}

	private static Vector2 GetVelocity()
	{
		Vector2 velocity = new(Main.windSpeedCurrent * Main.rand.NextFloat(2, 4), Main.rand.NextFloat(-0.2f, 0.2f));
		if (Sandstorm.Happening)
			velocity *= 2;

		return velocity;
	}
}