using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Forest.Glyphs.Dazzling;
using SpiritReforged.Content.Particles;
using System.Linq;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;

namespace SpiritReforged.Content.Forest.Glyphs.Sanguine;

public class SanguineGlyph : GlyphItem
{
	public sealed class SanguineStackingBuff : ModBuff
	{
		public override void SetStaticDefaults() => Main.buffNoSave[Type] = true;

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.GetModPlayer<SanguinePlayer>().stacks.Count > 0)
			{
				// find the stack with the most timer and use that for the time display
				player.buffTime[buffIndex] = player.GetModPlayer<SanguinePlayer>().stacks.OrderBy(s => s.timer).Last().timer;
			}				
			else
			{
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}

		public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
		{
			var stacks = Main.LocalPlayer.GetModPlayer<SanguinePlayer>().stacks;

			int count = stacks.Count;

			buffName = "Sanguine Energy [" + count + "]";

			float damage = 0;
			foreach (SanguineStack stack in stacks)
				damage += stack.damageBonus;

			tip = "Damage increased by " + Math.Round(damage * 100, 2) + "%";

			rare = ItemRarityID.Red;
		}

		public override void PostDraw(SpriteBatch spriteBatch, int buffIndex, BuffDrawParams drawParams)
		{
			var mp = Main.LocalPlayer.GetModPlayer<SanguinePlayer>();

			var stacks = mp.stacks;

			int count = stacks.Count;

			float lerp = mp.lifestealCooldown / 20f;

			Color drawColor = Color.Lerp(Color.White, Color.Red.Additive(), lerp);

			float scale = MathHelper.Lerp(1f, 1.2f, lerp);

			string text = count.ToString();

			Utils.DrawBorderString(spriteBatch, text, drawParams.Position + new Vector2(25, 20), drawColor, scale);
		}
	}

	internal class SanguineStack
	{
		/// <param name="decayTimer">How long the buff stack lasts, in ticks</param>
		/// <param name="damageBonus">How much bonus damage should be added, 0.05: 5% | Bonus is added to 1f</param>
		public SanguineStack(int decayTimer, float damageBonus)
		{
			timer = decayTimer;
			this.damageBonus = damageBonus;
		}

		public int timer;
		public float damageBonus;
	}

	public sealed class SanguinePlayer : ModPlayer
	{
		internal List<SanguineStack> stacks = new();
		internal int lifestealCooldown;

		public override void ResetEffects()
		{
			stacks ??= new();

			foreach (SanguineStack stack in stacks)
			{
				if (stack.timer > 0)
					stack.timer--;
			}

			stacks.RemoveAll(s => s.timer <= 0);

			if (lifestealCooldown > 0)
				lifestealCooldown--;
		}

		public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (item.GetGlyph().ItemType == ModContent.ItemType<SanguineGlyph>())
			{
				float damageBonus = 1f;
				foreach (SanguineStack stack in stacks)
					damageBonus += stack.damageBonus;

				modifiers.FinalDamage *= damageBonus;
			}
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (proj.GetGlyph().ItemType == ModContent.ItemType<SanguineGlyph>())
			{
				float damageBonus = 1f;
				foreach (SanguineStack stack in stacks)
					damageBonus += stack.damageBonus;

				modifiers.FinalDamage *= damageBonus;
			}
		}

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (item.GetGlyph().ItemType == ModContent.ItemType<SanguineGlyph>())
				HitEffects(target, damageDone);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (proj.GetGlyph().ItemType == ModContent.ItemType<SanguineGlyph>())
				HitEffects(target, damageDone);
		}

		internal void HitEffects(NPC target, int damageDone)
		{
			bool leechedLife = false;

			if (Player.statLife < Player.statLifeMax2 && target.canGhostHeal && lifestealCooldown <= 0)
			{
				float amountToHeal = (float)damageDone / 10;

				amountToHeal *= MathHelper.Lerp(1f, 3f, 1f - Player.statLife / (float)Player.statLifeMax2);
				if ((int)amountToHeal < 1)
					amountToHeal = 1;

				if (!Player.HasBuff<SanguineStackingBuff>())
					Player.AddBuff(ModContent.BuffType<SanguineStackingBuff>(), 60);

				if (amountToHeal > 10)
					amountToHeal = 10;

				Player.Heal((int)amountToHeal);
				stacks.Add(new SanguineStack(180, 0.03f + damageDone * 0.001f)); // 3% increase, plus 0.1% of the damage dealt, ex: 3% + (10 * 0.001) = 4% boost

				leechedLife = true;
				lifestealCooldown = 20;
			}

			float angle = Main.rand.NextFloat(MathHelper.Pi);

			Vector2 dir = target.DirectionTo(Player.Center);
			Vector2 position = target.Center + dir * target.width / 2;

			Color c1, c2;
			c1 = Color.DarkRed;
			c2 = new Color(200, 25, 100);

			ParticleHandler.SpawnParticle(new SmokeCloud(position, Main.rand.NextVector2Circular(1.5f, 1.5f), Color.DarkRed * 0.3f, 0.06f, EaseFunction.EaseQuadOut, 30, false)
			{
				Pixellate = true,
				PixelDivisor = 4
			});

			Dust dust = Dust.NewDustPerfect(position, DustID.Blood, Main.rand.NextVector2Circular(1.5f, 1.5f), 70, default, Main.rand.NextFloat(0.6f, 1.2f));
			dust.noGravity = Main.rand.NextBool();
			dust.fadeIn = 2;

			if (Main.rand.NextBool())
				ParticleHandler.SpawnParticle(new StickyBloodParticle(position, Main.rand.NextVector2Circular(1.5f, 1.5f), Main.rand.NextFloat(0.6f, 1.2f), Main.rand.Next(80, 120), 0.2f));

			if (leechedLife)
			{
				SoundEngine.PlaySound(SoundID.NPCHit1 with { Pitch = -0.3f, PitchVariance = 0.1f }, target.Center);

				ParticleHandler.SpawnParticle(new BloodHit(target, dir * target.width / 2, Main.rand.Next(20, 35), dir.ToRotation(), Main.rand.NextFloat(0.9f, 1.1f)));

				for (int i = 0; i < 2; i++)
				{
					dust = Dust.NewDustPerfect(position, DustID.Blood, -Vector2.UnitY * 2f + position.DirectionTo(Player.Center).RotatedByRandom(0.3f) * Main.rand.NextFloat(1f, 6f), 70, default, Main.rand.NextFloat(0.6f, 1.2f));
					dust.noGravity = Main.rand.NextBool();
					dust.fadeIn = 2;

					ParticleHandler.SpawnParticle(new StickyBloodParticle(position, -Vector2.UnitY * 2f + position.DirectionTo(Player.Center).RotatedByRandom(0.3f) * Main.rand.NextFloat(1f, 7f), Main.rand.NextFloat(0.6f, 1.2f), Main.rand.Next(80, 120), 0.1f));

					ParticleHandler.SpawnParticle(new SmokeCloud(position, position.DirectionTo(Player.Center).RotatedByRandom(0.3f) * Main.rand.NextFloat(1f, 3f), Color.DarkRed * 0.5f, 0.09f, EaseFunction.EaseQuadOut, 60, false)
					{
						Pixellate = true,
						PixelDivisor = 3
					});
				}
			}
		}
	}

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		GameShaders.Armor.BindShader(Type, new SanguineGlyphShaderData(AssetLoader.LoadedShaders["GlyphShader"], "mainPass"));
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.Green;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(Color.DarkRed);
	}

	protected override void OnApplyGlyph(Item item, IApplicationContext context)
	{
		item.damage -= (int)Math.Round(item.damage * 0.2f);
		base.OnApplyGlyph(item, context);
	}

	public override void DrawHeldItem(ref PlayerDrawSet drawInfo, DrawData input)
	{
		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			DrawData item = input;
			item.position += offset;
			item.color = Color.DarkRed * 0.5f;
			drawInfo.DrawDataCache.Add(item);

			offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;
			item = input;
			item.position += offset;
			item.color = Color.DarkRed * 0.15f;
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
		Effect effect = AssetLoader.LoadedShaders["GlyphShader"].Value;

		effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects * 0.0025f);
		effect.Parameters["screenPos"].SetValue(Main.screenPosition * new Vector2(0.5f, 0.1f) / new Vector2(Main.screenWidth, Main.screenHeight));
		effect.Parameters["intensity"].SetValue(0.15f * (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.01f)));
		effect.Parameters["uImage1"].SetValue(AssetLoader.LoadedTextures["swirlNoise"].Value);
		effect.Parameters["uImage2"].SetValue(AssetLoader.LoadedTextures["swirlNoise"].Value);
		effect.Parameters["itemSize"].SetValue(parameters.Texture.Size());

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.005f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.0075f));

		effect.Parameters["uColor1"].SetValue(Color.Lerp(Color.DarkRed, Color.Red, sin).ToVector4() * 0.5f);
		effect.Parameters["uColor2"].SetValue(Color.Lerp(Color.Black, new Color(200, 25, 100), cos).ToVector4() * 0.5f);
		effect.Parameters["uColor3"].SetValue(Color.Black.ToVector4());
		effect.Parameters["baseDepth"].SetValue(4f);
		effect.Parameters["scale"].SetValue(0.66f);

		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			spriteBatch.Draw(whiteTexture, parameters.Position + offset, parameters.Source, Color.DarkRed * 0.5f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);

			offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;
			spriteBatch.Draw(whiteTexture, parameters.Position + offset, parameters.Source, Color.DarkRed * 0.15f, parameters.Rotation, parameters.Origin, parameters.Scale, 0, 0);
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

	public override void UpdateInWorld(Item item, ref float gravity, ref float maxFallSpeed)
	{
		if (Main.rand.NextBool(60))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);

			ParticleHandler.SpawnParticle(new SmokeCloud(pos, Vector2.Zero, Color.DarkRed, 0.05f, EaseFunction.EaseQuadOut, 30, false));

			Dust dust = Dust.NewDustPerfect(pos, DustID.Blood, Main.rand.NextVector2Circular(0.5f, 0.5f), 150, default, 1.25f);
			dust.noGravity = true;
			dust.fadeIn = 3;
		}

		if (Main.rand.NextBool(75))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2CircularEdge(item.width / 3, item.height / 3);

			ParticleHandler.SpawnParticle(new StickyBloodParticle(pos, Vector2.Zero, Main.rand.NextFloat(0.6f, 1.2f), Main.rand.Next(80, 120), Main.rand.NextFloat(0.02f, 0.12f)));
		}
	}
}

public class SanguineGlyphShaderData(Asset<Effect> shader, string shaderPass) : ArmorShaderData(shader, shaderPass)
{
	private Effect GetEffect => shader.Value;

	public override void Apply(Entity entity, DrawData? drawData = null)
	{
		if (!drawData.HasValue)
			return;

		GetEffect.Parameters["time"].SetValue((float)Main.timeForVisualEffects * 0.0025f);
		GetEffect.Parameters["screenPos"].SetValue(Main.screenPosition * new Vector2(0.5f, 0.1f) / new Vector2(Main.screenWidth, Main.screenHeight));
		GetEffect.Parameters["intensity"].SetValue(0.15f * (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.01f)));
		GetEffect.Parameters["uImage1"].SetValue(AssetLoader.LoadedTextures["swirlNoise"].Value);
		GetEffect.Parameters["uImage2"].SetValue(AssetLoader.LoadedTextures["swirlNoise"].Value);
		GetEffect.Parameters["itemSize"].SetValue(drawData.Value.texture.Size());

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.005f));
		float cos = (float)Math.Abs(Math.Cos(Main.timeForVisualEffects * 0.0075f));

		GetEffect.Parameters["uColor1"].SetValue(Color.Lerp(Color.DarkRed, Color.Red, sin).ToVector4() * 0.5f);
		GetEffect.Parameters["uColor2"].SetValue(Color.Lerp(Color.Black, new Color(200, 25, 100), cos).ToVector4() * 0.5f);
		GetEffect.Parameters["uColor3"].SetValue(Color.Black.ToVector4());
		GetEffect.Parameters["baseDepth"].SetValue(4f);
		GetEffect.Parameters["scale"].SetValue(0.66f);

		Apply();
	}
}
