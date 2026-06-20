using SpiritReforged.Common.Easing;
using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Forest.Glyphs;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent.UI;

namespace SpiritReforged.Content.Underground.NPCs.KnightBoss;

[AutoloadBossHead]
public class SealedKnight : ModNPC
{
	public enum State
	{
		Inactive,
		Active,
		Jump,
		Mortar,
		Dash,
		Block,
		Staggered
	}

	private delegate void PatternDelegate(SealedKnight sealedKnight);

	private static PatternDelegate[] _patterns;

	public State CurrentState { get; private set; }

	public ref float Counter => ref NPC.ai[0];

	/// <summary> Whether this NPC has just changed its state. </summary>
	public bool ChangedState => _lastStateImmediate != CurrentState;

	public Player Target => Main.player[NPC.target];

	public int shieldLife;
	private State _lastStateImmediate;

	public override void SetStaticDefaults()
	{
		NPCID.Sets.TrailCacheLength[Type] = 8;
		NPCID.Sets.TrailingMode[Type] = 0;

		_patterns = new PatternDelegate[Enum.GetValues<State>().Length];
		_patterns[(int)State.Inactive] = Inactive;
		_patterns[(int)State.Active] = Active;
		_patterns[(int)State.Jump] = Jump;
		_patterns[(int)State.Mortar] = Mortar;
		_patterns[(int)State.Dash] = Dash;
		_patterns[(int)State.Block] = Block;
		_patterns[(int)State.Staggered] = Staggered;
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
		NPC.BossBar = ModContent.GetInstance<KnightBossBar>();
	}

	public override void AI()
	{
		if (!Main.dedServ && CurrentState != State.Inactive && Main.rand.NextBool(3)) //Candle visuals
		{
			var dust = Dust.NewDustPerfect(NPC.Top + Main.rand.NextVector2Circular(4, 4), Main.rand.NextFromList(DustID.Torch, DustID.Smoke), Vector2.UnitY * -Main.rand.NextFloat(1, 2));
			dust.noGravity = true;

			if (dust.type == DustID.Smoke)
				dust.alpha = 150;
		}

		State lastState = CurrentState; //Store the last state in case ChangeState is invoked on the next line
		_patterns[(int)CurrentState]?.Invoke(this); //Invoke the current pattern
		_lastStateImmediate = lastState;
	}

	public void ChangeState(State newState, bool retarget = true)
	{
		if (CurrentState == newState)
			return;

		if (retarget)
			NPC.TargetClosest();

		CurrentState = newState;
		Counter = 0;
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
		SpriteEffects effects = (NPC.spriteDirection == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		Vector2 drawOffset = new(11 * NPC.spriteDirection, NPC.gfxOffY + 2);
		Color color = (shieldLife > 0) ? Color.Lerp(NPC.DrawColor(drawColor), Color.White.Additive(), EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 30f) / 2) : NPC.DrawColor(drawColor);

		for (int i = 0; i < NPCID.Sets.TrailCacheLength[Type]; i++)
		{
			Color trailColor = color * (1f - i / (NPCID.Sets.TrailCacheLength[Type] - 1f)) * 0.25f;
			Vector2 position = NPC.oldPos[i] - Main.screenPosition + new Vector2(NPC.width / 2, NPC.height) + drawOffset;

			Main.EntitySpriteDraw(texture, position, source, trailColor, NPC.rotation, new(source.Width / 2, source.Height), NPC.scale, effects);
		} //Draw a trail

		Main.EntitySpriteDraw(texture, NPC.Bottom - screenPos + drawOffset, source, color, NPC.rotation, new(source.Width / 2, source.Height), NPC.scale, effects);

		if (CurrentState is not State.Inactive)
			DrawFlame(screenPos);

		Utils.DrawBorderString(spriteBatch, CurrentState.ToString(), NPC.Top - screenPos - new Vector2(0, 20), Main.MouseTextColorReal, 1, 0.5f, 0.5f); //DEBUG STATE INDICATOR

		return false;
	}

	public void DrawFlame(Vector2 screenPos)
	{
		Texture2D texture = TextureAssets.Flames[0].Value;
		Rectangle source = new(22, 0, 22, 22);

		for (int i = 0; i < 3; i++)
		{
			Vector2 position = NPC.Top - screenPos + new Vector2(2 * NPC.spriteDirection, NPC.gfxOffY + 2) + Main.rand.NextVector2Circular(2, 2);
			Main.EntitySpriteDraw(texture, position, source, NPC.DrawColor(Color.White.Additive()), NPC.rotation, source.Size() / 2, NPC.scale, 0);
		}
	}

	public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers) => ModifyHit(projectile.damage, ref modifiers);

	public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers) => ModifyHit(item.damage, ref modifiers);

	public void ModifyHit(int damage, ref NPC.HitModifiers modifiers)
	{
		if (shieldLife > 0)
		{
			if ((shieldLife -= modifiers.GetDamage(damage, false, true)) <= 0)
				ChangeState(State.Staggered);

			modifiers.FinalDamage *= 0;
			modifiers.HideCombatText();
			modifiers.DisableCrit();
		}
	}

	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.Write((byte)CurrentState);
		writer.Write(shieldLife);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		ChangeState((State)reader.ReadByte());
		shieldLife = reader.Read();
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
		NPC npc = sealedKnight.NPC;
		npc.velocity.X = 0;

		if (++sealedKnight.Counter >= 20)
			sealedKnight.ChangeState(Main.rand.NextFromList(State.Mortar, State.Jump, State.Dash, State.Block));
	}

	private static void Mortar(SealedKnight sealedKnight)
	{
		NPC npc = sealedKnight.NPC;

		if (Main.netMode != NetmodeID.MultiplayerClient && sealedKnight.Counter == 5 && npc.HasPlayerTarget)
		{
			Vector2 velocity = ArcVelocityHelper.GetArcVel(npc.Top, Main.player[npc.target].Center, Firefall.Gravity, 12, true);
			Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Top, velocity, ModContent.ProjectileType<Firefall>(), npc.damage, 4);
		}

		if (++sealedKnight.Counter >= 30)
			sealedKnight.ChangeState(State.Active);
	}

	private static void Jump(SealedKnight sealedKnight)
	{
		const int anticipation = 10;
		const int slam_range = 20;

		NPC npc = sealedKnight.NPC;
		bool falling = sealedKnight.Counter > anticipation;
		npc.noGravity = true;

		if (!npc.HasPlayerTarget)
			return;

		if (falling)
		{
			if (npc.velocity.Y == 0) //Collided
			{
				if (!Main.dedServ)
				{
					const int tile_width = 7;

					for (int x = 0; x < tile_width; x++)
					{
						float xOffset = (x - tile_width / 2) * 16f;
						Vector2 position = new(npc.Bottom.X + xOffset, npc.Bottom.Y);
						float ease = 1f - EaseFunction.EaseSine.Ease((float)x / (tile_width - 1f));

						ParticleHandler.SpawnParticle(new MovingBlockParticle(position, (int)(20 * ease), 6));
					}
				}

				npc.noGravity = false;
				sealedKnight.ChangeState(State.Active);
			}
			else
			{
				npc.velocity.Y = Math.Min(npc.velocity.Y + 1, 16);
			}
		}
		else if (sealedKnight.Counter > 0 || Math.Abs(sealedKnight.Target.Center.X - npc.Center.X) < slam_range)
		{
			npc.velocity *= 0.5f;
			sealedKnight.Counter++;
		}
		else //Instantly jump toward the target
		{
			npc.velocity = Vector2.Lerp(npc.velocity, npc.DirectionTo(sealedKnight.Target.Center - new Vector2(0, 200)) * 10, 0.5f);
		}
	}

	private static void Dash(SealedKnight sealedKnight)
	{
		const int telegraph = 10;
		const int overdash_distance = 50;
		const int dash_speed = 18;

		NPC npc = sealedKnight.NPC;

		if ((sealedKnight.Target.Center.X - npc.Center.X) * npc.direction < -overdash_distance)
		{
			sealedKnight.ChangeState(State.Active);
		}
		else if (sealedKnight.Counter >= telegraph)
		{
			npc.velocity.X = MathHelper.Lerp(npc.velocity.X, npc.direction * dash_speed, 0.5f);
			npc.Step();
		}

		sealedKnight.Counter++;
	}

	private static void Block(SealedKnight sealedKnight)
	{
		if (sealedKnight.ChangedState)
			sealedKnight.shieldLife = 100;

		if (sealedKnight.Counter++ >= 400)
		{
			sealedKnight.ChangeState(State.Active);
			sealedKnight.shieldLife = 0;
		}
	}

	private static void Staggered(SealedKnight sealedKnight)
	{
		const int duration = 160;
		NPC npc = sealedKnight.NPC;

		if (sealedKnight.ChangedState && Main.netMode != NetmodeID.MultiplayerClient)
			npc.Emote(duration, EmoteID.EmotionCry, new(npc));

		if (sealedKnight.Counter++ >= duration)
			sealedKnight.ChangeState(State.Active);
	}
	#endregion
}