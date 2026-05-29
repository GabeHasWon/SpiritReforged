using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using System.Linq;
using Terraria.Audio;

namespace SpiritReforged.Content.Forest.Glyphs;

public class BeeGlyph : GlyphItem
{
	public class BeeInOrbit : Particle
	{
		public NPC Parent => Main.npc[_parentWhoAmI];

		public override ParticleDrawType DrawType => ParticleDrawType.Custom;

		public bool drawBehind;
		private float _rotationOffset;
		private readonly int _parentWhoAmI;
		private readonly float _animationSpeed;

		public override ParticleLayer DrawLayer => drawBehind ? ParticleLayer.BelowSolid : ParticleLayer.AboveNPC;

		public BeeInOrbit(NPC npc, float speed)
		{
			_parentWhoAmI = npc.whoAmI;
			_animationSpeed = speed;

			MaxTime = 60 * 5;
			Scale = 1f;
		}

		public override void Update()
		{
			_rotationOffset += Main.rand.NextFloat(0.05f);

			float rate = TimeActive * _animationSpeed;
			float sin = (float)Math.Sin(rate);
			float cos = (float)Math.Cos(rate);

			Position = Parent.Center + new Vector2(Parent.width * cos, 0f).RotatedBy(_rotationOffset);
			Rotation = MathHelper.Lerp(Rotation, cos, 0.05f);

			if (sin is < 1f and > (-0.5f))
				drawBehind = true;
			else
				drawBehind = false;
		}

		public override void CustomDraw(SpriteBatch spriteBatch)
		{
			const int type = ProjectileID.Bee;

			Texture2D texture = TextureAssets.Projectile[type].Value;
			Rectangle source = texture.Frame(1, Main.projFrames[type], 0, (int)(TimeActive / 4 % Main.projFrames[type]), 0, 0);
			Color color = Lighting.GetColor(Position.ToTileCoordinates());
			SpriteEffects effects = (Position.X < Parent.Center.X) ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

			if (drawBehind)
				color = color.MultiplyRGB(Color.White * 0.75f);

			spriteBatch.Draw(texture, Position - Main.screenPosition, source, color, Rotation, source.Size() / 2, 1, effects, 0);
		}
	}

	public class BeeOnNPC : Particle
	{
		public NPC Parent => Main.npc[_parentWhoAmI];

		private readonly int _parentWhoAmI;

		private Vector2 _offset;

		private Vector2 PositionToBe => Parent.Center + _offset;
		
		public override ParticleDrawType DrawType => ParticleDrawType.Custom; 
		public override ParticleLayer DrawLayer => ParticleLayer.AboveNPC;

		public BeeOnNPC(NPC npc, Vector2 offset)
		{
			_parentWhoAmI = npc.whoAmI;
			_offset = offset;

			Position = npc.Center + offset;

			MaxTime = Main.rand.Next(5, 8) * 60;
			Velocity = Main.rand.NextVector2Circular(0.5f, 0.5f);
		}

		public override void Update()
		{
			if (!Parent.active)
				Kill();

			Position = Vector2.Lerp(Position, PositionToBe, 0.12f);

			if (Main.rand.NextBool(20))
				_offset = Main.rand.NextVector2Circular(Parent.width, Parent.height);

			_offset += Main.rand.NextVector2Circular(1.5f, 1.5f);
		}

		public override void CustomDraw(SpriteBatch spriteBatch)
		{
			const int type = ProjectileID.Bee;

			var texture = TextureAssets.Projectile[type].Value;
			var bloom = AssetLoader.LoadedTextures["BloomNonPremult"].Value;

			Rectangle source = texture.Frame(1, Main.projFrames[type], 0, (int)(TimeActive / 4 % Main.projFrames[type]), 0, 0);
			Color color = Lighting.GetColor(Position.ToTileCoordinates());
			SpriteEffects effects = (Position.X < Parent.Center.X) ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
			
			float fade = 1f;

			if (Progress < 0.25f)
				fade = (Progress / 0.25f);
			else if (Progress > 0.75f)
				fade = 1f - (Progress - 0.75f) / 0.25f;

			float glyphEffectProgress = 1f - Parent.GetGlobalNPC<BeeNPC>()._tagCooldown / (float)BeeNPC.MAX_TAG_COOLDOWN;

			Vector2 offset = Main.rand.NextVector2CircularEdge(0.5f, 0.5f) * EaseBuilder.EaseCircularIn.Ease(glyphEffectProgress);

			float scale = MathHelper.Lerp(0.6f, 1.1f, EaseBuilder.EaseCircularIn.Ease(glyphEffectProgress));
			
			spriteBatch.End();
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState, default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

			spriteBatch.Draw(bloom, Position + offset - Main.screenPosition, null, Color.Black * 0.35f * fade, 0f, bloom.Size() / 2, scale *0.5f, 0, 0);
			
			spriteBatch.End();
			spriteBatch.BeginDefault();

			spriteBatch.Draw(texture, Position + offset - Main.screenPosition, source, color * fade, Rotation, source.Size() / 2, scale, effects, 0);
		}
	}

	public sealed class BeeGlyphPlayer : ModPlayer
	{

		internal static int[] maxTimeLefts = new int[Main.maxCombatText];

		public override void Load()
		{
			On_CombatText.UpdateCombatText += FadeDamageText;
		}

		private void FadeDamageText(On_CombatText.orig_UpdateCombatText orig)
		{
			orig();

			for (int i = 0; i < Main.maxCombatText; i++)
			{
				CombatText text = Main.combatText[i];
				if (maxTimeLefts[i] > 0)
				{
					if (text.active)
					{
						Color blue, orange;

						blue = text.crit ? Color.Goldenrod : Color.Yellow;
						orange = text.crit ? CombatText.DamagedHostileCrit : CombatText.DamagedHostile;

						text.color = Color.Lerp(blue, orange, EaseBuilder.EaseCircularInOut.Ease(1f - text.lifeTime / (float)maxTimeLefts[i]));
					}
					else
					{
						maxTimeLefts[i] = 0;
					}
				}
			}
		}

		public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (item.GetGlyph().ItemType == ModContent.ItemType<BeeGlyph>())
				modifiers.HideCombatText();
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (proj.GetGlyph().ItemType == ModContent.ItemType<BeeGlyph>())
				modifiers.HideCombatText();
		}

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (item.GetGlyph().ItemType == ModContent.ItemType<BeeGlyph>())
				OnGlyphHit(target, hit, damageDone);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (proj.GetGlyph().ItemType == ModContent.ItemType<BeeGlyph>())
				OnGlyphHit(target, hit, damageDone);
		}

		public static void OnGlyphHit(NPC target, NPC.HitInfo hit, int damageDone)
		{
			Color orange = hit.Crit ? CombatText.DamagedHostileCrit : CombatText.DamagedHostile;

			CombatText.NewText(target.getRect(), orange, Math.Max((int)(damageDone * 0.8f), 1), hit.Crit);
			int magicDamage = CombatText.NewText(target.getRect(), Color.White, Math.Max((int)(damageDone * 0.2f), 1), hit.Crit);

			maxTimeLefts[magicDamage] = Main.combatText[magicDamage]?.lifeTime ?? 10;
		}
	}

	public sealed class BeeNPC : GlobalNPC
	{
		public static int MAX_TAG_COOLDOWN = 300;

		public override bool InstancePerEntity => true;

		public bool _tagged;
		public int _tagCooldown;
		public int _decayTimer;

		public bool CanExplode => _tagged && _tagCooldown <= 0;

		public override void ResetEffects(NPC npc)
		{
			if (_tagCooldown > 0)
				_tagCooldown--;

			if (_decayTimer > 0)
				_decayTimer--;
			else
				_tagged = false;
		}

		public override void AI(NPC npc)
		{
			if (!Main.dedServ && _tagged && Main.rand.NextBool(5) && ParticleHandler.Particles.Where(p => p is BeeOnNPC && (p as BeeOnNPC).Parent == npc).Count() < 3)
				ParticleHandler.SpawnParticle(new BeeOnNPC(npc, Main.rand.NextVector2Circular(25f, 25f)));

			if (Main.rand.NextBool(100) && _tagged)
				ParticleHandler.SpawnParticle(new LargeBeeParticle(npc.Center + Main.rand.NextVector2Circular(20f, 20f), Main.rand.NextVector2Circular(2f, 2f), 0f, Main.rand.NextFloat(0.8f, 1.1f), 90 + Main.rand.Next(60)));
		}

		public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
		{
			if (item.GetGlyph().ItemType == ModContent.ItemType<BeeGlyph>())
			{
				HitEffects(npc);
				if (!_tagged) 
					_tagged = true;

				_decayTimer = 600;
			}		
		}

		public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
		{
			if (!projectile.TryGetOwner(out Player owner))
				return;

			if (projectile.IsMinionOrSentryRelated && CanExplode)
			{
				foreach (Particle p in ParticleHandler.Particles)
				{
					if (p is BeeOnNPC && (p as BeeOnNPC).Parent == npc)
						p.Kill();
				}

				TagEffects(owner, npc);
				_tagged = false;
				_tagCooldown = MAX_TAG_COOLDOWN;
			}
			else if (!projectile.IsMinionOrSentryRelated && projectile.type is not ProjectileID.Bee or ProjectileID.GiantBee && projectile.GetGlyph().ItemType == ModContent.ItemType<BeeGlyph>())
			{
				HitEffects(npc);
				if (!_tagged)
					_tagged = true;

				_decayTimer = 600;
			}
		}

		private static void HitEffects(NPC target)
		{
			var position = target.Center + Main.rand.NextVector2Circular(target.width / 2, target.height / 2);

			for (int i = 0; i < 3; i++)
			{
				Dust.NewDustPerfect(position, DustID.Honey2, Main.rand.NextVector2Circular(2f, 2f), 50, default, 1.2f).noGravity = true;

				Vector2 pos = position + Main.rand.NextVector2CircularEdge(9f, 9f);

				ParticleHandler.SpawnParticle(new SharpStarParticle(pos, Vector2.Zero, Color.Orange, 0.2f, 20, 0));
				ParticleHandler.SpawnParticle(new SharpStarParticle(pos, Vector2.Zero, Color.White * 0.5f, 0.1f, 20, 0));
			}

			if (Main.rand.NextBool(5))
				ParticleHandler.SpawnParticle(new BeeOnNPC(target, Main.rand.NextVector2Circular(25f, 25f)));
		}

		private static void TagEffects(Player player, NPC target)
		{
			SoundEngine.PlaySound(SoundID.Item97 with { Volume = 1f, PitchVariance = 0.25f }, target.Center);

			for (int i = 0; i < 7; i++)
			{
				Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(target.width / 2, target.height / 2), DustID.Bee, Main.rand.NextVector2Circular(5f, 5f), 50, default, 1.2f).noGravity = true;
				
				Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(target.width / 2, target.height / 2), DustID.Honey, Main.rand.NextVector2Circular(5f, 5f), 50, default, 1.2f).noGravity = true;

				ParticleHandler.SpawnParticle(new StickyHoneyParticle(target.Center + Main.rand.NextVector2Circular(5f, 5f), Main.rand.NextVector2Circular(5f, 5f), 1f, 90, 0.15f));

				ParticleHandler.SpawnParticle(new StickyHoneyParticle(target.Center + Main.rand.NextVector2Circular(5f, 5f), Main.rand.NextVector2Circular(8f, 8f), 1f, 30, 0.15f));
			}

			int type = player.hornet ? ProjectileID.GiantBee : ProjectileID.Bee;

			for (int i = 0; i < 3; i++)
				Projectile.NewProjectile(target.GetSource_OnHurt(player), target.Center, Main.rand.NextVector2Unit(), type, 10, 0, player.whoAmI); // Make into a tag bonus
		}
	}

	public override void DrawInWorld(Item item, SpriteBatch spriteBatch, ItemMethods.ItemDrawParams parameters)
	{
		Main.GetItemDrawFrame(item.type, out Texture2D texture, out Rectangle frame);
		Vector2 position = item.Bottom - new Vector2(0, frame.Height / 2) - Main.screenPosition;
		Vector2 origin = frame.Size() / 2;

		Texture2D whiteTexture = TextureColorCache.ColorSolid(texture, Color.White);
		Effect effect = AssetLoader.LoadedShaders["LiquidGlyphShader"].Value;

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.005f));
		var c1 = Color.Lerp(new Color(255, 182, 0), new Color(254, 210, 37), sin);
		var c2 = new Color(211, 113, 11);

		effect.Parameters["uColor1"].SetValue(c1.ToVector4() * 0.5f);
		effect.Parameters["uColor2"].SetValue(c2.ToVector4() * 0.5f);
		effect.Parameters["uImage1"].SetValue(AssetLoader.LoadedTextures["noise"].Value);
		effect.Parameters["uImage2"].SetValue(AssetLoader.LoadedTextures["swirlNoise"].Value);

		effect.Parameters["uPixelRes"].SetValue(texture.Size().X / 2);

		effect.Parameters["uTime"].SetValue((float)Main.timeForVisualEffects * 0.005f);
		effect.Parameters["uStrength"].SetValue(0.2f);
		
		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			spriteBatch.Draw(whiteTexture, position + offset, frame, new Color(254, 210, 37), parameters.Rotation, origin, parameters.Scale, 0, 0);

			offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;
			spriteBatch.Draw(whiteTexture, position + offset, frame, new Color(211, 113, 11) * 0.3f, parameters.Rotation, origin, parameters.Scale, 0, 0);
		}

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			spriteBatch.Draw(whiteTexture, position + offset, frame, Color.White, parameters.Rotation, origin, parameters.Scale, 0, 0);
		}

		spriteBatch.RestartToDefault();

		base.DrawInWorld(item, spriteBatch, parameters);
	}

	public override void UpdateInWorld(Item item, ref float gravity, ref float maxFallSpeed)
	{
		if (Main.rand.NextBool(45))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2CircularEdge(item.width / 3, item.height / 3);
			ParticleHandler.SpawnParticle(new StickyHoneyParticle(pos, Vector2.Zero, Main.rand.NextFloat(0.8f, 1.5f), Main.rand.Next(100, 180), Main.rand.NextFloat(0.03f, 0.08f)));
		}

		if (Main.rand.NextBool(50))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);
			Vector2 velocity = -Vector2.UnitY * Main.rand.NextFloat(-0.5f, 0.5f);

			if (Main.rand.NextBool(3))
			{
				ParticleHandler.SpawnParticle(new LargeBeeParticle(pos, velocity, 0f, 1f, 180));
			}
			else
			{
				ParticleHandler.SpawnParticle(new BeeParticle(pos, velocity, 0f, 1f, 90));
			}
		}
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.Green;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(Color.Goldenrod);
	}

	public override bool CanApplyGlyph(Item item) => base.CanApplyGlyph(item) && !item.DamageType.CountsAsClass(DamageClass.Summon);

	protected override void OnApplyGlyph(Item item, IApplicationContext context)
	{
		item.DamageType = ModContent.GetInstance<HybridDamageClass>().Clone()
			.AddSubClass(new(item.DamageType, 0.8f))
			.AddSubClass(new(DamageClass.Summon, 0.2f));

		base.OnApplyGlyph(item, context);
	}
}