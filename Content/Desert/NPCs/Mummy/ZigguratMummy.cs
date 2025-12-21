using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Desert.Biome;
using SpiritReforged.Content.Desert.Tiles;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Desert.NPCs.Mummy;

[AutoloadBanner]
internal class ZigguratMummy : ModNPC
{
	private class TrapSpawnedRule(bool isTrapped) : IItemDropRuleCondition
	{
		public bool CanDrop(DropAttemptInfo info) => isTrapped == (info.npc.ai[1] == 1 && info.npc.type == ModContent.NPCType<ZigguratMummy>());
		public bool CanShowItemDropInUI() => !isTrapped; // Only the default drops will appear instead of having duplicates
		public string GetConditionDescription() => "";
	}

	private static readonly Asset<Texture2D> Alt = ModContent.Request<Texture2D>(DrawHelpers.RequestLocal(typeof(ZigguratMummy), "ZigguratMummy_Alt"));

	private ref float Variant => ref NPC.ai[0];

	private bool TrapSpawned => NPC.ai[1] == 1;

	public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 15;

	public override void SetDefaults()
	{
		NPC.CloneDefaults(NPCID.Mummy);
		NPC.lifeMax = 80;
		NPC.damage = 20;
		NPC.aiStyle = -1;

		SpawnModBiomes = [ModContent.GetInstance<ZigguratBiome>().Type];
	}

	public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
	{
		NPC.lifeMax = ModeUtils.ByMode(80, 90, 110, 150);
		NPC.damage = ModeUtils.ByMode(20, 37, 40, 60);
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "");

	public override void AI()
	{
		const float JumpSpeed = 5f;
		const float MoveSpeed = 1f;

		if (Variant == 0 && Main.netMode != NetmodeID.MultiplayerClient)
		{
			Variant = Main.rand.Next(2) + 1;
			NPC.netUpdate = true;
		}

		NPC.TargetClosest(false);
		Player target = Main.player[NPC.target];

		float dir = target.Center.X < NPC.Center.X ? -1 : 1;
		NPC.velocity.X += dir * 0.05f;
		NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X, -MoveSpeed, MoveSpeed * NPC.frame.Y switch
		{
			700 or 780 or 300 => 0.8f,
			240 or 660 => 0.6f,
			_ => 1f
		});

		Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
		bool grounded = Collision.SolidCollision(NPC.BottomLeft, NPC.width, 6) && NPC.velocity.Y >= 0;

		// Jump when blocked on either side
		if (NPC.velocity.X == dir * 0.05f)
		{
			if (Collision.SolidCollision(NPC.position + new Vector2(NPC.width, 0), 8, NPC.height - 2) && NPC.collideY && grounded)
				NPC.velocity.Y = -JumpSpeed;

			if (Collision.SolidCollision(NPC.position - new Vector2(8, 0), 8, NPC.height - 2) && NPC.collideY && grounded)
				NPC.velocity.Y = -JumpSpeed;
		}

		NPC.spriteDirection = NPC.direction = Math.Sign(NPC.velocity.X);
	}

	public override void FindFrame(int frameHeight)
	{
		NPC.frameCounter++;
		NPC.frame.Y = (int)(NPC.frameCounter * 0.2f % Main.npcFrameCount[Type]);
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		for (int i = 0; i < 2; ++i)
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Pearlsand);

		if (NPC.life > 0)
			return;

		for (int i = 0; i < 8; ++i)
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Pearlsand);

		if (!Main.dedServ)
		{
			for (int i = 0; i < 3; ++i)
				Gore.NewGore(NPC.GetSource_Death(), NPC.Center, NPC.velocity, Mod.Find<ModGore>("MummyGore_" + i).Type);

			if (Variant == 1)
			{
				Gore.NewGore(NPC.GetSource_Death(), NPC.Top, NPC.velocity, Mod.Find<ModGore>("MummyGore_3").Type);
				Gore.NewGore(NPC.GetSource_Death(), NPC.Top, NPC.velocity, Mod.Find<ModGore>("MummyGore_4").Type);
			}
			else
				Gore.NewGore(NPC.GetSource_Death(), NPC.Top, NPC.velocity, Mod.Find<ModGore>("MummyGore_5").Type);
		}
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo) => spawnInfo.SpawnTileType == ModContent.TileType<RedSandstoneBrick>() 
		|| spawnInfo.SpawnTileType == ModContent.TileType<RedSandstoneBrickCracked>() || spawnInfo.SpawnTileType == ModContent.TileType<RedSandstoneSlab>() ? 0.005f : 0;
	   
	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		LeadingConditionRule isTrapSpawned = new(new TrapSpawnedRule(true));
		isTrapSpawned.OnSuccess(ItemDropRule.OneFromOptions(4, ItemID.FastClock, ItemID.MummyMask, ItemID.MummyShirt, ItemID.MummyPants));
		LeadingConditionRule notTrapSpawned = new(new TrapSpawnedRule(false));
		notTrapSpawned.OnFailedConditions(ItemDropRule.OneFromOptions(20, ItemID.FastClock, ItemID.MummyMask, ItemID.MummyShirt, ItemID.MummyPants));
		npcLoot.Add(isTrapSpawned);
		npcLoot.Add(notTrapSpawned);
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		bool isVariant = NPC.IsABestiaryIconDummy ? Main.GameUpdateCount % 600 < 300 : Variant == 1;
		Texture2D tex = (isVariant ? TextureAssets.Npc[Type] : Alt).Value;
		Rectangle rect = NPC.frame;
		SpriteEffects effects = NPC.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
		Vector2 position = NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY);
		int frameHeight = tex.Height / Main.npcFrameCount[Type];
		rect.Y *= frameHeight;
		rect.Height = frameHeight + 2;

		if (isVariant)
			position.Y -= 6;

		Main.EntitySpriteDraw(tex, position, rect, drawColor, NPC.rotation, NPC.frame.Size() / 2f, 1f, effects, 0);
		return false;
	}
}
