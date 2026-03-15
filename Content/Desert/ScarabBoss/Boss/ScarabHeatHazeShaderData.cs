using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.Skies;
using System.Linq;
using Terraria.DataStructures;
using Terraria.Graphics;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss
{
    public class ScarabHeatHazeShaderData : ScreenShaderData
	{
		public static float heatHazeOpacity = 0f;

		public static float HeatHazeTargetOpacity
		{
			get => _heatHazeTargetOpacity;
			set => _heatHazeTargetOpacity = Math.Max(_heatHazeTargetOpacity, value);
		}
		private static float _heatHazeTargetOpacity = 0f;

		public static float HeatHazeTargetIntensity
		{
			get => _heatHazeTargetIntensity;
			set => _heatHazeTargetIntensity = Math.Max(_heatHazeTargetIntensity, value);
		}
		private static float _heatHazeTargetIntensity = 0f;

		public static float HeatHazeIntensity
		{
			get => _heatHazeIntensity;
			set => _heatHazeIntensity = Math.Max(_heatHazeIntensity, value);
		}
		private static float _heatHazeIntensity;

		private static Filter myFilter;

		public ScarabHeatHazeShaderData(Asset<Effect> shader, string passName)
            : base(shader, passName)
        {
        }

		public static void Load()
		{
			myFilter = new Filter(
				new ScarabHeatHazeShaderData(ModContent.Request<Effect>("SpiritReforged/Assets/Shaders/ScarabHeatHaze"), "ScarabHeatHazePass")
				.UseImage(DrawHelpers.RequestLocal<Scarabeus>("TonemapGradient", false), 0, SamplerState.LinearClamp) //Gradient map texture that color maps the screen to be bluer
				.UseImage(ModContent.Request<Texture2D>("SpiritReforged/Assets/Textures/DisplaceNoise"), 1, SamplerState.LinearWrap) //Distortion map
				, EffectPriority.High);

			Filters.Scene["SpiritReforged:ScarabHeatHaze"] = myFilter;
			SpiritReforgedSystem.PostUpdateEverythingEvent += UpdateShaderParameters;
			On_Main.DrawSunAndMoon += DrawSunHaze;
		}

		private static void DrawSunHaze(On_Main.orig_DrawSunAndMoon orig, Main self, Main.SceneArea sceneArea, Color moonColor, Color sunColor, float tempMushroomInfluence)
		{
			orig(self, sceneArea, moonColor, sunColor, tempMushroomInfluence);

			if (Main.remixWorld || !Main.dayTime || heatHazeOpacity <= 0.01f)
				return;

			Texture2D sunTex = TextureAssets.Sun.Value;
			Vector2 position = SunMoonILEdit.SunDrawData.Position;
			Main.spriteBatch.Draw(sunTex, position, null, (Color.White with { A = 0 }) * heatHazeOpacity, 0f, sunTex.Size() / 2f, SunMoonILEdit.SunDrawData.Scale * 1f, 0, 0);
			Main.spriteBatch.Draw(sunTex, position, null, (Color.White with { A = 0 }) * heatHazeOpacity * 0.2f, 0f, sunTex.Size() / 2f, SunMoonILEdit.SunDrawData.Scale * 1.4f, 0, 0);
			Main.spriteBatch.Draw(sunTex, position, null, (Color.White with { A = 0 }) * heatHazeOpacity, 0f, sunTex.Size() / 2f, SunMoonILEdit.SunDrawData.Scale * 0.7f, 0, 0);
		}

		private static void UpdateShaderParameters()
		{
			bool shouldShaderBeActive = HeatHazeTargetOpacity > 0;

			//Make the shader fade in and out
			if (shouldShaderBeActive)
			{
				heatHazeOpacity += 0.08f;
				if (heatHazeOpacity > HeatHazeTargetOpacity)
					heatHazeOpacity = HeatHazeTargetOpacity;
			}
			else
			{
				heatHazeOpacity -= 0.02f;
				if (heatHazeOpacity < 0f)
					heatHazeOpacity = 0f;
			}

			if (HeatHazeIntensity > 0.01f)
				heatHazeOpacity = Math.Min(HeatHazeIntensity, heatHazeOpacity);

			if (shouldShaderBeActive && !myFilter.IsActive())
				Filters.Scene.Activate("SpiritReforged:ScarabHeatHaze");
			else if (!shouldShaderBeActive && myFilter.IsActive())
				Filters.Scene.Deactivate("SpiritReforged:ScarabHeatHaze");

			_heatHazeIntensity = MathHelper.Lerp(_heatHazeIntensity, _heatHazeTargetIntensity, 0.04f);
			_heatHazeTargetIntensity = 0;
			_heatHazeTargetOpacity = 0;
		}

		public override void Update(GameTime gameTime)
		{
			UseOpacity(heatHazeOpacity);
			UseIntensity(HeatHazeIntensity);

            //Taken from sepia dst screenshader
            float screenPositionInTiles = (Main.screenPosition.Y + Main.screenHeight / 2f) / 16f;
            //Calculates how much on the surface we are
            float surfaceValue = 1f - Utils.SmoothStep((float)Main.worldSurface, (float)Main.worldSurface + 30f, screenPositionInTiles);
            Vector2 midnightDirection = Utils.GetDayTimeAsDirectionIn24HClock(0f);

            //Use the dot product between clock hand directions to find if its night or not and have a smooth transition
            //Then multiply the "surface value" by it so that during the night , surface is 0 as if we were undeground
            surfaceValue *= 1 - Utils.SmoothStep(0.2f, 0.4f, Vector2.Dot(midnightDirection, Utils.GetDayTimeAsDirectionIn24HClock()));

            //Lower opacity when on surface at day
            UseProgress(1 - surfaceValue * 0.7f);
        }

        public override void Apply()
        {
			base.Apply();
        }
    }
}
