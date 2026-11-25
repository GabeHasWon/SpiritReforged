using SpiritReforged.Common.Visuals.RenderTargets;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;

namespace SpiritReforged.Content.Granite.TotalRecall;

[Autoload(Side = ModSide.Client)]
public class TotalRecallDistortionHandler : ModSystem
{
	/// <summary>
	/// These shader items should be updated in the class that uses it
	/// </summary>
	public class DistortionShaderItem
	{
		/// <summary> Must be increased constantly to preserve this item. </summary>
		public int timeActive;
		public float DistortionStrength { get; set; }
		public float ColorInterpolant { get; set; }
		public float Scale { get; set; }
		public Vector2 Position { get; set; }
	}

	private static readonly EasyTarget Target = new();
	public static readonly List<DistortionShaderItem> ShaderItems = [];
	public static void AddItem(DistortionShaderItem item) => ShaderItems.Add(item);
	public override void Load()
	{
		TargetSetup.DrawIntoRendertargets += DrawContent;

		if (!Main.dedServ)
		{
			Asset<Effect> shader = ModContent.Request<Effect>("SpiritReforged/Assets/Shaders/TotalRecallDistortion");
			Filters.Scene["SpiritReforged:TotalRecallDistortion"] = new Filter(new ScreenShaderData(shader, "ScreenPass"), EffectPriority.VeryHigh);
		}
	}

	// drawing a bloom map here for the input to our shader
	private static void DrawContent()
	{
		if (ShaderItems.Count > 0)
		{
			GraphicsDevice graphics = Main.graphics.GraphicsDevice;
			SpriteBatch spriteBatch = Main.spriteBatch;
			Texture2D bloom = AssetLoader.LoadedTextures["BloomNonPremult"].Value;

			graphics.SetRenderTarget(Target.Value);
			graphics.Clear(Color.Transparent);

			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

			foreach (DistortionShaderItem item in ShaderItems)
			{
				Color dataColor = new(1f, item.DistortionStrength, item.ColorInterpolant, 1f);

				float scale = item.Scale;

				spriteBatch.Draw(bloom, item.Position - Main.screenPosition, null, dataColor, 0f, bloom.Size() / 2f, scale, 0f, 0f);
			}

			spriteBatch.End();
			graphics.SetRenderTarget(null);
		}
	}

	public override void PostUpdateEverything()
	{
		if (Main.dedServ)
			return;

		if (ShaderItems.Count > 0 && Target.Value != null)
		{
			if (!Main.dedServ && !Filters.Scene["SpiritReforged:TotalRecallDistortion"].IsActive())
				Filters.Scene.Activate("SpiritReforged:TotalRecallDistortion");

			Filters.Scene["SpiritReforged:TotalRecallDistortion"].GetShader().UseImage(Target.Value);
			Filters.Scene["SpiritReforged:TotalRecallDistortion"].GetShader().UseImage(AssetLoader.LoadedTextures["swirlNoise"], 1);
			Filters.Scene["SpiritReforged:TotalRecallDistortion"].GetShader().UseIntensity(2f * Main.GameViewMatrix.Zoom.X);
			Filters.Scene["SpiritReforged:TotalRecallDistortion"].GetShader().UseColor(Color.LightCyan);
			Filters.Scene["SpiritReforged:TotalRecallDistortion"].GetShader().UseSecondaryColor(Color.MediumVioletRed);
		}
		else if (Filters.Scene["SpiritReforged:TotalRecallDistortion"].IsActive())
		{
			Filters.Scene["SpiritReforged:TotalRecallDistortion"].GetShader().UseImage(TextureAssets.Npc[0]);
			Filters.Scene.Deactivate("SpiritReforged:TotalRecallDistortion");
		}

		for (int i = ShaderItems.Count - 1; i >= 0; i--)
			if (--ShaderItems[i].timeActive <= 0)
				ShaderItems.RemoveAt(i); //Update activity
	}
}
