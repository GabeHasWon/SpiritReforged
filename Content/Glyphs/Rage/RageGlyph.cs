using SpiritReforged.Common.CombatTextCommon;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;

namespace SpiritReforged.Content.Glyphs.Rage;

public class RageGlyph : GlyphItem
{
	public sealed class RageGlyphBuff : ModBuff
	{
		public override void SetStaticDefaults() => Main.buffNoSave[Type] = true;

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.GetModPlayer<RagePlayer>().OverflowDamage > 0)
			{
				// find the stack with the greatest timer and use that for the time display
				player.buffTime[buffIndex] = player.GetModPlayer<RagePlayer>().overflowDecayTimer;
			}
			else
			{
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}

		public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
		{
			int dmg = Main.LocalPlayer.GetModPlayer<RagePlayer>().OverflowDamage;

			buffName = Language.GetTextValue("Mods.SpiritReforged.Buffs.RageGlyphBuff.DisplayName", dmg);
			tip = Language.GetTextValue("Mods.SpiritReforged.Buffs.RageGlyphBuff.Description", dmg);
			rare = ItemRarityID.Red;
		}

		public override void PostDraw(SpriteBatch spriteBatch, int buffIndex, BuffDrawParams drawParams)
		{
			RagePlayer mp = Main.LocalPlayer.GetModPlayer<RagePlayer>();
			float lerp = mp.fadeInTimer / 20f;
			float scale = MathHelper.Lerp(0.8f, 1f, lerp);
			string text = mp.OverflowDamage.ToString();

			var drawColor = Color.Lerp(Color.Red, Color.OrangeRed, lerp);
			Vector2 shake = Main.rand.NextVector2Circular(0.5f, 0.5f) * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3.5f);

			Utils.DrawBorderString(spriteBatch, text, drawParams.Position + shake + new Vector2(25, 20), drawColor, scale);
		}
	}

	public sealed class RagePlayer : ModPlayer
	{
		public static readonly Asset<Texture2D> RageIcon = DrawHelpers.RequestLocal<RagePlayer>("RageGlyph_Icon", false);

		public const float OVERFLOW_DAMAGE_MULT = 2.25f;
		public const float DAMAGE_TAKEN_MULT = 1.5f;
		public const int OVERFLOW_DECAY_MAX = 600;

		public int OverflowDamage
		{
			get => Math.Min(_overflowDamage, Main.hardMode ? 2500 : 500);
			set
			{
				if (value != 0)
					overflowDecayTimer = OVERFLOW_DECAY_MAX;

				_overflowDamage = value;
			}
		}

		public int overflowDecayTimer;

		// drawing
		public int fadeOutTimer;
		public int fadeInTimer;

		private List<Vector2> _oldPositions;
		private int _overflowDamage;

		public static bool CanActivateRage(NPC npc) => npc.chaseable && npc.lifeMax > 5 && !npc.dontTakeDamage && !npc.immortal && !npc.friendly;

		public override void Load() => On_Main.DrawCachedProjs += DrawRage;

		private static void DrawRage(On_Main.orig_DrawCachedProjs orig, Main self, List<int> projCache, bool startSpriteBatch)
		{
			orig(self, projCache, startSpriteBatch);

			SpriteBatch sb = Main.spriteBatch;
			Texture2D rageIcon = RageIcon.Value;

			if (startSpriteBatch)
				sb.BeginDefault();

			if (projCache.Equals(Main.instance.DrawCacheProjsOverPlayers))
				foreach (Player player in Main.ActivePlayers)
				{
					if (!player.TryGetModPlayer(out RagePlayer ragePlayer) || ragePlayer.OverflowDamage <= 0 && ragePlayer.fadeOutTimer <= 0)
						continue;

					float scale = 1f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3.5f);

					if (ragePlayer.fadeOutTimer > 0)
						scale = MathHelper.Lerp(scale * 1.5f, scale, EaseFunction.EaseCircularOut.Ease(ragePlayer.fadeOutTimer / 10f));

					if (ragePlayer.fadeInTimer > 0)
						scale = MathHelper.Lerp(scale, scale * 2f, EaseFunction.EaseCircularIn.Ease(ragePlayer.fadeInTimer / 20f));

					Vector2 shake = Main.rand.NextVector2Circular(0.5f, 0.5f) * scale;
					if (scale < 1f || ragePlayer.fadeOutTimer > 0)
						shake *= 0;

					float fadeOut = 1f;

					if (ragePlayer.fadeOutTimer > 0)
						fadeOut = ragePlayer.fadeOutTimer / 10f;

					if (ragePlayer.fadeInTimer > 0)
						fadeOut = 1f - ragePlayer.fadeInTimer / 20f;

					Color color = Color.White;

					if (ragePlayer.fadeOutTimer > 0)
						color = Color.Lerp(color, Color.Orange, ragePlayer.fadeOutTimer / 10f);

					if (ragePlayer.fadeInTimer > 0)
						color = Color.Lerp(color, Color.Orange, ragePlayer.fadeInTimer / 20f);

					for (int i = 0; i < ragePlayer._oldPositions.Count; i++)
						sb.Draw(rageIcon, ragePlayer._oldPositions[i] - player.velocity * 0.2f - Main.screenPosition, null, color.Additive() * (i / 5f) * fadeOut, 0f, rageIcon.Size() / 2f, scale, 0f, 0f);

					sb.Draw(rageIcon, player.Center + new Vector2(-4 * player.direction, player.gfxOffY - 16) + shake - player.velocity * 0.2f - Main.screenPosition, null, color * fadeOut, 0f, rageIcon.Size() / 2f, scale, 0f, 0f);
				}

			if (startSpriteBatch)
				sb.End();
		}

		public override void OnHurt(Player.HurtInfo info)
		{
			if (Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<RageGlyph>())
			{
				OverflowDamage += (int)(info.Damage * DAMAGE_TAKEN_MULT);

				if (!Main.dedServ)
				{
					SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = -0.2f }, Player.Center);
					SoundEngine.PlaySound(SoundID.DD2_WitherBeastAuraPulse, Player.Center);

					for (int i = 0; i < 7; i++)
					{
						ParticleHandler.SpawnParticle(new SmokeCloud(Player.Top + new Vector2(0, 6), new Vector2(-Main.rand.NextFloat(1f, 3f), 0f).RotatedByRandom(0.2f), Color.White * 0.2f, Main.rand.NextFloat(0.1f), EaseFunction.EaseQuarticOut, 70, false));
						ParticleHandler.SpawnParticle(new SmokeCloud(Player.Top + new Vector2(0, 6), new Vector2(Main.rand.NextFloat(1f, 3f), 0f).RotatedByRandom(0.2f), Color.White * 0.2f, Main.rand.NextFloat(0.1f), EaseFunction.EaseQuarticOut, 70, false));
					}
				}
			}
		}

		public override void UpdateEquips()
		{
			if (fadeOutTimer > 0)
				fadeOutTimer--;

			if (fadeInTimer > 0)
				fadeInTimer--;

			if (overflowDecayTimer > 0)
				overflowDecayTimer--;
			else if (OverflowDamage > 0)
				Clear();

			if (!Main.dedServ)
			{
				if (_oldPositions is null)
				{
					_oldPositions = [];

					for (int i = 0; i < 5; i++)
						_oldPositions.Add(Player.Center + new Vector2(-4 * Player.direction, Player.gfxOffY - 16));
				}

				_oldPositions.Add(Player.Center + new Vector2(-4 * Player.direction, Player.gfxOffY - 16));

				while (_oldPositions.Count > 5)
					_oldPositions.RemoveAt(0);
			}

			//if (Player.HeldItem.GetGlyph().ItemType != ModContent.ItemType<RageGlyph>() && overflowDamage > 0)
			//	Clear();

			if (!Main.dedServ && OverflowDamage > 0)
			{
				float scale = 1f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3.5f);

				if (scale > 1.05f && Main.rand.NextBool(3))
				{
					ParticleHandler.SpawnParticle(new SmokeCloud(Player.Top + new Vector2(0, 6), new Vector2(-Main.rand.NextFloat(1f, 3f), 0f).RotatedByRandom(0.2f), Color.White * 0.2f, Main.rand.NextFloat(0.1f), EaseFunction.EaseQuarticOut, 70, false));
					ParticleHandler.SpawnParticle(new SmokeCloud(Player.Top + new Vector2(0, 6), new Vector2(Main.rand.NextFloat(1f, 3f), 0f).RotatedByRandom(0.2f), Color.White * 0.2f, Main.rand.NextFloat(0.1f), EaseFunction.EaseQuarticOut, 70, false));
				}
			}
		}

		public void Clear()
		{
			_oldPositions.Clear();
			fadeOutTimer = 10;
			OverflowDamage = 0;
		}

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (item.GetGlyph().ItemType == ModContent.ItemType<RageGlyph>())
			{
				RageHitEffects(target, Player);

				if (Main.netMode != NetmodeID.SinglePlayer)
					MultiplayerLoader.Send(nameof(RageHitEffects), -1, -1, target, Player);
			}
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (proj.type != ModContent.ProjectileType<RageHit>() && proj.GetGlyph().ItemType == ModContent.ItemType<RageGlyph>())
			{
				RageHitEffects(target, Player);

				if (Main.netMode != NetmodeID.SinglePlayer)
					MultiplayerLoader.Send(nameof(RageHitEffects), -1, -1, target, Player);
			}
		}

		[NetSynced(true)]
		public static void RageHitEffects(NPC target, Player owner)
		{
			if (!owner.TryGetModPlayer(out RagePlayer ragePlayer))
				return;

			int overDamage = target.life * -1;

			if (target.life > 0)
			{
				if (ragePlayer.OverflowDamage > 0)
				{
					SoundEngine.PlaySound(SoundID.DD2_WitherBeastAuraPulse, target.Center);

					if (owner.whoAmI == Main.myPlayer)
						Projectile.NewProjectile(target.GetSource_OnHurt(owner), target.Center, Vector2.Zero, ModContent.ProjectileType<RageHit>(), ragePlayer.OverflowDamage, 3f, owner.whoAmI, target.whoAmI);

					ragePlayer.OverflowDamage = 0;
				}
			}
			else if (overDamage > 0 && CanActivateRage(target))
			{
				// whatever was leftover from the hit, ie negative is what we store as extra damage
				ragePlayer.OverflowDamage += (int)(overDamage * OVERFLOW_DAMAGE_MULT);

				if (!Main.dedServ)
				{
					ParticleHandler.SpawnParticle(new LightBurst(target.Center, 0f, Color.Red.Additive(), 0.3f, 25));

					SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = -0.2f }, target.Center);
					SoundEngine.PlaySound(SoundID.DD2_WitherBeastAuraPulse, target.Center);

					ragePlayer.fadeInTimer = 20;

					for (int i = 0; i < 4; i++)
					{
						Vector2 pos = target.Center + Main.rand.NextVector2Circular(target.width / 2, target.height / 2);
						Vector2 velocity = -Vector2.UnitY * Main.rand.NextFloat(1f, 3f);

						ParticleHandler.SpawnParticle(new ImpactLine(pos, velocity, Color.Red.Additive(), new Vector2(0.7f, 1f), 30));
						ParticleHandler.SpawnQueuedParticle(new ImpactLine(pos, velocity, Color.Black, new Vector2(0.5f, 1f), 30), 1);
					}

					for (int i = 0; i < 7; i++)
					{
						ParticleHandler.SpawnParticle(new SmokeCloud(owner.Top + new Vector2(0, 6), new Vector2(-Main.rand.NextFloat(1f, 3f), 0f).RotatedByRandom(0.2f), Color.White * 0.2f, Main.rand.NextFloat(0.1f), EaseFunction.EaseQuarticOut, 70, false));
						ParticleHandler.SpawnParticle(new SmokeCloud(owner.Top + new Vector2(0, 6), new Vector2(Main.rand.NextFloat(1f, 3f), 0f).RotatedByRandom(0.2f), Color.White * 0.2f, Main.rand.NextFloat(0.1f), EaseFunction.EaseQuarticOut, 70, false));
					}
				}

				if (!owner.HasBuff<RageGlyphBuff>())
					owner.AddBuff(ModContent.BuffType<RageGlyphBuff>(), 60);
			}
		}

		public sealed class RageHit : ModProjectile
		{
			public override string Texture => AssetLoader.EmptyTexture;

			public int TargetWhoAmI
			{
				get => (int)Projectile.ai[0];
				set => Projectile.ai[0] = value;
			}

			public float Progress => 1f - Projectile.timeLeft / 60f;
			public override void SetDefaults()
			{
				Projectile.Size = new(20);

				Projectile.friendly = true;
				Projectile.DamageType = DamageClass.Generic;

				Projectile.tileCollide = false;
				Projectile.ignoreWater = true;

				Projectile.penetrate = 1;
				Projectile.stopsDealingDamageAfterPenetrateHits = true;

				Projectile.timeLeft = 60;
			}

			public override bool? CanDamage() => Progress > 0.5f;

			public override void AI()
			{
				NPC parent = Main.npc[TargetWhoAmI];

				if (parent is null || !parent.active)
				{
					Projectile.Kill();

					return;
				}

				Projectile.Center = parent.Center + new Vector2(0f, parent.gfxOffY);
			}

			public override bool PreDraw(ref Color lightColor)
			{
				Texture2D starNonPreMult = TextureAssets.Projectile[79].Value;
				float progress = EaseFunction.EaseCircularIn.Ease(Progress / 0.5f);

				if (Progress > 0.5f)
					progress = EaseFunction.EaseCircularOut.Ease(1f - (Progress - 0.5f) / 0.5f);

				Main.EntitySpriteDraw(starNonPreMult, Projectile.Center - Main.screenPosition, null, Color.Red.Additive(), 0f, starNonPreMult.Size() / 2f, 0.75f * progress, 0f);
				Main.EntitySpriteDraw(starNonPreMult, Projectile.Center - Main.screenPosition, null, Color.Black * 0.5f, 0f, starNonPreMult.Size() / 2f, 0.66f * progress, 0f);
				return false;
			}

			public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.HideCombatText();

			public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
			{
				Rectangle rect = target.getRect();
				int damage = Math.Max(damageDone, 1);
				int idx = CombatText.NewText(rect, Color.White, damage, hit.Crit);
				
				if (Main.netMode == NetmodeID.MultiplayerClient)
					NetMessage.SendData(MessageID.CombatTextInt, number: (int)Color.White.PackedValue, number2: rect.X, number3: rect.Y, number4: damage);

				ColoredCombatText.AddCombatText(idx, Color.Red, Color.DarkRed);

				SoundEngine.PlaySound(SoundID.DD2_WitherBeastCrystalImpact with { Pitch = -0.5f }, target.Center);
				SoundEngine.PlaySound(SoundID.DD2_CrystalCartImpact, target.Center);

				if (Main.myPlayer == Projectile.owner)
					ScreenshakeHelper.Shake(target.Center, target.DirectionTo(Main.player[Projectile.owner].Center), 1, 4, 10);

				for (int i = 0; i < 6; i++)
				{
					Vector2 offset = Main.rand.NextVector2CircularEdge(target.width / 2, target.height / 2);

					Vector2 pos = target.Center + offset;
					Vector2 velocity = offset * Main.rand.NextFloat(0.1f);

					ParticleHandler.SpawnParticle(new ImpactLine(pos, velocity, Color.Red.Additive(), new Vector2(0.7f, 1f) * Main.rand.NextFloat(0.8f, 1.1f), 30));
					ParticleHandler.SpawnQueuedParticle(new ImpactLine(pos, velocity, Color.Black, new Vector2(0.5f, 1f) * Main.rand.NextFloat(0.8f, 1.1f), 30), 1);

					ParticleHandler.SpawnParticle(new SmokeCloud(pos, velocity.RotatedByRandom(1.5f) * Main.rand.NextFloat(2f), Color.Black * 0.3f, 0.1f, EaseFunction.EaseQuinticOut, 30, false));

					float rot = Main.rand.NextFloat(6.28f);
					int dir = Main.rand.NextBool() ? -1 : 1;

					ParticleHandler.SpawnParticle(new LightFlash(target, Vector2.Zero, Color.DarkRed, Color.OrangeRed, new Vector2(0.3f, 0.75f) * Main.rand.NextFloat(0.75f, 1.25f), 30 + Main.rand.Next(5, 40), rot, dir)
					{ Layer = ParticleLayer.BelowSolid });

					rot = Main.rand.NextFloat(6.28f);
					dir = Main.rand.NextBool() ? -1 : 1;

					ParticleHandler.SpawnParticle(new LightFlash(target, Vector2.Zero, Color.DarkOrange, Color.Red, new Vector2(0.35f, 0.75f) * Main.rand.NextFloat(1f, 1.5f), 20 + Main.rand.Next(5, 40), rot, dir)
					{ Layer = ParticleLayer.BelowSolid });

					ParticleHandler.SpawnParticle(new TriangleParticle(target.Center, Main.rand.NextVector2CircularEdge(3f, 3f), Color.Red, Color.OrangeRed, Main.rand.NextFloat(0.6f, 0.9f), 35));
				}
			}
		}
	}

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		if (!Main.dedServ)
			GameShaders.Armor.BindShader(Type, new RageGlyphShaderData(AssetLoader.LoadedShaders["GlyphShader"], "mainPass"));
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.Green;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(new(176, 16, 20));
	}

	public override void DrawHeldItem(ref PlayerDrawSet drawInfo, DrawData input)
	{
		float shakeCounter = Math.Max((float)Math.Sin(Main.timeForVisualEffects * 0.025f), 0);
		Vector2 shake = Main.rand.NextVector2Circular(1.25f, 1.25f) * shakeCounter;

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			DrawData item = input;
			item.position += offset + shake;
			item.color = Color.Red * 0.5f;
			drawInfo.DrawDataCache.Add(item);

			offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;
			item = input;
			item.position += offset + shake;
			item.color = Color.Red * 0.15f;
			drawInfo.DrawDataCache.Add(item);
		}

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;

			DrawData item = input;
			item.position += offset + shake;
			item.shader = GameShaders.Armor.GetShaderIdFromItemId(Type);
			drawInfo.DrawDataCache.Add(item);
		}
	}

	public override void DrawInWorld(Item item, SpriteBatch spriteBatch, ItemMethods.ItemDrawParams parameters)
	{
		Texture2D texWhite = TextureColorCache.ColorSolid(parameters.Texture, Color.White);
		Effect effect = AssetLoader.LoadedShaders["GlyphShader"].Value;

		effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects * 0.0025f);
		effect.Parameters["screenPos"].SetValue(Main.screenPosition * new Vector2(0.5f, 0.1f) / new Vector2(Main.screenWidth, Main.screenHeight));
		effect.Parameters["intensity"].SetValue(0.15f * (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.01f)));

		var noise = AssetLoader.LoadedTextures["swirlNoise"].Value;
		//var gradient = AssetLoader.LoadedTextures["Glyphs/BaseGlyph_RampTexture"].Value;
		var noiseAlt = AssetLoader.LoadedTextures["swirlNoise"].Value;

		effect.Parameters["uImage1"].SetValue(noise);
		effect.Parameters["uImage2"].SetValue(noiseAlt);
		//effect.Parameters["uImage3"].SetValue(gradient);
		effect.Parameters["itemSize"].SetValue(parameters.Texture.Size());

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.005f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.0075f));

		effect.Parameters["uColor1"].SetValue(Color.Lerp(Color.OrangeRed, Color.Red, sin).ToVector4() * 0.5f);
		effect.Parameters["uColor2"].SetValue(Color.Lerp(Color.DarkRed, new Color(226, 0, 45), cos).ToVector4() * 0.5f);
		effect.Parameters["uColor3"].SetValue(Color.Orange.ToVector4());

		effect.Parameters["baseDepth"].SetValue(4f);
		effect.Parameters["scale"].SetValue(0.66f);

		float shakeCounter = Math.Max((float)Math.Sin(Main.timeForVisualEffects * 0.025f), 0);
		Vector2 shake = Main.rand.NextVector2Circular(1.25f, 1.25f) * shakeCounter;

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;

			spriteBatch.Draw(texWhite, parameters.Position + offset + shake, parameters.Source, Color.Red * 0.5f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);

			offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;

			spriteBatch.Draw(texWhite, parameters.Position + offset + shake, parameters.Source, Color.Red * 0.15f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
		}

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;

			spriteBatch.Draw(texWhite, parameters.Position + offset + shake, parameters.Source, Color.White, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
		}

		spriteBatch.RestartToDefault();

		base.DrawInWorld(item, spriteBatch, parameters);
	}

	public override void UpdateInWorld(Item item, ref float gravity, ref float maxFallSpeed)
	{
		if (!Main.dedServ && Main.rand.NextBool(100))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);
			Vector2 velocity = -Vector2.UnitY * Main.rand.NextFloat(0.5f);

			ParticleHandler.SpawnParticle(new ImpactLine(pos, velocity, Color.Red.Additive(), new Vector2(0.7f, 1f), 30)
			{ Layer = ParticleLayer.AboveItem });

			ParticleHandler.SpawnQueuedParticle(new ImpactLine(pos, velocity, Color.Black, new Vector2(0.5f, 1f), 30)
			{ Layer = ParticleLayer.AboveItem }, 3);
		}
	}

	public override void GlyphShootEffects(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (Main.dedServ)
			return;

		Vector2 normalized = velocity.SafeNormalize(Vector2.One);
		Vector2 pos = position + normalized * item.width;

		for (int i = 0; i < 3; i++)
		{
			Vector2 vel = normalized.RotatedByRandom(0.4f) * Main.rand.NextFloat(5f);

			ParticleHandler.SpawnParticle(new ImpactLine(pos, vel, Color.Red.Additive(), new Vector2(0.7f, 1f) * Main.rand.NextFloat(0.3f, 0.5f), 15));
			ParticleHandler.SpawnQueuedParticle(new ImpactLine(pos, vel, Color.Black, new Vector2(0.5f, 1f) * Main.rand.NextFloat(0.3f, 0.5f), 15), 1);
		}
	}

	public override void UpdateGlyphProjectile(Projectile projectile)
	{
		if (!Main.dedServ && Main.rand.NextBool(9 + 8 * projectile.extraUpdates))
		{
			Vector2 pos = projectile.Center + Main.rand.NextVector2Circular(projectile.width / 2, projectile.height / 2);
			Vector2 vel = projectile.velocity.SafeNormalize(Main.rand.NextVector2Circular(1f, 1f)).RotatedByRandom(0.5f) * Main.rand.NextFloat(1f, 4f) + Main.rand.NextVector2Circular(0.5f, 0.5f);

			ParticleHandler.SpawnParticle(new TriangleParticle(pos, vel, Color.Red, Color.OrangeRed, Main.rand.NextFloat(0.4f, 0.6f), 30));
		}
	}
}

public class RageGlyphShaderData(Asset<Effect> shader, string shaderPass) : ArmorShaderData(shader, shaderPass)
{
	private Effect GetEffect => shader.Value;

	public override void Apply(Entity entity, DrawData? drawData = null)
	{
		if (!drawData.HasValue)
			return;

		GetEffect.Parameters["time"].SetValue((float)Main.timeForVisualEffects * 0.0025f);
		GetEffect.Parameters["screenPos"].SetValue(Main.screenPosition * new Vector2(0.5f, 0.1f) / new Vector2(Main.screenWidth, Main.screenHeight));
		GetEffect.Parameters["intensity"].SetValue(0.15f * (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.01f)));

		var noise = AssetLoader.LoadedTextures["swirlNoise"].Value;
		//var gradient = AssetLoader.LoadedTextures["Glyphs/BaseGlyph_RampTexture"].Value;
		var noiseAlt = AssetLoader.LoadedTextures["swirlNoise"].Value;

		GetEffect.Parameters["uImage1"].SetValue(noise);
		GetEffect.Parameters["uImage2"].SetValue(noiseAlt);
		//effect.Parameters["uImage3"].SetValue(gradient);
		GetEffect.Parameters["itemSize"].SetValue(drawData.Value.texture.Size());

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.005f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.0075f));

		GetEffect.Parameters["uColor1"].SetValue(Color.Lerp(Color.OrangeRed, Color.Red, sin).ToVector4() * 0.5f);
		GetEffect.Parameters["uColor2"].SetValue(Color.Lerp(Color.DarkRed, new Color(226, 0, 45), cos).ToVector4() * 0.5f);
		GetEffect.Parameters["uColor3"].SetValue(Color.Orange.ToVector4());

		GetEffect.Parameters["baseDepth"].SetValue(4f);
		GetEffect.Parameters["scale"].SetValue(0.66f);

		Apply();
	}
}
