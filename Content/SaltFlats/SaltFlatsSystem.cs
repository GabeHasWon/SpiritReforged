using Microsoft.Xna.Framework.Graphics;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.SaltFlats.Biome;
using System.IO;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.SaltFlats;

internal class SaltFlatsSystem : ModSystem
{
	private readonly record struct Star(int Frame, float Rotation, Vector2 Position, float Opacity, float Scale)
	{
		public readonly void Draw(SpriteBatch spriteBatch, Vector2 scene, float opacity)
		{
			Vector2 position = Position / 1200f * scene;
			Color color = Color.Lerp(Color.White, Color.DarkGray, Utils.GetLerpValue(2f, 0.4f, Scale, true)) * opacity;
			Rectangle src = new(0, 44 * Frame, 40, 40);
			float sine = 1 + MathF.Sin(Main.GameUpdateCount * 0.02f) * 0.1f;

			spriteBatch.Draw(_starTex.Value, position, src, color.Additive() * Opacity * (sine - 0.2f), Rotation, src.Size() / 2f, Scale * sine, SpriteEffects.None, 0);
		}
	}

	[WorldBound]
	internal static int SurfaceHeight;

	[WorldBound]
	private static readonly List<Star> _stars = [];

	public static float saltFlatsOpacity = 0f;
	public static float nightSkyOpacity = 0f;
	public static float nightGlowOpacity = 0f;

	public static float snowMoonStrength = 0f;
	public static float bloodMoonStrength = 0f;
	public static float pumpkinMoonStrength = 0f;
	public static float eclipseStrength = 0f;

	private static readonly Asset<Texture2D> _starTex = ModContent.Request<Texture2D>("SpiritReforged/Assets/Textures/Backgrounds/SaltFlatStar");
	private static readonly Asset<Texture2D> _galaxyTex = ModContent.Request<Texture2D>("SpiritReforged/Assets/Textures/Backgrounds/SaltFlatGalaxy");

	private static Main.SceneArea cachedArea;

	public override void Load()
	{
		On_Main.DrawStarsInBackground += CacheSceneArea;
		On_Main.DrawSunAndMoon += DrawSaltFlatAtmosphere;
		On_Main.SetBackColor += EditMoonColor;
	}

	private void DrawSaltFlatAtmosphere(On_Main.orig_DrawSunAndMoon orig, Main self, Main.SceneArea sceneArea, Color moonColor, Color sunColor, float tempMushroomInfluence)
	{
		orig(self, sceneArea, moonColor, sunColor, tempMushroomInfluence);
		DrawSaltFlatsBackground();
	}

	private static void CacheSceneArea(On_Main.orig_DrawStarsInBackground orig, Main self, Main.SceneArea sceneArea, bool artificial)
	{
		orig(self, sceneArea, artificial);
		cachedArea = sceneArea;
	}

	public static void DrawSaltFlatsBackground()
	{
		bool playerInSaltBiome;
		if (!Main.gameMenu)
			playerInSaltBiome = Main.LocalPlayer.InModBiome<SaltBiome>();
		else
			playerInSaltBiome = MenuLoader.CurrentMenu is SaltMenuTheme;

		saltFlatsOpacity = MathHelper.Lerp(saltFlatsOpacity, playerInSaltBiome ? 1 : 0, playerInSaltBiome ? 0.02f : 0.06f);
		if (!playerInSaltBiome && saltFlatsOpacity < 0.01f)
			saltFlatsOpacity = 0f;

		if (Main.dayTime && !Main.eclipse)
		{
			nightSkyOpacity = 0f;
			nightGlowOpacity = 0f;
			bloodMoonStrength = 0f;
			snowMoonStrength = 0f;
			pumpkinMoonStrength = 0f;
			eclipseStrength = 0f;
			return;
		}

		eclipseStrength = MathHelper.Lerp(eclipseStrength, Main.eclipse ? 1f : 0f, 0.05f);
		bloodMoonStrength = MathHelper.Lerp(bloodMoonStrength, Main.bloodMoon ? 1f : 0f, 0.05f);
		snowMoonStrength = MathHelper.Lerp(snowMoonStrength, Main.snowMoon ? 1f : 0f, 0.05f);
		pumpkinMoonStrength = MathHelper.Lerp(pumpkinMoonStrength, Main.pumpkinMoon ? 1f : 0f, 0.05f);

		float nightFade = 1f;
		if (Main.time < 1600)
			nightFade = (float)(Main.time / 1600f);
		else if (Main.time > Main.nightLength - 1600)
			nightFade = (float)Utils.GetLerpValue(Main.nightLength, Main.nightLength - 1600, Main.time);

		nightSkyOpacity = saltFlatsOpacity * nightFade;
		nightGlowOpacity = saltFlatsOpacity * nightFade;

		//Scale the opacity by moonphase, except in the gamemenu... because uhh the spirit logo shines bright?
		if (!Main.gameMenu && !Main.eclipse && snowMoonStrength == 0 && pumpkinMoonStrength == 0)
		{
			switch (Main.GetMoonPhase())
			{
				case MoonPhase.Empty:
					nightSkyOpacity *= 0.15f;
					nightGlowOpacity *= 0.4f;
					break;
				case MoonPhase.QuarterAtLeft:
				case MoonPhase.QuarterAtRight:
					nightSkyOpacity *= 0.25f;
					nightGlowOpacity *= 0.76f;
					break;
				case MoonPhase.HalfAtLeft:
				case MoonPhase.HalfAtRight:
					nightSkyOpacity *= 0.35f;
					nightGlowOpacity *= 0.9f;
					break;
				case MoonPhase.ThreeQuartersAtLeft:
				case MoonPhase.ThreeQuartersAtRight:
					nightSkyOpacity *= 0.6f;
					nightGlowOpacity *= 0.95f;
					break;
			}
		}

		if (nightSkyOpacity > 0.01f)
		{
			if (_stars.Count == 0) //Initialize
			{
				for (int i = 0; i < 260; ++i)
				{
					Vector2 pos = new(Main.rand.Next(Main.screenWidth + 1), Main.rand.Next(Main.screenHeight + 1));
					int frame = Main.rand.Next(6);
					_stars.Add(new Star(frame, Main.rand.NextFloat(MathHelper.TwoPi), pos, Main.rand.NextFloat(0.6f, 1f), Main.rand.NextFloat(0.2f, 1f)));
				}
			}

			Effect bgShader = AssetLoader.LoadedShaders["SaltFlatsSky"].Value;
			Matrix projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);

			bgShader.Parameters["texColorUVLerper"].SetValue(0f);
			bgShader.Parameters["WorldViewProjection"].SetValue(projection);
			SetSkyColor(bgShader);
			bgShader.Parameters["viewMatrix"].SetValue(projection);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, null, null, bgShader);

			Main.spriteBatch.Draw(_galaxyTex.Value, new Vector2(-300), null, Color.White * nightSkyOpacity, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, null, null, null);

			//We dont draw extra stars during eclipses
			if (!Main.eclipse)
			{
				foreach (Star star in _stars)
				{
					Vector2 sceneOffset = new Vector2(cachedArea.totalWidth, cachedArea.totalHeight) + new Vector2(0f, cachedArea.bgTopY) + cachedArea.SceneLocalScreenPositionOffset;
					star.Draw(Main.spriteBatch, sceneOffset, nightSkyOpacity);
				}
			}
		}
	}

	public static void SetSkyColor(Effect bgShader)
	{
		Vector3 baseSkyColor = new Vector3(0f, 0f, 0.4f);
		Vector3 baseGradientColor = new Vector3(0.2f, 0.3f, 0f);

		baseSkyColor = Vector3.Lerp(baseSkyColor, new Vector3(0.4f, 0f, 0f), bloodMoonStrength);
		baseGradientColor = Vector3.Lerp(baseGradientColor, new Vector3(0.1f, 0.1f, 0.1f), bloodMoonStrength);

		baseSkyColor = Vector3.Lerp(baseSkyColor, new Vector3(0.1f, 0.2f, 0.2f), snowMoonStrength);
		baseGradientColor = Vector3.Lerp(baseGradientColor, new Vector3(0.1f, 0.1f, 0.15f), snowMoonStrength);

		baseSkyColor = Vector3.Lerp(baseSkyColor, new Vector3(0.4f, 0.15f, 0f), pumpkinMoonStrength);
		baseGradientColor = Vector3.Lerp(baseGradientColor, new Vector3(0.1f, 0f, 0f), pumpkinMoonStrength);

		baseSkyColor = Vector3.Lerp(baseSkyColor, new Vector3(0.3f, 0f, 0f), eclipseStrength);
		baseGradientColor = Vector3.Lerp(baseGradientColor, new Vector3(-0.35f, 0f, 0f), eclipseStrength);

		bgShader.Parameters["baseColor"].SetValue(baseSkyColor);
		bgShader.Parameters["gradientColor"].SetValue(baseGradientColor);
	}

	private void EditMoonColor(On_Main.orig_SetBackColor orig, Main.InfoToSetBackColor info, out Color sunColor, out Color moonColor)
	{
		orig(info, out sunColor, out moonColor);

		if (nightSkyOpacity > 0f)
		{
			float moonFade = 1f;
			float fadeTicks = 7000;

			if (Main.time < fadeTicks)
				moonFade = (float)(Main.time / fadeTicks);
			else if (Main.time > Main.nightLength - fadeTicks)
				moonFade = (float)Utils.GetLerpValue(Main.nightLength, Main.nightLength - fadeTicks, Main.time);

			moonFade = 1 - moonFade;

			moonColor = Color.Lerp(moonColor, Color.Black, nightSkyOpacity * (0.1f + 0.3f * moonFade));
		}
	}

	public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
	{
		Color colorTarget = new Color(0, 0.3f, 1f);
		colorTarget = Color.Lerp(colorTarget, new Color(0.1f, 0.3f, 0.4f), snowMoonStrength);
		colorTarget = Color.Lerp(colorTarget, new Color(0.7f, 0.15f, 0.1f), pumpkinMoonStrength);

		colorTarget = Color.Lerp(colorTarget, new Color(0.2f, 0.04f, 0.1f), eclipseStrength);

		backgroundColor = Color.Lerp(backgroundColor, colorTarget, nightGlowOpacity * 0.3f);
		tileColor = Color.Lerp(tileColor, colorTarget, nightGlowOpacity * 0.3f);
	}

	public override void SaveWorldData(TagCompound tag) => tag["height"] = SurfaceHeight;
	public override void LoadWorldData(TagCompound tag) => SurfaceHeight = tag.GetInt("height");

	public override void NetSend(BinaryWriter writer) => writer.Write((short)SurfaceHeight);
	public override void NetReceive(BinaryReader reader) => SurfaceHeight = reader.ReadInt16();
}