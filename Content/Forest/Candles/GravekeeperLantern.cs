using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Subclasses;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Forest.Candles;

public class GravekeeperLantern : ModItem, IDrawHeld, IManaBoon
{
	public sealed class GhostlyFlame : ModProjectile, IDrawPixelated
	{
		private readonly ParticleRenderer _renderer = new();

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

			Projectile.Opacity = 0;
		}

		public override void AI()
		{
			Projectile.velocity *= 0.97f;
			Projectile.rotation = Projectile.velocity.ToRotation();

			if (!Main.dedServ)
			{
				if (Projectile.timeLeft > 20 && Main.rand.NextBool(4))
				{
					_renderer.Add(new GhostFlameParticle()
					{
						LocalPosition = Projectile.Center + Main.rand.NextVector2Circular(10, 10) * Main.rand.NextFloat(),
						Velocity = Projectile.velocity * 0.2f,
						Scale = Vector2.One,
						TimeLeft = Main.rand.Next(15, 30)
					});
				}

				_renderer.Settings.AnchorPosition = -Main.screenPosition;
				_renderer.Update();
			}

			Projectile.scale = Math.Min(Projectile.timeLeft / 20f, 1);
			Projectile.Opacity = Math.Min(Projectile.Opacity + 0.1f, 1);
		}

		public override void OnKill(int timeLeft)
		{
			if (!Main.dedServ)
			{
				for (int i = 0; i < 3; i++)
					ParticleHandler.SpawnParticle(new CompositeSmoke(Projectile.Center, Main.rand.NextVector2Circular(1, 1), Color.White, 30, false));

				ParticleHandler.SpawnParticle(new LightBurst(Projectile.Center, 0, Color.White, 0.5f, 20)
				{ noLight = true });
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			_renderer.Draw(Main.spriteBatch);
			return false;
		}

		void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Vector2 scale = new Vector2(Math.Min(Projectile.velocity.Length() / 3f, 1), 1) * Projectile.scale * 0.5f;
			int length = ProjectileID.Sets.TrailCacheLength[Type];

			for (int i = 0; i < length; i++)
			{
				Vector2 trailPosition = Projectile.oldPos[i] + Projectile.Size / 2 - Main.screenPosition;
				float progress = i / (float)length;
				Color color = Color.Lerp(Color.White, Color.PaleVioletRed, progress) * (1f - progress);

				IDrawPixelated.PixelateDrawPosition(ref trailPosition);

				Main.EntitySpriteDraw(texture, trailPosition, null, Projectile.GetAlpha(color).Additive(150), Projectile.rotation, texture.Size() / 2, scale * (1f - progress), 0);
			}

			Vector2 position = Projectile.Center - Main.screenPosition;
			IDrawPixelated.PixelateDrawPosition(ref position);

			Main.EntitySpriteDraw(texture, position, null, Projectile.GetAlpha(Color.White).Additive(150), Projectile.rotation, texture.Size() / 2, scale, 0);
		}
	}

	public int ManaLimit => 100;

	public override void SetStaticDefaults()
	{
		Main.RegisterItemAnimation(Type, new NightlightLead.DrawGrid(3, 2, 1));
		MoRHelper.AddElement(Item, MoRHelper.Arcane, true);
	}

	public override void SetDefaults()
	{
		Item.DefaultToMagicWeapon(ModContent.ProjectileType<GhostlyFlame>(), 20, 10, true);
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
		player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, -MathHelper.PiOver2 * player.direction);
		
		if (!Main.dedServ)
			Lighting.AddLight(player.Center, Color.PaleTurquoise.ToVector3() * IManaBoon.GetManaStrength(this, player) * 0.7f);
	}

	public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
	{
		position += new Vector2(10 * player.direction, 12 * player.gravDir);
		velocity = new Vector2(velocity.Length(), 0).RotatedBy(position.AngleTo(Main.MouseWorld));
	}

	void IDrawHeld.DrawHeld(ref PlayerDrawSet drawinfo)
	{
		Player player = drawinfo.drawPlayer;
		Texture2D texture = TextureAssets.Item[Type].Value;
		Rectangle source = Main.itemAnimations[Type].GetFrame(texture, 3);

		Vector2 bobOffset = Main.OffsetsPlayerHeadgear[player.bodyFrame.Y / player.bodyFrame.Height] * player.gravDir;
		Vector2 center = player.MountedCenter + bobOffset + new Vector2(15 * player.direction, 6 * player.gravDir);
		Vector2 drawPosition = new((int)(center.X - Main.screenPosition.X), (int)(center.Y - Main.screenPosition.Y + player.gfxOffY));

		float rotation = 0; //player.itemRotation
		float strength = Math.Min(IManaBoon.GetManaStrength(this, player) * 1.1f, 1);
		Color color = Lighting.GetColor((int)center.X / 16, (int)center.Y / 16);

		if (strength > 0)
			source = Main.itemAnimations[Type].GetFrame(texture, 4);

		drawinfo.DrawDataCache.Add(new DrawData(texture, drawPosition, source, color, rotation, source.Size() / 2, 1, drawinfo.playerEffect, 0));

		source = Main.itemAnimations[Type].GetFrame(texture, 5);
		drawinfo.DrawDataCache.Add(new DrawData(texture, drawPosition, source, Color.White.Additive(100) * strength, rotation, source.Size() / 2, 1, drawinfo.itemEffect));
	}
}