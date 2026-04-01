using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework.Graphics;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.GameContent.Drawing;
using Terraria.Graphics.CameraModifiers;
using Terraria.Graphics.Renderers;
using static Terraria.GameContent.PlayerEyeHelper;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

public class SoilBallProjectile : ModProjectile
{
	private const int MAX_TIMELEFT = 360;
	public Point MimicTilePosition => new Point((int)Projectile.ai[0], (int)Projectile.ai[1]);

	private readonly ParticleRenderer _twirlParticleRenderer = new();
	private VertexTrail[] _trails;

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.DontAttachHideToAlpha[Type] = true;
		ProjectileID.Sets.TrailingMode[Type] = 0;
		ProjectileID.Sets.TrailCacheLength[Type] = 10;
	}

	public override void SetDefaults()
	{
		Projectile.Size = new(18, 18);
		Projectile.hostile = true;
		Projectile.tileCollide = true;
		Projectile.hide = true;
		Projectile.penetrate = -1;
		Projectile.timeLeft = MAX_TIMELEFT;		
	}

	public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
	{
		return Projectile.velocity.Y > 0;
	}

	public override void AI()
	{
		if (!Main.dedServ)
		{
			if (_trails == null)
				CreateTrail();

			foreach (VertexTrail trail in _trails)
				trail.Update();
		}

		Projectile.rotation += 0.035f + Math.Abs(Projectile.velocity.Y) * 0.02f;
		Projectile.velocity.Y += 0.2f;

		if (Projectile.velocity.Y > 0)
			Projectile.velocity.Y *= 1.03f;

		if (Projectile.velocity.Y > 16f)
			Projectile.velocity.Y = 16;

		Point tilePosition = MimicTilePosition;
		int dustIndex = WorldGen.KillTile_MakeTileDust(tilePosition.X, tilePosition.Y, Framing.GetTileSafely(tilePosition));
		
		Dust dust = Main.dust[dustIndex];
		dust.position = Projectile.Center + Main.rand.NextVector2Circular(20, 20);
		dust.velocity = -Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.3f);
		dust.noLightEmittence = true;
		dust.scale = Main.rand.NextFloat(0.5f, 1.2f);
		dust.alpha = 50 + Main.rand.Next(100); 
		dust.noGravity = Main.rand.NextBool();
		if (dust.noGravity)
			dust.scale *= 1.5f;
	}

	public override void OnKill(int timeLeft)
	{
		SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact, Projectile.Center);
		Point tilePosition = MimicTilePosition;

		Color[] colors = Scarabeus.GetTilePalette(MimicTilePosition.ToVector2());

		for (int i = 0; i < 15; i++)
		{
			int dustIndex = WorldGen.KillTile_MakeTileDust(tilePosition.X, tilePosition.Y, Framing.GetTileSafely(tilePosition));

			Dust dust = Main.dust[dustIndex];
			dust.position = Projectile.Center + Main.rand.NextVector2Circular(20, 20);
			dust.velocity = -Projectile.oldVelocity.RotatedByRandom(0.65f) * Main.rand.NextFloat(0.8f);
			dust.noLightEmittence = true;
			dust.scale = Main.rand.NextFloat(1f, 1.5f);
			dust.alpha = 50 + Main.rand.Next(50);
			dust.noGravity = true;

			ParticleHandler.SpawnParticle(new SmokeCloud(Projectile.Center, -Projectile.oldVelocity.RotatedByRandom(0.65f) * Main.rand.NextFloat(0.5f), colors[0], Main.rand.NextFloat(0.08f, 0.15f), EaseFunction.EaseCircularOut, Main.rand.Next(50, 80))
			{
				Pixellate = true,
				DissolveAmount = 1,
				Intensity = 0.9f,
				SecondaryColor = colors[1],
				TertiaryColor = colors[2],
				PixelDivisor = 3,
				Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
				ColorLerpExponent = 0.5f,
				Layer = ParticleLayer.BelowSolid
			});
		}
		
		for (int i = 0; i < Main.rand.Next(2, 5); i++)
		{
			while (!Main.tile[tilePosition].HasTile)
				tilePosition.Y += 1;

			ParticleHandler.SpawnParticle(
				new TileChunkParticle(
					tilePosition, 
					Projectile.Center + Main.rand.NextVector2Circular(25f, 25f), 
					-Vector2.UnitY.RotatedByRandom(0.8f) * Main.rand.NextFloat(3f, 6f), 
					30, 
					false)
				);
		}
	}

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		targetHitbox.Inflate(16, 16);
		return projHitbox.Intersects(targetHitbox);
	}

	public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
	{
		modifiers.HitDirectionOverride = Math.Sign(Projectile.velocity.X);
	}

	private int _variantTopLeft;
	private int _variantTopRight;
	private int _variantBottomLeft;
	private int _variantBottomRight;
	private bool _initializedAppearance = false;
	private int _tileType;
	private int _tileColor;
	private bool _tileFullbright;
	private bool _tileEcho;

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
	{
		behindNPCsAndTiles.Add(index);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		if (!_initializedAppearance)
		{
			_initializedAppearance = true;

			Tile t = Main.tile[MimicTilePosition];
			_variantTopLeft = Main.rand.Next(3);
			_variantTopRight = Main.rand.Next(3);
			_variantBottomLeft = Main.rand.Next(3);
			_variantBottomRight = Main.rand.Next(3);

			if (!t.HasTile || Main.tileFrameImportant[t.TileType])
			{
				_tileType = TileID.Sandstone;
				_tileColor = PaintID.None;
				_tileFullbright = false;
				_tileEcho = false;
			}
			else
			{
				_tileType = t.TileType;
				_tileColor = t.TileColor;
				_tileFullbright = t.IsTileFullbright;
				_tileEcho = t.IsTileInvisible;
			}
		}

		Texture2D texture = TextureAssets.Tile[_tileType].Value;
		if (_tileColor != PaintID.None)
		{
			Texture2D paintedTex = Main.instance.TilePaintSystem.TryGetTileAndRequestIfNotReady(_tileType, 0, _tileColor);
			texture = paintedTex ?? texture;
		}

		Vector2 position = Projectile.Center;
		float rotation = Projectile.rotation;
		float scale = Projectile.scale;

		Color color = Lighting.GetColor(position.ToTileCoordinates());
		if (_tileFullbright)
			color = Color.White;
		if (!Main.ShouldShowInvisibleWalls() && _tileEcho)
			color = Color.Cyan * 0.2f;

		Vector2 unitX = Vector2.UnitX.RotatedBy(rotation) * Projectile.scale * 8f;
		Vector2 unitY = Vector2.UnitY.RotatedBy(rotation) * Projectile.scale * 8f;

		for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
		{
			Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f;
			float lerp = 1f  -  i / (float)Projectile.oldPos.Length;

			Vector2 scaledUnitX = Vector2.UnitX.RotatedBy(rotation) * Projectile.scale * 8f * lerp;
			Vector2 scaledUnitY = Vector2.UnitY.RotatedBy(rotation) * Projectile.scale * 8f * lerp;

			float scaled = Projectile.scale * lerp;

			DrawTile(texture, pos, scaledUnitX, scaledUnitY, color * lerp, rotation, scaled);
		}

		if (_trails != null)
		{
			foreach (VertexTrail trail in _trails)
			{
				trail.Opacity = 1f;
				trail?.Draw(TrailSystem.TrailShaders, AssetLoader.BasicShaderEffect, Main.spriteBatch.GraphicsDevice);
			}
		}

		DrawTile(texture, position, unitX, unitY, color, rotation, scale);
		
		return false;
	}

	//Draw a chunky 2x2 cube of tiles
	internal void DrawTile(Texture2D texture, Vector2 position, Vector2 unitX, Vector2 unitY, Color color, float rotation, float scale)
	{
		Main.EntitySpriteDraw(texture, position - unitX - unitY - Main.screenPosition, new Rectangle(_variantTopLeft * 36, 54, 16, 16), color, rotation, Vector2.One * 8, scale, SpriteEffects.None, 0);
		Main.EntitySpriteDraw(texture, position + unitX - unitY - Main.screenPosition, new Rectangle(18 + _variantTopRight * 36, 54, 16, 16), color, rotation, Vector2.One * 8, scale, SpriteEffects.None, 0);
		Main.EntitySpriteDraw(texture, position - unitX + unitY - Main.screenPosition, new Rectangle(_variantBottomLeft * 36, 72, 16, 16), color, rotation, Vector2.One * 8, scale, SpriteEffects.None, 0);
		Main.EntitySpriteDraw(texture, position + unitX + unitY - Main.screenPosition, new Rectangle(18 + _variantBottomRight * 36, 72, 16, 16), color, rotation, Vector2.One * 8, scale, SpriteEffects.None, 0);
	}

	private void CreateTrail()
	{
		Color[] colors = Scarabeus.GetTilePalette(MimicTilePosition.ToVector2());

		ITrailCap tCap = new RoundCap();
		ITrailPosition tPos = new EntityTrailPosition(Projectile);
		ITrailShader tShader = new ImageShader(AssetLoader.LoadedTextures["GlowTrail"].Value, Vector2.One);

		_trails =
		[
			new VertexTrail(new GradientTrail(colors[0] with { A = 255 }, Color.Transparent, EaseFunction.EaseQuarticOut), tCap, tPos, tShader, 70, 250, -2),
			new VertexTrail(new GradientTrail(colors[1] with { A = 255 }, Color.Transparent, EaseFunction.EaseQuarticOut), tCap, tPos, tShader, 60, 200, -2),
		];
	}
}