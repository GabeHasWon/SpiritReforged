namespace SpiritReforged.Content.Desert.Scarabeus.Boss;

public partial class ScarabeusBoss : ModNPC
{
	private IDictionary<int, int> FrameCount = new Dictionary<int, int>
	{
		{0, 1}, //base
		{1, 8}, //walk
		{2, 10}, //horn swipe
		{3, 6}, //roll windup
		{4, 14}, //slam
		{5, 1}, //ball
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

		Texture2D bossTex = TextureAssets.Npc[NPC.type].Value;
		int verticalFrames = Main.npcFrameCount[NPC.type];
		const int horizontalFrames = 6;
		var frameSize = new Point(bossTex.Width / horizontalFrames, bossTex.Height / verticalFrames);

		var drawFrame = new Rectangle((int)_curFrame.X * frameSize.X + 2, (int)_curFrame.Y * frameSize.Y + 2, frameSize.X - 4, frameSize.Y - 2);

		//custom draw origin based on frame
		var frameOrigin = _curFrame.X switch
		{
			5 => new Vector2(80, 102),
			_ => new Vector2(100, 100),
		};

		var flip = (NPC.direction > 0) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		if (NPC.direction < 0)
			frameOrigin.X = drawFrame.Width - frameOrigin.X;

		//basic trail draw
		for(int i = NPCID.Sets.TrailCacheLength[NPC.type] - 1; i >= 0; i--)
		{
			if (!_contactDmgEnabled && CurrentPattern != (float)AIPatterns.Skitter)
				break;

			float progress = 1 - (i / (float)NPCID.Sets.TrailCacheLength[NPC.type]);
			Vector2 oldCenter = NPC.oldPos[i] - Main.screenPosition + NPC.Size / 2;

			Main.EntitySpriteDraw(bossTex, oldCenter, drawFrame, NPC.GetAlpha(drawColor) * progress * 0.5f, NPC.oldRot[i], frameOrigin, NPC.scale, flip);
		}

		Main.EntitySpriteDraw(bossTex, NPC.Center - Main.screenPosition, drawFrame, NPC.GetAlpha(drawColor), NPC.rotation, frameOrigin, NPC.scale, flip);

		return false;
	}
}