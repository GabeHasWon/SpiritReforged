using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;

namespace SpiritReforged.Content.Glyphs.Blaze;

public class BlazeGlyph : GlyphItem
{
	public const float MAX_CRIT_BONUS = 20f; // the critical strike chance bonus when the player is at 1hp
	public const float MIN_CRIT_BONUS = 5f; // the critical strike chance bonus when the player is at full hp

	public const float MAX_DAMAGE_BONUS = 0.4f; // the damage bonus when the player is at 1hp
	public const float MIN_DAMAGE_BONUS = 0.1f; // the damage bonus when the player is at full hp

	public sealed class BlazePlayer : ModPlayer
	{
		public override void UpdateBadLifeRegen()
		{
			if (Player.HasBuff<BlazeDebuff>() && Player.statLife > 5)
			{
				if (Player.lifeRegen > 0)
					Player.lifeRegen = 0;

				Player.lifeRegen -= 8 + (int)(Player.statLife * 0.075f);
			}
		}

		public override void MeleeEffects(Item item, Rectangle hitbox)
		{
			if (Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<BlazeGlyph>() && Main.rand.NextBool(5))
			{
				var dust = Dust.NewDustDirect(hitbox.TopLeft(), hitbox.Width, hitbox.Height, DustID.Torch);
				dust.noGravity = true;
				dust.fadeIn = 1.1f;
				dust.noLightEmittence = true;
			}
		}

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<BlazeGlyph>())
			{
				if (!Player.HasBuff<BlazeDebuff>())
					BlazeHitEffects(Player.Center, -MathHelper.PiOver2, 1.5f);

				target.AddBuff(BuffID.OnFire, 90);
				Player.AddBuff(ModContent.BuffType<BlazeDebuff>(), 60);

				Vector2 position = target.Hitbox.ClosestPointInRect(Player.Center);
				float rotation = target.DirectionTo(Player.Center).ToRotation();

				BlazeHitEffects(position, rotation, 1f);

				if (Main.netMode != NetmodeID.SinglePlayer)
					MultiplayerLoader.Send(nameof(BlazeHitEffects), -1, -1, position, rotation, 1f);
			}
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (proj.GetGlyph().ItemType == ModContent.ItemType<BlazeGlyph>())
			{
				if (!Player.HasBuff<BlazeDebuff>())
					BlazeHitEffects(Player.Center, -MathHelper.PiOver2, 1.5f);

				target.AddBuff(BuffID.OnFire, 90);
				Player.AddBuff(ModContent.BuffType<BlazeDebuff>(), 60);

				Vector2 position = proj.Center;
				float rotation = proj.DirectionTo(Player.Center).ToRotation();

				BlazeHitEffects(position, rotation, 1f);

				if (Main.netMode != NetmodeID.SinglePlayer)
					MultiplayerLoader.Send(nameof(BlazeHitEffects), -1, -1, position, rotation, 1f);
			}
		}

		[NetSynced(true)]
		public static void BlazeHitEffects(Vector2 position, float angle, float scale = 1f)
		{
			if (Main.dedServ)
				return;

			Color[] colors = [new(255, 200, 0, 100), new(255, 115, 0, 100), new(200, 3, 33, 100)];

			ParticleHandler.SpawnParticle(new SharpStarParticle(position, Vector2.Zero, Color.DarkOrange.Additive(), 0.3f * scale, 30, 0)
			{
				Layer = ParticleLayer.BelowNPC,
				Rotation = angle,
				TimeActive = 5
			});

			ParticleHandler.SpawnParticle(new SharpStarParticle(position, Vector2.Zero, Color.LightYellow.Additive() * 0.2f * scale, 0.25f, 25, 0)
			{
				Layer = ParticleLayer.BelowNPC,
				Rotation = angle,
				TimeActive = 5
			});

			for (int i = 0; i < 5; i++)
			{
				var dust = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(5f, 5f), DustID.Torch, Main.rand.NextVector2Circular(1f, 1f));
				dust.noGravity = !Main.rand.NextBool(5);
				if (dust.noGravity)
					dust.scale = 0.5f;
				else
					dust.fadeIn = 1.1f;
				dust.noLightEmittence = true;

				EmberParticle particle = new(position, Main.rand.NextVector2Circular(1f, 1f), Color.Orange, Main.rand.Next(colors), Main.rand.NextFloat(0.3f), 40, 5);
				particle.OverrideDrawLayer(ParticleLayer.BelowNPC);
				ParticleHandler.SpawnParticle(particle);

				particle = new EmberParticle(position, angle.ToRotationVector2().RotatedByRandom(0.5f) * Main.rand.NextFloat(3f), Color.Orange, Main.rand.Next(colors), Main.rand.NextFloat(0.3f), 40, 5);
				particle.OverrideDrawLayer(ParticleLayer.BelowNPC);
				ParticleHandler.SpawnParticle(particle);

				if (Main.rand.NextBool(3))
				{
					if (i == 0)
						SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Projectile/ElectricZap") with { Volume = 0.15f, PitchVariance = 0.15f }, position);

					ParticleHandler.SpawnParticle(new SmokeCloud(position, angle.ToRotationVector2().RotatedByRandom(0.5f) * Main.rand.NextFloat(1.5f), new Color(50, 50, 50, 155) * 0.15f, 0.15f * scale, EaseFunction.EaseQuadOut, 60, false)
					{ Layer = ParticleLayer.BelowNPC });

					ParticleHandler.SpawnParticle(new SmokeCloud(position, angle.ToRotationVector2().RotatedByRandom(0.5f) * Main.rand.NextFloat(1.5f), new Color(50, 50, 50, 155) * 0.2f, 0.1f * scale, EaseFunction.EaseQuadOut, 60, false)
					{ Layer = ParticleLayer.BelowNPC });
				}

				ParticleHandler.SpawnParticle(new FireParticle(position, angle.ToRotationVector2().RotatedByRandom(0.5f) * Main.rand.NextFloat(3f), colors, 1, Main.rand.NextFloat(0.05f, 0.125f) * scale, EaseFunction.EaseQuadOut, 40)
				{ Layer = ParticleLayer.BelowNPC });
			}
		}
	}

	public sealed class BlazeDebuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.debuff[Type] = true;
			Main.pvpBuff[Type] = true;
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;

			BuffID.Sets.LongerExpertDebuff[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			if (Main.dedServ)
				return;

			Color[] colors = [new(255, 200, 0, 100), new(255, 115, 0, 100), new(200, 3, 33, 100)];

			if (Main.rand.NextBool())
			{
				EmberParticle particle = new(player.Center + Main.rand.NextVector2Circular(player.width / 2, player.height / 2), -Vector2.UnitY * Main.rand.NextFloat(0.5f, 2f), Color.Orange, Main.rand.Next(colors), Main.rand.NextFloat(0.3f), 40, 5);
				particle.OverrideDrawLayer(ParticleLayer.BelowNPC);

				ParticleHandler.SpawnParticle(particle);
			}

			if (Main.rand.NextBool(6))
				ParticleHandler.SpawnParticle(new FireParticle(player.Center + Main.rand.NextVector2Circular(player.width / 2, player.height / 2), -Vector2.UnitY * Main.rand.NextFloat(0.5f, 2f), colors, 1, Main.rand.NextFloat(0.09f, 0.17f), EaseFunction.EaseQuadOut, 40)
				{ Layer = ParticleLayer.BelowNPC });

			if (Main.rand.NextBool(4))
				Dust.NewDustPerfect(player.Center + Main.rand.NextVector2Circular(player.width, player.height), DustID.Torch, -Vector2.UnitY * Main.rand.NextFloat(0.5f, 2f), 50, default, 2.5f).noGravity = true;
		}
	}

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		//Because of how Terraria is programmed, we have to bind one shader to one item id
		//Bound shaders for drawdata can't have their parameters dynamically adjusted when applied, to my knowledge
		//Therefore, we need to bind the same shader twice to two different item ids, requiring the use of a dummy id
		if (!Main.dedServ)
		{
			GameShaders.Armor.BindShader(ModContent.ItemType<ChromaticWax>(), new BlazeGlyphShaderData(AssetLoader.LoadedShaders["BlazeGlyphShader"], "mainPass", new(0.15f, 0.2f), false));
			GameShaders.Armor.BindShader(Type, new BlazeGlyphShaderData(AssetLoader.LoadedShaders["BlazeGlyphShader"], "mainPass", new(0.4f, 0.4f), true));
		}			
	}

	public override void SetDefaults()
	{
		Item.height = Item.width = 28;
		Item.rare = ItemRarityID.Pink;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(new(233, 143, 26));
	}

	protected override void OnApplyGlyph(Item item, IApplicationContext context)
	{
		MoRHelper.OverrideElement(item, MoRHelper.Fire);

		base.OnApplyGlyph(item, context);
	}

	protected override void OnRemoveGlyph(Item item, IApplicationContext context) => MoRHelper.OverrideElement(item, MoRHelper.Fire, -1);

	public override void DrawHeldItem(ref PlayerDrawSet drawInfo, DrawData input)
	{
		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 8f) * 4;
			DrawData item = input;
			item.position += offset;
			item.shader = GameShaders.Armor.GetShaderIdFromItemId(ModContent.ItemType<ChromaticWax>());

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

	public override void DrawInWorld(Item item, SpriteBatch spriteBatch, ItemMethods.ItemDrawParams parameters)
	{
		Texture2D whiteTexture = TextureColorCache.ColorSolid(parameters.Texture, Color.White);
		Effect effect = AssetLoader.LoadedShaders["BlazeGlyphShader"].Value;

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.005f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.0075f));

		var c1 = Color.Lerp(Color.Yellow, Color.DarkOrange, sin);
		var c2 = Color.Lerp(Color.Red, Color.OrangeRed, cos);

		effect.Parameters["uColor1"].SetValue(c1.ToVector4() * 0.15f);
		effect.Parameters["uColor2"].SetValue(c2.ToVector4() * 0.2f);

		effect.Parameters["uImage1"].SetValue(AssetLoader.LoadedTextures["noise"].Value);
		effect.Parameters["uImage2"].SetValue(AssetLoader.LoadedTextures["swirlNoise"].Value);
		effect.Parameters["uTime"].SetValue((float)Main.timeForVisualEffects * 0.0015f);
		effect.Parameters["uPixelRes"].SetValue(parameters.Source.Height);
		effect.Parameters["uStrength"].SetValue(MathHelper.Lerp(0.03f, 0.06f, Math.Abs((float)Math.Sin(Main.GlobalTimeWrappedHourly / 2))));

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 8f) * 4;
			spriteBatch.Draw(whiteTexture, parameters.Position + offset, parameters.Source, Color.White, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
		}

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

		effect.Parameters["uColor1"].SetValue(c1.Additive().ToVector4() * 0.4f);
		effect.Parameters["uColor2"].SetValue(c2.Additive().ToVector4() * 0.4f);

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			spriteBatch.Draw(whiteTexture, parameters.Position + offset, parameters.Source, Color.White, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
		}

		spriteBatch.RestartToDefault();

		base.DrawInWorld(item, spriteBatch, parameters);
	}

	public override void UpdateInWorld(Item item, ref float gravity, ref float maxFallSpeed)
	{
		if (Main.dedServ)
			return;

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.005f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.0075f));

		Color c1, c2;
		c1 = Color.Lerp(Color.Yellow, Color.DarkOrange, sin);
		c2 = Color.Lerp(Color.Red, Color.OrangeRed, cos);

		Lighting.AddLight(item.Center, Color.Lerp(c1, c2, sin).ToVector3() / 2);

		Color[] emberColors = {
			Color.Orange,
			Color.DarkOrange,
			Color.OrangeRed,
			Color.Goldenrod,
		};

		if (Main.rand.NextBool(60))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);
			Vector2 velocity = Vector2.Zero;

			var particle = new EmberParticle(pos, velocity, Color.Orange, Main.rand.Next(emberColors), 0.2f, 40);
			particle.OverrideDrawLayer(ParticleLayer.AboveItem);
			ParticleHandler.SpawnParticle(particle);
		}

		if (Main.rand.NextBool(15))
		{
			Vector2 pos = item.Center + new Vector2(Main.rand.Next(-item.width / 4, item.width / 4), -Main.rand.Next(item.height / 4));

			ParticleHandler.SpawnParticle(new SmokeCloud(pos, -Vector2.UnitY * Main.rand.NextFloat(2f), new Color(15, 15, 15, 255) * 0.25f, 0.07f, EaseFunction.EaseQuadOut, 60, false));
			ParticleHandler.SpawnParticle(new SmokeCloud(pos, -Vector2.UnitY * Main.rand.NextFloat(2f), new Color(15, 15, 15, 255) * 0.5f, 0.05f, EaseFunction.EaseQuadOut, 60, false));

			Color[] colors = [new(255, 200, 0, 100), new(255, 115, 0, 100), new(200, 3, 33, 100)];
			ParticleHandler.SpawnParticle(new FireParticle(pos, -Vector2.UnitY * Main.rand.NextFloat(0.5f), colors, 1, Main.rand.NextFloat(0.05f, 0.125f), EaseFunction.EaseQuadOut, 40)
			{
				Layer = ParticleLayer.BelowSolid
			});
		}

		if (Main.rand.NextBool(60))
		{
			Vector2 pos = item.Center + new Vector2(Main.rand.Next(-item.width / 4, item.width / 4), -Main.rand.Next(item.height / 4));

			Vector2 velocity = -Vector2.UnitY * Main.rand.NextFloat(1.25f, 1.5f);

			var particle = new EmberParticle(pos, velocity, Color.Orange, Main.rand.Next(emberColors), Main.rand.NextFloat(0.3f), 60, 5);
			particle.OverrideDrawLayer(ParticleLayer.BelowProjectile);
			ParticleHandler.SpawnParticle(particle);
		}
	}

	public override void ModifyGlyphedItemCrit(Player player, ref float crit) => crit += MathHelper.Lerp(MIN_CRIT_BONUS, MAX_CRIT_BONUS, 1f - player.statLife / (float)player.statLifeMax2);
	public override void ModifyGlyphedItemDamage(Player player, ref StatModifier damage) => damage += MathHelper.Lerp(MIN_DAMAGE_BONUS, MAX_DAMAGE_BONUS, 1f - player.statLife / (float)player.statLifeMax2);
	public override void GlyphShootEffects(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (Main.dedServ)
			return;

		Vector2 normalized = velocity.SafeNormalize(Vector2.One);

		for (int i = 0; i < 3; i++)
		{
			Dust.NewDustPerfect(position + normalized * item.width, DustID.Torch, normalized.RotatedByRandom(0.4f) * Main.rand.NextFloat(5f), 70, default, 1.2f).noGravity = true;
			ParticleHandler.SpawnParticle(new CurvingEmberParticle(position + normalized * item.width, normalized.RotatedByRandom(0.4f) * Main.rand.NextFloat(1.2f), Color.DarkOrange, 0.05f, 40, -Math.Sign(velocity.X), 20));
		}
	}

	public override void UpdateGlyphProjectile(Projectile projectile)
	{
		if (!Main.dedServ && Main.rand.NextBool(3 + 1 * projectile.extraUpdates))
			Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(projectile.width / 2, projectile.height / 2), DustID.Torch, -projectile.velocity.SafeNormalize(Main.rand.NextVector2Circular(1f, 1f)).RotatedByRandom(0.2f) * Main.rand.NextFloat(4f), 0, default, Main.rand.NextFloat(0.9f, 1.5f)).noGravity = true;
	}
}

public class BlazeGlyphShaderData(Asset<Effect> shader, string shaderPass, Vector2 colorMod, bool additive) : ArmorShaderData(shader, shaderPass)
{
	private Effect GetEffect => shader.Value;

	public override void Apply(Entity entity, DrawData? drawData = null)
	{
		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.005f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.0075f));

		Color c1, c2;
		c1 = Color.Lerp(Color.Yellow, Color.DarkOrange, sin);
		c2 = Color.Lerp(Color.Red, Color.OrangeRed, cos);
		if (additive)
		{
			c1 = c1.Additive();
			c2 = c2.Additive();
		}

		GetEffect.Parameters["uColor1"].SetValue(c1.ToVector4() * colorMod.X);
		GetEffect.Parameters["uColor2"].SetValue(c2.ToVector4() * colorMod.Y);

		var noise = AssetLoader.LoadedTextures["swirlNoise2"].Value;
		var noise2 = AssetLoader.LoadedTextures["swirlNoise"].Value;

		GetEffect.Parameters["uImage1"].SetValue(noise);
		GetEffect.Parameters["uImage2"].SetValue(noise2);
		GetEffect.Parameters["uTime"].SetValue((float)Main.timeForVisualEffects * 0.0015f);

		//Shouldn't ever actually be null but just in case
		float uPixelRes = drawData == null ? 1 : drawData.Value.texture.Size().X;
		GetEffect.Parameters["uPixelRes"].SetValue(uPixelRes);

		GetEffect.Parameters["uStrength"].SetValue(MathHelper.Lerp(0.03f, 0.06f, Math.Abs((float)Math.Sin(Main.GlobalTimeWrappedHourly / 2))));

		Apply();
	}
}