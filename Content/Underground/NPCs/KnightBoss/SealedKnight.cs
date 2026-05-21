using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Content.Forest.Glyphs;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Underground.NPCs.KnightBoss;

[AutoloadBossHead]
public class SealedKnight : ModNPC
{
	public enum State
	{
		Inactive,
		Active,
		Leap,
		Mortar,
		Swing,
		Block
	}

	private delegate void PatternDelegate(SealedKnight sealedKnight);

	private static PatternDelegate[] _patterns;

	public State CurrentState 
	{ 
		get => (State)NPC.ai[0];
		set => NPC.ai[0] = (float)value;
	}

	public ref float PatternTime => ref NPC.ai[1];

	public override void SetStaticDefaults()
	{
		_patterns = new PatternDelegate[Enum.GetValues<State>().Length];
		_patterns[(int)State.Inactive] = Inactive;
		_patterns[(int)State.Active] = Active;
		_patterns[(int)State.Leap] = Leap;
		_patterns[(int)State.Mortar] = Mortar;
		_patterns[(int)State.Swing] = Swing;
		_patterns[(int)State.Block] = Block;
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
		NPC.direction = 1; //DEBUG

		if (CurrentState != State.Inactive && Main.rand.NextBool(3))
		{
			var dust = Dust.NewDustPerfect(NPC.Top + Main.rand.NextVector2Circular(4, 4), Main.rand.NextFromList(DustID.Torch, DustID.Smoke), Vector2.UnitY * -Main.rand.NextFloat(1, 2));
			dust.noGravity = true;

			if (dust.type == DustID.Smoke)
				dust.alpha = 150;
		}

		_patterns[(int)CurrentState]?.Invoke(this); //Invoke the current pattern
		PatternTime++;
	}

	public void ChangeState(State newState)
	{
		if (CurrentState == newState)
			return;

		CurrentState = newState;
		PatternTime = 0;
		NPC.netUpdate = true;
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (!Main.dedServ && NPC.life <= 0)
			for (int i = 1; i < 10; i++) //Spawn death gores
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity * 0.5f, Mod.Find<ModGore>("SealedKnight" + i).Type);
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		npcLoot.Add(ItemDropRule.ByCondition(new Conditions.IsExpert(), ModContent.ItemType<ChromaticWax>(), 1, 4, 7));
		npcLoot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(), ModContent.ItemType<ChromaticWax>(), 1, 3, 5));
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		NPC.spriteDirection = NPC.direction;

		Texture2D texture = TextureAssets.Npc[Type].Value;
		Rectangle source = NPC.frame;
		SpriteEffects effects = NPC.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		Main.EntitySpriteDraw(texture, NPC.Bottom - screenPos + new Vector2(-11 * NPC.spriteDirection, NPC.gfxOffY + 2), source, NPC.DrawColor(drawColor), NPC.rotation, new(source.Width / 2, source.Height), NPC.scale, effects);

		if (CurrentState is not State.Inactive)
			DrawFlame(screenPos);

		Utils.DrawBorderString(spriteBatch, CurrentState.ToString(), NPC.Top - screenPos - new Vector2(0, 20), Main.MouseTextColorReal, 1, 0.5f, 0.5f); //DEBUG

		return false;
	}

	private void DrawFlame(Vector2 screenPos)
	{
		Texture2D texture = TextureAssets.Flames[0].Value;
		Rectangle source = new(22, 0, 22, 22);

		for (int i = 0; i < 3; i++)
		{
			Vector2 position = NPC.Top - screenPos + new Vector2(2 * NPC.spriteDirection, NPC.gfxOffY + 2) + Main.rand.NextVector2Circular(2, 2);
			Main.EntitySpriteDraw(texture, position, source, NPC.DrawColor(Color.White.Additive()), NPC.rotation, source.Size() / 2, NPC.scale, 0);
		}
	}

	#region patterns
	private static void Inactive(SealedKnight sealedKnight)
	{
		const int activation_range = 150;

		NPC npc = sealedKnight.NPC;
		npc.TargetClosest(false);

		if (npc.HasPlayerTarget && npc.DistanceSQ(Main.player[npc.target].Center) < activation_range * activation_range)
			sealedKnight.ChangeState(State.Active);
	}

	private static void Active(SealedKnight sealedKnight)
	{
		if (sealedKnight.PatternTime >= 100)
			sealedKnight.ChangeState(State.Mortar);
	}

	private static void Mortar(SealedKnight sealedKnight)
	{
		NPC npc = sealedKnight.NPC;

		if (Main.netMode != NetmodeID.MultiplayerClient && sealedKnight.PatternTime == 5 && npc.HasPlayerTarget)
		{
			Vector2 velocity = ArcVelocityHelper.GetArcVel(npc.Top, Main.player[npc.target].Center, Firefall.Gravity, 12, true);
			Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Top, velocity, ModContent.ProjectileType<Firefall>(), npc.damage, 4);
		}

		if (sealedKnight.PatternTime >= 30)
			sealedKnight.ChangeState(State.Active);
	}

	private static void Leap(SealedKnight sealedKnight)
	{

	}

	private static void Swing(SealedKnight sealedKnight)
	{

	}

	private static void Block(SealedKnight sealedKnight)
	{
		if (sealedKnight.PatternTime >= 180)
			sealedKnight.ChangeState(State.Active);
	}
	#endregion
}