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
		public readonly int Rows;
		private readonly int[] FrameCount;

		public int Columns => FrameCount.Length;
		/// <summary> Safely gets the number of frames in the provided column. </summary>
		public readonly int GetFrameCount(int column) => (column >= Columns || column < 0) ? 0 : FrameCount[column];

		public VisualProfile(Asset<Texture2D> Texture, int[] FrameCount)
		{
			this.Texture = Texture;
			this.FrameCount = FrameCount;
			Rows = FrameCount.OrderBy(static x => x).Last();
		}
	}

	public enum FrameState { Progressed, Stopped, Looped }

	/// <summary> The current visual profile to be used for drawing. Contains frame count and texture information. </summary>
	public VisualProfile Profile { get; private set; }
	public static readonly Asset<Texture2D> Glowmask = DrawHelpers.RequestLocal<Scarabeus>("ScarabeusPhaseTwo_Glow", false);

	public Point currentFrame;
	/// <summary> Whether this NPC should draw a trail. Resets every frame. </summary>
	public bool showTrail;

	private FrameState UpdateFrame(int column, int framesPerSecond, bool loop = true)
	{
		FrameState result = FrameState.Progressed;

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
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		NPC.spriteDirection = NPC.direction;
		Texture2D texture = Profile.Texture.Value;
		SpriteEffects effects = (NPC.spriteDirection == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		Vector2 position = NPC.Center - Main.screenPosition - new Vector2(0, 8);
		Vector2 origin = new(108, 98);

		if (showTrail)
		{
			Color color = phaseTwo ? NPC.DrawColor(Color.PaleGoldenrod).Additive() : NPC.DrawColor(drawColor);

			for (int c = 0; c < NPCID.Sets.TrailCacheLength[Type]; c++)
			{
				Color trailColor = color * (1f - c / (float)NPCID.Sets.TrailCacheLength[Type]) * 0.5f;
				Main.EntitySpriteDraw(texture, NPC.oldPos[c] - Main.screenPosition + NPC.Size / 2 - new Vector2(0, 8), NPC.frame, trailColor, NPC.oldRot[c], origin, NPC.scale, effects);
			}
		}

		Main.EntitySpriteDraw(texture, position, NPC.frame, NPC.DrawColor(drawColor), NPC.rotation, origin, NPC.scale, effects);

		if (Profile == PhaseTwoProfile)
		{
			float lerp = 0.5f + (float)Math.Sin(Main.timeForVisualEffects / 30f) * 0.5f;
			Main.EntitySpriteDraw(Glowmask.Value, position, NPC.frame, NPC.DrawColor(Color.White), NPC.rotation, origin, NPC.scale, effects);

			DrawHelpers.DrawOutline(spriteBatch, Glowmask.Value, NPC.Center - Main.screenPosition, default, (offset) =>
				Main.EntitySpriteDraw(Glowmask.Value, position + offset, NPC.frame, NPC.DrawColor(Color.White).Additive(80) * 0.25f * lerp, NPC.rotation, origin, NPC.scale, effects));
		}

		return false;
	}
}