using SpiritReforged.Content.Vanilla.Items.Food;
using Terraria.ModLoader.Utilities;

namespace SpiritReforged.Content.Ocean.NPCs;

[AutoloadCritter]
public class Floater : ModNPC
{
	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[NPC.type] = 40;

		var drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers() { Hide = true };
		NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);
	}

	public override void SetDefaults()
	{
		NPC.width = 18;
		NPC.height = 22;
		NPC.damage = 0;
		NPC.defense = 0;
		NPC.dontCountMe = true;
		NPC.lifeMax = 5;
		NPC.HitSound = SoundID.NPCHit25;
		NPC.DeathSound = SoundID.NPCDeath28;
		NPC.knockBackResist = .35f;
		NPC.aiStyle = 18;
		NPC.noGravity = true;
		NPC.npcSlots = 0;
		AIType = NPCID.PinkJellyfish;
    }

	bool txt = false;

	public override bool PreAI()
	{
		if (!txt)
		{
			for (int i = 0; i < 8; ++i)
			{
				var dir = Vector2.Normalize(Main.player[NPC.target].Center - NPC.Center);
				int newNPC = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X + Main.rand.Next(-20, 20), (int)NPC.Center.Y + Main.rand.Next(-20, 20), ModContent.NPCType<Floater1>(), NPC.whoAmI);
				Main.npc[newNPC].velocity = dir;
			}

			txt = true;
			NPC.netUpdate = true;
			Lighting.AddLight((int)(NPC.Center.X / 16f), (int)(NPC.Center.Y / 16f), .3f, .2f, .3f);
		}

		return true;
	}

	public override void FindFrame(int frameHeight)
	{
		NPC.frameCounter += 0.15f;
		NPC.frameCounter %= Main.npcFrameCount[NPC.type];
		int frame = (int)NPC.frameCounter;
		NPC.frame.Y = frame * frameHeight;
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo)
	{
		if (spawnInfo.PlayerSafe || Main.dayTime)
			return 0f;

		return SpawnCondition.OceanMonster.Chance * 0.173f;
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		var effects = NPC.direction == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
		spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY), NPC.frame, drawColor, NPC.rotation, NPC.frame.Size() / 2, NPC.scale, effects, 0);
		return false;
	}

	// TODO
	//public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) => GlowmaskUtils.DrawNPCGlowMask(spriteBatch, NPC, ModContent.Request<Texture2D>("SpiritMod/NPCs/Critters/Ocean/Floater_Critter_Glow", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, screenPos);

	public override void HitEffect(NPC.HitInfo hit)
	{
		for (int k = 0; k < 30; k++)
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.PinkTorch, 2.5f * hit.HitDirection, -2.5f, 0, Color.White, Main.rand.NextFloat(.2f, .8f));
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot) => npcLoot.AddCommon<RawFish>();
}
