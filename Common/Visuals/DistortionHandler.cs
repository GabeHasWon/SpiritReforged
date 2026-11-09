using SpiritReforged.Common.Visuals.RenderTargets;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using static SpiritReforged.Content.Glyphs.Void.SingularityRenderSystem;

namespace SpiritReforged.Common.Visuals;
/// <summary>
/// This class serves as a central visual system for basic distortion effects, that can be tinted.
/// </summary>

[Autoload(Side = ModSide.Client)]
public class DistortionHandler : ModSystem
{
	/// <summary>
	/// These shader items should be updated in the class that uses it
	/// </summary>
	public class DistortionShaderItem
	{
		/// <summary> Must be increased constantly to preserve this item. </summary>
		public int timeActive;
		public Color tintColor;

		public float Progress { get; set; }
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
			Asset<Effect> shader = ModContent.Request<Effect>("SpiritReforged/Assets/Shaders/TintedDistortion");
			Filters.Scene["SpiritReforged:TintedDistortion"] = new Filter(new ScreenShaderData(shader, "ScreenPass"), EffectPriority.VeryHigh);
		}
	}

	// drawing a bloom map here for the input to our shader
	private static void DrawContent()
	{
		if (ShaderItems.Count > 0)
		{
			GraphicsDevice graphics = Main.graphics.GraphicsDevice;
			SpriteBatch spriteBatch = Main.spriteBatch;
			Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;

			graphics.SetRenderTarget(Target.Value);
			graphics.Clear(Color.Transparent);	

			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

			foreach (DistortionShaderItem item in ShaderItems)
			{
				float progress = item.Progress;

				Color tintColor = item.tintColor;

				// We will use the red channel of the bloom drawn to encode the progress of the distortion
				Color dataColor = new Color(progress, 1f, 1f, 1f);

				// pack our tint color into a byte to use in the shader
				// this is very lossy, but we can encode up to 512 colors this way
				byte packedColor = (byte)((tintColor.R << 5) | (tintColor.G << 2) | tintColor.B);

				// we will encode the packed tint color into the blue channel of our color
				dataColor.B = packedColor;

				spriteBatch.Draw(bloom, item.Position - Main.screenPosition, null, dataColor, 0f, bloom.Size() / 2f, item.Scale, 0f, 0f);
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
			if (!Main.dedServ && !Filters.Scene["SpiritReforged:TintedDistortion"].IsActive())
				Filters.Scene.Activate("SpiritReforged:TintedDistortion");

			Filters.Scene["SpiritReforged:TintedDistortion"].GetShader().UseImage(Target.Value);
			Filters.Scene["SpiritReforged:TintedDistortion"].GetShader().UseImage(AssetLoader.LoadedTextures["swirlNoise"], 1);
			Filters.Scene["SpiritReforged:TintedDistortion"].GetShader().UseIntensity(2f * Main.GameViewMatrix.Zoom.X);
		}
		else if (Filters.Scene["SpiritReforged:TintedDistortion"].IsActive())
		{
			Filters.Scene["SpiritReforged:TintedDistortion"].GetShader().UseImage(TextureAssets.Npc[0]);
			Filters.Scene.Deactivate("SpiritReforged:TintedDistortion");
		}

		for (int i = ShaderItems.Count - 1; i >= 0; i--)
		{
			if (--ShaderItems[i].timeActive <= 0)
				ShaderItems.RemoveAt(i); //Update activity
		}
	}
}
