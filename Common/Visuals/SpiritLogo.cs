using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.PrimitiveRendering;
using System.IO;

namespace SpiritReforged.Common.Visuals;

public class SpiritLogo : ILoadable
{
	//Shader assets
	private static Effect logoShader;
	private static Asset<Texture2D> sTexture;
	private static Asset<Texture2D> sOutlineTexture; //The colored inner outline
	private static Asset<Texture2D> piritTexture;
	private static Asset<Texture2D> piritOutlineTexture;
	private static Asset<Texture2D> reforgedTexture;
	private static Asset<Texture2D> noiseTrail; //Triple trail texture that gets twisted into a vortex for the noise overlay on the letters

	private static Asset<Texture2D> pirtTexture;
	private static Asset<Texture2D> pirtOutlineTexture;
	private static RenderTarget2D debug_captureTarget;

	private static float logoTime = 0f;
	private static float clickWobbleTimer = 0f;
	private static bool SpirtMod = false;
	private static bool debugDrawQueued = false;

	private static void LoadAssets() 
	{
		logoShader = AssetLoader.LoadedShaders["SpiritLogo"];

		sTexture = AssetLoader.LoadedTextures["ModLogo/LogoS"];
		sOutlineTexture = AssetLoader.LoadedTextures["ModLogo/LogoSOutline"];
		piritTexture = AssetLoader.LoadedTextures["ModLogo/LogoPirit"];
		piritOutlineTexture = AssetLoader.LoadedTextures["ModLogo/LogoPiritOutline"];
		reforgedTexture = AssetLoader.LoadedTextures["ModLogo/LogoReforged"];
		noiseTrail = AssetLoader.LoadedTextures["ModLogo/TrailTri"];

		pirtTexture = AssetLoader.LoadedTextures["ModLogo/LogoPirt"];
		pirtOutlineTexture = AssetLoader.LoadedTextures["ModLogo/LogoPirtOutline"];
	}

	/// <summary>
	/// Resets the wobble timer on click, and re-rolls the dice for the 1/500 chance to display spirt mod
	/// </summary>
	public static void Reset()
	{
		clickWobbleTimer = 0f;
		SpirtMod = Main.rand.NextBool(500); // Spirt mod
	}

	public static void Update(float deltaTime)
	{
		float logoTimeMultiplier = 1f;

		if (clickWobbleTimer > 0f)
		{
			clickWobbleTimer -= deltaTime;
			if (clickWobbleTimer < 0f)
				clickWobbleTimer = 0f;
			logoTimeMultiplier += clickWobbleTimer * 8f;
		}

		logoTime += deltaTime * logoTimeMultiplier;
	}

	/// <summary>
	/// The "default" palette for spirit, with a pearlescent gradient of blue towards pink
	/// </summary>
	public static void PearlescentBluePalette(out Color outlineColor, out Color underlineColor, out Color shadowColor, //General colors
		out Color sFillColor, out Color sGradientTopColor, out Color sGradientBottomColor, out Color sOutlineGlowColor, //S colors
		out Color fillColorBase, out Color fillColorSecondary, out Color piritFillGlowColor, out Color piritOutlineBaseColor, out Color innerOutlineGlowColor, //Pirit colors
		out Color reforgedColorLeft, out Color reforgedColorRight) //Reforged colors
	{
		outlineColor = new Color(45, 87, 255);				//Indigo outline
		underlineColor = Color.Black;
		shadowColor = new Color(0f, 0f, 0f, 0.2f);

		sFillColor = new Color(255, 255, 255);				//Pure white S
		sGradientTopColor = new Color(98, 186, 255);		//Desatured blue
		sGradientBottomColor = new Color(255, 186, 230);    //Desaturated pink
		sOutlineGlowColor = new Color(62, 164, 255);		//Lighter azure blue outline

		fillColorBase = new Color(139, 230, 255);			//Azure blue	
		fillColorSecondary = new Color(221, 190, 208);		//Pinkish gray towards the right
		piritFillGlowColor = new Color(76, 128, 76);		//Glows a dark green, which combines nicely with the colors of the fill
		piritOutlineBaseColor = new Color(192, 253, 255);	//Light blue outlines for the letters
		innerOutlineGlowColor = new Color(98, 38, 0);		//Glows a dark red

		reforgedColorLeft = new Color(40, 110, 255);		//Gradient from blue to pink
		reforgedColorRight = new Color(108, 255, 111);
	}

	// So many fvcking colors
	public delegate void GetSpiritPaletteDelegate(
		out Color outlineColor, out Color underlineColor, out Color shadowColor, //General colors
		out Color sFillColor, out Color sGradientTopColor, out Color sGradientBottomColor, out Color sOutlineGlowColor, //S colors
		out Color fillColorBase, out Color fillColorSecondary, out Color piritFillGlowColor, out Color piritOutlineBaseColor, out Color innerOutlineGlowColor, //Pirit colors
		out Color reforgedColorLeft, out Color reforgedColorRight); //Reforged colors

	//This code is so fucking fugly
	private static void FillInColorParameters(Effect effect, GetSpiritPaletteDelegate palette, float paletteLerper = 0f, GetSpiritPaletteDelegate altPalette = null)
	{
		palette(out Color outlineColor, out Color underlineColor, out Color shadowColor, //General colors
		out Color sFillColor, out Color sGradientTopColor, out Color sGradientBottomColor, out Color sOutlineGlowColor, //S colors
		out Color fillColorBase, out Color fillColorSecondary, out Color piritFillGlowColor, out Color piritOutlineBaseColor, out Color innerOutlineGlowColor, //Pirit colors
		out Color reforgedColorLeft, out Color reforgedColorRight); //Reforged colors

		if (paletteLerper > 0f && altPalette != null)
		{
			altPalette(out Color outlineColor2, out Color underlineColor2, out Color shadowColor2, //General colors
		out Color sFillColor2, out Color sGradientTopColor2, out Color sGradientBottomColor2, out Color sOutlineGlowColor2, //S colors
		out Color fillColorBase2, out Color fillColorSecondary2, out Color piritFillGlowColor2, out Color piritOutlineBaseColor2, out Color innerOutlineGlowColor2, //Pirit colors
		out Color reforgedColorLeft2, out Color reforgedColorRight2); //Reforged colors

			//Never stop lerping
			outlineColor = Color.Lerp(outlineColor, outlineColor2, paletteLerper);
			underlineColor = Color.Lerp(underlineColor, underlineColor2, paletteLerper);
			shadowColor = Color.Lerp(shadowColor, shadowColor2, paletteLerper);
			sFillColor = Color.Lerp(sFillColor, sFillColor2, paletteLerper);
			sGradientTopColor = Color.Lerp(sGradientTopColor, sGradientTopColor2, paletteLerper);
			sGradientBottomColor = Color.Lerp(sGradientBottomColor, sGradientBottomColor2, paletteLerper);
			sOutlineGlowColor = Color.Lerp(sOutlineGlowColor, sOutlineGlowColor2, paletteLerper);
			fillColorBase = Color.Lerp(fillColorBase, fillColorBase2, paletteLerper);
			fillColorSecondary = Color.Lerp(fillColorSecondary, fillColorSecondary2, paletteLerper);
			piritFillGlowColor = Color.Lerp(piritFillGlowColor, piritFillGlowColor2, paletteLerper);
			piritOutlineBaseColor = Color.Lerp(piritOutlineBaseColor, piritOutlineBaseColor2, paletteLerper);
			innerOutlineGlowColor = Color.Lerp(innerOutlineGlowColor, innerOutlineGlowColor2, paletteLerper);
			reforgedColorLeft = Color.Lerp(reforgedColorLeft, reforgedColorLeft2, paletteLerper);
			reforgedColorRight = Color.Lerp(reforgedColorRight, reforgedColorRight2, paletteLerper);
		}

		effect.Parameters["indigoOutline"].SetValue(outlineColor.ToVector4());                 //Outline for all letters
		effect.Parameters["darkUnderlineColor"].SetValue(underlineColor.ToVector4());          //Color of the 1 px offset underline of the letters 
		effect.Parameters["shadowColor"].SetValue(shadowColor.ToVector4());                    //Color of the shadows below the letters

		effect.Parameters["sFillColor"].SetValue(sFillColor.ToVector4());                      //Inside fill for the S
		effect.Parameters["sGradientTopColor"].SetValue(sGradientTopColor.ToVector4());        //Top-down gradient for the internal outline of the S
		effect.Parameters["sGradientBottomColor"].SetValue(sGradientBottomColor.ToVector4());
		effect.Parameters["indigoOutlineGlowing"].SetValue(sOutlineGlowColor.ToVector4());     //Glowing color for the S outline as it grows

		effect.Parameters["fillColorBase"].SetValue(fillColorBase.ToVector4());                 // Base fill color for the pirit letters
		effect.Parameters["fillColorSecondary"].SetValue(fillColorSecondary.ToVector4());       // Secondary fill color for the pirit letters, tinting the right side
		effect.Parameters["piritFillGlowColor"].SetValue(piritFillGlowColor.ToVector4());       // Additive color for the pirit letters, applied with the dithery noise
		effect.Parameters["piritOutlineBaseColor"].SetValue(piritOutlineBaseColor.ToVector4()); // Base color for the inner outline of the letters
		effect.Parameters["innerOutlineGlowColor"].SetValue(innerOutlineGlowColor.ToVector4()); // Additive color layered ontop of the inner outline. Affected by the dithery noise

		effect.Parameters["reforgedColorLeft"].SetValue(reforgedColorLeft.ToVector4());         // Colors for the L/R gradient on the reforged subtext
		effect.Parameters["reforgedColorRight"].SetValue(reforgedColorRight.ToVector4()); 
	}

	public static void Draw(SpriteBatch spriteBatch, Vector2 logoDrawCenter, float logoScale, GetSpiritPaletteDelegate palette, float paletteLerper = 0f, 
		GetSpiritPaletteDelegate altPalette = null, bool restartSpriteBatch = true)
	{
		if (debugDrawQueued)
		{
			DebugCapture();
			return;
		}

		if (restartSpriteBatch)
		{
			spriteBatch.End();
			spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
		}

		if (logoShader == null)
			LoadAssets();

		Effect effect = logoShader;
		effect.Parameters["time"].SetValue(logoTime);
		effect.Parameters["textureResolution"].SetValue(sTexture.Size());
		effect.Parameters["sCenterCoordinates"].SetValue(new Vector2(120, 120));
		effect.Parameters["wobbleMult"].SetValue(1 + MathF.Pow(clickWobbleTimer, 1.6f) * 3.5f);

		effect.Parameters["sTexture"].SetValue(sTexture.Value);
		effect.Parameters["sOutlineTexture"].SetValue(sOutlineTexture.Value);

		//Use the spirt texture if we rolled for it
		if (!SpirtMod)
		{
			effect.Parameters["piritTexture"].SetValue(piritTexture.Value);
			effect.Parameters["piritOutlineTexture"].SetValue(piritOutlineTexture.Value);
		}
		else
		{
			effect.Parameters["piritTexture"].SetValue(pirtTexture.Value);
			effect.Parameters["piritOutlineTexture"].SetValue(pirtOutlineTexture.Value);
		}

		effect.Parameters["time"].SetValue(logoTime);
		effect.Parameters["wobbleMult"].SetValue(1 + MathF.Pow(clickWobbleTimer, 1.6f) * 3.5f);
		effect.Parameters["reforgedTexture"].SetValue(reforgedTexture.Value);
		effect.Parameters["noiseTexture"].SetValue(noiseTrail.Value);

		FillInColorParameters(effect, palette, paletteLerper, altPalette);

		//Check if the cursor overlaps the logo, and if so check for clicks
		Rectangle approximateLogoRectangle = new((int)(logoDrawCenter.X - sTexture.Width() * logoScale * 0.5f), (int)(logoDrawCenter.Y - sTexture.Height() * logoScale * 0.5f), 
			sTexture.Width(), sTexture.Height());
		if (approximateLogoRectangle.Contains(Main.MouseScreen.ToPoint()))
		{
			if (Main.mouseLeft && Main.mouseLeftRelease && !Main.LocalPlayer.mouseInterface)
				clickWobbleTimer = 1f;
			//Debug rendering
			if (false && Main.mouseRight && Main.mouseRightRelease)
				debugDrawQueued = true;
		}

		var square = new SquarePrimitive
		{
			Color = Color.White,
			Height = sTexture.Height() * logoScale,
			Length = sTexture.Width() * logoScale,
			Position = logoDrawCenter,
			Rotation = 0f,
		};
		PrimitiveRenderer.DrawPrimitiveShape(square, effect);

		if (restartSpriteBatch)
		{
			spriteBatch.End();
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
		}
	}

	private static void DebugCapture(int frameCount = 300)
	{
		Vector2 rtSize = sTexture.Size() + Vector2.One * 100;

		if (debug_captureTarget is null || debug_captureTarget.Size() != rtSize)
			Main.QueueMainThreadAction(() => debug_captureTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, (int)rtSize.X, (int)rtSize.Y));
		if (debug_captureTarget is null || debug_captureTarget.Size() != rtSize)
			return;

		Main.graphics.GraphicsDevice.SetRenderTarget(debug_captureTarget);
		Main.spriteBatch.End();
		Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);

		logoTime = 0;
		debugDrawQueued = false;

		float logoTimeIncrement = 1 / 60f;

		for (int i = 0; i < frameCount; i++)
		{
			//Draw our frame to the RT
			Main.graphics.GraphicsDevice.Clear(Color.Transparent);
			Draw(Main.spriteBatch, rtSize / 2f, 1f, SavannaMenuTheme.SavannaOrangePalette, 0f, null, false);

			//Save the rendertarget
			string path = $"{Main.SavePath}/SpiritLogoCapture";
			Stream saveStream = File.OpenWrite(path + "/Logo" + i.ToString() + ".png");
			debug_captureTarget.SaveAsPng(saveStream, (int)rtSize.X, (int)rtSize.Y);
			saveStream.Dispose();

			//Advance time
			logoTime += logoTimeIncrement;
		}

		Main.spriteBatch.End();
		Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);

		Main.graphics.GraphicsDevice.SetRenderTargets(null);
	}

	public void Load(Mod mod) { } // Nothing on load, we load the textures when we need them, and after assetLoader has ran
	public void Unload() => logoShader = null;
}
