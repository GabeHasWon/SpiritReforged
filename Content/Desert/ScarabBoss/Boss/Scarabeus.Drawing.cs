using SpiritReforged.Common.NPCCommon;
using System.Linq;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

public partial class Scarabeus : ModNPC
{
	public readonly record struct VisualProfile
	{
		public readonly Asset<Texture2D> Texture;
		public readonly int[] FrameCount;
		public readonly int Rows;

		public int Columns => FrameCount.Length;

		public VisualProfile(Asset<Texture2D> Texture, int[] FrameCount)
		{
			this.Texture = Texture;
			this.FrameCount = FrameCount;
			Rows = FrameCount.OrderBy(static x => x).Last();
		}
	}

	public VisualProfile Profile => phaseTwo ? PhaseTwoProfile : PhaseOneProfile;

	public Point currentFrame;

	private void UpdateFrame(int column, int framesPerSecond, bool loop = true)
	{
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

			if (reversed)
			{
				if (currentFrame.Y > 0)
					currentFrame.Y--;
				else if (loop)
					currentFrame.Y = Profile.FrameCount[column] - 1;
			}
			else
			{
				if (currentFrame.Y < Profile.FrameCount[column] - 1)
					currentFrame.Y++;
				else if (loop)
					currentFrame.Y = 0;
			}
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
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		Texture2D texture = Profile.Texture.Value;
		SpriteEffects effects = (NPC.spriteDirection == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		Vector2 origin = new(100, 110);

		Main.EntitySpriteDraw(texture, NPC.Center - Main.screenPosition, NPC.frame, NPC.DrawColor(drawColor), NPC.rotation, origin, NPC.scale, effects);
		return false;
	}
}