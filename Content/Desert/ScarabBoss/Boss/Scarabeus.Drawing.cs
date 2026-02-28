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

	public VisualProfile Profile { get; private set; }
	public static readonly Asset<Texture2D> Glowmask = DrawHelpers.RequestLocal<Scarabeus>("ScarabeusPhaseTwo_Glow", false);

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

			if (framesPerSecond < 0) //Reverse the animation
				currentFrame.Y = loop ? ((currentFrame.Y > 0) ? currentFrame.Y - 1 : Profile.FrameCount[column] - 1) : Math.Max(currentFrame.Y - 1, 0);
			else
				currentFrame.Y = loop ? ((currentFrame.Y + 1) % Profile.FrameCount[column]) : Math.Min(currentFrame.Y + 1, Profile.FrameCount[column] - 1);
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
		NPC.spriteDirection = NPC.direction;
		Texture2D texture = Profile.Texture.Value;
		SpriteEffects effects = (NPC.spriteDirection == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		Vector2 origin = new(100, 110);

		/*if (false)
		{
			for (int c = 0; c < NPCID.Sets.TrailCacheLength[Type]; c++)
			{
				Color trailColor = NPC.DrawColor(drawColor) * (1f - c / (float)NPCID.Sets.TrailCacheLength[Type]) * 0.5f;
				Main.EntitySpriteDraw(texture, NPC.oldPos[c] - Main.screenPosition + NPC.Size / 2, NPC.frame, trailColor, NPC.oldRot[c], origin, NPC.scale, effects);
			}
		}*/

		Main.EntitySpriteDraw(texture, NPC.Center - Main.screenPosition, NPC.frame, NPC.DrawColor(drawColor), NPC.rotation, origin, NPC.scale, effects);

		if (Profile == PhaseTwoProfile)
		{
			float lerp = 0.5f + (float)Math.Sin(Main.timeForVisualEffects / 30f) * 0.5f;
			Main.EntitySpriteDraw(Glowmask.Value, NPC.Center - Main.screenPosition, NPC.frame, NPC.DrawColor(Color.White), NPC.rotation, origin, NPC.scale, effects);

			DrawHelpers.DrawOutline(spriteBatch, Glowmask.Value, NPC.Center - Main.screenPosition, default, (offset) =>
				Main.EntitySpriteDraw(Glowmask.Value, NPC.Center - Main.screenPosition + offset, NPC.frame, NPC.DrawColor(Color.White).Additive(80) * 0.25f * lerp, NPC.rotation, origin, NPC.scale, effects));
		}

		return false;
	}
}