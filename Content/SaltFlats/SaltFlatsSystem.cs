using SpiritReforged.Common.Misc;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.SaltFlats.Biome;
using System.IO;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.SaltFlats;

internal class SaltFlatsSystem : ModSystem
{
	private readonly record struct Star(int Frame, float Rotation, Vector2 Position, float Opacity, float Scale)
	{
		public readonly void Draw(SpriteBatch spriteBatch, Vector2 scene)
		{
			Vector2 position = Position / 1200f * scene;
			Color color = Color.Lerp(Color.White, Color.DarkGray, Utils.GetLerpValue(2f, 0.4f, Scale, true)) * _starOpacity;
			Rectangle src = new(0, 44 * Frame, 40, 40);
			float sine = 1 + MathF.Sin(Main.GameUpdateCount * 0.02f) * 0.1f;

			spriteBatch.Draw(_starTex.Value, position, src, color * Opacity * (sine - 0.2f), Rotation, src.Size() / 2f, Scale * sine, SpriteEffects.None, 0);
		}
	}

	[WorldBound]
	internal static int SurfaceHeight;

	[WorldBound]
	private static readonly List<Star> _stars = [];

	private static float _starOpacity = 0f;
	private static readonly Asset<Texture2D> _starTex = ModContent.Request<Texture2D>("SpiritReforged/Assets/Textures/Backgrounds/SaltFlatStar");
	private static readonly Asset<Texture2D> _galaxyTex = ModContent.Request<Texture2D>("SpiritReforged/Assets/Textures/Backgrounds/SaltFlatGalaxy");

	public override void Load() => On_Main.DrawStarsInBackground += DrawSaltFlatsBG;
	private static void DrawSaltFlatsBG(On_Main.orig_DrawStarsInBackground orig, Main self, Main.SceneArea sceneArea, bool artificial)
	{
		orig(self, sceneArea, artificial);

		if (Main.gameMenu)
			return;

		_starOpacity = MathHelper.Lerp(_starOpacity, (Main.LocalPlayer.InModBiome<SaltBiome>() && !Main.dayTime) ? 1 : 0, 0.05f);
		if (_starOpacity > 0.01f)
		{
			if (_stars.Count == 0) //Initialize
			{
				for (int i = 0; i < 260; ++i)
				{
					Vector2 pos = new(Main.rand.Next(Main.screenWidth + 1), Main.rand.Next(Main.screenHeight + 1));
					int frame = Main.rand.Next(6);
					_stars.Add(new Star(frame, Main.rand.NextFloat(MathHelper.TwoPi), pos, Main.rand.NextFloat(0.6f, 1f), Main.rand.NextFloat(0.5f, 1.2f)));
				}
			}

			Main.spriteBatch.Draw(_galaxyTex.Value, new Vector2(-300), null, Color.White.Additive() * _starOpacity * 0.3f, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);

			foreach (Star star in _stars)
			{
				Vector2 sceneOffset = new Vector2(sceneArea.totalWidth, sceneArea.totalHeight) + new Vector2(0f, sceneArea.bgTopY) + sceneArea.SceneLocalScreenPositionOffset;
				star.Draw(Main.spriteBatch, sceneOffset);
			}
		}
	}

	public override void SaveWorldData(TagCompound tag) => tag["height"] = SurfaceHeight;
	public override void LoadWorldData(TagCompound tag) => SurfaceHeight = tag.GetInt("height");

	public override void NetSend(BinaryWriter writer) => writer.Write((short)SurfaceHeight);
	public override void NetReceive(BinaryReader reader) => SurfaceHeight = reader.ReadInt16();
}