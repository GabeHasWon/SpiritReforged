using Microsoft.CodeAnalysis;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using Terraria.GameContent.Drawing;
using Terraria.Graphics.CameraModifiers;
using static Terraria.GameContent.PlayerEyeHelper;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

public class SoilBallProjectile : ModProjectile
{
	private const int MAX_TIMELEFT = 360;
	public Point MimicTilePosition => new Point((int)Projectile.ai[0], (int)Projectile.ai[1]);

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

		Texture2D tileTexture = TextureAssets.Tile[_tileType].Value;
		if (_tileColor != PaintID.None)
		{
			Texture2D paintedTex = Main.instance.TilePaintSystem.TryGetTileAndRequestIfNotReady(_tileType, 0, _tileColor);
			tileTexture = paintedTex ?? tileTexture;
		}

		Texture2D ramp = TextureColorCache.GetDominantPaletteInTileTexture(tileTexture);
		Texture2D texture = TextureAssets.Projectile[Type].Value;

		Vector2 position = Projectile.Center;
		float rotation = Projectile.rotation;
		float scale = Projectile.scale;
		Rectangle frame = texture.Frame(1, 3, 0, TileID.Sets.CanBeDugByShovel[_tileType] ? 1 : 0, 0, -2);

		Color color = Lighting.GetColor(position.ToTileCoordinates());
		if (_tileFullbright)
			color = Color.White;
		if (!Main.ShouldShowInvisibleWalls() && _tileEcho)
			color = Color.Cyan * 0.2f;

		Effect recolorShader = AssetLoader.LoadedShaders["ApplyPalette"].Value;
		recolorShader.Parameters["recolorRamp"].SetValue(ramp);
		recolorShader.Parameters["smoothRamp"].SetValue(true);

		Main.spriteBatch.End();
		Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, recolorShader, Main.GameViewMatrix.ZoomMatrix);

		Main.spriteBatch.Draw(texture, position - Main.screenPosition, frame,color, rotation, frame.Size() / 2f, scale, 0, 0);

		Main.spriteBatch.End();
		Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.ZoomMatrix);
		return false;
	}
}