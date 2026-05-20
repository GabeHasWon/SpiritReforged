using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Forest.Glyphs;

[AutoloadBossHead]
public class SealedKnight : ModNPC
{
	public enum State
	{
		Inactive,
		Idle
	}

	public State CurrentState 
	{ 
		get => (State)NPC.ai[0];
		set => NPC.ai[0] = (float)value;
	}

	public override void SetDefaults()
	{
		NPC.width = 30;
		NPC.height = 70;
		NPC.damage = 10;
		NPC.lifeMax = 500;
		NPC.defense = 5;
		NPC.knockBackResist = 0;
		NPC.boss = true;
	}

	public override void AI()
	{
		const int activation_range = 150;
		NPC.direction = 1; //DEBUG

		if (CurrentState is State.Inactive)
		{
			NPC.TargetClosest(false);

			if (NPC.HasPlayerTarget && NPC.DistanceSQ(Main.player[NPC.target].Center) < activation_range * activation_range)
				ChangeState(State.Idle);

			return;
		}

		if (Main.rand.NextBool(3))
		{
			var dust = Dust.NewDustPerfect(NPC.Top + Main.rand.NextVector2Circular(4, 4), Main.rand.NextFromList(DustID.Torch, DustID.Smoke), Vector2.UnitY * -Main.rand.NextFloat(1, 2));
			dust.noGravity = true;

			if (dust.type == DustID.Smoke)
				dust.alpha = 150;
		}
	}

	public void ChangeState(State newState)
	{
		if (CurrentState == newState)
			return;

		CurrentState = newState;
		NPC.netUpdate = true;
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (!Main.dedServ && NPC.life <= 0)
		{
			for (int i = 1; i < 8; i++) //Spawn death gores
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity * 0.5f, Mod.Find<ModGore>("SealedKnight" + i).Type);
		}
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		LeadingConditionRule isExpertRule = new(new Conditions.IsExpert());
		isExpertRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ChromaticWax>(), 1, 3, 5));
		isExpertRule.OnFailedConditions(ItemDropRule.Common(ModContent.ItemType<ChromaticWax>(), 1, 4, 7));

		npcLoot.Add(isExpertRule);
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		NPC.spriteDirection = NPC.direction;

		Texture2D texture = TextureAssets.Npc[Type].Value;
		Rectangle source = NPC.frame;
		SpriteEffects effects = (NPC.spriteDirection == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		Main.EntitySpriteDraw(texture, NPC.Bottom - screenPos + new Vector2(-11 * NPC.spriteDirection, NPC.gfxOffY + 2), source, NPC.DrawColor(drawColor), NPC.rotation, new(source.Width / 2, source.Height), NPC.scale, effects);

		if (CurrentState is not State.Inactive)
			DrawFlame(spriteBatch, screenPos);

		return false;
	}

	private void DrawFlame(SpriteBatch spriteBatch, Vector2 screenPos)
	{
		Texture2D texture = TextureAssets.Flames[0].Value;
		Rectangle source = new(22, 0, 22, 22);

		for (int i = 0; i < 3; i++)
		{
			Vector2 position = NPC.Top - screenPos + new Vector2(2 * NPC.spriteDirection, NPC.gfxOffY + 2) + Main.rand.NextVector2Circular(2, 2);
			Main.EntitySpriteDraw(texture, position, source, NPC.DrawColor(Color.White.Additive()), NPC.rotation, source.Size() / 2, NPC.scale, 0);
		}
	}
}