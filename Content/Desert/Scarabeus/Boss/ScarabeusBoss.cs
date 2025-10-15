using System.IO;
using System.Linq;
using Terraria.Audio;
using Terraria.GameContent.Events;
using Terraria.GameContent.Bestiary;
using SpiritReforged.Common.Visuals.Glowmasks;

namespace SpiritReforged.Content.Desert.Scarabeus.Boss;

[AutoloadBossHead]
[AutoloadGlowmask("255, 255, 255", false)]
public class ScarabeusBoss : ModNPC
{
	public float AiTimer { get => NPC.ai[0]; set => NPC.ai[0] = value; }
	public float CurrentPattern { get => NPC.ai[1]; set => NPC.ai[1] = value; }

	private enum AIPatterns
	{
		RollDash,
		GroundedSlam,
		Dig,
		Leap,
		BounceGroundPound,
		FlyingDash,
		ChainGroundPound,
		DigErupt,
		Sunbeams,
		KamikazeScarabs
	}

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[NPC.type] = 22;
		NPCID.Sets.TrailCacheLength[NPC.type] = 5;
		NPCID.Sets.TrailingMode[NPC.type] = 0;

		var drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers()
		{
			Position = new Vector2(8f, 12f),
			PortraitPositionXOverride = 0f
		};
		NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, drawModifiers);
	}

	public override void SetDefaults()
	{
		NPC.width = 64;
		NPC.height = 64;
		NPC.value = 30000;
		NPC.damage = 40;
		NPC.defense = 10;
		NPC.lifeMax = 1750;
		NPC.aiStyle = -1;
		Music = MusicLoader.GetMusicSlot(Mod, "Sounds/Music/ScarabeusBoss");
		NPC.boss = true;
		NPC.npcSlots = 15f;
		NPC.HitSound = SoundID.NPCHit31;
		NPC.DeathSound = SoundID.NPCDeath5;
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "Desert");

	public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
	{
		NPC.lifeMax = (int)(NPC.lifeMax * (Main.masterMode ? 0.85f : 1.0f) * 0.7143f * balance);
		NPC.damage = (int)(NPC.damage * 0.626f);
	}

	public override bool CheckActive()
	{
		Player player = Main.player[NPC.target];
		if (!player.active || player.dead)
			return false;

		return true;
	}

	public override void AI()
	{
		NPC.TargetClosest(true);
		Player player = Main.player[NPC.target];

		switch (CurrentPattern)
		{

		}
	}

	private void RollDash()
	{

	}

	#region utilities
	private void CheckPlatform(Player player)
	{
		bool onplatform = true;
		for (int i = (int)NPC.position.X; i < NPC.position.X + NPC.width; i += NPC.width / 4)
		{ //check tiles beneath the boss to see if they are all platforms
			Tile tile = Framing.GetTileSafely(new Point((int)NPC.position.X / 16, (int)(NPC.position.Y + NPC.height + 8) / 16));
			if (!TileID.Sets.Platforms[tile.TileType])
				onplatform = false;
		}

		if (onplatform && NPC.Center.Y < player.position.Y - 20) //if they are and the player is lower than the boss, temporarily let the boss ignore tiles to go through them
			NPC.noTileCollide = true;
		else
			NPC.noTileCollide = false;
	}

	private void CheckPit(float velmult = 1.7f, bool boostsxvel = true) //quirky lazy bad code but it works mostly and making the boss not break on vanilla random worldgen is tiring
	{
		if (NPC.velocity.Y != 0)
			return;

		bool pit = true;
		int pitwidth = 0;
		int width = 5;
		int height = 8;
		for (int j = 1; j <= width; j++)
		{
			for (int i = 1; i <= height; i++)
			{
				Tile forwardtile = Framing.GetTileSafely(new Point((int)(NPC.Center.X / 16) + NPC.spriteDirection * j, (int)(NPC.Center.Y / 16) + i));
				if (WorldGen.SolidTile(forwardtile) || WorldGen.SolidTile2(forwardtile) || WorldGen.SolidTile3(forwardtile))
				{
					pit = false;
					break;
				}
			}

			if (!pit)
				break;

			pitwidth++;
		}

		if (pit && pitwidth <= width * 2)
		{
			NPC.velocity.Y -= pitwidth * velmult;
			if (boostsxvel)
				NPC.velocity.X = NPC.spriteDirection * pitwidth * velmult;
		}
		else if (pit)
			NPC.velocity.X *= -1f;
	}

	private void UpdateFrame(int speed, int minframe, int maxframe, bool usesspeed = false) //method of updating the frame without copy pasting this every time animation is needed
	{
		timer++;
		float timeperframe = usesspeed ? 5f / Math.Abs(NPC.velocity.X) * speed : speed;
		if (timer >= timeperframe)
		{
			frame++;
			timer = 0;
		}

		if (frame >= maxframe)
			frame = minframe;

		if (frame < minframe)
			frame = minframe;
	}

	private void SyncNPC()
	{
		if (Main.netMode != NetmodeID.SinglePlayer)
			NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
	}

	private void NextAttack(bool skipto4 = false) //reset most variables and netupdate to sync the boss in multiplayer
	{
		trailBehind = false;
		if (skipto4)
			CurrentPattern = 4;
		else
			CurrentPattern++;
		AiTimer = 0;
		NPC.ai[2] = 0;
		NPC.rotation = 0;
		NPC.noTileCollide = false;
		NPC.noGravity = false;
		hasjumped = false;
		NPC.behindTiles = false;
		NPC.knockBackResist = 0f;
		BaseVel = Vector2.UnitX;
		statictarget[0] = Vector2.Zero;
		statictarget[1] = Vector2.Zero;
		SyncNPC();
	}

	private void StepUp(Player player)
	{
		bool flag15 = true; //copy pasted collision step code from zombies
		if (player.Center.Y * 16 - 32 > NPC.position.Y)
			flag15 = false;

		if (!flag15 && NPC.velocity.Y == 0f)
			Collision.StepDown(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);

		if (NPC.velocity.Y >= 0f)
			Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY, 1, flag15, 1);
	}
	#endregion

	public override void SendExtraAI(BinaryWriter writer)
	{
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
	}

	public override int SpawnNPC(int tileX, int tileY)
	{
		NPC.velocity.Y = 1;
		return base.SpawnNPC(tileX, tileY);
	}

	public override bool CanHitPlayer(Player target, ref int cooldownSlot) => canHitPlayer;
	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		SpriteEffects effects = NPC.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
		spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, NPC.Center - screenPos + new Vector2(-10 * NPC.spriteDirection, NPC.gfxOffY - 16 + extraYoff).RotatedBy(NPC.rotation), NPC.frame,
						 drawColor, NPC.rotation, NPC.frame.Size() / 2, NPC.scale, effects, 0);
		if (trailBehind)
		{
			Vector2 drawOrigin = NPC.frame.Size() / 2;
			for (int k = 0; k < NPC.oldPos.Length; k++)
			{
				Vector2 drawPos = NPC.oldPos[k] - screenPos + new Vector2(NPC.width / 2, NPC.height / 2) + new Vector2(-10 * NPC.spriteDirection, NPC.gfxOffY - 16 + extraYoff).RotatedBy(NPC.rotation);
				Color color = NPC.GetAlpha(drawColor) * (float)((NPC.oldPos.Length - k) / (float)NPC.oldPos.Length / 2);
				spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, drawPos, NPC.frame, color, NPC.rotation, drawOrigin, NPC.scale, effects, 0f);
			}
		}

		return false;
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		for (int k = 0; k < 5; k++)
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, hit.HitDirection, -1f, 0, default, 1f);

		if (NPC.life <= 0 && Main.netMode != NetmodeID.Server)
		{
			//SoundEngine.TryGetActiveSound(wingSoundSlot, out ActiveSound sound);

			//if (sound is not null && sound.IsPlaying)
			//{
			//	sound.Stop();
				//wingSoundSlot = SlotId.Invalid;
			//}

			SpawnGores();
		}
	}

	public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
	{
		modifiers.Knockback *= 0.7f;

		if (Main.player[projectile.owner].HeldItem.type == ItemID.Minishark)
		{
			//shadow nerfing minishark on scarab because meme balance weapon
			modifiers.Knockback *= 0.5f;
			modifiers.FinalDamage *= 0.6f;
		}

		if (!Main.player[projectile.owner].ZoneDesert)
			modifiers.FinalDamage /= 3;
	}

	public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
	{
		if (!player.ZoneDesert)
			modifiers.FinalDamage /= 3;
	}

	public override void FindFrame(int frameHeight)
	{
		if (NPC.IsABestiaryIconDummy)
		{
			if (frame < 18)
				frame = 18;

			NPC.frameCounter += 1;

			if (NPC.frameCounter > 4)
			{
				frame++;
				NPC.frameCounter = 0;
			}

			if (frame > 21)
				frame = 18;
		}

		NPC.frame.Y = frameHeight * frame;
	}

	public override bool PreKill()
	{
		if (Main.netMode != NetmodeID.SinglePlayer)
			NetMessage.SendData(MessageID.WorldData);

		//NPC.PlayDeathSound("ScarabDeathSound");
		return true;
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		/*npcLoot.AddMasterModeRelicAndPet<ScarabeusRelicItem, ScarabPetItem>();
		npcLoot.AddBossBag<BagOScarabs>();

		var notExpertRule = new LeadingConditionRule(new Conditions.NotExpert());
		notExpertRule.AddCommon<ScarabMask>(7);
		notExpertRule.AddCommon<Trophy1>(10);
		notExpertRule.AddCommon<SandsOfTime>(15);
		notExpertRule.AddCommon<Chitin>(1, 25, 36);
		notExpertRule.AddOneFromOptions<ScarabBow, LocustCrook, RoyalKhopesh, RadiantCane>();

		npcLoot.Add(notExpertRule);*/
	}

	private void SpawnGores()
	{
		for (int i = 1; i <= 7; i++)
			Gore.NewGoreDirect(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Scarab" + i.ToString()).Type, 1f);

		NPC.position += NPC.Size / 2;
		NPC.Size = new Vector2(100, 60);
		NPC.position -= NPC.Size / 2;

		static int randomDustType() => Main.rand.Next(3) switch
		{
			0 => 5,
			1 => 36,
			_ => 32,
		};

		for (int i = 0; i < 30; i++)
			Dust.NewDustDirect(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, randomDustType(), 0f, 0f, 100, default, Main.rand.NextBool() ? 2f : 0.5f).velocity *= 3f;

		for (int j = 0; j < 50; j++)
		{
			var dust = Dust.NewDustDirect(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, randomDustType(), 0f, 0f, 100, default, 1f);
			dust.velocity *= 5f;
			dust.noGravity = true;

			Dust.NewDustDirect(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, randomDustType(), 0f, 0f, 100, default, .82f).velocity *= 2f;
		}
	}
}