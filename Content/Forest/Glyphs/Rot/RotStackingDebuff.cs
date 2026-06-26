using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Content.Forest.Glyphs.Sanguine;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Glyphs.Rot;

public class RotStackingDebuff : ModBuff
{
	public sealed class RotPlayer : ModPlayer
	{
		public BlightSpread blightSpread = new(); //Local changes will need to be synced

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (item.GetGlyph().ItemType == ModContent.ItemType<RotGlyph>())
				HitEffects(target);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (proj.GetGlyph().ItemType == ModContent.ItemType<RotGlyph>())
				HitEffects(target);
		}

		public void HitEffects(NPC target)
		{
			if (!target.TryGetGlobalNPC(out RotGlobalNPC rotGlobalNPC))
				return;

			SpreadNearby(target.Center, 100);

			if (!Main.dedServ)
			{
				Vector2 position = target.Hitbox.ClosestPointInRect(Player.Center);
				float angle = Main.rand.NextFloat(MathHelper.Pi);

				SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/Explosion_Liquid") with { Volume = 0.05f, PitchVariance = 0.5f }, target.Center);

				for (int i = 0; i < 3; i++)
				{
					ParticleHandler.SpawnParticle(new FlyParticle(position, target.Center.DirectionTo(Player.Center).RotatedByRandom(0.2f)
						* Main.rand.NextFloat(1.5f), 0f, 0.5f, 45));

					ParticleHandler.SpawnParticle(new MaggotParticle(position, target.Center.DirectionTo(Player.Center).RotatedByRandom(0.3f)
						* Main.rand.NextFloat(2.5f) - Vector2.UnitY, Main.rand.NextFloat(MathHelper.TwoPi), Main.rand.NextFloat(0.8f, 1.1f), 20 + Main.rand.Next(20)));

					ParticleHandler.SpawnParticle(new SmokeCloud(position, Main.rand.NextVector2Circular(2f, 2f) * Main.rand.NextFloat(0.2f, 1.2f), new Color(87, 94, 1, 255) * 0.2f, Main.rand.NextFloat(0.01f, 0.05f), EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 3,
						Layer = ParticleLayer.BelowNPC
					});

					ParticleHandler.SpawnParticle(new SmokeCloud(position, target.DirectionTo(Player.Center).RotatedByRandom(1f) * Main.rand.NextFloat(0.2f, 1.2f), new Color(131, 124, 1) * 0.15f, Main.rand.NextFloat(0.04f, 0.08f), EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 3,
						Layer = ParticleLayer.BelowNPC
					});

					ParticleHandler.SpawnParticle(new SmokeCloud(position, target.DirectionTo(Player.Center).RotatedByRandom(1f) * Main.rand.NextFloat(0.2f, 1.2f), new Color(169, 158, 38) * 0.25f, Main.rand.NextFloat(0.02f, 0.05f), EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 3,
						Layer = ParticleLayer.BelowNPC
					});
				}
			}
		}

		public override void ResetEffects() => blightSpread.Decay();

		public override void UpdateBadLifeRegen()
		{
			if (blightSpread.Active)
				Player.lifeRegen = Math.Min(Player.lifeRegen, 0) - blightSpread.stacks * 3;
		}

		public override void UpdateEquips()
		{
			if (!Main.dedServ && blightSpread.Active && Main.rand.NextBool(24 - blightSpread.stacks * 2))
			{
				Vector2 position = Player.Center + Main.rand.NextVector2CircularEdge(Player.width / 2, Player.height / 2);
				ParticleHandler.SpawnParticle(new FlyParticle(position, -Vector2.UnitY * Main.rand.NextFloat(-0.5f, 0.5f), 0f, Main.rand.NextFloat(0.8f, 1.2f), Main.rand.Next(30, 90)));

				if (Main.rand.NextBool(3))
				{
					ParticleHandler.SpawnParticle(new MaggotParticle(position, Main.rand.NextVector2Circular(1f, 1f), Main.rand.NextFloat(MathHelper.TwoPi), Main.rand.NextFloat(0.8f, 1.1f), 40)
					{
						Layer = ParticleLayer.AbovePlayer
					});
				}

				for (int i = 0; i < 2; i++)
				{
					position = Player.Center + Main.rand.NextVector2CircularEdge(Player.width / 2, Player.height / 2);

					ParticleHandler.SpawnParticle(new SmokeCloud(position, Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(0.2f, 1.2f), new Color(87, 94, 1, 255) * 0.4f, 0.03f + Main.rand.NextFloat(0.01f, 0.05f), EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 3,
						Layer = ParticleLayer.BelowNPC
					});

					position = Player.Center + Main.rand.NextVector2Circular(Player.width / 2, Player.height / 2);

					ParticleHandler.SpawnParticle(new SmokeCloud(position, -Vector2.UnitY * Main.rand.NextFloat(0.2f, 1.2f), new Color(131, 124, 1) * 0.3f, 0.03f + Main.rand.NextFloat(0.04f, 0.08f), EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 3,
						Layer = ParticleLayer.BelowNPC
					});

					position = Player.Center + Main.rand.NextVector2Circular(Player.width / 2, Player.height / 2);

					ParticleHandler.SpawnParticle(new SmokeCloud(position, -Vector2.UnitY * Main.rand.NextFloat(0.2f, 1.2f), new Color(169, 158, 38) * 0.3f, 0.03f + Main.rand.NextFloat(0.02f, 0.05f), EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 3,
						Layer = ParticleLayer.BelowNPC
					});
				}
			}
		}

		public void AddStack(int stackCount)
		{
			if (!Player.HasBuff<RotStackingDebuff>())
				Player.AddBuff(ModContent.BuffType<RotStackingDebuff>(), 60);

			blightSpread.stacks = Math.Min(blightSpread.stacks + stackCount, BlightSpread.MaxStacks);
			blightSpread.decayTime = 180;
		}
	}

	public sealed class RotGlobalNPC : GlobalNPC
	{
		public BlightSpread blightSpread = new(); //Local changes will need to be synced

		public override bool InstancePerEntity => true;

		public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.CanBeChasedBy();

		public override void ResetEffects(NPC npc) => blightSpread.Decay();

		public override void UpdateLifeRegen(NPC npc, ref int damage)
		{
			if (blightSpread.Active)
			{
				if (blightSpread.Active)
					npc.lifeRegen = Math.Min(npc.lifeRegen, 0) - blightSpread.stacks * 3;

				damage = 1;
			}
		}

		public override void DrawEffects(NPC npc, ref Color drawColor)
		{
			if (blightSpread.Active)
				drawColor = Color.Lerp(drawColor, Color.Lerp(drawColor, new Color(241, 255, 16), (float)Math.Abs(Math.Sin(Main.GlobalTimeWrappedHourly * 2f))), blightSpread.stacks / (float)BlightSpread.MaxStacks);
		}

		public override void AI(NPC npc)
		{
			if (!blightSpread.Active)
				return;

			if (Main.netMode != NetmodeID.MultiplayerClient && blightSpread.decayTime % 45 == 0)
				SpreadNearby(npc.Center, 80, 1); //Periodically spread to nearby NPCs

			if (!Main.dedServ && npc.Opacity > 0 && Main.rand.NextBool(30 - blightSpread.stacks * 2))
			{
				Vector2 position = npc.Center + Main.rand.NextVector2CircularEdge(npc.width / 2, npc.height / 2);
				ParticleHandler.SpawnParticle(new FlyParticle(position, -Vector2.UnitY * Main.rand.NextFloat(-0.5f, 0.5f), 0f, Main.rand.NextFloat(0.8f, 1.2f), Main.rand.Next(30, 90)));

				if (Main.rand.NextBool(3))
				{
					ParticleHandler.SpawnParticle(new MaggotParticle(position, Main.rand.NextVector2Circular(1f, 1f), Main.rand.NextFloat(MathHelper.TwoPi), Main.rand.NextFloat(0.8f, 1.1f), 40)
					{
						Layer = ParticleLayer.AboveNPC
					});
				}

				for (int i = 0; i < 2; i++)
				{
					position = npc.Center + Main.rand.NextVector2CircularEdge(npc.width / 2, npc.height / 2);

					ParticleHandler.SpawnParticle(new SmokeCloud(position, Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(0.2f, 1.2f), new Color(87, 94, 1, 255) * 0.3f, npc.width * 0.001f + Main.rand.NextFloat(0.01f, 0.05f), EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 3,
						Layer = ParticleLayer.BelowNPC
					});

					position = npc.Center + Main.rand.NextVector2Circular(npc.width / 2, npc.height / 2);

					ParticleHandler.SpawnParticle(new SmokeCloud(position, -Vector2.UnitY * Main.rand.NextFloat(0.2f, 1.2f), new Color(131, 124, 1) * 0.2f, npc.width * 0.001f + Main.rand.NextFloat(0.04f, 0.08f), EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 3,
						Layer = ParticleLayer.BelowNPC
					});

					position = npc.Center + Main.rand.NextVector2Circular(npc.width / 2, npc.height / 2);

					ParticleHandler.SpawnParticle(new SmokeCloud(position, -Vector2.UnitY * Main.rand.NextFloat(0.2f, 1.2f), new Color(169, 158, 38) * 0.2f, npc.width * 0.001f + Main.rand.NextFloat(0.02f, 0.05f), EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 3,
						Layer = ParticleLayer.BelowNPC
					});
				}
			}
		}

		public override void OnHitPlayer(NPC npc, Player target, Player.HurtInfo hurtInfo)
		{
			if (blightSpread.Active)
				target.GetModPlayer<RotPlayer>().AddStack(1 + blightSpread.stacks / 2);
		}

		public void AddStack(NPC npc, int stackCount)
		{
			if (!npc.HasBuff<RotStackingDebuff>())
				npc.AddBuff(ModContent.BuffType<RotStackingDebuff>(), 60);

			blightSpread.stacks = Math.Min(blightSpread.stacks + stackCount, BlightSpread.MaxStacks);
			blightSpread.decayTime = 240;
		}
	}

	public static void SpreadNearby(Vector2 origin, int range, int stackLimit = BlightSpread.MaxStacks)
	{
		foreach (NPC npc in Main.ActiveNPCs)
		{
			if (npc.CanBeChasedBy() && npc.DistanceSQ(origin) < range * range && npc.TryGetGlobalNPC(out RotGlobalNPC rotGlobalNPC))
			{
				if (rotGlobalNPC.blightSpread.stacks >= stackLimit)
					continue;

				rotGlobalNPC.AddStack(npc, 1);

				if (Main.dedServ || rotGlobalNPC.blightSpread.stacks > 1)
					continue;
				
				SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/Explosion_Liquid") with { Volume = 0.1f, PitchVariance = 0.5f }, npc.Center);

				for (int i = 0; i < 8; i++)
				{
					Vector2 center = npc.Center;
					ParticleHandler.SpawnParticle(new FlyParticle(center, Main.rand.NextVector2CircularEdge(1f, 1f), 0f, Main.rand.NextFloat(0.7f, 1.1f), 60));

					ParticleHandler.SpawnParticle(new SmokeCloud(center, Main.rand.NextVector2CircularEdge(1.5f, 1.5f), new Color(87, 94, 1, 255) * 0.2f, 0.03f + Main.rand.NextFloat(0.01f, 0.05f), EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 3,
						Layer = ParticleLayer.AboveNPC
					});

					ParticleHandler.SpawnParticle(new SmokeCloud(center, Main.rand.NextVector2CircularEdge(1.5f, 1.5f), new Color(131, 124, 1) * 0.15f, 0.02f + Main.rand.NextFloat(0.04f, 0.08f), EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 3,
						Layer = ParticleLayer.AboveNPC
					});

					ParticleHandler.SpawnParticle(new SmokeCloud(center, Main.rand.NextVector2CircularEdge(1.5f, 1.5f), new Color(169, 158, 38) * 0.25f, 0.01f + Main.rand.NextFloat(0.02f, 0.05f), EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 3,
						Layer = ParticleLayer.AboveNPC
					});
				}
			}
		}
	}

	public override void SetStaticDefaults() => Main.buffNoSave[Type] = true;

	public override void Update(Player player, ref int buffIndex)
	{
		RotPlayer rotPlayer = player.GetModPlayer<RotPlayer>();
		if (rotPlayer.blightSpread.Active)
		{
			player.buffTime[buffIndex] = 60 * rotPlayer.blightSpread.stacks;
		}
		else
		{
			player.DelBuff(buffIndex);
			buffIndex--;
		}
	}

	public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
	{
		buffName = DisplayName.WithFormatArgs(Main.LocalPlayer.GetModPlayer<RotPlayer>().blightSpread.stacks).Value;
		rare = ItemRarityID.Green;
	}

	public override void PostDraw(SpriteBatch spriteBatch, int buffIndex, BuffDrawParams drawParams)
	{
		RotPlayer rotPlayer = Main.LocalPlayer.GetModPlayer<RotPlayer>();
		float lerp = rotPlayer.blightSpread.decayIndicator / 20f;
		var drawColor = Color.Lerp(Color.White, Color.Green.Additive(), lerp);
		float scale = MathHelper.Lerp(1f, 1.2f, lerp);

		Utils.DrawBorderString(spriteBatch, rotPlayer.blightSpread.stacks.ToString(), drawParams.Position + new Vector2(25, 20), drawColor, scale);
	}
}