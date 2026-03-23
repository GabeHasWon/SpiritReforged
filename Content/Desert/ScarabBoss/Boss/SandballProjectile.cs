using Microsoft.CodeAnalysis;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;
using Terraria.GameContent.Drawing;
using Terraria.Graphics.CameraModifiers;
using static Terraria.GameContent.PlayerEyeHelper;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

public class SandballProjectile : ModProjectile
{
	private const int MAX_TIMELEFT = 360;
	public Point MimicTilePosition => new Point((int)Projectile.ai[0], (int)Projectile.ai[1]);

	public override string Texture => AssetLoader.EmptyTexture;

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.DontAttachHideToAlpha[Type] = true;
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
		Projectile.rotation += Projectile.velocity.X * 0.04f;
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

		//Draw a chunky 2x2 cube of tiles
		Main.EntitySpriteDraw(texture, position - unitX - unitY - Main.screenPosition, new Rectangle(_variantTopLeft * 36, 54, 16, 16), color, rotation, Vector2.One * 8, scale, SpriteEffects.None, 0);
		Main.EntitySpriteDraw(texture, position + unitX - unitY - Main.screenPosition, new Rectangle(18 + _variantTopRight * 36, 54, 16, 16), color, rotation, Vector2.One * 8, scale, SpriteEffects.None, 0);
		Main.EntitySpriteDraw(texture, position - unitX + unitY - Main.screenPosition, new Rectangle(_variantBottomLeft * 36, 72, 16, 16), color, rotation, Vector2.One * 8, scale, SpriteEffects.None, 0);
		Main.EntitySpriteDraw(texture, position + unitX + unitY - Main.screenPosition, new Rectangle(18 + _variantBottomRight * 36, 72, 16, 16), color, rotation, Vector2.One * 8, scale, SpriteEffects.None, 0);
		return false;
	}
}