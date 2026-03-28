using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Content.Forest.Safekeeper;
using SpiritReforged.Content.Vanilla.Food;
using SpiritReforged.Content.Ziggurat.Biome;
using SpiritReforged.Content.Ziggurat.Vanity;
using System.Diagnostics;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Ziggurat.NPCs.Mummy;

[AutoloadBanner]
internal class DecrepitMummy : ModNPC
{
	private class TrapSpawnedRule(bool isTrapped) : IItemDropRuleCondition
	{
		public bool CanDrop(DropAttemptInfo info) => isTrapped == (info.npc.SpawnedFromStatue && info.npc.type == ModContent.NPCType<DecrepitMummy>());
		public bool CanShowItemDropInUI() => !isTrapped; // Only the default drops will appear instead of having duplicates
		public string GetConditionDescription() => string.Empty;
	}

	public static readonly SoundStyle[] MummyMoan = [
	SoundID.Zombie3 with
	{
		Pitch = 0.5f,
		PitchVariance = 0.25f
	},
	SoundID.Zombie4 with
	{
		Pitch = 0.5f,
		PitchVariance = 0.25f
	}];

	public float LifeProgress => 1f - NPC.life / (float)NPC.lifeMax;
	public ref float Style => ref NPC.ai[0];

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 15;
		PersistentNPCSystem.PersistentTypes.Add(Type);

		MoRHelper.AddNPCToElementList(Type, MoRHelper.NPCType_Undead);
		MoRHelper.AddNPCToElementList(Type, MoRHelper.NPCType_Humanoid);
	}

	public override void SetDefaults()
	{
		NPC.CloneDefaults(NPCID.Mummy);
		NPC.lifeMax = 80;
		NPC.damage = 28;
		NPC.aiStyle = -1;

		SpawnModBiomes = [ModContent.GetInstance<ZigguratBiome>().Type];
		UndeadNPC.UndeadTypes.Add(Type);
	}

	public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
	{
		NPC.lifeMax = ModeUtils.ByMode(90, 100, 130, 190);
		NPC.damage = ModeUtils.ByMode(20, 37, 40, 60);
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "");

	public override void OnSpawn(IEntitySource source)
	{
		Style = Main.rand.Next(3);
		NPC.netUpdate = true;
	}

	public override void AI()
	{
		const float JumpSpeed = 5f;

		NPC.TargetClosest(false);

		Player target = Main.player[NPC.target];
		float moveSpeed = MathHelper.Lerp(1f, 2f, LifeProgress);
		int direction = (target.Center.X < NPC.Center.X) ? -1 : 1;

		NPC.velocity.X += direction * 0.05f;
		NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X, -moveSpeed, moveSpeed * NPC.frame.Y switch
		{
			700 or 780 or 300 => 0.8f,
			240 or 660 => 0.6f,
			_ => 1f
		});

		Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
		bool grounded = Collision.SolidCollision(NPC.BottomLeft, NPC.width, 6) && NPC.velocity.Y >= 0;

		// Jump when blocked on either side
		if (NPC.velocity.X == direction * 0.05f)
		{
			if (Collision.SolidCollision(NPC.position + new Vector2(NPC.width, 0), 8, NPC.height - 2) && NPC.collideY && grounded)
				NPC.velocity.Y = -JumpSpeed;

			if (Collision.SolidCollision(NPC.position - new Vector2(8, 0), 8, NPC.height - 2) && NPC.collideY && grounded)
				NPC.velocity.Y = -JumpSpeed;
		}

		if (Main.rand.NextBool(1000))
			SoundEngine.PlaySound(MummyMoan[Main.rand.Next(MummyMoan.Length)], NPC.Center);

		if ((Math.Sign(NPC.velocity.X) != direction) || NPC.direction != direction)
			NPC.direction = NPC.spriteDirection = Math.Sign(direction);
	}

	public override void FindFrame(int frameHeight)
	{
		float frameRate = MathHelper.Lerp(0.2f, 0.5f, LifeProgress);
		NPC.frameCounter = (NPC.frameCounter + frameRate) % Main.npcFrameCount[Type];

		NPC.frame.Width = 44;
		NPC.frame.Y = (int)NPC.frameCounter * frameHeight;
		NPC.frame.X = (int)(NPC.frame.Width * Style);
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (Main.dedServ)
			return;

		Vector2 velocity = NPC.velocity;

		for (int i = 0; i < 3; i++)
			Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Bone, velocity.X, velocity.Y);

		if (hit.Damage > NPC.lifeMax / 8)
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, velocity, Mod.Find<ModGore>("Mummy" + Main.rand.NextFromList(7, 8)).Type, 1f);

		if (NPC.life <= 0)
		{
			for (int i = 1; i < 4; i++)
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, velocity, Mod.Find<ModGore>("Mummy" + i).Type, 1f);

			Gore.NewGore(NPC.GetSource_Death(), NPC.position, velocity, Mod.Find<ModGore>("Mummy" + (4 + (int)Style)).Type, 1f);

			for (int i = 0; i < 12; i++)
			{
				var dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Smoke, velocity.X, -1, 100);
				dust.fadeIn = 2;
				dust.noGravity = true;
			}
		}
	}

	public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
	{
		if ((Main.expertMode || Main.masterMode) && Main.rand.NextBool(8))
			target.AddBuff(BuffID.Slow, 60 * 15);
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo) => spawnInfo.Common() && ZigguratGlobalNPC.InBiome(spawnInfo) ? 0.005f : 0;

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		LeadingConditionRule isTrapSpawned = new(new TrapSpawnedRule(true));
		isTrapSpawned.OnSuccess(ItemDropRule.OneFromOptions(4, ItemID.FastClock, ItemID.MummyMask, ItemID.MummyShirt, ItemID.MummyPants));
		LeadingConditionRule notTrapSpawned = new(new TrapSpawnedRule(false));
		notTrapSpawned.OnFailedConditions(ItemDropRule.OneFromOptions(20, ItemID.FastClock, ItemID.MummyMask, ItemID.MummyShirt, ItemID.MummyPants));
		npcLoot.Add(isTrapSpawned);
		npcLoot.Add(notTrapSpawned);

		npcLoot.AddCommon(ModContent.ItemType<CarrotCake>(), 25);

		int maskType = Main.rand.NextBool() ? ModContent.ItemType<BullRitualMask>() : ModContent.ItemType<AvianRitualMask>();
		npcLoot.AddCommon(maskType, 33);
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		NPC.spriteDirection = NPC.direction;

		Texture2D texture = TextureAssets.Npc[Type].Value;
		Rectangle source = NPC.frame with { Height = NPC.frame.Height - 2, Width = 42 }; //Remove padding
		Vector2 position = NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY - (source.Height - NPC.height) / 2 + 2);
		SpriteEffects effects = (NPC.spriteDirection == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		Main.EntitySpriteDraw(texture, position, source, NPC.DrawColor(drawColor), NPC.rotation, source.Size() / 2, NPC.scale, effects);
		return false;
	}
}
