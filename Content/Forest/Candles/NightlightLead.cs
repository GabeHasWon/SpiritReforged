using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.Subclasses;
using SpiritReforged.Content.Desert;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Candles;

public class NightlightLead : ModItem, IDrawHeld, IManaBoon
{
	public class DrawGrid : DrawAnimation
	{
		public readonly int Columns;
		public readonly int Rows;

		public DrawGrid(int columns, int rows, int frame = 0)
		{
			Frame = frame;
			FrameCounter = 0;
			TicksPerFrame = 1;
			Rows = rows;
			Columns = columns;
		}

		public override void Update() { }

		public override Rectangle GetFrame(Texture2D texture, int frameCounterOverride = -1)
		{
			int frame = (frameCounterOverride >= 0) ? Math.Clamp(frameCounterOverride, 0, Columns * Rows - 1) : Frame;
			return texture.Frame(Columns, Rows, frame % Columns, frame / Columns, (Columns == 1) ? 0 : -2, (Rows == 1) ? 0 : -2);
		}
	}

	public sealed class NightlightFireball : ModProjectile
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
				
				for (int i = 0; i < 2; i++)
					ParticleHandler.SpawnParticle(new FireParticle(Projectile.Center, Vector2.UnitY * -3, colors, 0.8f, 0.05f, EaseFunction.EaseQuarticOut, 18)
					{ PixelDivisor = 2 });

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

	public sealed class NightlightAura : ModProjectile
	{
		public const int MaxTimeLeft = 300;

		public override string Texture => AssetLoader.EmptyTexture;

		public float distance;

		public override void SetDefaults()
		{
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			const int fade_out_time = 30;
			const int maximum_range = 150;

			Player owner = Main.player[Projectile.owner];
			float strength = IManaBoon.GetManaStrength(ModContent.GetInstance<NightlightLead>(), Main.player[Projectile.owner]);
			float result = maximum_range * strength;

			Projectile.Center = owner.Center;
			Projectile.gfxOffY = owner.gfxOffY;

			foreach (NPC npc in Main.ActiveNPCs) //Debuff nearby NPCs of any kind
				if (!npc.immortal && npc.Distance(Projectile.Center) < distance)
					npc.AddBuff(ModContent.BuffType<Slowed>(), 60);

			if (strength > 0 && owner.HeldItem.ModItem is NightlightLead)
				Projectile.timeLeft = MaxTimeLeft;

			if (result > distance)
				distance = MathHelper.Lerp(distance, result, 0.05f);
			else
				distance *= 1f - 1f / MaxTimeLeft;

			if (Projectile.timeLeft < fade_out_time)
				Projectile.Opacity -= 1 / (float)fade_out_time;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float thicknessReduction = distance * 0.0001f;
			DrawRing(Color.PaleVioletRed, lightColor, Projectile.Opacity * 0.2f, 0.08f - thicknessReduction);
			DrawRing(Color.White, lightColor, Projectile.Opacity * 0.2f, 0.04f - thicknessReduction);

			return false;
		}

		private void DrawRing(Color color, Color lightColor, float opacity, float thickness)
		{
			float scale = distance * 4;
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

	public int ManaLimit => 100;

	public override void SetStaticDefaults()
	{
		Main.RegisterItemAnimation(Type, new DrawGrid(3, 2, 1));
		MoRHelper.AddElement(Item, MoRHelper.Arcane, true);
	}

	public override void SetDefaults()
	{
		Item.DefaultToMagicWeapon(ModContent.ProjectileType<NightlightFireball>(), 20, 10, true);
		Item.damage = 11;
		Item.mana = 8;
		Item.useStyle = ItemUseStyleID.HoldUp;
		Item.noUseGraphic = true;
		Item.UseSound = SoundID.Item1;
		Item.maxStack = 1;
		Item.value = Item.sellPrice(silver: 40);
	}

	public override void HoldItem(Player player)
	{
		player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Quarter, -MathHelper.PiOver2 * player.direction);
		float strength = IManaBoon.GetManaStrength(this, player);

		if (player.ItemAnimationActive)
		{
			float progress = 1f - player.itemAnimation / (float)player.itemAnimationMax;
			var stretch = (progress > 0.5f) ? Player.CompositeArmStretchAmount.Full : Player.CompositeArmStretchAmount.Quarter;

			player.SetCompositeArmFront(true, stretch, (EaseFunction.EaseCubicOut.Ease(progress) * 2 - 1.5f - MathHelper.PiOver2) * player.direction);
		}

		if (strength > 0 && Main.myPlayer == player.whoAmI)
		{
			int type = ModContent.ProjectileType<NightlightAura>();

			if (player.ownedProjectileCounts[type] == 0)
				Projectile.NewProjectile(Item.GetSource_FromThis(), player.Center, Vector2.Zero, type, 0, 0, player.whoAmI);
		}

		if (!Main.dedServ)
		{
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
		position = player.Center + new Vector2(20 * player.direction, -10 * player.gravDir);
		velocity = new Vector2(velocity.Length(), 0).RotatedBy(position.AngleTo(Main.MouseWorld));
	}

	void IDrawHeld.DrawHeld(ref PlayerDrawSet drawinfo)
	{
		Player player = drawinfo.drawPlayer;
		Texture2D texture = TextureAssets.Item[Type].Value;
		Rectangle source = Main.itemAnimations[Type].GetFrame(texture, 3);

		Vector2 bobOffset = Main.OffsetsPlayerHeadgear[player.bodyFrame.Y / player.bodyFrame.Height] * player.gravDir;
		Vector2 center = player.MountedCenter + bobOffset + new Vector2(18 * player.direction, -6 * player.gravDir);
		Vector2 drawPosition = new((int)(center.X - Main.screenPosition.X), (int)(center.Y - Main.screenPosition.Y + player.gfxOffY));

		float rotation = 0; //player.itemRotation
		float strength = Math.Min(IManaBoon.GetManaStrength(this, player) * 1.1f, 1);
		Color color = Lighting.GetColor((int)center.X / 16, (int)center.Y / 16);

		if (strength > 0)
			source = Main.itemAnimations[Type].GetFrame(texture, 4);

		drawinfo.DrawDataCache.Add(new DrawData(texture, drawPosition, source, color, rotation, source.Size() / 2, 1, drawinfo.playerEffect, 0));

		if (strength > 0)
		{
			for (int i = 0; i < 2; i++)
			{
				source = Main.itemAnimations[Type].GetFrame(texture, 5);

				drawinfo.DrawDataCache.Add(new DrawData(texture, drawPosition + Main.rand.NextVector2Circular(2, 2), source, Color.Orange.Additive(100),
					rotation + (float)Math.Sin(Main.timeForVisualEffects / 5f) * 0.1f * strength, source.Size() / 2, 1, drawinfo.itemEffect));
			}
		}
	}
}