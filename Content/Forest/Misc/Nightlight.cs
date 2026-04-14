using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Content.Desert;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Misc;

public class Nightlight : ModItem, IDrawHeld
{
	public class NightlightFireball : ModProjectile
	{
		private bool _didSpawnEffects;

		public ref float Angle => ref Projectile.ai[0];

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 8;
			ProjectileID.Sets.TrailingMode[Type] = 0;
		}

		public override void SetDefaults()
		{
			Projectile.Size = new(12);
			Projectile.friendly = true;
			Projectile.tileCollide = false;
			Projectile.timeLeft = 80;
		}

		public override void AI()
		{
			if (!_didSpawnEffects)
			{
				Color[] colors = [Color.Goldenrod.Additive(100), Color.PaleVioletRed.Additive(100), Color.Red.Additive(100)];
				ParticleHandler.SpawnParticle(new FireParticle(Projectile.Center, Vector2.UnitY * -3, colors, 0.8f, 0.1f, EaseFunction.EaseQuarticOut, 18));

				_didSpawnEffects = true;

				if (Projectile.owner == Main.myPlayer)
				{
					Player owner = Main.player[Projectile.owner];
					float angle = owner.Center.Y - Projectile.Center.Y;
					Angle = angle / 1800f * owner.direction;
				}
			}

			Projectile.rotation += Projectile.velocity.Length() * 0.05f * Projectile.direction;
			Projectile.velocity *= 0.97f;
			Projectile.velocity = Projectile.velocity.RotatedBy(Angle);

			if (Main.rand.NextBool(5))
			{
				var dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, newColor: Color.Pink);
				dust.velocity = Projectile.velocity * 0.8f;
				dust.noGravity = true;
				dust.fadeIn = 1.1f * Projectile.scale;
			}

			Projectile.scale = Math.Min(Projectile.timeLeft / 20f, 1);
		}

		public override void OnKill(int timeLeft)
		{
			if (timeLeft <= 0)
				return;

			ParticleHandler.SpawnParticle(new TexturedPulseCircle(Projectile.Center, Color.PaleVioletRed, 0.5f, 100, 18, "supPerlin", Vector2.One * 3, EaseFunction.EaseCubicOut).WithSkew(0.5f, Main.rand.NextFloat(MathHelper.PiOver2)));
			ParticleHandler.SpawnParticle(new TexturedPulseCircle(Projectile.Center, Color.PaleVioletRed, 0.5f, 100, 18, "supPerlin", Vector2.One * 3, EaseFunction.EaseCubicOut).WithSkew(0.5f, Main.rand.NextFloat(MathHelper.PiOver2)));
			ParticleHandler.SpawnParticle(new LightBurst(Projectile.Center, 0, Color.Goldenrod, 0.5f, 16));

			for (int i = 0; i < 4; i++)
				ParticleHandler.SpawnParticle(new EmberParticle(Projectile.Center, Main.rand.NextVector2Circular(2, 2), Color.Goldenrod, Color.Red, 0.5f, 30, 3));
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			int length = ProjectileID.Sets.TrailCacheLength[Type];

			for (int i = 0; i < length; i++)
			{
				Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size / 2 - Main.screenPosition;
				float progress = i / (float)length;
				var color = Color.Lerp(Color.White, Color.PaleVioletRed, progress).Additive() * (1f - progress);

				Main.EntitySpriteDraw(texture, drawPosition, null, color, Projectile.rotation, texture.Size() / 2, Projectile.scale * (1f - progress), 0);
			}

			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(Color.White).Additive(100), Projectile.rotation, texture.Size() / 2, Projectile.scale, 0);
			return false;
		}
	}

	public class NightlightAura : ModProjectile
	{
		public const int MaxTimeLeft = 300;

		public override string Texture => AssetLoader.EmptyTexture;

		public float Distance => 150 * GetStrength(Main.player[Projectile.owner]) * ((float)Projectile.timeLeft / MaxTimeLeft);

		public override void SetDefaults()
		{
			Projectile.Opacity = 0.2f;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];

			Projectile.Center = owner.Center;
			Projectile.gfxOffY = owner.gfxOffY;

			foreach (NPC npc in Main.ActiveNPCs)
			{
				if (npc.Distance(Projectile.Center) < Distance)
					npc.AddBuff(ModContent.BuffType<Slowed>(), 60);
			}

			if (owner.HeldItem.type == ModContent.ItemType<Nightlight>())
			{
				Projectile.timeLeft = MaxTimeLeft;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float thicknessReduction = Distance * 0.0001f;
			DrawRing(Color.PaleVioletRed, lightColor, Projectile.Opacity, 0.04f - thicknessReduction);
			DrawRing(Color.White, lightColor, Projectile.Opacity, 0.02f - thicknessReduction);

			return false;
		}

		private void DrawRing(Color color, Color lightColor, float opacity, float thickness)
		{
			float scale = Distance * 4;
			Effect effect = AssetLoader.LoadedShaders["PulseCircle"].Value;

			effect.Parameters["RingColor"].SetValue(color.ToVector4() * opacity);
			effect.Parameters["BloomColor"].SetValue(Color.White.ToVector4() * opacity);
			effect.Parameters["RingWidth"].SetValue(thickness);
			effect.Parameters["uTexture"].SetValue(AssetLoader.LoadedTextures["LiquidTrail"].Value);
			effect.Parameters["textureStretch"].SetValue(Vector2.One);
			effect.Parameters["scroll"].SetValue((float)Main.timeForVisualEffects / 200f);

			PrimitiveRenderer.DrawPrimitiveShape(new SquarePrimitive
			{
				Color = lightColor * opacity,
				Height = scale,
				Length = scale,
				Position = Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY),
				Rotation = 0
			}, effect, "TexturedStyle");
		}

		public override bool? CanDamage() => false;
	}

	public static float GetStrength(Player player)
	{
		float value = 1f - player.statMana / (float)player.statManaMax2;
		return Math.Min(value * 1.1f, 1f);
	}

	public override void SetStaticDefaults()
	{
		Main.RegisterItemAnimation(Type, new DrawAnimationVertical(2, 3) { NotActuallyAnimating = true });
		MoRHelper.AddElement(Item, MoRHelper.Arcane, true);
	}

	public override void SetDefaults()
	{
		Item.DefaultToMagicWeapon(ModContent.ProjectileType<NightlightFireball>(), 20, 10, true);
		Item.damage = 11;
		Item.mana = 8;
		Item.useStyle = ItemUseStyleID.HoldUp;
		Item.holdStyle = ItemHoldStyleID.HoldFront;
		Item.UseSound = SoundID.Item1;
		Item.maxStack = 1;
		Item.value = Item.sellPrice(silver: 40);
	}

	public override void UseItemFrame(Player player) => player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, -MathHelper.PiOver2 * player.direction);

	public override void HoldItem(Player player)
	{
		if (Main.myPlayer == player.whoAmI)
		{
			int type = ModContent.ProjectileType<NightlightAura>();
			if (player.ownedProjectileCounts[type] == 0)
				Projectile.NewProjectile(Item.GetSource_FromThis(), player.Center, Vector2.Zero, type, 0, 0, player.whoAmI);
		}

		if (!Main.dedServ)
		{
			float strength = GetStrength(player);
			Lighting.AddLight(player.Center, new Vector3(0.8f, 0.7f, 0.38f) * strength);

			if (strength > 0 && Main.rand.NextFloat() < strength / 2f)
			{
				Vector2 top = player.RotatedRelativePoint(player.Center + new Vector2(19 * player.direction, -14));
				var dust = Dust.NewDustPerfect(top + Main.rand.NextVector2Circular(4, 4), Main.rand.NextFromList(DustID.Torch, DustID.Smoke), Vector2.UnitY * -Main.rand.NextFloat(3 * strength), Scale: 1.3f);
				dust.noGravity = true;

				if (dust.type == DustID.Smoke)
					dust.alpha = 150;
			}
		}
	}

	public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
	{
		position = player.Center + Main.rand.NextVector2CircularEdge(30, 50);
		velocity = new Vector2(velocity.Length(), 0).RotatedBy(position.AngleTo(Main.MouseWorld));
	}

	public void DrawHeld(ref PlayerDrawSet drawinfo)
	{
		Texture2D texture = TextureAssets.Item[Type].Value;
		Rectangle source = texture.Frame(1, 3, 0, 0, 0, -2);

		Vector2 origin = source.Size() / 2;
		Vector2 dirOffset = drawinfo.drawPlayer.ItemAnimationActive ? new(15, -2) : new(15, 0);

		dirOffset.X *= drawinfo.drawPlayer.direction;
		Vector2 location = (drawinfo.drawPlayer.Center - Main.screenPosition + dirOffset + new Vector2(0, drawinfo.drawPlayer.gfxOffY)).Floor();
		Color color = drawinfo.drawPlayer.HeldItem.GetAlpha(Lighting.GetColor((drawinfo.ItemLocation / 16).ToPoint()));
		float strength = Math.Min(GetStrength(drawinfo.drawPlayer) * 1.1f, 1);

		if (strength > 0)
			source = texture.Frame(1, 3, 0, 1, 0, -2);

		drawinfo.DrawDataCache.Add(new DrawData(texture, location, source, color, drawinfo.drawPlayer.itemRotation, origin, 1, drawinfo.itemEffect));

		Rectangle flameSource = new(0, 64, 10, 10);
		
		for (int i = 0; i < 2; i++)
		{
			Vector2 flameLocation = location + new Vector2(4 * drawinfo.drawPlayer.direction, -13) + Main.rand.NextVector2Circular(2, 2);
			float sine = (float)Math.Sin(Main.timeForVisualEffects / 10f) * 0.25f;

			drawinfo.DrawDataCache.Add(new DrawData(texture, flameLocation, flameSource, Color.White.Additive(100) * 0.5f, drawinfo.drawPlayer.itemRotation + (float)Math.Sin(Main.timeForVisualEffects / 5f) * 0.1f,
				new Vector2(flameSource.Width / 2, flameSource.Height), new Vector2(1 + sine, 1 - sine) * strength, drawinfo.itemEffect)); //Frame
		}
	}
}