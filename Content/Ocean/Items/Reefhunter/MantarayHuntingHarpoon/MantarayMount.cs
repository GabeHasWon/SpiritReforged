﻿namespace SpiritReforged.Content.Ocean.Items.Reefhunter.MantarayHuntingHarpoon;

public class MantarayMount : ModMount
{
	private float _frameProgress;
	private const float MaxSpeed = 16.5f;

	public override void SetStaticDefaults()
	{
		MountData.spawnDust = 103;
		MountData.buff = ModContent.BuffType<MantarayBuff>();
		MountData.heightBoost = 14;
		MountData.flightTimeMax = 0;
		MountData.fatigueMax = 0;
		MountData.fallDamage = 0.0f;
		MountData.usesHover = true;
		MountData.runSpeed = 8f;
		MountData.dashSpeed = 3f;
		MountData.acceleration = 0.35f;
		MountData.constantJump = false;
		MountData.jumpHeight = 10;
		MountData.jumpSpeed = 3f;
		MountData.swimSpeed = 95f;
		MountData.blockExtraJumps = true;
		MountData.totalFrames = 7;

		int[] yOffsets = new int[MountData.totalFrames];
		for (int index = 0; index < yOffsets.Length; ++index)
			yOffsets[index] = 12;
		MountData.playerYOffsets = yOffsets;

		MountData.xOffset = -10;
		MountData.bodyFrame = 3;
		MountData.yOffset = 20;
		MountData.playerHeadOffset = 31;
		MountData.standingFrameCount = 7;
		MountData.standingFrameDelay = 4;
		MountData.standingFrameStart = 0;
		MountData.runningFrameCount = 7;
		MountData.runningFrameDelay = 4;
		MountData.runningFrameStart = 0;
		MountData.flyingFrameCount = 7;
		MountData.flyingFrameDelay = 4;
		MountData.flyingFrameStart = 0;
		MountData.inAirFrameCount = 7;
		MountData.inAirFrameDelay = 4;
		MountData.inAirFrameStart = 0;
		MountData.idleFrameCount = 0;
		MountData.idleFrameDelay = 0;
		MountData.idleFrameStart = 0;
		MountData.idleFrameLoop = false;
		MountData.swimFrameCount = 7;
		MountData.swimFrameDelay = 12;
		MountData.swimFrameStart = 0;

		if (Main.netMode != NetmodeID.Server)
		{
			MountData.textureWidth = MountData.backTexture.Width();
			MountData.textureHeight = MountData.backTexture.Height();
		}
	}

	public override void UpdateEffects(Player player)
	{
		if (player.velocity.Y <= -MaxSpeed)
			player.velocity.Y = -MaxSpeed;

		player.fullRotationOrigin = (player.Hitbox.Size() + new Vector2(0, 42)) / 2;
		int direction = (Math.Abs(player.velocity.X) == 0) ? 0 :
			(player.direction == Math.Sign(player.velocity.X)) ? 1 : -1;
		player.fullRotation = player.velocity.Y * 0.05f * player.direction * direction * MountData.jumpHeight / 14f;

		if (!player.wet)
		{
			MountData.flightTimeMax = 0;
			MountData.usesHover = false;
			MountData.acceleration = 0.05f;
			MountData.dashSpeed = 0f;
			MountData.runSpeed = 1.3f;
			player.autoJump = true;

			if (player.velocity.Y == 0 && player.oldVelocity.Y == 0) //Grounded check
				MountData.runSpeed = 0.1f;
		}
		else
		{
			MountData.flightTimeMax = 9999;
			MountData.fatigueMax = 9999;
			MountData.acceleration = 0.2f;
			MountData.dashSpeed = 3f;
			MountData.runSpeed = 12f;
			MountData.usesHover = true;

			player.gravity = 0f;
		}

		player.gills = true;
	}

	public override bool UpdateFrame(Player mountedPlayer, int state, Vector2 velocity)
	{
		//2% of the total frames per tick, up to 6% based on how fast you're moving
		_frameProgress += 0.02f + Math.Min(velocity.Length() / MaxSpeed, 1) * 0.04f;
		_frameProgress %= 1;
		mountedPlayer.mount._frame = (int)(MountData.totalFrames * _frameProgress);

		//Fun fact! If you return false here for god knows what reason the mount isn't able to hover/swim
		//So the default framecounter is constantly set to 0 amd we return true instead as a bandaid fix
		mountedPlayer.mount._frameCounter = 0;
		return true;
	}
}