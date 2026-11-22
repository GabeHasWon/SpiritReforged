using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;
using Terraria.Graphics.Renderers;
using Terraria.Graphics.Shaders;

namespace SpiritReforged.Content.Glyphs.Bee;

public class BeeGlyph : GlyphItem
{
	public class BeeOnNPC : Particle
	{
		public const int MAX_TIME = 600;

		public NPC Parent => Main.npc[_parentWhoAmI];

		private readonly int _parentWhoAmI;

		private Vector2 _offset;

		private Vector2 PositionToBe => Parent.Center + _offset;

		public BeeOnNPC(NPC npc, Vector2 offset)
		{
			_parentWhoAmI = npc.whoAmI;
			_offset = offset;

			Position = npc.Center + offset;
			MaxTime = MAX_TIME;
			Velocity = Main.rand.NextVector2Circular(0.5f, 0.5f);
		}

		public override void Update(ref ParticleRendererSettings settings)
		{
			base.Update(ref settings);

			if (!Parent.active)
				ShouldBeRemovedFromRenderer = true;

			Position = Vector2.Lerp(Position, PositionToBe, 0.12f);

			if (Main.rand.NextBool(20))
				_offset = Main.rand.NextVector2Circular(Parent.width, Parent.height);

			_offset += Main.rand.NextVector2Circular(1.5f, 1.5f);
		}

		public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spriteBatch)
		{
			const int type = ProjectileID.Bee;

			var texture = TextureAssets.Projectile[type].Value;
			var bloom = AssetLoader.LoadedTextures["BloomNonPremult"].Value;

			Rectangle source = texture.Frame(1, Main.projFrames[type], 0, (int)(TimeActive / 4 % Main.projFrames[type]), 0, 0);
			Color color = Lighting.GetColor(Position.ToTileCoordinates());
			SpriteEffects effects = Position.X < Parent.Center.X ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

			float fade = 1f;

			if (Progress < 0.25f)
				fade = Progress / 0.25f;
			else if (Progress > 0.75f)
				fade = 1f - (Progress - 0.75f) / 0.25f;

			float glyphEffectProgress = 1f - Parent.GetGlobalNPC<BeeGlobalNPC>().tagCooldown / (float)BeeGlobalNPC.MAX_TAG_COOLDOWN;

			Vector2 offset = Main.rand.NextVector2CircularEdge(0.5f, 0.5f) * EaseFunction.EaseCircularIn.Ease(glyphEffectProgress);

			float scale = MathHelper.Lerp(0.6f, 1.1f, EaseFunction.EaseCircularIn.Ease(glyphEffectProgress));

			spriteBatch.End(); //BATCH ME!!!
			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState, default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

			spriteBatch.Draw(bloom, Position + offset + settings.AnchorPosition, null, Color.Black * 0.35f * fade, 0f, bloom.Size() / 2, scale * 0.5f, 0, 0);

			spriteBatch.End();
			spriteBatch.BeginDefault();

			spriteBatch.Draw(texture, Position + offset + settings.AnchorPosition, source, color * fade, Rotation, source.Size() / 2, scale, effects, 0);
		}
	}

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		if (!Main.dedServ)
			GameShaders.Armor.BindShader(Type, new BeeGlyphShaderData(AssetLoader.LoadedShaders["LiquidGlyphShader"], "mainPass"));
	}

	public override void SetDefaults()
	{
		Item.width = Item.height = 28;
		Item.rare = ItemRarityID.Green;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(Color.Goldenrod);
	}

	public override bool CanApplyGlyph(Item item) => base.CanApplyGlyph(item) && !ContentSamples.ItemsByType[item.type].DamageType.CountsAsClass(DamageClass.Summon);

	protected override void OnApplyGlyph(Item item, IApplicationContext context)
	{
		item.DamageType = ModContent.GetInstance<HybridDamageClass>().Clone()
			.AddSubClass(new(item.DamageType, 0.8f))
			.AddSubClass(new(DamageClass.Summon, 0.2f));

		MoRHelper.OverrideElement(item, MoRHelper.Nature);

		base.OnApplyGlyph(item, context);
	}

	protected override void OnRemoveGlyph(Item item, IApplicationContext context) => MoRHelper.OverrideElement(item, MoRHelper.Nature, -1);

	public override void DrawHeldItem(ref PlayerDrawSet drawInfo, DrawData input)
	{
		for (int j = 0; j < 4; j++)
		{
			Vector2 offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 2;
			DrawData item = input;
			item.position += offset;
			item.color = new Color(254, 210, 37);
			drawInfo.DrawDataCache.Add(item);

			offset = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * j / 4f) * 4;

			item = input;
			item.position += offset;
			item.color = new Color(211, 113, 11) * 0.3f;
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
		if (Main.dedServ)
			return;

		if (Main.rand.NextBool(45))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2CircularEdge(item.width / 3, item.height / 3);
			ParticleRenderers.OverPlayers.Add(new StickyHoneyParticle(pos, Vector2.Zero, Main.rand.NextFloat(0.8f, 1.5f), Main.rand.Next(100, 180), Main.rand.NextFloat(0.03f, 0.08f)));
		}

		if (Main.rand.NextBool(50))
		{
			Vector2 pos = item.Center + Main.rand.NextVector2Circular(item.width / 2, item.height / 2);

			Vector2 velocity = -Vector2.UnitY * Main.rand.NextFloat(-0.5f, 0.5f);

			if (Main.rand.NextBool(3))
				ParticleRenderers.OverPlayers.Add(new LargeBeeParticle(pos, velocity, 0f, 1f, 180));
			else
				ParticleRenderers.OverPlayers.Add(new BeeParticle(pos, velocity, 0f, 1f, 90));
		}
	}

	public override void GlyphShootEffects(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (Main.dedServ)
			return;

		Vector2 normalized = velocity.SafeNormalize(Vector2.One);
		for (int i = 0; i < 3; i++)
		{
			Dust.NewDustPerfect(position + normalized * item.width, DustID.Honey2, normalized.RotatedByRandom(0.4f) * Main.rand.NextFloat(5f), 70, default, 1.5f).noGravity = true;

			if (Main.rand.NextBool())
				Dust.NewDustPerfect(position + normalized * item.width, DustID.Honey2, normalized.SafeNormalize(Vector2.One).RotatedByRandom(0.4f) * Main.rand.NextFloat(3f), 100, default, 0.85f);
		}
	}

	public override void UpdateGlyphProjectile(Projectile projectile)
	{
		if (Main.dedServ)
			return;

		if (Main.rand.NextBool(2 + 1 * projectile.extraUpdates))
			Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(projectile.width / 2, projectile.height / 2), DustID.Honey2, -projectile.velocity.SafeNormalize(Main.rand.NextVector2Circular(1f, 1f)).RotatedByRandom(0.2f) * Main.rand.NextFloat(4f), 50 + Main.rand.Next(100), default, Main.rand.NextFloat(0.5f, 1.5f)).noGravity = true;

		if (Main.rand.NextBool(25 + 20 * projectile.extraUpdates))
			ParticleRenderers.OverPlayers.Add(new BeeParticle(projectile.Center, Main.rand.NextVector2Circular(3f, 3f), 0f, Main.rand.NextFloat(0.8f, 1.2f), 40));
	}
}

public class BeeGlyphShaderData(Asset<Effect> shader, string shaderPass) : ArmorShaderData(shader, shaderPass)
{
	private Effect GetEffect => _shader.Value;

	private readonly Asset<Effect> _shader = shader;

	public override void Apply(Entity entity, DrawData? drawData = null)
	{
		if (!drawData.HasValue)
			return;

		float sin = (float)Math.Abs(Math.Sin(Main.timeForVisualEffects * 0.005f));
		var c1 = Color.Lerp(new Color(255, 182, 0), new Color(254, 210, 37), sin);
		var c2 = new Color(211, 113, 11);

		GetEffect.Parameters["uColor1"].SetValue(c1.ToVector4() * 0.5f);
		GetEffect.Parameters["uColor2"].SetValue(c2.ToVector4() * 0.5f);
		GetEffect.Parameters["uImage1"].SetValue(AssetLoader.LoadedTextures["noise"].Value);
		GetEffect.Parameters["uImage2"].SetValue(AssetLoader.LoadedTextures["swirlNoise"].Value);

		GetEffect.Parameters["uPixelRes"].SetValue(drawData.Value.texture.Size().X / 2);

		GetEffect.Parameters["uTime"].SetValue((float)Main.timeForVisualEffects * 0.005f);
		GetEffect.Parameters["uStrength"].SetValue(0.2f);

		Apply();
	}
}