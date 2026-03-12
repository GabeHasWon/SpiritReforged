using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Visuals;
using System.Linq;

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

	/// <summary> Dig frame for profile one. </summary>
	public static readonly Point DigFrame = new(0, 4);

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

	public void FlipShadersOnOff(SpriteBatch spriteBatch, Effect effect, bool immediate)
	{
		if (NPC.IsABestiaryIconDummy)
		{
			RasterizerState priorRasterizer = spriteBatch.GraphicsDevice.RasterizerState;
			Rectangle priorScissorRectangle = spriteBatch.GraphicsDevice.ScissorRectangle;
			spriteBatch.End();
			spriteBatch.GraphicsDevice.RasterizerState = priorRasterizer;
			spriteBatch.GraphicsDevice.ScissorRectangle = priorScissorRectangle;
			spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, priorRasterizer, effect, Main.UIScaleMatrix);
		}
		else
		{
			spriteBatch.End();
			SpriteSortMode sortMode = immediate ? SpriteSortMode.Immediate : SpriteSortMode.Deferred;
			spriteBatch.Begin(sortMode, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.Transform);
		}
	}
}