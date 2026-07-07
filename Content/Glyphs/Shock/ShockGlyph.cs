using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.Graphics.Renderers;
using System.Linq;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Multiplayer;
using System.IO;
using SpiritReforged.Common.CombatTextCommon;
using System.Runtime.CompilerServices;
using static System.Net.Mime.MediaTypeNames;
using Terraria.ModLoader.IO;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Microsoft.Xna.Framework.Graphics;
using SpiritReforged.Content.Glyphs.Dazzling;
using static SpiritReforged.Content.Glyphs.Void.VoidGlyph;
using SpiritReforged.Content.Dusts;

namespace SpiritReforged.Content.Glyphs.Shock;

public class ShockGlyph : GlyphItem
{
	public sealed class ShockPlayer : ModPlayer
	{
		private class ShockPacket : PacketData
		{
			private readonly short _player;
			private readonly short _npc;
			private readonly int _damage;

			public ShockPacket() : base() { }

			public ShockPacket(short npc, short player, int damage)
			{
				_npc = npc;
				_player = player;
				_damage = damage;
			}

			public override void OnReceive(BinaryReader reader, int whoAmI)
			{
				short npc = reader.ReadInt16();
				short player = reader.ReadInt16();
				int damage = reader.ReadInt32();

				ChannelLightning(Main.player[player], Main.npc[npc], damage);
			}

			public override void OnSend(ModPacket modPacket)
			{
				modPacket.Write(_npc);
				modPacket.Write(_player);
				modPacket.Write(_damage);
			}
		}

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (hit.Crit && item.GetGlyph().ItemType == ModContent.ItemType<ShockGlyph>() && Main.myPlayer == Player.whoAmI) 
			{
				if (Main.netMode == NetmodeID.MultiplayerClient)
					new ShockPacket((short)target.whoAmI, (short)Player.whoAmI, damageDone).Send(ignoreClient: Player.whoAmI);

				ChannelLightning(Player, target, damageDone);
			}			
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (hit.Crit && proj.GetGlyph().ItemType == ModContent.ItemType<ShockGlyph>() && proj.type != ModContent.ProjectileType<ShockGlyphLightningBolt>() && Main.myPlayer == Player.whoAmI)
			{
				if (Main.netMode == NetmodeID.MultiplayerClient)
					new ShockPacket((short)target.whoAmI, (short)Player.whoAmI, damageDone).Send(ignoreClient: Player.whoAmI);

				ChannelLightning(Player, target, damageDone);
			}
		}

		public static void ChannelLightning(Player Player, NPC target, int damage)
		{
			NPC[] closestNPCs = Main.npc.Where(n => n.whoAmI != target.whoAmI && n.CanBeChasedBy(Player) && n.DistanceSQ(target.Center) < 250000f).OrderBy(n => n.DistanceSQ(target.Center)).Take(3).ToArray();

			if (closestNPCs.Length <= 0)
				return;

			for (int i = 0; i < closestNPCs.Length; i++)
			{
				Projectile.NewProjectile(Player.GetSource_OnHit(target), target.Center, Vector2.Zero,
					ModContent.ProjectileType<ShockGlyphLightningBolt>(), 5 + (int)(damage * 0.35f), 1f, Player.whoAmI, closestNPCs[i].whoAmI, ai2: i == 0 ? 1 : 0);
			}
		}
	}

	public class ShockGlyphLightningBolt : ModProjectile, LightningSystem.ILightningProjectile
	{
		public override string Texture => AssetLoader.EmptyTexture;

		public int TargetWhoAmI => (int)Projectile.ai[0];

		public int Delay
		{
			get => (int)Projectile.ai[1];
			set => Projectile.ai[1] = value;
		}

		public bool Initialized = false;

		public float Progress => 1f - Projectile.timeLeft / 40f;

		public bool Invalid { get; set; }
		public bool Dying;

		public Vector2 startPos;

		private VertexTrail[] _trails;

		public override void SetDefaults()
		{
			Projectile.Size = new Vector2(64);

			Projectile.DamageType = DamageClass.Generic;

			Projectile.hostile = false;
			Projectile.friendly = true;

			Projectile.tileCollide = false;

			Projectile.timeLeft = 40;
			Projectile.extraUpdates = 5;

			Projectile.penetrate = 1;
			Projectile.stopsDealingDamageAfterPenetrateHits = true;

			// TODO: Balance Adjustments here
			Projectile.ArmorPenetration = Main.hardMode ? 20 : 10;
		}

		public override bool? CanHitNPC(NPC target) => target.whoAmI == TargetWhoAmI;

		public override void OnKill(int timeLeft) 
		{
			Invalid = true;
			LightningSystem.projectiles.Remove(this);
		}
		

		public override void AI()
		{
			if (Delay > 0)
			{
				Delay--;
				Projectile.timeLeft = 40;
			}

			if (!Initialized)
			{
				if (Projectile.ai[2] == 1 && !Main.dedServ)
				{
					SoundEngine.PlaySound(ElectricSting, Projectile.Center);
					SoundEngine.PlaySound(ElectricZap, Projectile.Center);

					ScreenshakeHelper.Shake(Projectile.Center, Main.rand.NextVector2Circular(1f, 1f), 1, 4, 10);

					for (int i = 0; i < 3; i++)
					{
						ParticleHandler.SpawnParticle(new LightningBoltParticle(Projectile.Center + Main.rand.NextVector2Circular(2f, 2f), Main.rand.NextVector2CircularEdge(4f, 4f) * Main.rand.NextFloat(0.5f, 1.1f),
							Color.Yellow, Color.Cyan, 0f, Main.rand.NextFloat(0.4f, 0.9f), 10 + Main.rand.Next(10, 30)));

						ParticleHandler.SpawnParticle(new LightningBoltParticle(Projectile.Center + Main.rand.NextVector2Circular(2f, 2f), Main.rand.NextVector2CircularEdge(5f, 5f) * Main.rand.NextFloat(0.5f, 1.1f),
							Color.Yellow, Color.LightGoldenrodYellow, 0f, Main.rand.NextFloat(0.4f, 0.9f), 10 + Main.rand.Next(10, 60)));

						Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(5f, 5f);
						Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);

						ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.Yellow.Additive(), 0.6f, 40, extraUpdateAction: DecelerateAction));
						ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.White.Additive(), 0.45f, 40, extraUpdateAction: DecelerateAction));

						pos = Projectile.Center + Main.rand.NextVector2Circular(5f, 5f);
						velocity = Main.rand.NextVector2Circular(4f, 4f);

						ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.Cyan.Additive(), 0.6f, 40, extraUpdateAction: DecelerateAction));
						ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.White.Additive(), 0.45f, 40, extraUpdateAction: DecelerateAction));
					}

					for (int i = 0; i < 5; i++)
					{
						Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<YellowElectricDust>(), Main.rand.NextVector2CircularEdge(7f, 7f) * Main.rand.NextFloat(0.9f, 1.1f), 0, default, 0.65f).noGravity = true;

						Dust.NewDustPerfect(Projectile.Center, DustID.Electric, Main.rand.NextVector2CircularEdge(5f, 5f) * Main.rand.NextFloat(0.9f, 1.1f), 0, default, 0.65f).noGravity = true;
					}

					static void DecelerateAction(Particle p) => p.Velocity *= 0.9f;
				}

				LightningSystem.projectiles.Add(this);

				startPos = Projectile.Center;
				Projectile.netUpdate = true;

				Delay = 10 * Main.rand.Next(7);

				if (!Main.dedServ && _trails == null)
					CreateTrail();

				Initialized = true;
			}

			if (!Main.dedServ && _trails is not null)
			{
				foreach (VertexTrail trail in _trails)
					trail.Update();
			}

			Color color = Color.Yellow * 0.66f;

			float progress = EaseFunction.EaseCircularInOut.Ease(Progress);

			if (Dying)
				progress = Projectile.timeLeft / 200f;

			Lighting.AddLight(Projectile.Center, color.R / 255f * progress, color.G / 255f * progress, color.B / 255f * progress);

			if (!Dying && !Main.dedServ)
			{
				if (Progress > 0.25f)
				{
					if (Main.rand.NextBool(25))
					{
						Vector2 vel = Projectile.DirectionTo(Main.npc[TargetWhoAmI].Center).RotatedByRandom(0.3f) * Main.rand.NextFloat(5f);
						Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(2f, 2f);
						ParticleHandler.SpawnParticle(new LightningBoltParticle(pos, vel, Color.Yellow, Color.Cyan, 0f, Main.rand.NextFloat(0.4f, 0.9f), 20 + Main.rand.Next(30, 60)));
					}

					if (Main.rand.NextBool(25))
					{
						Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(2f, 2f);
						Vector2 vel = Projectile.DirectionTo(Main.npc[TargetWhoAmI].Center).RotatedByRandom(0.3f) * Main.rand.NextFloat(4f, 5f);
						ParticleHandler.SpawnParticle(new LightningBoltParticle(pos, vel, Color.Yellow, Color.LightGoldenrodYellow, 0f, Main.rand.NextFloat(0.4f, 0.9f), 20 + Main.rand.Next(30, 60)));
					}
				}

				Projectile.Center = Vector2.Lerp(startPos, Main.npc[TargetWhoAmI].Center, Progress) + Main.rand.NextVector2CircularEdge(11f, 11f) * MathHelper.Lerp(0.4f, 1f, 1f - Progress);
			}

			if (Projectile.timeLeft == 1 && !Dying && Main.myPlayer == Projectile.owner)
			{
				Dying = true;
				Projectile.netUpdate = true;

				Projectile.timeLeft = 200;
				Projectile.Center = Main.npc[TargetWhoAmI].Center + Main.npc[TargetWhoAmI].velocity;
			}
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.HideCombatText();

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			int idx = CombatText.NewText(target.getRect(), Color.White, Math.Max(damageDone, 1), hit.Crit);
			ColoredCombatText.AddCombatText(idx, Color.Cyan, Color.DarkCyan);
			
			if (Main.dedServ)
				return;

			for (int i = 0; i < 2; i++)
			{
				ParticleHandler.SpawnParticle(new LightningBoltParticle(target.Center + Main.rand.NextVector2Circular(2f, 2f), Main.rand.NextVector2CircularEdge(4f, 4f) * Main.rand.NextFloat(0.5f, 1.1f),
					Color.Yellow, Color.Cyan, 0f, Main.rand.NextFloat(0.4f, 0.9f), 10 + Main.rand.Next(10, 30)));

				ParticleHandler.SpawnParticle(new LightningBoltParticle(target.Center + Main.rand.NextVector2Circular(2f, 2f), Main.rand.NextVector2CircularEdge(5f, 5f) * Main.rand.NextFloat(0.5f, 1.1f),
					Color.Yellow, Color.LightGoldenrodYellow, 0f, Main.rand.NextFloat(0.4f, 0.9f), 10 + Main.rand.Next(10, 60)));

				Vector2 pos = target.Center + Main.rand.NextVector2Circular(5f, 5f);
				Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);

				ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.Yellow.Additive(), 0.6f, 40, extraUpdateAction: DecelerateAction));
				ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.White.Additive(), 0.45f, 40, extraUpdateAction: DecelerateAction));

				pos = target.Center + Main.rand.NextVector2Circular(5f, 5f);
				velocity = Main.rand.NextVector2Circular(4f, 4f);

				ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.Cyan.Additive(), 0.6f, 40, extraUpdateAction: DecelerateAction));
				ParticleHandler.SpawnParticle(new GlowParticle(pos, velocity, Color.White.Additive(), 0.45f, 40, extraUpdateAction: DecelerateAction));
			}

			static void DecelerateAction(Particle p) => p.Velocity *= 0.9f;
		}

		private void CreateTrail()
		{
			ITrailCap tCap = new RoundCap();
			ITrailPosition tPos = new EntityTrailPosition(Projectile);
			ITrailShader tShader = new ImageShader(AssetLoader.LoadedTextures["GlowTrail"].Value, Vector2.One);

			_trails =
			[
				new VertexTrail(new GradientTrail(new Color(255, 240, 65, 0), new Color(0, 255, 255, 0), EaseFunction.EaseQuarticInOut), tCap, tPos, tShader, 30, 360, 1),
				new VertexTrail(new GradientTrail(Color.White.Additive(), Color.Transparent, EaseFunction.EaseQuarticOut), tCap, tPos, tShader, 25, 360, 1),
			];
		}

		public override bool PreDraw(ref Color lightColor)
		{
			var tex = AssetLoader.LoadedTextures["Bloom"].Value;

			float progress = EaseFunction.EaseCircularInOut.Ease(Progress);

			if (Dying)
				progress = Projectile.timeLeft / 200f;

			Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.Yellow with { A = 0 } * 0.1f * progress, 0, tex.Size() / 2, 0.3f, SpriteEffects.None, 0);
			Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.Cyan with { A = 0 } * 0.09f * progress, 0, tex.Size() / 2, 0.25f, SpriteEffects.None, 0);

			Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.Yellow with { A = 0 } * 0.5f * progress, 0, tex.Size() / 2, 0.15f, SpriteEffects.None, 0);
			Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.Cyan with { A = 0 } * 0.4f * progress, 0, tex.Size() / 2, 0.1f, SpriteEffects.None, 0);

			Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.LightCyan with { A = 0 } * 0.4f * progress, 0, tex.Size() / 2, 0.1f, SpriteEffects.None, 0);

			return false;
		}

		public void LightningDraw(SpriteBatch spriteBatch)
		{
			if (_trails != null)
				foreach (VertexTrail trail in _trails)
				{
					trail.Opacity = EaseFunction.EaseCircularInOut.Ease(Progress);
					if (Dying)
						trail.Opacity = Projectile.timeLeft / 200f;

					trail?.Draw(TrailSystem.TrailShaders, AssetLoader.BasicShaderEffect, spriteBatch.GraphicsDevice);
				}
		}

		public override void SendExtraAI(BinaryWriter writer) => writer.Write(Dying);
		public override void ReceiveExtraAI(BinaryReader reader) => Dying = reader.ReadBoolean();
	}

	public sealed class ShockGlobalItem : GlobalItem
	{
		public override bool InstancePerEntity => true;

		public int shockTimer;

		public override void Update(Item item, ref float gravity, ref float maxFallSpeed)
		{
			if (shockTimer > 0)
				shockTimer--;
		}
	}

	public static readonly SoundStyle ElectricSting = new("SpiritReforged/Assets/SFX/Projectile/ElectricSting")
	{
		Volume = 1.5f
	};

	public static readonly SoundStyle ElectricZap = new("SpiritReforged/Assets/SFX/Projectile/ElectricZap")
	{
		Volume = 0.5f
	};

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		if (!Main.dedServ)
			GameShaders.Armor.BindShader(Type, new ShockGlyphShaderData(AssetLoader.LoadedShaders["GlyphShader"], "mainPass"));
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.Green;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(Color.Yellow);
	}

	public override void DrawInWorld(Item item, SpriteBatch spriteBatch, ItemMethods.ItemDrawParams parameters)
	{
		Texture2D whiteTexture = TextureColorCache.ColorSolid(parameters.Texture, Color.White);
		Effect effect = AssetLoader.LoadedShaders["GlyphShader"].Value;

		effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects * 0.0025f);
		effect.Parameters["screenPos"].SetValue(Main.screenPosition * new Vector2(0.5f, 0.1f) / new Vector2(Main.screenWidth, Main.screenHeight));
		effect.Parameters["intensity"].SetValue(MathHelper.Lerp(0.03f, 0.3f, (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.02f))));

		effect.Parameters["uImage1"].SetValue(AssetLoader.LoadedTextures["swirlNoise2"].Value);
		effect.Parameters["uImage2"].SetValue(AssetLoader.LoadedTextures["ElectricNoise"].Value);
		effect.Parameters["itemSize"].SetValue(parameters.Texture.Size() / 2);

		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.03f));

		effect.Parameters["uColor1"].SetValue(Color.Cyan.ToVector4() * 0.5f);
		effect.Parameters["uColor2"].SetValue(Color.Lerp(Color.LightYellow, Color.CornflowerBlue, cos).ToVector4() * 0.5f);
		effect.Parameters["uColor3"].SetValue(Color.Yellow.Additive().ToVector4());

		effect.Parameters["baseDepth"].SetValue(4f);
		effect.Parameters["scale"].SetValue(1f);

		var globalItem = item.GetGlobalItem<ShockGlobalItem>();

		Vector2 pos = parameters.Position;
		if (globalItem.shockTimer > 0)
			pos += Main.rand.NextVector2CircularEdge(1.5f, 1.5f) * globalItem.shockTimer / 40f;

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			spriteBatch.Draw(whiteTexture, pos + offset, parameters.Source, Color.CornflowerBlue.Additive() * 0.05f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);

			offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;
			spriteBatch.Draw(whiteTexture, pos + offset, parameters.Source, Color.Cyan.Additive() * 0.05f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
		}

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			spriteBatch.Draw(whiteTexture, pos + offset, parameters.Source, Color.White, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
		}

		spriteBatch.RestartToDefault();

		base.DrawInWorld(item, spriteBatch, parameters);
	}

	public override void DrawHeldItem(ref PlayerDrawSet drawInfo, DrawData input)
	{
		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;
			DrawData item = input;
			item.position += offset;
			item.color = Color.CornflowerBlue.Additive() * 0.1f;
			drawInfo.DrawDataCache.Add(item);
		}

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			DrawData item = input;
			item.position += offset;
			item.shader = GameShaders.Armor.GetShaderIdFromItemId(Type);
			drawInfo.DrawDataCache.Add(item);
		}
	}

	// Summon weapons cannot crit
	// Zealous is a crit-chance only reforge so can be used to check if a weapon can crit (I think)
	public override bool CanApplyGlyph(Item item) 
	{
		// We need to check the sample item because if an item has a glyph applied no prefixes can be applied, thus wrongly returning false here
		Item sampleItem = ContentSamples.ItemsByType[item.type];

		bool prefix = sampleItem.CanApplyPrefix(PrefixID.Zealous);

		return base.CanApplyGlyph(item) && !item.CountsAsClass(DamageClass.Summon) && !item.CountsAsClass(DamageClass.SummonMeleeSpeed) && prefix;
	}
	

	public override void UpdateInWorld(Item item, ref float gravity, ref float maxFallSpeed)
	{
		if (Main.dedServ)
			return;

		ShockGlobalItem globalItem = item.GetGlobalItem<ShockGlobalItem>();

		if (Main.rand.NextBool(120) && globalItem.shockTimer <= 0)
		{
			SoundEngine.PlaySound(ElectricZap with { Volume = 0.3f }, item.Center);

			globalItem.shockTimer = 40;
			for (int i = 0; i < 5; i++)
			{
				Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);
				ParticleHandler.SpawnParticle(new LightningBoltParticle(pos, Main.rand.NextVector2CircularEdge(4f, 4f) * Main.rand.NextFloat(0.5f, 1.1f), Color.Yellow, Color.Cyan, 0f, Main.rand.NextFloat(0.4f, 0.9f), 20 + Main.rand.Next(20, 50)));
			}
		}

		if (Main.rand.NextBool(50))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);
			ParticleHandler.SpawnParticle(new LightningBoltParticle(pos, Main.rand.NextVector2CircularEdge(4f, 4f) * Main.rand.NextFloat(0.5f, 1.1f), Color.Yellow, Color.Cyan, 0f, Main.rand.NextFloat(0.4f, 0.9f), 20 + Main.rand.Next(20, 50)));
		}
	}

	public override void GlyphShootEffects(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		Vector2 normalized = velocity.SafeNormalize(Vector2.One);

		for (int i = 0; i < 3; i++)
		{
			Dust.NewDustPerfect(position + normalized * item.width, Main.rand.NextBool() ? DustID.Electric : ModContent.DustType<YellowElectricDust>(), normalized.RotatedByRandom(0.4f) * Main.rand.NextFloat(9f), 0, default, 0.5f).noGravity = true;
		}
	}

	public override void UpdateGlyphProjectile(Projectile projectile)
	{
		if (Main.rand.NextBool(25 + 20 * projectile.extraUpdates))
			ParticleHandler.SpawnParticle(new LightningBoltParticle(projectile.Center, projectile.velocity * 0.4f, Color.Yellow, Color.Cyan, 0f, Main.rand.NextFloat(0.4f, 0.7f), 20 + Main.rand.Next(10, 30)));

		if (Main.rand.NextBool(12 + 10 * projectile.extraUpdates))
			Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(projectile.width / 2, projectile.height / 2), Main.rand.NextBool() ? DustID.Electric : ModContent.DustType<YellowElectricDust>(), -projectile.velocity.SafeNormalize(Main.rand.NextVector2Circular(1f, 1f)).RotatedByRandom(0.2f) * Main.rand.NextFloat(12f), 0, default, Main.rand.NextFloat(0.4f, 0.6f)).noGravity = true;
	}

	public class ShockGlyphShaderData(Asset<Effect> shader, string shaderPass) : ArmorShaderData(shader, shaderPass)
	{
		private Effect GetEffect => shader.Value;

		public override void Apply(Entity entity, DrawData? drawData = null)
		{
			if (!drawData.HasValue)
				return;

			GetEffect.Parameters["time"].SetValue((float)Main.timeForVisualEffects * 0.0025f);
			GetEffect.Parameters["screenPos"].SetValue(Main.screenPosition * new Vector2(0.5f, 0.1f) / new Vector2(Main.screenWidth, Main.screenHeight));
			GetEffect.Parameters["intensity"].SetValue(MathHelper.Lerp(0.03f, 0.3f, (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.02f))));

			GetEffect.Parameters["uImage1"].SetValue(AssetLoader.LoadedTextures["swirlNoise2"].Value);
			GetEffect.Parameters["uImage2"].SetValue(AssetLoader.LoadedTextures["ElectricNoise"].Value);
			GetEffect.Parameters["itemSize"].SetValue(drawData.Value.texture.Size() / 2);

			float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.03f));

			GetEffect.Parameters["uColor1"].SetValue(Color.Cyan.ToVector4() * 0.5f);
			GetEffect.Parameters["uColor2"].SetValue(Color.Lerp(Color.LightYellow, Color.CornflowerBlue, cos).ToVector4() * 0.5f);
			GetEffect.Parameters["uColor3"].SetValue(Color.Yellow.Additive().ToVector4());

			GetEffect.Parameters["baseDepth"].SetValue(4f);
			GetEffect.Parameters["scale"].SetValue(1f);

			Apply();
		}
	}
}