using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.RenderTargets;
using Terraria.Graphics.Effects;

namespace SpiritReforged.Content.Glyphs.Void;

[Autoload(Side = ModSide.Client)]
public class SingularityRenderSystem : ModSystem
{
	public class ShaderItem
	{
		/// <summary> Must be increased constantly to preserve this item. </summary>
		public int timeActive;

		public float Progress { get; set; }
		public float Intensity { get; set; }
		public float Scale { get; set; }
		public Vector2 Position { get; set; }
	}

	private static readonly EasyTarget SingularityTarget = new();
	public static readonly List<ShaderItem> ShaderItems = [];

	public override void Load() => TargetSetup.DrawIntoRendertargets += DrawContent;

	// drawing a bloom map here for the input to our shader
	private static void DrawContent()
	{
		if (ShaderItems.Count > 0)
		{
			GraphicsDevice graphics = Main.graphics.GraphicsDevice;
			SpriteBatch spriteBatch = Main.spriteBatch;
			Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;

			graphics.SetRenderTarget(SingularityTarget.Value);
			graphics.Clear(Color.Transparent);

			spriteBatch.Begin(SpriteSortMode.Deferred, DrawHelpers.AdditiveNoAlpha, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

			foreach (ShaderItem item in ShaderItems)
			{
				float progress = item.Progress;
				float intensity = item.Intensity;

				// Shader uses the G channel for the progress of the black hole.
				// Shader uses the B channel for the stacks of the black hole (increases singularity intensity)
				Color dataColor = new(1f, progress, intensity, 1f);

				float sizeInterpolant = (progress < 0.5f) ? progress / 0.5f : 1f - (progress - 0.5f) / 0.5f;
				float scale = item.Scale * sizeInterpolant;

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

		if (ShaderItems.Count > 0 && SingularityTarget.Value != null)
		{
			if (!Main.dedServ && !Filters.Scene["SpiritReforged:VoidGlyphSingularity"].IsActive())
				Filters.Scene.Activate("SpiritReforged:VoidGlyphSingularity");

			Filters.Scene["SpiritReforged:VoidGlyphSingularity"].GetShader().UseImage(SingularityTarget.Value);
			Filters.Scene["SpiritReforged:VoidGlyphSingularity"].GetShader().UseImage(AssetLoader.LoadedTextures["swirlNoise"], 1);
			Filters.Scene["SpiritReforged:VoidGlyphSingularity"].GetShader().UseIntensity(2f * Main.GameViewMatrix.Zoom.X);
		}
		else if (Filters.Scene["SpiritReforged:VoidGlyphSingularity"].IsActive())
		{
			Filters.Scene["SpiritReforged:VoidGlyphSingularity"].GetShader().UseImage(TextureAssets.Npc[0]);
			Filters.Scene.Deactivate("SpiritReforged:VoidGlyphSingularity");
		}

		for (int i = ShaderItems.Count - 1; i >= 0; i--)
		{
			if (--ShaderItems[i].timeActive <= 0)
				ShaderItems.RemoveAt(i); //Update activity
		}
	}
}