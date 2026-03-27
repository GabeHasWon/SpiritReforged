using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
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
		On_Cloud.cloudColor += EditCloudColor;
	}

	private Color EditCloudColor(On_Cloud.orig_cloudColor orig, Cloud self, Color bgColor)
	{
		Color output = orig(self, bgColor);
		float cloudOpacity = self.scale * self.Alpha;
		if (cloudOpacity > 1f)
			cloudOpacity = 1f;
		cloudOpacity *= 1f - bloodMoonStrength * 0.6f;

		Color tintTarget = GetBackgroundTintColor(out float tintStrength);
		tintStrength += 0.4f * bloodMoonStrength;
		tintTarget *= cloudOpacity;
		return Color.Lerp(output, tintTarget, Math.Min(1, tintStrength * 1.5f));
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

	public static void UpdateSkyStrengthVariable(ref float variable, bool toggled)
	{
		variable = MathHelper.Lerp(variable, toggled ? 1f : 0f, 0.05f);
		if (!toggled && variable < 0.01f)
			variable = 0;
		else if (toggled && variable > 0.99f)
			variable = 1;
	}

	public static void DrawSaltFlatsBackground()
	{
		_dirtyBgTintVariables = true;
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

		UpdateSkyStrengthVariable(ref eclipseStrength, Main.eclipse);
		UpdateSkyStrengthVariable(ref bloodMoonStrength, Main.bloodMoon || Main.SceneMetrics.BloodMoonMonolith || Main.LocalPlayer.bloodMoonMonolithShader);
		UpdateSkyStrengthVariable(ref snowMoonStrength, Main.snowMoon);
		UpdateSkyStrengthVariable(ref pumpkinMoonStrength, Main.pumpkinMoon);

		float nightFade = 1f;
		if (Main.time < 1600)
			nightFade = (float)(Main.time / 1600f);

		if (Main.eclipse && Main.dayTime)
		{
			if (Main.time > Main.dayLength - 1600)
				nightFade = (float)Utils.GetLerpValue(Main.dayLength, Main.dayLength - 1600, Main.time);
		}
		else if (Main.time > Main.nightLength - 1600)
		{
			nightFade = (float)Utils.GetLerpValue(Main.nightLength, Main.nightLength - 1600, Main.time);
		}

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
			var projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);

			bgShader.Parameters["WorldViewProjection"].SetValue(projection);
			bgShader.Parameters["matrixZoom"].SetValue(Main.BackgroundViewMatrix.Zoom);
			SetSkyColor(bgShader);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, null, null, bgShader);

			Vector2 galaxyScale = new Vector2(Math.Max(Math.Max(Main.screenWidth / 1920, 1), Main.screenWidth / (float)(_galaxyTex.Value.Width)), 1);
			Main.spriteBatch.Draw(_galaxyTex.Value, new Vector2(-300), null, Color.White * nightSkyOpacity, 0, Vector2.Zero, galaxyScale, SpriteEffects.None, 0);

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
		baseGradientColor = Vector3.Lerp(baseGradientColor, new Vector3(0.1f, 0.06f, 0.0f), bloodMoonStrength);

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

		//float nightGlowStrength = 0.3f + Main.bgAlphaFarBackLayer[ModContent.GetInstance<SaltBGStyle>().Slot] * 0.6f; //Salt bg intensifies the glow during the night
		backgroundColor = Color.Lerp(backgroundColor, colorTarget, nightGlowOpacity * 0.3f);
		tileColor = Color.Lerp(tileColor, colorTarget, nightGlowOpacity * 0.3f);
	}

	public static void ModifySkyGradientColors(ref Color topColor, ref Color middleColor, ref Color bottomColor)
	{
		if (bloodMoonStrength > 0)
		{
			topColor = Color.Lerp(topColor, Color.Black, bloodMoonStrength);
			middleColor = Color.Lerp(middleColor, new Color(0.8f, 0.4f, 0.2f), bloodMoonStrength);
		}

		if (pumpkinMoonStrength > 0)
		{
			topColor = Color.Lerp(topColor, Color.Black, pumpkinMoonStrength);
			middleColor = Color.Lerp(middleColor, new Color(0.8f, 0.6f, 0.1f), pumpkinMoonStrength);
		}

		//Darken the sky entirely when its the eclipse, the only tint on the sky will come from the SaltFlatsSky shader drawing behind the sun, drawing a black to red gradient
		if (eclipseStrength > 0)
		{
			topColor = Color.Lerp(topColor, Color.Black, eclipseStrength);
			middleColor = Color.Lerp(middleColor, Color.Black, eclipseStrength);
			bottomColor = Color.Lerp(bottomColor, Color.Black, eclipseStrength);
		}
	}

	private static bool _dirtyBgTintVariables = true;
	private static float _bgTintStrength;
	private static Color _bgTintColorTarget;
	public static Color GetBackgroundTintColor(out float tintStrength)
	{
		if (!_dirtyBgTintVariables)
		{
			tintStrength = _bgTintStrength;
			return _bgTintColorTarget;
		}

		_dirtyBgTintVariables = false;
		tintStrength = 0f;

		if (nightSkyOpacity == 0)
			return Color.White;

		tintStrength = MathF.Pow(nightSkyOpacity, 2f) * 0.3f;
		tintStrength *= 1 - eclipseStrength;
		_bgTintStrength = tintStrength;

		_bgTintColorTarget = new Color(80, 120, 255);
		_bgTintColorTarget = Color.Lerp(_bgTintColorTarget, new Color(0.7f, 0.4f, 0.2f), bloodMoonStrength);
		_bgTintColorTarget = Color.Lerp(_bgTintColorTarget, new Color(0.1f, 0.3f, 0.4f), snowMoonStrength);
		_bgTintColorTarget = Color.Lerp(_bgTintColorTarget, new Color(0.7f, 0.15f, 0.1f), pumpkinMoonStrength);
		return _bgTintColorTarget;
	}

	public override void SaveWorldData(TagCompound tag) => tag["height"] = SurfaceHeight;
	public override void LoadWorldData(TagCompound tag) => SurfaceHeight = tag.GetInt("height");

	public override void NetSend(BinaryWriter writer) => writer.Write((short)SurfaceHeight);
	public override void NetReceive(BinaryReader reader) => SurfaceHeight = reader.ReadInt16();
}