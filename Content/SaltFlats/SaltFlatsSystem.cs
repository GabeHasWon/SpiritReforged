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
	private static Asset<Texture2D> _galaxyTex = null;

	public override void Load()
	{
		On_Main.DrawStarsInBackground += DrawSaltFlatsBG;

		if (!Main.dedServ)
		{
			_starTex = ModContent.Request<Texture2D>("SpiritReforged/Assets/Textures/Backgrounds/SaltFlatStar");
			_galaxyTex = ModContent.Request<Texture2D>("SpiritReforged/Assets/Textures/Backgrounds/SaltFlatGalaxy");
		}
	}

	private void DrawSaltFlatsBG(On_Main.orig_DrawStarsInBackground orig, Main self, Main.SceneArea sceneArea, bool artificial)
	{
		orig(self, sceneArea, artificial);

		if (Main.gameMenu)
			return;

		float opacity = 1f;

		if (Main.dayTime)
			return;
		else
		{
			if (Main.time < 1600)
				opacity = (float)(Main.time / 1600f);
			else if (Main.time > Main.nightLength - 1600)
				opacity = (float)Utils.GetLerpValue(Main.nightLength, Main.nightLength - 1600, Main.time);
		}

		_starOpacity = MathHelper.Lerp(_starOpacity, Main.LocalPlayer.InModBiome<SaltBiome>() ? 1 : 0, 0.05f);

		if (_starOpacity < 0.01f)
			return;

		float finalOpacity = _starOpacity * opacity;

		if (_stars.Count == 0)
		{
			for (int i = 0; i < 260; ++i)
			{
				Vector2 pos = new(Main.rand.Next(Main.screenWidth + 1), Main.rand.Next(Main.screenHeight + 1));
				int frame = Main.rand.Next(6);
				_stars.Add(new Star(frame, Main.rand.NextFloat(MathHelper.TwoPi), pos, Main.rand.NextFloat(0.6f, 1f), Main.rand.NextFloat(0.5f, 1.2f)));
			}
		}

		Main.spriteBatch.Draw(_galaxyTex.Value, new Vector2(-300), null, Color.White with { A = 0 } * finalOpacity * 0.3f, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);

		foreach (Star star in _stars)
		{
			Vector2 position = star.Position / 1200f * new Vector2(sceneArea.totalWidth, sceneArea.totalHeight)
				+ new Vector2(0f, sceneArea.bgTopY) + sceneArea.SceneLocalScreenPositionOffset;
			var color = Color.Lerp(Color.White, Color.DarkGray, Utils.GetLerpValue(2f, 0.4f, star.Scale, true)) * finalOpacity;
			Rectangle src = new(0, 44 * star.Frame, 40, 40);
			float sine = 1 + MathF.Sin(Main.GameUpdateCount * 0.02f) * 0.1f;

			Main.spriteBatch.Draw(_starTex.Value, position, src, color * star.Opacity * (sine - 0.2f), star.Rotation, src.Size() / 2f, star.Scale * sine, SpriteEffects.None, 0);
		}
	}

	public override void SaveWorldData(TagCompound tag) => tag["height"] = SurfaceHeight;
	public override void LoadWorldData(TagCompound tag) => SurfaceHeight = tag.GetInt("height");

	public override void NetSend(BinaryWriter writer) => writer.Write((short)SurfaceHeight);
	public override void NetReceive(BinaryReader reader) => SurfaceHeight = reader.ReadInt16();
}
