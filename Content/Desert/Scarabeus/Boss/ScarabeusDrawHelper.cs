namespace SpiritReforged.Content.Desert.Scarabeus.Boss;

public partial class ScarabeusBoss : ModNPC
{
	private Dictionary<int, int> FrameCount = new Dictionary<int, int>
	{
		// Phase 1
		{0, 1}, //base
		{1, 8}, //walk
		{2, 10}, //horn swipe
		{3, 6}, //roll windup
		{4, 14}, //slam
		{5, 1}, //ball

		// Phase 2
		{6, 4} // flydle
	};

	private void AnimateFrame(int horizontalFrame, int framesPerSecond, bool loop = true)
	{
		if(_curFrame.X != horizontalFrame)
		{
			_curFrame.X = horizontalFrame;
			_curFrame.Y = 0;
		}

		NPC.frameCounter++;

		if (NPC.frameCounter > 60.0 / Math.Abs(framesPerSecond))
		{
			NPC.frameCounter = 0;
			bool reversed = framesPerSecond < 0;

			if (reversed)
			{
				if (_curFrame.Y > 0)
					_curFrame.Y--;

				else if (loop)
					_curFrame.Y = FrameCount[horizontalFrame] - 1;
			}

			else
			{
				if (_curFrame.Y < FrameCount[horizontalFrame] - 1)
					_curFrame.Y++;

				else if (loop)
					_curFrame.Y = 0;
			}
		}
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		if (_inGround)
			return false;

		// Draw afterimages before main body
		for (int i = NPCID.Sets.TrailCacheLength[NPC.type] - 1; i >= 0; i--)
		{
			if (!_contactDmgEnabled && CurrentPattern != (float)AIPatterns.Skitter)
				break;

			float progress = 1 - i / (float)NPCID.Sets.TrailCacheLength[NPC.type];
			Vector2 oldCenter = NPC.oldPos[i] - Main.screenPosition + NPC.Size / 2;

			DrawMain(drawColor * progress * 0.5f, oldCenter);
		}

		DrawMain(drawColor);
		return false;
	}

	private void DrawMain(Color drawColor, Vector2? overrideCenter = null)
	{
		// Note for animation functionality: 
		// The spritesheet should always be a multiple of 200, and the below number should be the multiplier to 200;
		// for example, it's 1600 right now, so this is 8. It used to be 1200, and thus was 6.
		const int horizontalFrames = 8;

		Texture2D bossTex = TextureAssets.Npc[NPC.type].Value;
		int verticalFrames = Main.npcFrameCount[NPC.type];

		// Further note: 
		// This, by default, assumes frames of equal size (200 pixels wide, unsure of the height) - there'll be a separate system
		// to use bespoke sizes, as phase 2 has some frames larger than 200 pixels wide. This is TBD
		var frameSize = new Point(bossTex.Width / horizontalFrames, _curFrame.Z == -1 ? bossTex.Height / verticalFrames : (int)_curFrame.Z);
		var drawFrame = new Rectangle((int)_curFrame.X * frameSize.X + 2, (int)_curFrame.Y * frameSize.Y + 2, frameSize.X - 4, frameSize.Y - 2);

		// Custom draw origin based on frame
		GetDrawOrigin(drawFrame, out Vector2 frameOrigin, out SpriteEffects flip);

		// Get draw position (used to account for afterimages)
		Vector2 drawPosition = overrideCenter ?? NPC.Center - Main.screenPosition;
		Color useColor = NPC.GetAlpha(drawColor);

		// Draw behind the NPC

		if ((AIPatterns)CurrentPattern == AIPatterns.FlyHover)
		{
			// Back carapace (WIP)
			DrawExtra(drawPosition, new Rectangle(76, 2204, 48, 52), drawFrame, useColor);
		}

		Main.EntitySpriteDraw(bossTex, drawPosition, drawFrame, useColor, NPC.rotation, frameOrigin, NPC.scale, flip);
	}

	private void GetDrawOrigin(Rectangle drawFrame, out Vector2 frameOrigin, out SpriteEffects flip)
	{
		frameOrigin = _curFrame.X switch
		{
			5 => new Vector2(80, 102),
			_ => new Vector2(100, 100),
		};

		flip = (NPC.direction > 0) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		if (NPC.direction < 0)
			frameOrigin.X = drawFrame.Width - frameOrigin.X;
	}

	private void DrawExtra(Vector2 drawPosition, Rectangle baseFrame, Rectangle drawFrame, Color useColor)
	{
		Texture2D bossTex = TextureAssets.Npc[NPC.type].Value;
		GetDrawOrigin(drawFrame, out Vector2 frameOrigin, out SpriteEffects flip);

		baseFrame.Y += (int)((baseFrame.Height + 2) * _curFrame.Y);

		Main.EntitySpriteDraw(bossTex, drawPosition, drawFrame, useColor, NPC.rotation, frameOrigin, NPC.scale, flip);
	}
}