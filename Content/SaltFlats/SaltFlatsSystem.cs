using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.SaltFlats.Biome;
using System.IO;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.SaltFlats;

internal class SaltFlatsSystem : ModSystem
{
	private readonly record struct Star(int Frame, float Rotation, Vector2 Position, float Opacity, float Scale);

	[WorldBound]
	internal static int SurfaceHeight;

	[WorldBound]
	private static readonly List<Star> _stars = [];

	private static float _starOpacity = 0f;
	private static Asset<Texture2D> _starTex = null;

	public override void Load()
	{
		On_Main.DrawStarsInBackground += DrawSaltFlatsBG;

		if (!Main.dedServ)
		{
			_starTex = ModContent.Request<Texture2D>("SpiritReforged/Assets/Textures/Backgrounds/SaltFlatStar");
		}
	}

	private void DrawSaltFlatsBG(On_Main.orig_DrawStarsInBackground orig, Main self, Main.SceneArea sceneArea, bool artificial)
	{
		orig(self, sceneArea, artificial);

		if (Main.gameMenu)
		{
			return;
		}

		_starOpacity = MathHelper.Lerp(_starOpacity, Main.LocalPlayer.InModBiome<SaltBiome>() ? 1 : 0, 0.05f);

		if (_starOpacity < 0.01f)
		{
			return;
		}

		if (_stars.Count == 0)
		{
			for (int i = 0; i < 280; ++i)
			{
				Vector2 pos = new(Main.rand.Next(1921), Main.rand.Next(1201));
				int frame = Main.rand.Next(6);
				_stars.Add(new Star(frame, Main.rand.NextFloat(MathHelper.TwoPi), pos, Main.rand.NextFloat(0.3f, 0.9f), Main.rand.NextFloat(0.5f, 1.2f)));
			}
		}

		foreach (Star star in _stars)
		{
			Vector2 position = star.Position / 1200f * new Vector2(sceneArea.totalWidth, sceneArea.totalHeight)
				+ new Vector2(0f, sceneArea.bgTopY) + sceneArea.SceneLocalScreenPositionOffset;
			var color = Color.Lerp(Color.White, Color.DarkGray, Utils.GetLerpValue(2f, 0.4f, star.Scale, true));
			Rectangle src = new(0, 22 * star.Frame, 20, 20);
			float sine = 1 + MathF.Sin(Main.GameUpdateCount * 0.02f) * 0.1f;

			Main.spriteBatch.Draw(_starTex.Value, position, src, color * star.Opacity * (sine - 0.2f), star.Rotation, src.Size() / 2f, star.Scale * sine, SpriteEffects.None, 0);
		}
	}

	public override void SaveWorldData(TagCompound tag) => tag["height"] = SurfaceHeight;
	public override void LoadWorldData(TagCompound tag) => SurfaceHeight = tag.GetInt("height");

	public override void NetSend(BinaryWriter writer) => writer.Write((short)SurfaceHeight);
	public override void NetReceive(BinaryReader reader) => SurfaceHeight = reader.ReadInt16();
}
