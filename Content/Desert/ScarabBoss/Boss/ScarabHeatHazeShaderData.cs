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
		public static float HeatHazeIntensity = 0f;
		public static float HeatHazeOpacity = 0f;
		public static float HeatHazeTargetOpacity = 0f;
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

			if (Main.remixWorld || !Main.dayTime || HeatHazeOpacity <= 0.01f)
				return;

			Texture2D sunTex = TextureAssets.Sun.Value;
			Vector2 position = SunMoonILEdit.SunDrawData.Position;
			Main.spriteBatch.Draw(sunTex, position, null, (Color.White with { A = 0 }) * HeatHazeOpacity, 0f, sunTex.Size() / 2f, SunMoonILEdit.SunDrawData.Scale * 1f, 0, 0);
			Main.spriteBatch.Draw(sunTex, position, null, (Color.White with { A = 0 }) * HeatHazeOpacity * 0.2f, 0f, sunTex.Size() / 2f, SunMoonILEdit.SunDrawData.Scale * 1.4f, 0, 0);
			Main.spriteBatch.Draw(sunTex, position, null, (Color.White with { A = 0 }) * HeatHazeOpacity, 0f, sunTex.Size() / 2f, SunMoonILEdit.SunDrawData.Scale * 0.7f, 0, 0);
		}

		private static void UpdateShaderParameters()
		{
			bool shouldShaderBeActive = HeatHazeTargetOpacity > 0;
			HeatHazeTargetOpacity = 0;

			//Make the shader fade in and out
			if (shouldShaderBeActive)
			{
				HeatHazeOpacity += 0.08f;
				if (HeatHazeOpacity > HeatHazeTargetOpacity)
					HeatHazeOpacity = HeatHazeTargetOpacity;
			}
			else
			{
				HeatHazeOpacity -= 0.02f;
				if (HeatHazeOpacity < 0f)
					HeatHazeOpacity = 0f;
			}

			if (shouldShaderBeActive && !myFilter.IsActive())
				Filters.Scene.Activate("SpiritReforged:ScarabHeatHaze");
			else if (!shouldShaderBeActive && myFilter.IsActive())
				Filters.Scene.Deactivate("SpiritReforged:ScarabHeatHaze");

			HeatHazeIntensity = MathHelper.Lerp(HeatHazeIntensity, 0f, 0.04f);
		}

		public override void Update(GameTime gameTime)
		{
			UseOpacity(HeatHazeOpacity);
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
