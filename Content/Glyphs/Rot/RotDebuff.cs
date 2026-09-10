using SpiritReforged.Common.DebuffOverhaul;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Particles.Basic;
using Terraria.Audio;
using Terraria.DataStructures;
using static SpiritReforged.Common.DebuffOverhaul.BuffExtension;

namespace SpiritReforged.Content.Glyphs.Rot;

public class RotDebuff : ModBuff
{
	public class BlightExtension : DoTExtension
	{
		public override BuffSettings Settings => new(/*0.06f, 500, true,*/ Category.Poison);

		public override void PostDrawHealthBar(SpriteBatch spriteBatch, NPC npc, HealthBarHook.Options options)
		{
			float progress = (float)npc.life / npc.lifeMax;
			float fadeout = MathHelper.Min(BuffTime / 30f, 1);
			float lightness = options.Lightness;
			float sine = EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 30f);

			Texture2D front = TextureAssets.Hb1.Value;
			Rectangle bounds = new(0, 0, (int)(front.Width * progress), front.Height);
			Color color = new(220, 198, 57);

			HealthBarHook.DrawSimpleBar(spriteBatch, front, options.Position, bounds, options.Scale, color * fadeout * lightness);

			Vector2 endPosition = options.Position + new Vector2(front.Width * progress, front.Height / 2) * options.Scale;
			Texture2D skull = ModContent.GetInstance<RotGlyph>().IconTexture.Value;

			spriteBatch.Draw(skull, endPosition, null, color * fadeout * lightness * 2, sine * 0.1f, skull.Size() / 2, Math.Min(progress * 10, options.Scale) + sine * 0.1f, default, 0);

			if ((int)Main.timeForVisualEffects % 18 == 0 && fadeout == 1)
				TerrariaParticles.OverHealthBars.Add(new BubbleParticle(40, color * lightness, npc)
				{
					LocalPosition = endPosition + Main.screenPosition - npc.Center,
					Scale = new Vector2(0.8f) * options.Scale,
					AccelerationPerFrame = new(Main.rand.NextFloat(-0.01f, 0.01f), -0.02f)
				});
		}
	}

	public sealed class RotPlayer : ModPlayer
	{
		public int blightStacks;

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (item.GetGlyph().ItemType == ModContent.ItemType<RotGlyph>())
			{
				BlightHitEffects(target, Player);

				if (Main.netMode != NetmodeID.SinglePlayer)
					MultiplayerLoader.Send(nameof(BlightHitEffects), -1, -1, target, Player);
			}
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (proj.GetGlyph().ItemType == ModContent.ItemType<RotGlyph>())
			{
				BlightHitEffects(target, Player);

				if (Main.netMode != NetmodeID.SinglePlayer)
					MultiplayerLoader.Send(nameof(BlightHitEffects), -1, -1, target, Player);
			}
		}

		[NetSynced(true)]
		public static void BlightHitEffects(NPC target, Player owner)
		{
			SpreadNearby(target.Center, 100);

			if (!Main.dedServ)
			{
				Vector2 position = target.Hitbox.ClosestPointInRect(owner.Center);
				float angle = Main.rand.NextFloat(MathHelper.Pi);

				SoundEngine.PlaySound(RotGlyph.BlightImpact, target.Center);

				for (int i = 0; i < 3; i++)
				{
					ParticleHandler.SpawnParticle(new FlyParticle(position, target.Center.DirectionTo(owner.Center).RotatedByRandom(0.2f) * Main.rand.NextFloat(1.5f), 0f, 0.5f, 45));

					ParticleHandler.SpawnParticle(new MaggotParticle(position, target.Center.DirectionTo(owner.Center).RotatedByRandom(0.3f)
						* Main.rand.NextFloat(2.5f) - Vector2.UnitY, Main.rand.NextFloat(MathHelper.TwoPi), Main.rand.NextFloat(0.8f, 1.1f), 20 + Main.rand.Next(20)));

					ParticleHandler.SpawnParticle(new SmallCompositeSmoke(position, target.Center.DirectionTo(owner.Center).RotatedByRandom(0.5f)
						* Main.rand.NextFloat(2.5f), new Color(87, 94, 1), 40, false, false)
						{ Layer = ParticleLayer.BelowNPC });
				}
			}
		}

		public override void UpdateBadLifeRegen()
		{
			if (Player.HasBuff<RotDebuff>())
			{
				Player.lifeRegen = Math.Min(Player.lifeRegen, 0) - 4 * (blightStacks + 1);
				Player.lifeRegenTime = 0;
			}
			else
			{
				blightStacks = 0;
			}
		}
	}

	public sealed class RotNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public int blightStacks;

		public override void DrawEffects(NPC npc, ref Color drawColor)
		{
			int buffType = ModContent.BuffType<RotDebuff>();
			if (npc.HasBuff(buffType))
			{
				float intensity = MathHelper.Min((float)npc.buffTime[npc.FindBuffIndex(buffType)] / STACK_TIME, 1);
				drawColor = Color.Lerp(drawColor, Color.Lerp(drawColor, new Color(241, 255, 16), (float)Math.Abs(Math.Sin(Main.GlobalTimeWrappedHourly * 2f))), intensity);
			}
		}

		public override void UpdateLifeRegen(NPC npc, ref int damage)
		{
			if (npc.HasBuff<RotDebuff>())
			{
				npc.lifeRegen -= 4 * blightStacks;
			}
			else
			{
				blightStacks = 0;
			}
		}

		public override void OnKill(NPC npc)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient && npc.HasBuff<RotDebuff>())
				SpreadNearby(npc.Center, 500);
		}

		public override void OnHitPlayer(NPC npc, Player target, Player.HurtInfo hurtInfo)
		{
			if (npc.HasBuff<RotDebuff>())
				target.AddBuff(ModContent.BuffType<RotDebuff>(), 240);
		}
	}

	public const int STACK_TIME = 60;
	public const int MAX_STACKS = 20;

	private static int GetDisplayStacks(int buffTime) => (int)Math.Min(buffTime / 20f, Main.LocalPlayer.TryGetModPlayer(out RotPlayer rotPlayer) ? (rotPlayer.blightStacks + 1) : 1);

	public override void SetStaticDefaults()
	{
		Main.buffNoSave[Type] = true;
		Main.debuff[Type] = true;
		BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;

		BuffHandler.Register(ModContent.GetInstance<BlightExtension>(), ModContent.BuffType<RotDebuff>());
	}

	public override bool ReApply(Player player, int time, int buffIndex)
	{
		const int max_time = 600;
		player.buffTime[buffIndex] = Math.Min(player.buffTime[buffIndex] + time / 2, max_time);

		if (player.TryGetModPlayer(out RotPlayer rotPlayer))
			rotPlayer.blightStacks = Math.Min(rotPlayer.blightStacks + 1, MAX_STACKS);

		return true;
	}

	public override bool ReApply(NPC npc, int time, int buffIndex)
	{
		const int max_time = 600;
		npc.buffTime[buffIndex] = Math.Min(npc.buffTime[buffIndex] + time / 2, max_time);

		if (npc.TryGetGlobalNPC(out RotNPC rotNPC))
			rotNPC.blightStacks = Math.Min(rotNPC.blightStacks + 1, MAX_STACKS);

		return true;
	}

	public override void Update(Player player, ref int buffIndex) => UpdateBlight(player, 0);

	public override void Update(NPC npc, ref int buffIndex) => UpdateBlight(npc, npc.TryGetGlobalNPC(out RotNPC rotNPC) ? rotNPC.blightStacks / STACK_TIME : 0);

	public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
	{
		buffName = DisplayName.WithFormatArgs(GetDisplayStacks(Main.LocalPlayer.buffTime[Main.LocalPlayer.FindBuffIndex(Type)])).Value;
		rare = ItemRarityID.Green;
	}

	public override void PostDraw(SpriteBatch spriteBatch, int buffIndex, BuffDrawParams drawParams)
	{
		int buffTime = Main.LocalPlayer.buffTime[buffIndex];
		int playerStacks = Main.LocalPlayer.TryGetModPlayer(out RotPlayer rotPlayer) ? rotPlayer.blightStacks : 1;
		int timeStacks = buffTime / 20;

		float lerp = (timeStacks <= playerStacks) ? MathHelper.Clamp(buffTime % 20 / 20f, 0, 1) : 0;
		var drawColor = Color.Lerp(Color.White, Color.Green.Additive(), lerp);
		float scale = MathHelper.Lerp(1f, 1.2f, lerp);

		Utils.DrawBorderString(spriteBatch, GetDisplayStacks(buffTime).ToString(), drawParams.Position + new Vector2(25, 20), drawColor, scale);
	}

	private static void UpdateBlight(Entity entity, float intensity)
	{
		if (Main.dedServ || Main.rand.NextFloat() > intensity)
			return;

		for (int i = 0; i < 2; i++)
		{
			Vector2 position = Main.rand.NextVector2FromRectangle(entity.Hitbox);

			if (Main.rand.NextBool(2))
				ParticleHandler.SpawnParticle(new FlyParticle(position, -Vector2.UnitY * Main.rand.NextFloat(-0.5f, 0.5f), 0f, Main.rand.NextFloat(0.8f, 1.2f), Main.rand.Next(30, 90)));

			if (Main.rand.NextBool(6))
				ParticleHandler.SpawnParticle(new MaggotParticle(position, Main.rand.NextVector2Circular(1f, 1f), Main.rand.NextFloat(MathHelper.TwoPi), Main.rand.NextFloat(0.8f, 1.1f), 40)
				{ Layer = ParticleLayer.AbovePlayer });

			ParticleHandler.SpawnParticle(new CompositeSmoke(position, Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(0.2f, 1.2f), new Color(87, 94, 1), 50, false, false, SmokeUpdate)
			{ Layer = ParticleLayer.BelowNPC });

			ParticleHandler.SpawnParticle(new SmallCompositeSmoke(position, Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(0.2f, 1.2f), new Color(169, 158, 38), 40, false, false, SmokeUpdate)
			{ Layer = ParticleLayer.BelowNPC });

			ParticleHandler.SpawnParticle(new AttachedCompositeSmoke(entity, Main.rand.NextVector2FromRectangle(entity.Hitbox), Vector2.UnitY * Main.rand.NextFloat(1.5f), new Color(169, 158, 38), 45, false, false, SmokeUpdate)
			{ Layer = ParticleLayer.BelowNPC });
		}

		static void SmokeUpdate(Particle p)
		{
			p.Velocity.Y -= 0.02f;
			p.Velocity.X *= 0.95f;
		}
	}

	/// <summary> Spreads to NPCs near <paramref name="origin"/> within a limit. </summary>
	public static void SpreadNearby(Vector2 origin, int range)
	{
		const int spread_limit = 5;

		NPC[] possibleVectors = new NPC[spread_limit];
		int buffType = ModContent.BuffType<RotDebuff>();
		int index = 0;

		foreach (NPC npc in Main.ActiveNPCs)
		{
			if (npc.CanBeChasedBy() && npc.DistanceSQ(origin) < range * range)
			{
				possibleVectors[index] = npc;

				if (++index >= spread_limit)
					break;
			}
		}

		foreach (NPC npc in possibleVectors)
		{
			if (npc != null)
			{
				bool hasBuff = npc.HasBuff(buffType);
				npc.AddBuff(buffType, 180);

				if (Main.dedServ || hasBuff)
					continue;

				SoundEngine.PlaySound(RotGlyph.BlightImpact, npc.Center);
				Vector2 center = npc.Center;

				for (int i = 0; i < 8; i++)
				{
					ParticleHandler.SpawnParticle(new FlyParticle(center, Main.rand.NextVector2CircularEdge(1f, 1f), 0f, Main.rand.NextFloat(0.7f, 1.1f), 60));

					ParticleHandler.SpawnParticle(new CompositeSmoke(center, Main.rand.NextVector2CircularEdge(4f, 4f) * Main.rand.NextFloat(0.9f, 1f), new Color(87, 94, 1), 50, false, false, SmokeUpdate)
					{ Layer = ParticleLayer.BelowNPC });

					ParticleHandler.SpawnParticle(new CompositeSmoke(center, Main.rand.NextVector2CircularEdge(4f, 4f) * Main.rand.NextFloat(0.9f, 1f), new Color(169, 158, 38), 50, false, false, SmokeUpdate)
					{ Layer = ParticleLayer.BelowNPC });
				}
			}
		}

		static void SmokeUpdate(Particle p)
		{
			p.Velocity.Y -= 0.05f;
			p.Velocity *= 0.93f;
		}
	}
}