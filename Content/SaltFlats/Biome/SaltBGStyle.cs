using ILLogger;
using MonoMod.Cil;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.RenderTargets;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.Graphics.Effects;

namespace SpiritReforged.Content.SaltFlats.Biome;

public class SaltBGStyle : CustomSurfaceBackgroundStyle
{
	private static readonly Asset<Texture2D> FarMountains = ModContent.Request<Texture2D>("SpiritReforged/Assets/Textures/Backgrounds/SaltBackgroundFar_Mountains");
	private static readonly Asset<Texture2D> FarClouds = ModContent.Request<Texture2D>("SpiritReforged/Assets/Textures/Backgrounds/SaltBackgroundFar_Clouds");
	private static readonly Asset<Texture2D> SkyReflectionMask = ModContent.Request<Texture2D>("SpiritReforged/Assets/Textures/Backgrounds/SaltBackgroundFar_Mask");

	private static float MiddleOffset;

	private float ReflectionFadeAwayValue = 0f;

	public static float ReflectedSkyRenderOffset = 200f;

	public override int MiddleTexture => -1;
	public override int CloseTexture => -1;

	public static ModTarget2D skyReflectionsTarget;
	public static ModTarget2D backgroundTarget;

	public bool UseTargets => !Main.gameMenu && Main.bgAlphaFarBackLayer[Slot] > 0;

	public override void Load()
	{
		base.Load();
		if (!Main.dedServ)
		{
			skyReflectionsTarget = new ModTarget2D(() => UseTargets, RenderSkyObjects);
			backgroundTarget = new ModTarget2D(() => UseTargets, CompositeFullBackground);

			IL_Cloud.addCloud += RemoveForegroundCloudsInSaltFlats;
			On_Cloud.Update += KillActiveForegroundCloudsInSaltFlats;
		}
	}

	#region Foreground clouds be gone
	private void KillActiveForegroundCloudsInSaltFlats(On_Cloud.orig_Update orig, Cloud self)
	{
		orig(self);

		if (SaltFlatsSystem.saltFlatsOpacity <= 0.5f)
			return;

		//Kill all foreground clouds
		if (self.active && self.scale >= 1f)
			self.kill = true;
	}

	private void RemoveForegroundCloudsInSaltFlats(ILContext il)
	{
		ILCursor cursor = new ILCursor(il);

		int newCloudIndexIndex = 0;

		if (!cursor.TryGotoNext(MoveType.After,
			i => i.MatchLdsfld<Main>("cloud"),
			i => i.MatchLdloc(out newCloudIndexIndex),
			i => i.MatchLdelemRef()))
		{
			SpiritReforgedMod.Instance.LogIL("Hide foreground clouds in salt flats", "Call Main.cloud[i] not found.");
			return;
		}

		if (!cursor.TryGotoNext(MoveType.After, i => i.MatchStfld<Cloud>("scale")))
		{
			SpiritReforgedMod.Instance.LogIL("Hide foreground clouds in salt flats", "Cloud scale assignment could not be found");
			return;
		}

		cursor.EmitLdloc(newCloudIndexIndex);
		cursor.EmitDelegate(ResizeCloudIfInSaltFlats);
	}

	public static void ResizeCloudIfInSaltFlats(int cloudIndex)
	{
		//We only care abt foreground clouds
		if (Main.cloud[cloudIndex].scale < 1)
			return;

		if (SaltFlatsSystem.saltFlatsOpacity <= 0.5f)
			return;

		//Rerandomize scale without foreground ones
		Main.cloud[cloudIndex].scale = Main.rand.NextFloat(0.07f, 0.99f);
	}
	#endregion

	public override void Unload()
	{
		base.Unload();

		if (!Main.dedServ)
		{
			Main.QueueMainThreadAction(() =>
			{
				skyReflectionsTarget?.Dispose();
				backgroundTarget?.Dispose();
			});
		}
	}

	#region Capture sky objects for reflection
	public void RenderSkyObjects(SpriteBatch spriteBatch)
	{
		spriteBatch.End();

		Matrix transformMatrix = Main.BackgroundViewMatrix.EffectMatrix;
		transformMatrix.Translation += Vector3.UnitY * ReflectedSkyRenderOffset;
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, transformMatrix);

		int bgTopY = (int)((double)(0f - Main.screenPosition.Y) / (Main.worldSurface * 16.0 - 600.0) * 200.0);
		Main.SceneArea sceneArea = default(Main.SceneArea);
		sceneArea.bgTopY = bgTopY;
		sceneArea.totalHeight = Main.screenHeight;
		sceneArea.totalWidth = Main.screenWidth;
		sceneArea.SceneLocalScreenPositionOffset = Vector2.Zero;
		if (Main.shimmerAlpha != 1f)
			BackgroundStyleHelper.DrawBackgroundStars(Main.instance, sceneArea, false);

		if ((double)(Main.screenPosition.Y / 16f) < Main.worldSurface + 2.0)
		{
			Main.InfoToSetBackColor info = default(Main.InfoToSetBackColor);
			info.isInGameMenuOrIsServer = Main.gameMenu || Main.netMode == 2;
			info.CorruptionBiomeInfluence = Main.SceneMetrics.EvilTileCount / (float)SceneMetrics.CorruptionTileMax;
			info.CrimsonBiomeInfluence = Main.SceneMetrics.BloodTileCount / (float)SceneMetrics.CrimsonTileMax;
			info.JungleBiomeInfluence = Main.SceneMetrics.JungleTileCount / (float)SceneMetrics.JungleTileMax;
			info.MushroomBiomeInfluence = Main.SmoothedMushroomLightInfluence;
			info.GraveyardInfluence = Main.GraveyardVisualIntensity;
			info.BloodMoonActive = Main.bloodMoon || Main.SceneMetrics.BloodMoonMonolith;
			info.LanternNightActive = LanternNight.LanternsUp;
			BackgroundStyleHelper.FillSunAndMoonColor(Main.instance, info, out Color sunColor, out Color moonColor);
			BackgroundStyleHelper.DrawSunAndMoon(Main.instance, sceneArea, moonColor, sunColor, Main.SmoothedMushroomLightInfluence);
		}

		if (Main.screenPosition.Y >= Main.worldSurface * 16.0 + 16.0)
			return;

		CustomSurfaceBackgroundStyle.RestartSpritebatch(null, Vector3.UnitY * ReflectedSkyRenderOffset);

		float globalCloudAlpha = SkyManager.Instance.ProcessCloudAlpha() * Main.atmo;

		float scAdj = BackgroundStyleHelper.ScreenAdj;
		float screenOff = Main.screenHeight - 600f;
		bgTopY = (int)((double)(-Main.screenPosition.Y + screenOff / 2f) / (Main.worldSurface * 16.0) * 1200.0 + 1190.0) + (int)scAdj;

		float worldSurface = Math.Max(1, (float)Main.worldSurface);
		float backgroundTopAnchor = Main.screenPosition.Y + Main.screenHeight / 2 - 600f;
		float backgroundTopMagicNumber = (-backgroundTopAnchor + screenOff / 2f) / (worldSurface * 16f);

		#region Furthest clouds
		for (int i = 0; i < Main.maxClouds; i++)
		{
			Cloud cloud = Main.cloud[i];

			if (cloud.active && cloud.scale < 1f)
			{
				Color cloudColor = cloud.cloudColor(Main.ColorOfTheSkies);
				float num8 = cloud.scale * 0.8f;
				float num9 = (cloud.scale + 1f) / 2f * 0.9f;
				cloudColor.R = (byte)((int)cloudColor.R * num8);
				cloudColor.G = (byte)((int)cloudColor.G * num9);
				float cloudHeight = cloud.position.Y + (float)(int)(backgroundTopMagicNumber * 750.0 + 830.0) + (int)scAdj;

				DrawCloudSpoof(cloud, cloudColor * globalCloudAlpha, cloudHeight);
			}
		}
		#endregion

		SkyManager.Instance.ResetDepthTracker();

		#region Background cloud layers that appear during storms
		if (Main.BackgroundEnabled && Main.cloudBGAlpha > 0f)
		{
			Texture2D furthestCloudBG = LoadBackground(Main.cloudBG[0]);
			float furthestCloudBGWidth = Main.backgroundWidth[Main.cloudBG[0]];
			Texture2D cloudWallBG = LoadBackground(Main.cloudBG[1]);
			float cloudWallBGWidth = Main.backgroundWidth[Main.cloudBG[1]];

			float bgScale = 1.65f;
			double bgParallax = 0.09000000357627869;

			//Draws any sky object between the last depth and our BG clouds
			if (Reflections.Detail > 1)
				SkyManager.Instance.DrawToDepth(Main.spriteBatch, 1f / (float)bgParallax);

			float scaledBackgroundWidth = furthestCloudBGWidth * bgScale;
			bgTopY = (int)(backgroundTopMagicNumber * 900.0 + 600.0) + (int)scAdj + 30;
			float bgStartX = (int)(-Math.IEEERemainder(Main.screenPosition.X * bgParallax, scaledBackgroundWidth) - (double)(scaledBackgroundWidth * 1.5));
			bgStartX += (int)Main.cloudBGX[0];
			float bgLoops = Main.screenWidth / (int)scaledBackgroundWidth + 2 + 2;

			float cloudBGAlpha = Math.Min(1, Main.cloudBGAlpha);
			Color bgColor = Main.ColorOfTheSkies * cloudBGAlpha;

			for (int j = 0; j < bgLoops; j++)
			{
				Main.spriteBatch.Draw(furthestCloudBG, new Vector2(bgStartX + scaledBackgroundWidth * j, bgTopY), null, bgColor * globalCloudAlpha, 0f, Vector2.Zero, bgScale, SpriteEffects.None, 0f);
			}

			bgScale = 1.85f;
			bgParallax = 0.12;

			//Draws any sky object between the last depth and our BG clouds
			if (Reflections.Detail > 1)
				SkyManager.Instance.DrawToDepth(Main.spriteBatch, 1f / (float)bgParallax);

			cloudBGAlpha = Math.Min(1, cloudBGAlpha * 1.5f);
			bgColor = Main.ColorOfTheSkies * cloudBGAlpha;

			scaledBackgroundWidth = (float)cloudWallBGWidth * bgScale;
			bgTopY = (int)(backgroundTopMagicNumber * 1100.0 + 750.0) + (int)scAdj + 30;

			bgStartX = (int)(0.0 - Math.IEEERemainder((double)Main.screenPosition.X * bgParallax, scaledBackgroundWidth) - (double)(scaledBackgroundWidth * 1.5));
			bgStartX += (int)Main.cloudBGX[1];
			bgLoops = Main.screenWidth / (int)scaledBackgroundWidth + 2 + 2;
			for (int k = 0; k < bgLoops; k++)
			{
				Main.spriteBatch.Draw(cloudWallBG, new Vector2(bgStartX + scaledBackgroundWidth * k, bgTopY), null, bgColor * globalCloudAlpha, 0f, Vector2.Zero, bgScale, SpriteEffects.None, 0f);
			}
		}
		#endregion

		SkyManager.Instance.DrawToDepth(Main.spriteBatch, 5f);

		//BG clouds from the background itself
		if (Main.BackgroundEnabled)
		{
			Texture2D cloudTexture = FarClouds.Value;
			Color color = Main.ColorOfTheSkies;
			float softParallaxX = Main.screenPosition.X * 0.02f % 2048;
			float softParallaxY = Main.screenPosition.Y * 0.005f % 2048;
			int crop = 530 + (int)softParallaxY; //The amount of vertical space to crop from the reflected cloud scroll
			Rectangle bounds = GetBounds(FarTexture);
			Rectangle reflectionBounds = new(bounds.X, bounds.Y, bounds.Width, bounds.Height - crop);
			int loops = BackgroundStyleHelper.BackgroundLoops;
			int cloudLoops = loops + 1;
			SpoofFarBackgroundParameters(scAdj, backgroundTopMagicNumber, 2, 30);
			bgTopY = BackgroundStyleHelper.BackgroundTopY;
			DrawScroll((position, scale) => Main.spriteBatch.Draw(cloudTexture, position + new Vector2(MiddleOffset - softParallaxX, -softParallaxY), bounds, color, 0, default, scale, SpriteEffects.None, 0), -1, cloudLoops);
		}

		#region Mid-layer clouds
		float cloudTop = bgTopY - 50;
		for (int l = 0; l < Main.maxClouds; l++)
		{
			Cloud cloud = Main.cloud[l];

			if (cloud.active && (double)cloud.scale < 1.15 && cloud.scale >= 1f)
			{
				Color cloudColor = cloud.cloudColor(Main.ColorOfTheSkies);
				if (Main.atmo < 1f)
					cloudColor *= Main.atmo;

				float cloudHeight = cloud.position.Y * (Main.screenHeight / 600f);
				DrawCloudSpoof(cloud, cloudColor * globalCloudAlpha, cloudHeight + cloudTop + 200f);
			}
		}
		#endregion

		#region Front layer clouds
		if (Main.BackgroundEnabled)
		{
			cloudTop = (float)bgTopY * 1.01f - 150f;
			for (int n = 0; n < Main.maxClouds; n++)
			{
				Cloud cloud = Main.cloud[n];
				if (cloud.active && cloud.scale >= 1.15f)
				{
					Color cloudColor = cloud.cloudColor(Main.ColorOfTheSkies);
					if (Main.atmo < 1f)
						cloudColor *= Main.atmo;

					float cloudHeight = cloud.position.Y * (Main.screenHeight / 600f) - 100f;
					DrawCloudSpoof(cloud, cloudColor * globalCloudAlpha, cloudHeight + cloudTop);
				}
			}
		}
		#endregion

		//Draw to depth 1 which would end before the blizzard fullscreen overlay
		if (Reflections.Detail > 1)
			SkyManager.Instance.DrawToDepth(Main.spriteBatch,-1f);

		if (Main.shimmerAlpha > 0f)
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, Vector2.Zero, null, Color.Black * Main.shimmerAlpha, 0f, Vector2.Zero, new Vector2(Main.Camera.UnscaledSize.X + (float)(Main.offScreenRange * 2), Main.Camera.UnscaledSize.Y + (float)(Main.offScreenRange * 2)), SpriteEffects.None, 0f);
	}

	public void SpoofFarBackgroundParameters(float scAdj, double backgroundTopMagicNumber, float bgGlobalScaleMultiplier, int pushBGTopHack)
	{
		//Taken from the start of Main.DrawSurfaceBG_BackMountainsStep1()
		float bgScale = bgGlobalScaleMultiplier;

		int bgWidthScaled = (int)(1024f * bgScale);
		if (bgWidthScaled == 0)
			bgWidthScaled = 1024;

		double bgParallax = 0.15;
		int bgStartX = (int)(-Math.IEEERemainder(Main.screenPosition.X * bgParallax, bgWidthScaled) - bgWidthScaled / 2f);

		//This code doesn't spoof vanilla code but instead spoofs the code found in the normal draw method
		float screenCenterY = Main.screenPosition.Y + Main.screenHeight / 2f;
		float dif = ((SaltFlatsSystem.SurfaceHeight == 0) ? (float)(Main.worldSurface * 0.67f) : SaltFlatsSystem.SurfaceHeight) * 16 - screenCenterY;
		BackgroundStyleHelper.BackgroundTopY = (int)(dif - dif * 0.8f);

		BackgroundStyleHelper.BackgroundScale = bgScale;
		BackgroundStyleHelper.BackgroundStartX = bgStartX;
		BackgroundStyleHelper.BackgroundWidthScaled = bgWidthScaled;
	}

	public static void DrawCloudSpoof(Cloud cloud, Color color, float yOffset, int index = -1)
	{
		Texture2D texture = TextureAssets.Cloud[cloud.type].Value;
		Vector2 position = new Vector2(cloud.position.X + texture.Width * 0.5f, yOffset + texture.Height * 0.5f);
		Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
		float rotation = cloud.rotation;
		Vector2 origin = texture.Size() / 2f;
		float scale = cloud.scale;
		SpriteEffects effects = cloud.spriteDir;

		//Color brightenedColor = Color.Lerp(color, Color.White, 0.24f) with { A = color.A };

		DrawData drawData = new DrawData(texture, position, sourceRectangle, color, rotation, origin, scale, effects);
		if (cloud.ModCloud?.Draw(Main.spriteBatch, cloud, index, ref drawData) == false)
			return;
		drawData.Draw(Main.spriteBatch);
	}
	#endregion

	#region Composite the full background
	public static bool DrawingComposite = false;

	public void CompositeFullBackground(SpriteBatch spriteBatch)
	{
		//Compute values to be equal to what they would be when drawing the background
		float scAdj = BackgroundStyleHelper.ScreenAdj;
		float screenOff = Main.screenHeight - 600f;
		float worldSurface = Math.Max(1, (float)Main.worldSurface);
		float backgroundTopAnchor = Main.screenPosition.Y + Main.screenHeight / 2 - 600f;
		float backgroundTopMagicNumber = (-backgroundTopAnchor + screenOff / 2f) / (worldSurface * 16f);
		SpoofFarBackgroundParameters(scAdj, backgroundTopMagicNumber, 2, 30);

		Color modifiedSurfaceBgColor = Main.ColorOfTheSkies;
		if (Main.cloudBGActive > 0)
			modifiedSurfaceBgColor *= Math.Max(1, Main.cloudBGAlpha * 1.5f);
		BackgroundStyleHelper.SurfaceBackgroundModified = modifiedSurfaceBgColor;

		CustomSurfaceBackgroundStyle.RestartSpritebatch(null);

		//Draw the background
		DrawingComposite = true;
		Draw(spriteBatch, LayerType.Far);
	}
	#endregion

	public void TestMe()
	{
		Vector2 defaultScreenPos = Main.screenPosition;
		int defaultScreenWidth = Main.screenWidth;
		int defaultScreenHeight = Main.screenHeight;

		int newScreenWidth = (int)((float)Main.screenWidth / Main.BackgroundViewMatrix.Zoom.X);
		int newScreenHeight = (int)((float)Main.screenWidth / Main.BackgroundViewMatrix.Zoom.X);
		Vector2 newScreenPos = Main.screenPosition + Main.BackgroundViewMatrix.Translation;

		Matrix transformationMatrix = Main.BackgroundViewMatrix.TransformationMatrix;
		transformationMatrix.Translation -= Main.BackgroundViewMatrix.ZoomMatrix.Translation * new Vector3(1f, Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically) ? (-1f) : 1f, 1f);
	}

	public override bool Draw(SpriteBatch spriteBatch, LayerType layer)
	{
		bool shouldFadeDownBackground =
			SkyManager.Instance["Nebula"].IsActive() ||
			SkyManager.Instance["Stardust"].IsActive() ||
			SkyManager.Instance["Vortex"].IsActive() ||
			SkyManager.Instance["Solar"].IsActive() ||
			SkyManager.Instance["MonolithNebula"].IsActive() ||
			SkyManager.Instance["MonolithStardust"].IsActive() ||
			SkyManager.Instance["MonolithVortex"].IsActive() ||
			SkyManager.Instance["MonolithSolar"].IsActive() ||
			SkyManager.Instance["MoonLord"].IsActive() ||
			SkyManager.Instance["MonolithMoonLord"].IsActive();

		shouldFadeDownBackground |= Main.shimmerAlpha > 0;

		if (shouldFadeDownBackground)
			ReflectionFadeAwayValue = MathHelper.Lerp(ReflectionFadeAwayValue, 1f, 0.02f);
		else
			ReflectionFadeAwayValue = MathHelper.Lerp(ReflectionFadeAwayValue, 0f, 0.01f);

		if (layer == LayerType.Far)
		{
			int slot = FarTexture;

			//Since we already drew the assembled background in the composite target drawing step, don't redraw it a second time and simply just reuse the target
			if (!DrawingComposite && Main.LocalPlayer.gravDir == 1 && !Main.gameMenu && backgroundTarget != null && !backgroundTarget.Target.IsDisposed)
			{
				spriteBatch.End();
				spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null);
				spriteBatch.Draw(backgroundTarget.Target, Vector2.Zero, null, Color.White * Main.bgAlphaFarBackLayer[Slot], 0, Vector2.Zero, 1, 0, 0);
				RestartSpritebatch(null); 
				TestMe();
				return false;
			}

			DrawingComposite = false;

			if (slot >= 0 && slot < TextureAssets.Background.Length)
			{
				Texture2D texture = LoadBackground(slot);
				Texture2D cloudTexture = FarClouds.Value;
				Texture2D mountainTexture = FarMountains.Value;
				Texture2D nightSkyMaskTexture = SkyReflectionMask.Value;

				Color color = BackgroundStyleHelper.SurfaceBackgroundModified;
				color = Color.Lerp(color, new Color(80, 120, 255), MathF.Pow(SaltFlatsSystem.nightSkyOpacity, 2f) * 0.3f);

				Rectangle bounds = GetBounds(slot);
				int loops = BackgroundStyleHelper.BackgroundLoops;
				int cloudLoops = loops + 1;

				float screenCenterY = Main.screenPosition.Y + Main.screenHeight / 2f;
				float dif = ((SaltFlatsSystem.SurfaceHeight == 0) ? (float)(Main.worldSurface * 0.67f) : SaltFlatsSystem.SurfaceHeight) * 16 - screenCenterY;
				BackgroundStyleHelper.BackgroundTopY = (int)(dif - dif * 0.8f);

				if (Main.gameMenu)
				{
					BackgroundStyleHelper.BackgroundTopY = 130;
				}

				float softParallaxX = Main.screenPosition.X * 0.02f % 2048;
				float softParallaxY = Main.screenPosition.Y * 0.005f % 2048;

				if (Main.BackgroundEnabled)
				{
					DrawScroll((position, scale) =>
					{
						spriteBatch.Draw(cloudTexture, position + new Vector2(MiddleOffset - softParallaxX, -softParallaxY), bounds, color, 0, default, scale, SpriteEffects.None, 0);

						// I'm lazy and this fixes occasional cutoffs sometimes
						spriteBatch.Draw(cloudTexture, position + new Vector2(MiddleOffset - softParallaxX + 2048, -softParallaxY), bounds, color, 0, default, scale, SpriteEffects.None, 0);
					}, -1, cloudLoops);

					DrawScroll((position, scale) => spriteBatch.Draw(texture, position, bounds, color, 0, Vector2.Zero, BackgroundStyleHelper.BackgroundScale, SpriteEffects.None, 0));
				}

				float debugValue = BackgroundStyleHelper.BackgroundTopY;

				int crop = 530 + (int)softParallaxY; //The amount of vertical space to crop from the reflected cloud scroll
				Rectangle reflectionBounds = new(bounds.X, bounds.Y, bounds.Width, bounds.Height - crop);

				//If in the gamemenu, just draw the reflection of the background mountain clouds. Otherwise, the mountain cloud reflections draw through the reflection shader
				if (Main.BackgroundEnabled)
				{
					if (Main.gameMenu || Main.LocalPlayer.gravDir == -1)
						DrawScroll((position, scale) => spriteBatch.Draw(cloudTexture, position + new Vector2(MiddleOffset - softParallaxX, -softParallaxY + crop - 110), reflectionBounds, color * 0.5f, 0, default, BackgroundStyleHelper.BackgroundScale, SpriteEffects.FlipVertically, 0), -1, cloudLoops);
					DrawScroll((position, scale) => spriteBatch.Draw(mountainTexture, position, bounds, color, 0, Vector2.Zero, BackgroundStyleHelper.BackgroundScale, SpriteEffects.None, 0));
				}

				if (!Main.gameMenu && Main.LocalPlayer.gravDir == 1 && skyReflectionsTarget != null && !skyReflectionsTarget.Target.IsDisposed)
				{
					Vector2 reflectionCanvasSize = skyReflectionsTarget.Target.Size();

					Effect bgShader = AssetLoader.LoadedShaders["SaltBackgroundReflection"].Value;
					var projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);
					bgShader.Parameters["WorldViewProjection"].SetValue(projection);
					bgShader.Parameters["reflectionMaskTexture"].SetValue(nightSkyMaskTexture);
					//height of the horizon on the background texture
					bgShader.Parameters["reflectionHorizonHeight"].SetValue(205 / 750f);
					bgShader.Parameters["cloudTargetYOffset"].SetValue(ReflectedSkyRenderOffset / reflectionCanvasSize.Y);
					bgShader.Parameters["topFadeStrength"].SetValue(ReflectionFadeAwayValue);
					bgShader.Parameters["shimmerAlpha"].SetValue(Main.shimmerAlpha);
					bgShader.Parameters["matrixZoom"].SetValue(Main.BackgroundViewMatrix.Zoom);
					SaltFlatsSystem.SetSkyColor(bgShader);
					bgShader.Parameters["texColorUVLerper"].SetValue(1f);
					bgShader.Parameters["doMask"].SetValue(SaltFlatsSystem.nightSkyOpacity > 0f);

					Matrix maskTransformMatrix = Matrix.Identity;
					Vector2 maskScaleRatio = reflectionCanvasSize / nightSkyMaskTexture.Size();

					maskTransformMatrix *= Matrix.Invert(spriteBatch.GetTransformMatrix());

					//WHAT ARE YOUUUUUU WHY ARE YOU OFFSET WRONG!! IT DOESNT EVEN WORK FOR 4K CUZ THE OFFSET IS TOO BIG OVER THERE!!!
					maskTransformMatrix *= Matrix.CreateTranslation(8f / reflectionCanvasSize.X, 4f / reflectionCanvasSize.Y, 0f);
					maskTransformMatrix *= Matrix.CreateTranslation(new Vector3(-BackgroundStyleHelper.BackgroundStartX / reflectionCanvasSize.X, -BackgroundStyleHelper.BackgroundTopY / reflectionCanvasSize.Y, 0f));
					//Adjust the bg scale by the ratio between the RT we're rendering and the BG size
					maskTransformMatrix *= Matrix.CreateScale(maskScaleRatio.X, maskScaleRatio.Y, 1);
					//Adjust by the background scale itself
					maskTransformMatrix *= Matrix.CreateScale(1 / BackgroundStyleHelper.BackgroundScale);

					bgShader.Parameters["maskTransform"].SetValue(maskTransformMatrix);
					bgShader.Parameters["maskReverseTransform"].SetValue(Matrix.Invert(maskTransformMatrix));

					spriteBatch.End();
					spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, bgShader);

					spriteBatch.Draw(skyReflectionsTarget.Target, Vector2.Zero, null, Color.White * 0.8f, 0, Vector2.Zero, 1, 0, 0);
					RestartSpritebatch(null);
				}

				if (!Main.gamePaused) //Move out of a drawing method
				{
					MiddleOffset += 0.4f * Main.windSpeedCurrent * (float)Main.dayRate;
					MiddleOffset %= 2048; // Needs to be looped, otherwise the textures run out
				}
			}
		}

		DrawingComposite = false;
		return false;
	}
}