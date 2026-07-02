using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.RenderTargets;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Renderers;
using Terraria.Graphics.Shaders;

namespace SpiritReforged.Content.Glyphs.Storm;

public class StormGlyph : GlyphItem
{
	/// this is from windshear scepter with minor changes to color and behavior
	[Autoload(Side = ModSide.Client)]
	public sealed class StormMetaballSystem : ModSystem
	{
		private static readonly ModTarget2D StormTarget = new(static () => StormParticleRenderer.Particles.Count != 0 || Data.Count != 0, DrawCloudTarget);
		private static readonly ParticleRenderer StormParticleRenderer = new();
		private static readonly HashSet<DrawData> Data = [];
		private static readonly List<WindBurstProjectile> bursts = new();

		public static void Add(ABasicParticle particle) => StormParticleRenderer.Add(particle);
		public static void Add(DrawData drawData) => Data.Add(drawData);
		public static void Add(WindBurstProjectile burst) => bursts.Add(burst);

		public override void Load() => On_Main.DrawProjectiles += DrawShader;

		private static void DrawShader(On_Main.orig_DrawProjectiles orig, Main self)
		{
			if (StormTarget != null && StormTarget.Active)
			{
				Effect s = AssetLoader.LoadedShaders["CloudMetaball"].Value;
				SpriteBatch spriteBatch = Main.spriteBatch;

				s.Parameters["primaryColor"].SetValue(Color.LightCyan.ToVector4());
				s.Parameters["secondaryColor"].SetValue(new Color(50, 50, 50, 150).ToVector4());
				s.Parameters["numColors"].SetValue(3);
				ShaderHelpers.SetEffectMatrices(ref s);

				spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, s, Main.Transform);

				spriteBatch.Draw(StormTarget, Vector2.Zero, Color.White);

				spriteBatch.End();
			}

			orig(self);
		}

		private static void DrawCloudTarget(SpriteBatch spriteBatch)
		{
			StormParticleRenderer.Settings.AnchorPosition = -Main.screenPosition;
			StormParticleRenderer.Draw(spriteBatch);

			foreach (DrawData data in Data)
				spriteBatch.Draw(data.texture, data.position, data.sourceRect, data.color, data.rotation, data.origin, data.scale, data.effect, 0);

			foreach (WindBurstProjectile burst in bursts)
				if (burst.Projectile.active)
					burst.Draw(spriteBatch);

			Data.Clear();
		}

		public override void PostUpdateProjectiles()
		{
			StormParticleRenderer.Update();

			bursts.RemoveAll(p => !p.Projectile.active);
		}
	}

	public class StormParticle(int style) : ABasicParticle
	{
		public static readonly Asset<Texture2D> Texture = DrawHelpers.RequestLocal(typeof(StormParticle), "StormParticle", false);

		public float Opacity;
		public int TimeToLive = 60;

		public float floatSpeed = 0f;

		private readonly int _style = style;
		private int _timeSinceSpawn;

		public override void Update(ref ParticleRendererSettings settings)
		{
			base.Update(ref settings);

			if (++_timeSinceSpawn > TimeToLive)
				ShouldBeRemovedFromRenderer = true;
			else
			{
				Velocity *= 0.95f;
				Rotation += Velocity.X * 0.01f;

				if (floatSpeed > 0)
					Velocity.Y -= floatSpeed;

				int halfTime = TimeToLive / 2;

				if (_timeSinceSpawn > halfTime)
					Opacity -= 1f / halfTime;
			}
		}

		public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
		{
			Texture2D texture = Texture.Value;
			Vector2 position = settings.AnchorPosition + LocalPosition;
			float frame = (float)_timeSinceSpawn / TimeToLive;
			Rectangle source = texture.Frame(2, 5, _style, (int)(EaseFunction.EaseCubicIn.Ease(frame) * 5f), -2, -2);

			spritebatch.Draw(texture, position, source, Color.White * Opacity, Rotation, source.Size() / 2, Scale, default, 0);
		}
	}

	public sealed class StormGlyphPlayer : ModPlayer
	{
		internal int _cooldown;
		public bool Active => Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<StormGlyph>();

		public override void ModifyShootStats(Item item, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
		{
			if (Active)
				// projectiles get an extra update when they do the wind burst (double speed)
				if (_cooldown > 0)
					velocity *= 1.5f;
		}

		public override bool Shoot(Item item, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			if (Active)
				if (_cooldown <= 0)
				{
					for (float k = 0; k < 6.28f; k += 0.1f)
					{
						float x = (float)Math.Cos(k) * 30;
						float y = (float)Math.Sin(k) * 10;

						ParticleHandler.SpawnParticle(new SmokeCloud(position + velocity,
							velocity * 0.15f + new Vector2(x, y).RotatedBy(velocity.ToRotation() + MathHelper.PiOver2) * 0.05f, Color.WhiteSmoke * 0.05f, Main.rand.NextFloat(0.02f, 0.07f), EaseFunction.EaseQuadOut, 60, false));

						ParticleHandler.SpawnParticle(new SmokeCloud(position + velocity * 2f,
							velocity * 0.2f + new Vector2(x, y).RotatedBy(velocity.ToRotation() + MathHelper.PiOver2) * 0.06f, Color.LightCyan * 0.05f, Main.rand.NextFloat(0.02f, 0.07f), EaseFunction.EaseQuadOut, 60, false));
					}

					SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/SwordSlash1") with { Volume = 1.5f, PitchVariance = 0.2f }, position);
				}
				else
					SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/SmallProjectileWoosh_1") with { Volume = 2f, PitchVariance = 0.2f }, position);

			return base.Shoot(item, source, position, velocity, type, damage, knockback);
		}

		public override void ResetEffects()
		{
			if (_cooldown > 0)
				_cooldown--;
		}
	}

	public sealed class StormGlyphGlobalProjectile : GlobalProjectile
	{
		public override bool InstancePerEntity => true;
		public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.friendly;

		private readonly ParticleRenderer _stormParticleRenderer = new();
		private VertexTrail[] _trails;

		public bool doVisuals;
		public bool doWindBurst;
		public override void OnSpawn(Projectile projectile, IEntitySource source)
		{
			if (source is IEntitySource_WithStatsFromItem { Item: Item item } && item.GetGlyph() is GlyphType itemGlyph && itemGlyph.ItemType == ModContent.ItemType<StormGlyph>())
			{
				Player player = Main.player[projectile.owner];

				var mp = player.GetModPlayer<StormGlyphPlayer>();

				if (mp._cooldown <= 0)
				{
					doWindBurst = true;
					projectile.extraUpdates++;
					projectile.velocity *= 1.2f;
					mp._cooldown = 60 * 5; // 5 seconds;
				}

				doVisuals = true;
				projectile.netUpdate = true;
			}
		}

		public override void AI(Projectile projectile)
		{
			if (doVisuals)
			{
				if (!Main.dedServ)
				{
					if (_trails == null)
						CreateTrail(projectile);

					foreach (VertexTrail trail in _trails)
						trail.Update();
				}

				if (projectile.timeLeft % 3 == 0)
				{
					Vector2 pos = projectile.Center;

					ParticleHandler.SpawnParticle(new SmokeCloud(pos, projectile.velocity * Main.rand.NextFloat(0.3f), Color.White * 0.15f, 0.03f, EaseFunction.EaseQuadOut, 60, false));

					ParticleHandler.SpawnParticle(new SmokeCloud(pos, projectile.velocity * Main.rand.NextFloat(0.3f), Color.LightCyan * 0.25f, 0.02f, EaseFunction.EaseQuadOut, 60, false));
				}

				if (doWindBurst)
				{
					if (Main.rand.NextBool(17))
					{
						Vector2 pos = projectile.Center + Main.rand.NextVector2Circular(20f, 20f);
						Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);
						float scale = Main.rand.NextFloat(0.1f, 0.3f);

						ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.LightCyan.Additive(), scale, 90, 12, Main.rand.NextBool() ? SpinAction : SpinAction_2));
						ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.White.Additive(), scale * 0.5f, 90, 12, Main.rand.NextBool() ? SpinAction : SpinAction_2));
					}

					StormMetaballSystem.Add(new StormParticle(Main.rand.Next(2))
					{
						LocalPosition = projectile.Center,
						Scale = Vector2.One * Main.rand.NextFloat(0.4f, 0.8f),
						Velocity = projectile.velocity * Main.rand.NextFloat(),
						TimeToLive = 30,
						Opacity = 1f,
						Rotation = Main.rand.NextFloat()
					});
				}

				// yes this is silly
				// yes I think this is the most reasonable way to do this
				static void SpinAction(Particle p)
				{
					p.Velocity *= 0.97f;
					p.Velocity = p.Velocity.RotatedBy(0.08f);
				}

				static void SpinAction_2(Particle p)
				{
					p.Velocity *= 0.97f;
					p.Velocity = p.Velocity.RotatedBy(-0.08f);
				}
			}
		}

		public override void PostAI(Projectile projectile)
		{
			// check for held projctiles- if a glyph is applied to one we want to make sure its ignored
			// works great for things like the vortex beater- entity sources carry the glyph to the bullets fired but the vortex beater itself does not have the effects
			if (doVisuals && Main.player[projectile.owner].heldProj > -1 && Main.player[projectile.owner].heldProj == projectile.whoAmI)
			{
				doVisuals = false;
				if (doWindBurst)
				{
					doWindBurst = false;
					projectile.extraUpdates--;
				}
			}
		}

		public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity)
		{
			if (doWindBurst)
			{
				if (Main.myPlayer == projectile.owner)
				{
					ScreenshakeHelper.Shake(projectile.Center, projectile.DirectionTo(Main.player[projectile.owner].Center), 1, 4, 10);
					var p = Projectile.NewProjectileDirect(projectile.GetSource_FromThis(), projectile.Center, Vector2.Zero, ModContent.ProjectileType<WindBurstProjectile>(), (int)(10 * projectile.knockBack), projectile.knockBack * Main.rand.NextFloat(3f, 5f), projectile.owner);

					StormMetaballSystem.Add(p.ModProjectile as WindBurstProjectile);
				}

				WindBurstEffects(projectile);

				doWindBurst = false;
			}

			return base.OnTileCollide(projectile, oldVelocity);
		}

		public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (doVisuals)
				for (int i = 0; i < 4; i++)
				{
					Vector2 pos = projectile.Center + Main.rand.NextVector2Circular(5f, 5f);
					Vector2 velocity = Main.rand.NextVector2Circular(3f, 3f);
					float scale = Main.rand.NextFloat(0.1f, 0.3f);

					ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.LightCyan.Additive(), scale, 60, 15, Main.rand.NextBool() ? SpinAction : SpinAction_2));
					ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.White.Additive(), scale * 0.5f, 60, 15, Main.rand.NextBool() ? SpinAction : SpinAction_2));

					static void SpinAction(Particle p)
					{
						p.Velocity *= 0.965f;
						p.Velocity = p.Velocity.RotatedBy(0.09f);
					}

					static void SpinAction_2(Particle p)
					{
						p.Velocity *= 0.965f;
						p.Velocity = p.Velocity.RotatedBy(-0.09f);
					}
				}

			if (doWindBurst)
			{
				if (Main.myPlayer == projectile.owner)
				{
					ScreenshakeHelper.Shake(target.Center, target.DirectionTo(Main.player[projectile.owner].Center), 1, 4, 10);
					var p = Projectile.NewProjectileDirect(projectile.GetSource_FromThis(), projectile.Center, Vector2.Zero, ModContent.ProjectileType<WindBurstProjectile>(), (int)(10 * projectile.knockBack), projectile.knockBack * Main.rand.NextFloat(3f, 5f), projectile.owner);

					StormMetaballSystem.Add(p.ModProjectile as WindBurstProjectile);
				}

				WindBurstEffects(projectile);

				doWindBurst = false;
			}
		}

		public override bool PreDraw(Projectile projectile, ref Color lightColor)
		{
			Main.instance.LoadProjectile(79);
			var star = TextureAssets.Projectile[79].Value;

			if (doVisuals)
			{
				_stormParticleRenderer.Draw(Main.spriteBatch);

				if (_trails != null)
					foreach (VertexTrail trail in _trails)
					{
						trail.Opacity = 1f;
						trail?.Draw(TrailSystem.TrailShaders, AssetLoader.BasicShaderEffect, Main.spriteBatch.GraphicsDevice);
					}

				if (doWindBurst)
					Main.spriteBatch.Draw(star, projectile.Center - Main.screenPosition, null, Color.White.Additive(), 0f, star.Size() / 2f, 0.35f, 0f, 0f);
			}

			return base.PreDraw(projectile, ref lightColor);
		}

		private void WindBurstEffects(Projectile projectile)
		{
			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/SwordSlash1") with { Volume = 1.5f, PitchVariance = 0.2f }, projectile.Center);
			SoundEngine.PlaySound(SoundID.DD2_WitherBeastAuraPulse with { Volume = 1f, PitchVariance = 0.1f }, projectile.Center);
			SoundEngine.PlaySound(SoundID.DD2_SonicBoomBladeSlash with { Volume = 1f, PitchVariance = 0.2f }, projectile.Center);
			SoundEngine.PlaySound(SoundID.DoubleJump with { Volume = 2f, PitchVariance = 0.2f, Pitch = -0.2f }, projectile.Center);

			for (int i = 0; i < 35; i++)
			{
				StormMetaballSystem.Add(new StormParticle(Main.rand.Next(2))
				{
					LocalPosition = projectile.Center + Main.rand.NextVector2Circular(40f, 40f),
					Scale = Vector2.One * Main.rand.NextFloat(0.7f, 1.2f),
					Velocity = Main.rand.NextVector2Circular(4.5f, 4.5f) - Vector2.UnitY * Main.rand.NextFloat(),
					TimeToLive = Main.rand.Next(30, 70),
					Opacity = 1f,
					floatSpeed = Main.rand.NextFloat(0.01f, 0.1f),
					Rotation = Main.rand.NextFloat()
				});

				ParticleHandler.SpawnParticle(new SmokeCloud(projectile.Center + Main.rand.NextVector2Circular(40f, 40f),
					Main.rand.NextVector2Circular(2.5f, 2.5f) - Vector2.UnitY * Main.rand.NextFloat(), Color.LightGray * 0.4f, 0.12f, EaseFunction.EaseQuadOut, Main.rand.Next(45, 75), false)
				{
					Pixellate = true,
					PixelDivisor = 3,
				});
			}
		}

		private void CreateTrail(Projectile proj)
		{
			ITrailCap tCap = new RoundCap();
			ITrailPosition tPos = new EntityTrailPosition(proj);
			ITrailShader tShader = new ImageShader(AssetLoader.LoadedTextures["GlowTrail"].Value, Vector2.One);

			_trails =
			[
				new VertexTrail(new GradientTrail(Color.LightCyan, Color.Transparent, EaseFunction.EaseQuarticOut), tCap, tPos, tShader, 40, 150, -2),
				new VertexTrail(new StandardColorTrail(Color.Gray * 0.25f), tCap, tPos, tShader, 20, 150, -2),
			];

			if (doWindBurst)
				_trails =
				[
					.. _trails,
					new VertexTrail(new GradientTrail(Color.LightCyan.Additive(), Color.White.Additive() * 0.2f, EaseFunction.EaseQuarticOut), tCap, tPos, tShader, 10, 170, -2),
				];
		}
	}

	public class WindBurstProjectile : ModProjectile
	{
		public override string Texture => AssetLoader.EmptyTexture;

		public override void SetDefaults()
		{
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;

			Projectile.penetrate = 5;
			Projectile.stopsDealingDamageAfterPenetrateHits = true;
			Projectile.aiStyle = -1;
			Projectile.timeLeft = 45;

			Projectile.Size = new(120);
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Generic;

			Projectile.tileCollide = false;
			Projectile.rotation = Main.rand.NextFloat(6.28f);
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			modifiers.HitDirectionOverride = Main.player[Projectile.owner].Center.X > target.Center.X ? -1 : 1;

			//modifiers.HideCombatText();
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			var tex = DrawHelpers.RequestLocal(typeof(StormParticle), "StormParticle", false).Value;

			float frame = 1f - Projectile.timeLeft / 45f;
			Rectangle source = tex.Frame(2, 5, 0, (int)(EaseFunction.EaseCubicIn.Ease(frame) * 5f), -2, -2);

			spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, source, Color.White * EaseFunction.EaseCircularOut.Ease(1f - frame), Projectile.rotation, source.Size() / 2, MathHelper.Lerp(0.5f, 2.5f, EaseFunction.EaseCircularOut.Ease(frame)), default, 0);
		}

		public override void PostDraw(Color lightColor)
		{
			var star = AssetLoader.LoadedTextures["Star"].Value;

			float progress = Projectile.timeLeft / 45f;

			Main.spriteBatch.Draw(star, Projectile.Center - Main.screenPosition, null, Color.LightCyan.Additive() * progress, 0f, star.Size() / 2f, new Vector2(MathHelper.Lerp(0.25f, 0.6f, EaseFunction.EaseCircularIn.Ease(1f - progress)), MathHelper.Lerp(0.2f, 0.01f, EaseFunction.EaseCircularIn.Ease(1f - progress))), 0f, 0f);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.velocity.Y -= Main.rand.NextFloat(1f, 3.33f);
			target.netUpdate = true;

			//int idx = CombatText.NewText(target.getRect(), Color.White, damageDone, hit.Crit);

			//ColoredCombatText.AddCombatText(idx, Color.GhostWhite, Color.LightGray);
		}
	}

	// I hate it here
	private static List<int> vanillaBlacklist = [
		// swords with the slash thingies (theyre techincally projectiles)
		ItemID.NightsEdge,
		ItemID.TrueExcalibur,
		ItemID.TheHorsemansBlade,

		// yoyos
		ItemID.WoodYoyo,
		ItemID.Rally,
		ItemID.CorruptYoyo,
		ItemID.CrimsonYoyo,
		ItemID.JungleYoyo,
		ItemID.Code1,
		ItemID.Code2,
		ItemID.HiveFive,
		ItemID.Valor,
		ItemID.Cascade,
		ItemID.FormatC,
		ItemID.Gradient,
		ItemID.Chik,
		ItemID.HelFire,
		ItemID.Amarok,
		3286, // yelets
		ItemID.RedsYoyo,
		ItemID.ValkyrieYoyo,
		ItemID.Kraken,
		ItemID.TheEyeOfCthulhu,
		ItemID.Terrarian,

		// spears
		ItemID.Spear,
		ItemID.Trident,
		ItemID.ThunderSpear,
		ItemID.TheRottedFork,
		ItemID.Swordfish,
		ItemID.DarkLance,
		ItemID.CobaltNaginata,
		ItemID.PalladiumPike,
		ItemID.MythrilHalberd,
		ItemID.OrichalcumHalberd,
		ItemID.AdamantiteGlaive,
		ItemID.TitaniumTrident,
		ItemID.Gungnir,
		3836, // ghastly glaive
		ItemID.ChlorophytePartisan,
		ItemID.MushroomSpear,
		ItemID.ObsidianSwordfish,
		ItemID.NorthPole,

		// flails
		ItemID.Mace,
		ItemID.FlamingMace,
		ItemID.BallOHurt,
		ItemID.TheMeatball,
		ItemID.BlueMoon,
		ItemID.Sunfury,
		ItemID.ChainKnife,
		ItemID.DripplerFlail,
		ItemID.DaoofPow,
		ItemID.FlowerPow,
		ItemID.Anchor,
		ItemID.ChainGuillotines,
		ItemID.KOCannon,
		ItemID.GolemFist,
		ItemID.Flairon,
		// kill me
		// misc
		ItemID.Arkhalis,
		ItemID.Terragrim,
		ItemID.JoustingLance,
		ItemID.HallowJoustingLance,
		ItemID.ShadowJoustingLance,
		3835, // sleepy octopod
		3858, // sky dragons fury
		ItemID.PiercingStarlight,
		ItemID.SolarEruption,
		ItemID.Zenith,
	];

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		GameShaders.Armor.BindShader(Type, new StormGlyphShaderData(AssetLoader.LoadedShaders["GlyphShader"], "mainPass"));
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.Green;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(new(142, 186, 231));
	}

	public override bool CanApplyGlyph(Item item)
	{
		// todo: add checks for Rapiers, Katanas, etc when subclassdate
		if (item.ModItem is ClubItem)
			return false;

		if (vanillaBlacklist.Contains(item.type))
			return false;

		return base.CanApplyGlyph(item) && item.shoot != ProjectileID.None && item.DamageType != DamageClass.Summon;
	}

	public override void DrawHeldItem(ref PlayerDrawSet drawInfo, DrawData input)
	{
		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			DrawData item = input;
			item.texture = TextureColorCache.ColorSolid(item.texture, Color.White);
			item.color = Color.WhiteSmoke.Additive() * 0.35f;
			item.position += offset;
			drawInfo.DrawDataCache.Add(item);

			offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;
			item.color = Color.LightCyan.Additive() * 0.05f;
			item.position = input.position + offset;
			drawInfo.DrawDataCache.Add(item);
		}

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			DrawData item = input;
			item.texture = TextureColorCache.ColorSolid(item.texture, Color.White);
			item.position += offset;
			item.shader = GameShaders.Armor.GetShaderIdFromItemId(Type);
			drawInfo.DrawDataCache.Add(item);
		}
	}

	public override void DrawInWorld(Item item, SpriteBatch spriteBatch, ItemMethods.ItemDrawParams parameters)
	{
		Texture2D whiteTexture = TextureColorCache.ColorSolid(parameters.Texture, Color.White);
		Effect effect = AssetLoader.LoadedShaders["GlyphShader"].Value;

		effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects * 0.0025f);
		effect.Parameters["screenPos"].SetValue(Main.screenPosition * new Vector2(0.5f, 0.1f) / new Vector2(Main.screenWidth, Main.screenHeight));
		effect.Parameters["intensity"].SetValue(0.15f * (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.01f)));

		effect.Parameters["uImage1"].SetValue(AssetLoader.LoadedTextures["noise"].Value);
		effect.Parameters["uImage2"].SetValue(AssetLoader.LoadedTextures["swirlNoise"].Value);
		effect.Parameters["itemSize"].SetValue(parameters.Texture.Size());

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.005f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.0075f));

		effect.Parameters["uColor1"].SetValue(Color.Lerp(Color.LightCyan, Color.Cyan, sin).ToVector4() * 0.5f);
		effect.Parameters["uColor2"].SetValue(Color.Lerp(Color.LightGray, Color.White, cos).ToVector4() * 0.5f);
		effect.Parameters["uColor3"].SetValue(Color.WhiteSmoke.ToVector4());

		effect.Parameters["baseDepth"].SetValue(4f);
		effect.Parameters["scale"].SetValue(0.66f);

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			spriteBatch.Draw(whiteTexture, parameters.Position + offset, parameters.Source, Color.WhiteSmoke.Additive() * 0.35f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);

			offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;
			spriteBatch.Draw(whiteTexture, parameters.Position + offset, parameters.Source, Color.LightCyan.Additive() * 0.05f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
		}

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			spriteBatch.Draw(whiteTexture, parameters.Position + offset, parameters.Source, Color.White, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
		}

		spriteBatch.RestartToDefault();

		base.DrawInWorld(item, spriteBatch, parameters);
	}

	public static Vector2 FindGroundFromPosition(Vector2 input)
	{
		const int dimensions = 8;

		while (!CollisionChecks.Tiles(new((int)input.X - dimensions / 2, (int)input.Y - dimensions / 2, dimensions, dimensions), CollisionChecks.AnySurface))
			input.Y += dimensions;

		while (CollisionChecks.Tiles(new((int)input.X - dimensions / 2, (int)input.Y - dimensions / 2, dimensions, dimensions), CollisionChecks.AnySurface))
			input.Y -= dimensions;

		return input + new Vector2(0, dimensions);
	}

	sealed class StormGlyphGlobalItem : GlobalItem
	{
		public Vector2? tilePosition;

		public override bool InstancePerEntity => true;
	}

	public override void UpdateInWorld(Item item, ref float gravity, ref float maxFallSpeed)
	{
		var globalItem = item.GetGlobalItem<StormGlyphGlobalItem>();

		if (globalItem.tilePosition is null && item.velocity == Vector2.Zero)
			globalItem.tilePosition = FindGroundFromPosition(item.Center);

		gravity *= 0.33f;

		if (globalItem.tilePosition.HasValue && Math.Abs(item.Center.Y - globalItem.tilePosition.Value.Y) < 18 && Math.Abs(item.Center.Y - globalItem.tilePosition.Value.Y) > 0)
			item.velocity.Y -= 0.06f;

		if (item.velocity.X != 0)
			globalItem.tilePosition = null;

		if (item.velocity.Y < 0)
		{
			if (Main.rand.NextBool(3))
			{
				Vector2 pos = item.Center + new Vector2(Main.rand.Next(-item.width / 2, item.width / 2), -Main.rand.Next(item.height / 4));

				StormMetaballSystem.Add(new StormParticle(Main.rand.Next(2))
				{
					LocalPosition = pos,
					Scale = Vector2.One * Main.rand.NextFloat(0.3f, 0.5f),
					Velocity = Vector2.UnitY * Main.rand.NextFloat(2f),
					TimeToLive = 20,
					Opacity = 1f,
					Rotation = Main.rand.NextFloat()
				});
			}

			if (Main.rand.NextBool(5))
			{
				Vector2 pos = item.Center + new Vector2(Main.rand.Next(-item.width / 4, item.width / 4), -Main.rand.Next(item.height / 4));

				ParticleHandler.SpawnParticle(new SmokeCloud(pos, Vector2.UnitY * Main.rand.NextFloat(2f), Color.White * 0.25f, 0.06f, EaseFunction.EaseQuadOut, 60, false));

				ParticleHandler.SpawnParticle(new SmokeCloud(pos, Vector2.UnitY * Main.rand.NextFloat(2f), Color.LightCyan * 0.35f, 0.04f, EaseFunction.EaseQuadOut, 60, false));
			}
		}

		if (Main.rand.NextBool(50))
		{
			Vector2 pos = item.Center + new Vector2(Main.rand.Next(-item.width / 4, item.width / 4), -Main.rand.Next(item.height / 4));

			Vector2 velocity = Main.rand.NextVector2CircularEdge(2f, 2f);

			float scale = Main.rand.NextFloat(0.1f, 0.3f);

			ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.LightCyan.Additive(), scale, 120, 3, SpinAction));
			ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.White.Additive(), scale * 0.5f, 120, 3, SpinAction));
		}

		if (Main.rand.NextBool(10) && item.velocity.Y < 0)
		{
			Vector2 pos = item.Center + new Vector2(Main.rand.Next(-item.width / 2, item.width / 2), -Main.rand.Next(item.height / 4));

			Vector2 velocity = Vector2.UnitY * Main.rand.NextFloat(1f, 3f);

			float scale = Main.rand.NextFloat(0.1f, 0.3f);

			ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.LightCyan.Additive(), scale, 60, 3, DecelerateAction));
			ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.White.Additive(), scale * 0.5f, 60, 3, DecelerateAction));

			velocity = Vector2.UnitY * Main.rand.NextFloat(0.5f, 1f);

			ParticleHandler.SpawnParticle(new SmokeCloud(pos, velocity, Color.White * 0.15f, 0.05f, EaseFunction.EaseQuadOut, 60, false));

			ParticleHandler.SpawnParticle(new SmokeCloud(pos, velocity, Color.LightCyan * 0.25f, 0.03f, EaseFunction.EaseQuadOut, 60, false));
		}

		static void SpinAction(Particle p)
		{
			p.Velocity *= 0.97f;
			p.Velocity = p.Velocity.RotatedBy(0.05f);
		}

		static void DecelerateAction(Particle p)
		{
			p.Velocity *= 0.95f;
		}
	}
}

public class StormGlyphShaderData(Asset<Effect> shader, string shaderPass) : ArmorShaderData(shader, shaderPass)
{
	private Effect GetEffect => shader.Value;

	public override void Apply(Entity entity, DrawData? drawData = null)
	{
		if (!drawData.HasValue)
			return;

		GetEffect.Parameters["time"].SetValue((float)Main.timeForVisualEffects * 0.0025f);
		GetEffect.Parameters["screenPos"].SetValue(Main.screenPosition * new Vector2(0.5f, 0.1f) / new Vector2(Main.screenWidth, Main.screenHeight));
		GetEffect.Parameters["intensity"].SetValue(0.15f * (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.01f)));

		GetEffect.Parameters["uImage1"].SetValue(AssetLoader.LoadedTextures["noise"].Value);
		GetEffect.Parameters["uImage2"].SetValue(AssetLoader.LoadedTextures["swirlNoise"].Value);
		GetEffect.Parameters["itemSize"].SetValue(drawData.Value.texture.Size());

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.005f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.0075f));

		GetEffect.Parameters["uColor1"].SetValue(Color.Lerp(Color.LightCyan, Color.Cyan, sin).ToVector4() * 0.5f);
		GetEffect.Parameters["uColor2"].SetValue(Color.Lerp(Color.LightGray, Color.White, cos).ToVector4() * 0.5f);
		GetEffect.Parameters["uColor3"].SetValue(Color.WhiteSmoke.ToVector4());

		GetEffect.Parameters["baseDepth"].SetValue(4f);
		GetEffect.Parameters["scale"].SetValue(0.66f);

		Apply();
	}
}