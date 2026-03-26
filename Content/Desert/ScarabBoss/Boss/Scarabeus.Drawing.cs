using Newtonsoft.Json.Linq;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.Visuals;
using System.Linq;
using Terraria.GameContent.UI;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

public partial class Scarabeus : ModNPC
{
	public readonly record struct VisualProfile
	{
		public readonly Asset<Texture2D> Texture, SheenMask, GlowMask, Wings;
		public readonly int Rows;
		private readonly int[] FrameCount;
		public readonly bool Simulated = false;

		public int Columns => FrameCount.Length;
		/// <summary> Safely gets the number of frames in the provided column. </summary>
		public readonly int GetFrameCount(int column) => (column >= Columns || column < 0) ? 0 : FrameCount[column];

		public VisualProfile(Asset<Texture2D> texture, Asset<Texture2D> sheenMask, int[] frameCount, Asset<Texture2D> glowMask = null, Asset<Texture2D> wings = null, bool simulated = false)
		{
			this.Texture = texture;
			this.SheenMask = sheenMask;
			this.GlowMask = glowMask;
			this.Wings = wings;
			this.FrameCount = frameCount;
			this.Simulated = simulated;
			Rows = frameCount.OrderBy(static x => x).Last();
		}
	}

	public enum FrameState { Progressed, Stopped, Looped }

	/// <summary> Roll frame for profile one. </summary>
	public static readonly Point RollFrame = new(0, 4);

	/// <summary> The current visual profile to be used for drawing. Contains frame count and texture information. </summary>
	public VisualProfile Profile { get; private set; }

	/// <summary> The selected frame in the 2D spritesheet.<para/>
	/// Prefer <see cref="UpdateFrame"/> and <see cref="SetFrame"/> for assignment. </summary>
	public Point currentFrame;
	/// <summary> The opacity of Scarabeus' trail. Resets every frame. </summary>
	public float trailOpacity;
	public float squishY = 1f;
	public float iridescenceBoost;
	public int scarabColorIndex = 0;
	public float wingFrameCounter;

	#region framing methods
	private FrameState UpdateFrame(int column, int framesPerSecond, VisualProfile profile, bool loop = true)
	{
		FrameState result = FrameState.Progressed;
		Profile = profile;

		if (currentFrame.X != column)
		{
			currentFrame.X = column;
			currentFrame.Y = 0;

			NPC.frameCounter = 0;
		}

		if (++NPC.frameCounter > 60.0 / Math.Abs(framesPerSecond))
		{
			NPC.frameCounter = 0;
			bool reversed = framesPerSecond < 0;

			if (reversed ? currentFrame.Y == 0 : currentFrame.Y == Profile.GetFrameCount(column) - 1)
				result = loop ? FrameState.Looped : FrameState.Stopped;

			if (reversed) //Reverse the animation
				currentFrame.Y = loop ? ((currentFrame.Y > 0) ? currentFrame.Y - 1 : Profile.GetFrameCount(column) - 1) : Math.Max(currentFrame.Y - 1, 0);
			else
				currentFrame.Y = loop ? ((currentFrame.Y + 1) % Profile.GetFrameCount(column)) : Math.Min(currentFrame.Y + 1, Profile.GetFrameCount(column) - 1);
		}

		return result;
	}

	/*private FrameState UpdateFrame(int column, int startFrame, int framesPerSecond, VisualProfile profile, bool loop = true)
	{
		bool reversed = framesPerSecond < 0;

		if (reversed && currentFrame.Y > startFrame)
			currentFrame.Y = startFrame;
		else if (!reversed && currentFrame.Y < startFrame)
			currentFrame.Y = startFrame;

		return UpdateFrame(column, framesPerSecond, profile, loop);
	}*/

	private void SetFrame(int column, int row, VisualProfile profile)
	{
		Profile = profile;
		currentFrame = new(column, row);
	}

	private void SetFrame(Point frame, VisualProfile profile) => SetFrame(frame.X, frame.Y, profile);
	#endregion

	public bool OriginAtFeet()
	{
		if (Profile == PhaseOneProfile)
		{
			if (currentFrame.X != 0)
				return true;
			if (currentFrame.Y is < 1 or > 5)
				return true;
			return false;
		}
		else if (Profile == TakeoffProfile)
			return true;
		else
		{
			return currentFrame.X > 3;
		}
	}

	public override void FindFrame(int frameHeight)
	{
		if (!Main.dedServ)
		{
			Texture2D texture = Profile.Texture.Value;

			NPC.frame.Width = texture.Width / Profile.Columns;
			NPC.frame.Height = texture.Height / Profile.Rows;
		}

		NPC.frame.X = NPC.frame.Width * currentFrame.X;
		NPC.frame.Y = NPC.frame.Height * currentFrame.Y;

		squishY = MathHelper.Lerp(squishY, 1f, 0.3f);

		if (NPC.IsABestiaryIconDummy)
			UpdateFrame(currentFrame.X, 12, PhaseOneProfile);
	}

	public Effect GetShader(Texture2D texture, Texture2D sheenTexture, Rectangle frame)
	{
		Effect sheenShader = AssetLoader.LoadedShaders["ScarabeusIridescence"].Value;
		sheenShader.Parameters["sourceRect"].SetValue(new Vector4(frame.X, frame.Y, frame.Width, frame.Height));
		sheenShader.Parameters["resolution"].SetValue(texture.Size());
		sheenShader.Parameters["sheenOpacityMultiplier"].SetValue(0.15f);
		sheenShader.Parameters["saturationBoost"].SetValue(0.15f);
		sheenShader.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
		sheenShader.Parameters["sheenMasks"].SetValue(sheenTexture);
		sheenShader.Parameters["shellColorShift"].SetValue(scarabColorIndex * 0.3f);
		return sheenShader;
	}

	public const float WING_OPACITY = 0.75f;

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		if (NPC.Opacity == 0)
			return false;

		NPC.spriteDirection = NPC.direction;

		//Skip all of this if ball because drawing is entirely different
		if (currentFrame == RollFrame && (Profile == PhaseOneProfile || Profile == PhaseTwoProfile))
		{
			DrawBall(drawColor);
			return false;
		}

		Texture2D texture = Profile.Texture.Value;
		var bloom = AssetLoader.LoadedTextures["Bloom"].Value;
		var solid = TextureColorCache.ColorSolid(texture, Color.White);
		SpriteEffects effects = (NPC.spriteDirection == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		bool originAtFeet = OriginAtFeet();
		Vector2 position = originAtFeet ? NPC.Bottom : NPC.Center;
		Vector2 origin = originAtFeet ? new(108, 148) : new(108, 98);

		//Takeoff sheet is a big taller due to the wing deployment
		if (Profile == TakeoffProfile)
			origin = new(104, 154);

		if (effects == SpriteEffects.FlipHorizontally)
			origin.X = NPC.frame.Width - origin.X;

		position -= screenPos + new Vector2(0, NPC.IsABestiaryIconDummy ? 0 : 8);
		Vector2 positionOffset = Vector2.Zero;
		if (CurrentState == AIState.Roll && ExtraMemory > 0 && ExtraMemory < 3)
			positionOffset.Y += 16;
		position += positionOffset;

		Vector2 scale = new Vector2(2 - MathF.Pow(squishY, 2f), MathF.Pow(squishY, 0.7f)) * NPC.scale;
		if (squishY > 1)
			scale = new Vector2(1 - MathF.Pow(squishY - 1, 2f), 1f + MathF.Pow(squishY - 1, 0.7f)) * NPC.scale;

		if (trailOpacity > 0)
		{
			bool glowingImages = Profile == PhaseTwoProfile && (CurrentState != AIState.GroundPound) && CurrentState != AIState.Dig && CurrentState != AIState.Roll;

			Color color = glowingImages ? NPC.DrawColor(Color.PaleGoldenrod).Additive() : NPC.DrawColor(drawColor);
			color *= trailOpacity;

			for (int c = 0; c < NPCID.Sets.TrailCacheLength[Type]; c++)
			{
				Color trailColor = color * (1f - c / (float)NPCID.Sets.TrailCacheLength[Type]) * 0.5f;
				Main.EntitySpriteDraw(texture, NPC.oldPos[c] - Main.screenPosition + NPC.Size / 2 - new Vector2(0, 8) + positionOffset, NPC.frame, trailColor, NPC.oldRot[c], origin, NPC.scale, effects);
			}
		}

		if (_shakeTimer > 0)
			position += Main.rand.NextVector2CircularEdge(7f, 7f) * _shakeTimer / 20f;

		if (CurrentState == AIState.DeathAnim)
			drawColor = Color.Lerp(drawColor, Color.Black, Counter / 480f);

		if (CurrentState == AIState.Swarm) //Swarm flash visuals
		{
			Vector2 orbPos = NPC.Center + new Vector2(0f, -NPC.height / 2).RotatedBy(NPC.rotation) - Main.screenPosition;

			float opacity = 1f - Counter / 15f;
			if (opacity > 0)
			{
				Texture2D star = AssetLoader.LoadedTextures["Star"].Value;
				Texture2D star2 = AssetLoader.LoadedTextures["Star2"].Value;
				Color color = Color.Lerp(Color.LightGoldenrodYellow, Color.Goldenrod, 0.5f).Additive() * opacity;
				float flashScale = MathHelper.Lerp(0.5f, 1f, opacity);

				for (int i = 0; i < 2; i++)
				{
					float flashRotation = MathHelper.PiOver2 * (i + (float)(Main.timeForVisualEffects * 0.05f));

					Main.EntitySpriteDraw(star2, orbPos, null, color, flashRotation, star2.Size() / 2, flashScale * 1.5f, 0);
					Main.EntitySpriteDraw(star, orbPos, null, color, flashRotation, star.Size() / 2, flashScale * 2, 0);
				}
			}

			float opacity2 = 1f - Counter / 720f;
			if (opacity2 > 0)
			{
				Texture2D godrays = AssetLoader.LoadedTextures["GodrayCircle"].Value;
				Main.EntitySpriteDraw(godrays, orbPos, null, Color.Goldenrod.Additive() * opacity2, (float)(Main.timeForVisualEffects * 0.01f), godrays.Size() / 2, 0.3f * opacity2, 0);
				Main.EntitySpriteDraw(godrays, orbPos, null, Color.LightGoldenrodYellow.Additive() * opacity2, (float)(Main.timeForVisualEffects * 0.02f), godrays.Size() / 2, 0.3f * opacity2, 0);
			}
		}

		if (Profile.Simulated)
			DrawSimulated(spriteBatch, NPC.DrawColor(drawColor), effects);
		else
		{
			Effect sheenShader = GetShader(texture, Profile.SheenMask.Value, NPC.frame);
			FlipShadersOnOff(spriteBatch, sheenShader, true);
			Main.EntitySpriteDraw(texture, position, NPC.frame, NPC.DrawColor(drawColor), NPC.rotation, origin, scale, effects);
			FlipShadersOnOff(spriteBatch, null, false);

			if (Profile.Wings != null && Profile != SimulatedProfile) //Draw wings in transparent
			{
				Texture2D wings = Profile.Wings.Value;
				Main.EntitySpriteDraw(wings, position, NPC.frame, NPC.DrawColor(Color.White) * WING_OPACITY, NPC.rotation, origin, scale, effects);
			}

			if (Profile.GlowMask != null) //Draw a glowmask
			{
				Texture2D glowmask = Profile.GlowMask.Value;
				float lerp = 0.5f + (float)Math.Sin(Main.timeForVisualEffects / 30f) * 0.5f;
				Main.EntitySpriteDraw(glowmask, position, NPC.frame, NPC.DrawColor(Color.White), NPC.rotation, origin, scale, effects);

				DrawHelpers.DrawOutline(spriteBatch, glowmask, NPC.Center - Main.screenPosition, default, (offset) =>
					Main.EntitySpriteDraw(glowmask, position + offset, NPC.frame, NPC.DrawColor(Color.White).Additive(80) * 0.25f * lerp, NPC.rotation, origin, scale, effects));
			}
		}

		if (CurrentState == AIState.Charmed)
			DrawEmote(spriteBatch, (NPC.direction == -1) ? NPC.TopLeft : NPC.TopRight, EmoteID.EmotionLove);
		else if (CurrentState == AIState.Dance)
			DrawEmote(spriteBatch, ((NPC.direction == -1) ? NPC.TopLeft : NPC.TopRight) + new Vector2(0, (float)Math.Sin(Main.timeForVisualEffects / 30f) * 3), EmoteID.EmoteNote);

		//Utils.DrawBorderString(spriteBatch, CurrentState.ToString(), position - Vector2.UnitY * 80f, Color.White); //DEBUG STATE INDICATOR

		if (NPC.IsABestiaryIconDummy) //Bestiary hover interactions
		{
			Rectangle portraitBox = new((int)position.X - NPC.frame.Width / 2, (int)position.Y - NPC.frame.Height / 2, NPC.frame.Width, NPC.frame.Height);
			var dimensions = Main.BestiaryUI.GetDimensions().ToRectangle();
			Rectangle bestiaryBox = new(dimensions.X + (int)(dimensions.Width * 0.6f), dimensions.Y, (int)(dimensions.Width * 0.4f), dimensions.Height);

			int oldFrameX = currentFrame.X;
			currentFrame.X = (bestiaryBox.Contains(Main.MouseScreen.ToPoint()) && portraitBox.Contains(Main.MouseScreen.ToPoint())) ? 6 : 1;

			if (oldFrameX != currentFrame.X)
				currentFrame.Y = 0; //Reset
		}

		if (CurrentState == AIState.DeathAnim)
		{
			Main.spriteBatch.Draw(bloom, NPC.Center + new Vector2(-10f * NPC.direction, -20f).RotatedBy(NPC.rotation) - Main.screenPosition, null, Color.Orange.Additive() * (Counter / 360f), 0f, bloom.Size() / 2f, 1f, 0f, 0f);
		}

		return false;
	}

	private void DrawBall(Color lightColor)
	{
		const int ballPadding = 35;

		FlipShadersOnOff(Main.spriteBatch, null, true);

		Texture2D texture = BallProfile.Texture.Value;
		Texture2D sheenMask = BallProfile.SheenMask.Value;
		Rectangle frame = texture.Frame(1, 2, 0, phaseTwo ? 1 : 0);

		frame.Inflate(-ballPadding, -ballPadding);

		float squishAmount = MathHelper.Lerp(0, 0.07f, EaseFunction.EaseQuadIn.Ease(Math.Min(NPC.velocity.Length() / 20, 1)));
		var squishScale = new Vector2(1 + squishAmount, 1 - squishAmount);
		squishScale *= Vector2.Lerp(Vector2.One, new Vector2(squishY, 1 / squishY), 0.5f);

		var primDimensions = new Vector2(frame.Width * squishScale.X, frame.Height * squishScale.Y);
		bool flipped = NPC.spriteDirection > 0;
		float rotation = NPC.velocity.ToRotation() + MathHelper.PiOver2;
		if(NPC.velocity.Length() < 3)
			rotation = 0;

		IPrimitiveShape square = new SquarePrimitive()
		{
			Color = NPC.DrawColor(lightColor),
			Height = primDimensions.X,
			Length = primDimensions.Y,
			Position = NPC.Center - Main.screenPosition + Vector2.UnitY * (40 - (40 * squishScale.Y)),
			Rotation = rotation
		};

		Effect sheenShader = GetShader(texture, sheenMask, frame);

		sheenShader.Parameters["rotation"].SetValue((-NPC.rotation + rotation) * NPC.spriteDirection);
		sheenShader.Parameters["flip"].SetValue(flipped);

		Main.graphics.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
		Main.graphics.GraphicsDevice.Textures[0] = texture;
		PrimitiveRenderer.DrawPrimitiveShape(square, sheenShader, "BallPass");
	}

	public override void DrawBehind(int index)
	{
		if (CurrentState == AIState.DeathAnim)
			Main.instance.DrawCacheNPCsBehindNonSolidTiles.Add(index);
	}

	private static void DrawEmote(SpriteBatch spriteBatch, Vector2 position, int emote)
	{
		Texture2D texture = TextureAssets.Extra[ExtrasID.EmoteBubble].Value;
		SpriteEffects effect = SpriteEffects.None;

		Rectangle source = texture.Frame(EmoteBubble.EMOTE_SHEET_HORIZONTAL_FRAMES, EmoteBubble.EMOTE_SHEET_VERTICAL_FRAMES);
		Vector2 origin = new(source.Width / 2, source.Height);

		int frame = (int)Main.timeForVisualEffects / 12 % 2;
		source = texture.Frame(EmoteBubble.EMOTE_SHEET_HORIZONTAL_FRAMES, 39, emote * 2 % 8 + frame, 1 + emote / 4);

		DrawHelpers.DrawOutline(spriteBatch, texture, position - Main.screenPosition, Color.White, (offset) =>
			spriteBatch.Draw(texture, position - Main.screenPosition + offset, source, Color.White.Additive(), 0f, origin, 1f, effect, 0f));

		spriteBatch.Draw(texture, position - Main.screenPosition, source, Color.White, 0f, origin, 1f, effect, 0f);
	}

	public void FlipShadersOnOff(SpriteBatch spriteBatch, Effect effect, bool immediate)
	{
		if (NPC.IsABestiaryIconDummy)
		{
			RasterizerState priorRasterizer = spriteBatch.GraphicsDevice.RasterizerState;
			Rectangle priorScissorRectangle = spriteBatch.GraphicsDevice.ScissorRectangle;
			spriteBatch.End();
			spriteBatch.GraphicsDevice.RasterizerState = priorRasterizer;
			spriteBatch.GraphicsDevice.ScissorRectangle = priorScissorRectangle;
			spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, priorRasterizer, null, Main.UIScaleMatrix);

			if (effect == null)
				return;

			ShaderHelpers.GetWorldViewProjection(out Matrix view, out Matrix projection, false);

			if (effect.HasParameter("WorldViewProjection"))
				effect.Parameters["WorldViewProjection"].SetValue(view * projection);

			foreach (EffectPass pass in effect.CurrentTechnique.Passes.Where(x => x.Name == "DefaultPass"))
				pass.Apply();
		}
		else
		{
			spriteBatch.End();
			SpriteSortMode sortMode = immediate ? SpriteSortMode.Immediate : SpriteSortMode.Deferred;
			spriteBatch.Begin(sortMode, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.ZoomMatrix);

			if (effect == null)
				return;

			ShaderHelpers.GetWorldViewProjection(out Matrix view, out Matrix projection, false);

			if (effect.HasParameter("WorldViewProjection"))
				effect.Parameters["WorldViewProjection"].SetValue(view * projection);

			foreach (EffectPass pass in effect.CurrentTechnique.Passes.Where(x => x.Name == "DefaultPass"))
					pass.Apply();
		}
	}
}