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
		public readonly Asset<Texture2D> Texture;
		public readonly Asset<Texture2D> SheenMask;
		public readonly int Rows;
		private readonly int[] FrameCount;

		public int Columns => FrameCount.Length;
		/// <summary> Safely gets the number of frames in the provided column. </summary>
		public readonly int GetFrameCount(int column) => (column >= Columns || column < 0) ? 0 : FrameCount[column];

		public VisualProfile(Asset<Texture2D> Texture, Asset<Texture2D> SheenMask, int[] FrameCount)
		{
			this.Texture = Texture;
			this.SheenMask = SheenMask;
			this.FrameCount = FrameCount;
			Rows = FrameCount.OrderBy(static x => x).Last();
		}
	}

	public struct TrailData(bool enabled, bool isGlowing = false, float opacity = 1)
	{
		public bool Enabled = enabled;
		public bool Glowing = isGlowing;
		public float Opacity = opacity;
	}

	public enum FrameState { Progressed, Stopped, Looped }

	/// <summary> Roll frame for profile one. </summary>
	public static readonly Point RollFrame = new(0, 4);

	/// <summary> The current visual profile to be used for drawing. Contains frame count and texture information. </summary>
	public VisualProfile Profile { get; private set; }
	public static readonly Asset<Texture2D> Glowmask = DrawHelpers.RequestLocal<Scarabeus>("ScarabeusPhaseTwo_Glow", false);

	/// <summary> The selected frame in the 2D spritesheet.<para/>
	/// Prefer <see cref="UpdateFrame"/> and <see cref="SetFrame"/> for assignment. </summary>
	public Point currentFrame;
	/// <summary> Whether this NPC should draw a trail, whether it should be glowing or normal, and what the opacity should be. Resets every frame. </summary>
	public TrailData afterimageTrail;

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

		if (NPC.IsABestiaryIconDummy)
			UpdateFrame(currentFrame.X, 12, PhaseOneProfile);
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		if (NPC.Opacity == 0)
			return false;

		NPC.spriteDirection = NPC.direction;
		Texture2D texture = Profile.Texture.Value;
		SpriteEffects effects = (NPC.spriteDirection == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		Vector2 position = NPC.Center - screenPos - new Vector2(0, NPC.IsABestiaryIconDummy ? 20 : 8);
		Vector2 origin = new(108, 98);

		if (currentFrame == RollFrame)
		{
			DrawBall(texture, origin, drawColor);
		}

		else
		{
			if (afterimageTrail.Enabled)
			{
				Color color = afterimageTrail.Glowing ? NPC.DrawColor(Color.PaleGoldenrod).Additive() : NPC.DrawColor(drawColor);
				color *= afterimageTrail.Opacity;

				for (int c = 0; c < NPCID.Sets.TrailCacheLength[Type]; c++)
				{
					Color trailColor = color * (1f - c / (float)NPCID.Sets.TrailCacheLength[Type]) * 0.5f;
					Main.EntitySpriteDraw(texture, NPC.oldPos[c] - Main.screenPosition + NPC.Size / 2 - new Vector2(0, 8), NPC.frame, trailColor, NPC.oldRot[c], origin, NPC.scale, effects);
				}
			}

			Effect sheenShader = AssetLoader.LoadedShaders["ScarabeusIridescence"].Value;
			sheenShader.Parameters["sourceRect"].SetValue(new Vector4(NPC.frame.X, NPC.frame.Y, NPC.frame.Width, NPC.frame.Height));
			sheenShader.Parameters["resolution"].SetValue(texture.Size());
			sheenShader.Parameters["sheenOpacityMultiplier"].SetValue(0.15f);
			sheenShader.Parameters["saturationBoost"].SetValue(0.15f);
			sheenShader.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
			sheenShader.Parameters["sheenMasks"].SetValue(Profile.SheenMask.Value);
			FlipShadersOnOff(spriteBatch, sheenShader, true);

			Main.EntitySpriteDraw(texture, position, NPC.frame, NPC.DrawColor(drawColor), NPC.rotation, origin, NPC.scale, effects);

			FlipShadersOnOff(spriteBatch, null, false);

		}

		if (_charmed)
			DrawEmote(spriteBatch, (NPC.direction == -1) ? NPC.TopLeft : NPC.TopRight, EmoteID.EmotionLove);

		if (Profile == PhaseTwoProfile)
		{
			float lerp = 0.5f + (float)Math.Sin(Main.timeForVisualEffects / 30f) * 0.5f;
			Main.EntitySpriteDraw(Glowmask.Value, position, NPC.frame, NPC.DrawColor(Color.White), NPC.rotation, origin, NPC.scale, effects);

			DrawHelpers.DrawOutline(spriteBatch, Glowmask.Value, NPC.Center - Main.screenPosition, default, (offset) =>
				Main.EntitySpriteDraw(Glowmask.Value, position + offset, NPC.frame, NPC.DrawColor(Color.White).Additive(80) * 0.25f * lerp, NPC.rotation, origin, NPC.scale, effects));
		}

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

		return false;
	}

	private void DrawBall(Texture2D texture, Vector2 origin, Color lightColor)
	{
		float squishAmount = MathHelper.Lerp(0, 0.15f, EaseFunction.EaseQuadOut.Ease(Math.Min(NPC.velocity.Length() / 20, 1)));
		var squishScale = new Vector2(1 + squishAmount, 1 - squishAmount);
		List<SquarePrimitive> ballTrail = [];
		var primDimensions = new Vector2(140 * squishScale.X, 140 * squishScale.Y);
		bool flipped = NPC.spriteDirection > 0;
		for (int i = NPCID.Sets.TrailCacheLength[NPC.type] - 1; i > 0; i--)
		{
			float progress = 1 - i / (float)NPCID.Sets.TrailCacheLength[NPC.type];
			float trailOpacity = progress / 5;

			var square = new SquarePrimitive()
			{
				Color = NPC.DrawColor(lightColor) * trailOpacity,
				Height = primDimensions.X,
				Length = primDimensions.Y,
				Position = NPC.oldPos[i] + NPC.Size / 2 - Main.screenPosition + Vector2.UnitY * (40 - (40 * squishScale.Y)),
				Rotation = NPC.velocity.ToRotation() + MathHelper.PiOver2 - (flipped ? MathHelper.Pi : 0)
			};
			ballTrail.Add(square);
		}

		ballTrail.Add(new SquarePrimitive()
		{
			Color = NPC.DrawColor(lightColor),
			Height = primDimensions.X,
			Length = primDimensions.Y,
			Position = NPC.Center - Main.screenPosition + Vector2.UnitY * (40 - (40 * squishScale.Y)),
			Rotation = NPC.velocity.ToRotation() + MathHelper.PiOver2 - (flipped ? MathHelper.Pi : 0)
		});

		Effect sheenShader = AssetLoader.LoadedShaders["ScarabeusIridescence"].Value;
		sheenShader.Parameters["uTexture"].SetValue(BallProfile.Texture.Value);
		sheenShader.Parameters["sourceRect"].SetValue(new Vector4(0, 0, BallProfile.Texture.Width(), BallProfile.Texture.Height()));
		sheenShader.Parameters["resolution"].SetValue(BallProfile.Texture.Size());
		sheenShader.Parameters["sheenOpacityMultiplier"].SetValue(0.15f);
		sheenShader.Parameters["saturationBoost"].SetValue(0.15f);
		sheenShader.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
		sheenShader.Parameters["sheenMasks"].SetValue(BallProfile.SheenMask.Value);
		sheenShader.Parameters["rotation"].SetValue(-NPC.rotation * NPC.spriteDirection);
		sheenShader.Parameters["origin"].SetValue(new Vector2(38, 38));
		sheenShader.Parameters["flip"].SetValue(flipped);

		PrimitiveRenderer.DrawPrimitiveShapeBatched(ballTrail.ToArray(), sheenShader, "BallPass");
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
			spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, priorRasterizer, null, Main.UIScaleMatrix);

			if (effect == null)
				return;

			foreach (EffectPass pass in effect.CurrentTechnique.Passes)
				if(pass.Name == "GeometricStyle")
					pass.Apply();
		}
		else
		{
			spriteBatch.End();
			SpriteSortMode sortMode = immediate ? SpriteSortMode.Immediate : SpriteSortMode.Deferred;
			spriteBatch.Begin(sortMode, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

			if (effect == null)
				return;

			foreach (EffectPass pass in effect.CurrentTechnique.Passes)
				if (pass.Name == "GeometricStyle")
					pass.Apply();
		}
	}
}