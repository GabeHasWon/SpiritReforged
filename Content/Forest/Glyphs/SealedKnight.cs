using SpiritReforged.Common.NPCCommon;

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
		NPC.height = 50;
		NPC.damage = 10;
		NPC.lifeMax = 500;
		NPC.defense = 5;
		NPC.knockBackResist = 0;
		NPC.boss = true;
	}

	public override void AI()
	{
		const int activation_range = 500;

		if (CurrentState is State.Inactive)
		{
			NPC.TargetClosest(false);

			if (NPC.HasPlayerTarget && NPC.DistanceSQ(Main.player[NPC.target].Center) < activation_range)
				ChangeState(State.Idle);

			return;
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

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		NPC.spriteDirection = NPC.direction;

		Texture2D texture = TextureAssets.Npc[Type].Value;
		Rectangle source = NPC.frame;
		SpriteEffects effects = (NPC.spriteDirection == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		Main.EntitySpriteDraw(texture, NPC.Bottom - Main.screenPosition + new Vector2(0, NPC.gfxOffY + 2), source, NPC.DrawColor(drawColor), NPC.rotation, new(source.Width / 2, source.Height), NPC.scale, effects);

		return false;
	}
}