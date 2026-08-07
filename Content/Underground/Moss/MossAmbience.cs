using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Moss.Oganesson;
using SpiritReforged.Content.Underground.Moss.Radon;
using Terraria.DataStructures;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Underground.Moss;

[Autoload(Side = ModSide.Client)]
public sealed class MossAmbience : GlobalTile
{
	public static readonly ParticleRenderer OverPlayers = new();
	public static readonly Dictionary<int, Color> ColorByMoss = [];

	private static readonly Dictionary<Point16, Color> ColorSampleChunks = [];

	public static void SetChunk(int x, int y, Color color, int accuracy = 5)
	{
		(x, y) = (x / accuracy, y / accuracy);
		ColorSampleChunks.TryAdd(new Point16(x, y), color);
	}

	public static bool GetChunk(int x, int y, out Color color, int accuracy = 5)
	{
		(x, y) = (x / accuracy, y / accuracy);
		if (ColorSampleChunks.TryGetValue(new Point16(x, y), out color))
			return true;

		return false;
	}

	public override void Load()
	{
		On_Main.UpdateParticleSystems += UpdateParticles;
		On_Main.DrawInfernoRings += DrawAbovePlayer;

		WaterAlpha.OnWaterColor += ApplyMossWaterAlpha;
	}

	public override void SetStaticDefaults()
	{
		ColorByMoss.Add(TileID.XenonMoss, new Color(0, 184, 255));
		ColorByMoss.Add(TileID.ArgonMoss, new Color(255, 92, 160));
		ColorByMoss.Add(TileID.KryptonMoss, new Color(105, 255, 41));
		ColorByMoss.Add(TileID.VioletMoss, new Color(210, 97, 255));
		ColorByMoss.Add(TileID.RainbowMoss, Main.DiscoColor);
		ColorByMoss.Add(TileID.LavaMoss, new Color(252, 90, 3));
		ColorByMoss.Add(ModContent.TileType<RadonMoss>(), new Color(248, 255, 56));
		ColorByMoss.Add(ModContent.TileType<OganessonMoss>(), new Color(255, 255, 255));
	}

	private static void UpdateParticles(On_Main.orig_UpdateParticleSystems orig, Main self)
	{
		OverPlayers.Update();
		orig(self);
	}

	public override void NearbyEffects(int i, int j, int type, bool closer)
	{
		if (!closer || Main.gamePaused || !NeonMossScene.InNeonMoss)
			return;

		Tile tileAbove = Main.tile[i, j - 1];
		if (/*Main.tileMoss[type] && Main.tileLighted[type] && */!WorldGen.SolidTile(tileAbove) && tileAbove.LiquidAmount > 0 && tileAbove.WallType == WallID.None && Main.rand.NextBool(130))
		{
			if (ColorByMoss.TryGetValue(type, out Color waterMossColor))
			{
				Vector2 position = new Vector2(i, j - 1).ToWorldCoordinates(Main.rand.NextFloat(16), 8);
				Color color = waterMossColor;
				Color outlineColor = Color.Lerp(waterMossColor, Color.Black, 0.5f);

				OverPlayers.Add(new FloatingMoss(Main.rand.Next(FloatingMoss.FRAME_COUNT), color, outlineColor)
				{
					LocalPosition = position,
					Scale = Vector2.One * 0.8f,
					Velocity = Vector2.Zero,
					Rotation = Main.rand.NextFloat(MathHelper.Pi)
				});
			}
		}

		if (Main.rand.NextBool(2400) && ColorByMoss.TryGetValue(type, out Color floatingColor))
			SpawnFloatingParticles(i, j, floatingColor);
	}

	private static void DrawAbovePlayer(On_Main.orig_DrawInfernoRings orig, Main self)
	{
		foreach (IParticle particle in OverPlayers.Particles)
		{
			if (particle is FloatingMoss floatingMoss)
				floatingMoss.drawOutline = true; //Set to outline mode
		}

		OverPlayers.Draw(Main.spriteBatch);

		foreach (IParticle particle in OverPlayers.Particles)
		{
			if (particle is FloatingMoss floatingMoss)
				floatingMoss.drawOutline = false; //Set to non-outline mode
		}

		OverPlayers.Draw(Main.spriteBatch);

		orig(self);
	}

	private static bool ApplyMossWaterAlpha(int x, int y, ref VertexColors colors, bool isPartial)
	{
		const int full_depth = 2;
		if (NeonMossScene.InNeonMoss && GetChunksInDepth(x, y, full_depth, out Color color))
		{
			LightRange(x, y, full_depth, ref colors, color.Additive());
			//ColorSampleChunks.Clear();

			return false;
		}

		return true;

		static bool GetChunksInDepth(int x, int y, int depth, out Color color) //Calls GetChunk with multiple tile depth support
		{
			for (int i = 0; i <= depth; i++)
			{
				if (GetChunk(x, y - i, out color))
					return true;
			}

			color = default;
			return false;
		}

		static void LightRange(int x, int y, int depth, ref VertexColors colors, Color tint)
		{
			for (int i = 0; i < depth; i++)
			{
				Tile tile = Framing.GetTileSafely(x, y - i - 1);
				if (tile.LiquidAmount < 255)
				{
					if (i == depth - 1)
					{
						ClampColor(ref colors.TopLeftColor, tint, x, y);
						ClampColor(ref colors.TopRightColor, tint, x + 1, y);
					}
					else
					{
						ClampColor(ref colors.TopLeftColor, tint, x, y);
						ClampColor(ref colors.TopRightColor, tint, x + 1, y);
						ClampColor(ref colors.BottomLeftColor, tint, x, y + 1);
						ClampColor(ref colors.BottomRightColor, tint, x + 1, y + 1);
					}

					break;
				}
			}
		}

		static void ClampColor(ref Color color, Color tint, int x, int y) => color = Color.Lerp(color, tint, Lighting.Brightness(x, y) * 2);
	}

	private static void SpawnFloatingParticles(int i, int j, Color mossColor)
	{
		Vector2 startPos = new Vector2(i, j).ToWorldCoordinates();
		Vector2 velocity = new(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-0.6f, -0.2f));

		ParticleHandler.SpawnParticle(new GlowParticle(startPos, velocity,
			mossColor * 0.225f, 0.25f, Main.rand.Next(260, 400), 4, p =>
			{
				p.Velocity = p.Velocity.RotatedBy(Main.rand.NextFloat(-0.005f, 0.005f)) * 0.98f;
				p.Velocity.Y -= Main.rand.NextFloat(0.02f, 0.01f);
				p.Scale = 0.5f + 0.15f * (float)Math.Sin(p.TimeActive * 0.05f);

				//constantly update helium moss colors
				if (mossColor == Main.DiscoColor)
					p.Color = Main.DiscoColor;
			}));
	}

	#region moss floor visuals
	public override void FloorVisuals(int type, Player player)
	{
		if (Main.gamePaused)
			return;

		int chance = (int)Math.Clamp(45 - 7.5f * player.velocity.Length(), 1, 45);
		if (chance >= 1 && Main.rand.NextBool(chance))
		{
			if (type is TileID.XenonMoss or TileID.XenonMossBrick)
				XenonFloorParticles(player);
			else if (type is TileID.ArgonMoss or TileID.ArgonMossBrick)
				ArgonFloorParticles(player);
			else if (type is TileID.KryptonMoss or TileID.KryptonMossBrick)
				KryptonFloorParticles(player);
			else if (type is TileID.VioletMoss or TileID.VioletMossBrick)
				NeonFloorParticles(player);
			else if (type is TileID.RainbowMoss or TileID.RainbowMossBrick)
				HeliumFloorParticles(player);
			else if (type is TileID.LavaMoss or TileID.LavaMossBrick)
				LavaFloorParticles(player);
			else if (type == ModContent.TileType<RadonMoss>() || type == ModContent.TileType<RadonMossGrayBrick>())
				RadonFloorParticles(player);
			else if (type == ModContent.TileType<OganessonMoss>() || type == ModContent.TileType<OganessonMossGrayBrick>())
				OganessonFloorParticles(player);
		}
	}

	//Xenon: Simple rise and linger
	private static void XenonFloorParticles(Player player)
	{
		Vector2 start = player.BottomLeft + new Vector2(Main.rand.Next(player.width), 0);
		Vector2 velocity = new Vector2(Main.rand.NextFloat(-1f, 1f), -1).RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(0.4f, 1f);

		ParticleHandler.SpawnParticle(new GlowParticle(start, velocity,
			new Color(0, 184, 255) * 0.5f, Main.rand.NextFloat(0.25f, 0.45f), 180, 8, p =>
			{
				p.Velocity.X *= Main.rand.NextFloat(.8f, .9f);
				p.Velocity.Y *= Main.rand.NextFloat(.96f, .99f);
			}));
	}

	//Argon: Spirals based on player position and movement
	private static void ArgonFloorParticles(Player player)
	{
		Vector2 center = player.Center;
		Vector2 start = player.BottomLeft + new Vector2(Main.rand.Next(player.width), 0);
		Vector2 velocity = (start - center).SafeNormalize(Vector2.UnitY).RotatedByRandom(MathHelper.PiOver2) * Main.rand.NextFloat(.5f, 1f);

		float distance = Vector2.Distance(start, center);
		float rotationDir = 1f;

		if (distance < 100f)
			rotationDir *= Math.Sign(player.velocity.X);
		else
			rotationDir = 1f;

		ParticleHandler.SpawnParticle(new GlowParticle(start, velocity,
			new Color(255, 92, 160) * 0.65f, Main.rand.NextFloat(0.25f, 0.45f), Main.rand.Next(90, 140), 8, p =>
			{
				Vector2 toCenter = (center - p.Position).SafeNormalize(Vector2.Zero);

				p.Velocity = p.Velocity.RotatedBy(0.2f * rotationDir) * 0.97f;
				p.Velocity += toCenter * 0.09f;
			}));
	}

	//Krypton: flits back and forth energetically while rising
	private static void KryptonFloorParticles(Player player)
	{
		Vector2 start = player.BottomLeft + new Vector2(Main.rand.Next(player.width), 0);
		Vector2 velocity = new Vector2(Main.rand.NextFloat(-1f, 1f), -1).RotatedByRandom(MathHelper.PiOver2) * Main.rand.NextFloat(1f, 2f);

		ParticleHandler.SpawnParticle(new GlowParticle(start, velocity,
			new Color(105, 255, 41) * 0.5f, Main.rand.NextFloat(0.25f, 0.45f), 120, 7, p =>
			{
				if (Main.rand.NextBool(24))
					p.Velocity.X += Main.rand.NextFloat(-2f, 2f);

				if (Math.Abs(p.Velocity.X) > 1f)
					p.Velocity.X *= .9f;

				p.Velocity *= .96f;
			}));
	}

	//Neon: Slowly drift toward players
	private static void NeonFloorParticles(Player player)
	{
		Vector2 start = player.BottomLeft + new Vector2(Main.rand.Next(player.width), 0);
		Vector2 velocity = new Vector2(Main.rand.NextFloat(-0.2f, 0.2f), -0.4f);

		ParticleHandler.SpawnParticle(new GlowParticle(start, velocity,
			new Color(210, 97, 255) * 0.5f, Main.rand.NextFloat(0.25f, 0.45f), Main.rand.Next(95, 135), 8, p =>
			{
				Vector2 toPlayer = (player.Center - p.Position).SafeNormalize(Vector2.Zero);
				p.Velocity += toPlayer * 0.015f;

				p.Velocity.Y *= 0.98f;
				p.Velocity.X *= 0.97f;
			}));
	}

	//Helium: Random circles (i ran out of ideas lol + it's already rainbow)
	private static void HeliumFloorParticles(Player player)
	{
		Vector2 start = player.BottomLeft + new Vector2(Main.rand.Next(player.width), 0);
		Vector2 velocity = new Vector2(Main.rand.NextFloat(-0.2f, 0.2f), -0.4f);

		ParticleHandler.SpawnParticle(new GlowParticle(start, velocity,
			Main.DiscoColor, Main.rand.NextFloat(0.25f, 0.45f), Main.rand.Next(95, 135), 8, p =>
			{
				p.Velocity = p.Velocity.RotatedBy(Main.rand.NextFloat(-.3f, .3f)) * 0.98f;
				p.Color = Main.DiscoColor;
			}));
	}

	//Lava: Rise then fall
	private static void LavaFloorParticles(Player player)
	{
		Vector2 start = player.BottomLeft + new Vector2(Main.rand.Next(player.width), 0);
		Vector2 velocity = new Vector2(0, -1).RotatedByRandom(MathHelper.PiOver2) * Main.rand.NextFloat(.25f, .5f);

		ParticleHandler.SpawnParticle(new GlowParticle(start, velocity,
			new Color(252, 90, 3) * 0.85f, Main.rand.NextFloat(0.25f, 0.45f), 90, 4, p =>
			{
				p.Velocity.Y += Main.rand.NextFloat(.02f, .03f);
			}));
	}

	//Radon: Straight upwards in random direction, pulsates and lasts longer than all other mosses
	private static void RadonFloorParticles(Player player)
	{
		Vector2 start = player.BottomLeft + new Vector2(Main.rand.Next(player.width), 0);
		Vector2 velocity = new Vector2(Main.rand.NextFloat(-.5f, .5f), Main.rand.NextFloat(-1.2f, -0.3f));

		ParticleHandler.SpawnParticle(new GlowParticle(start, velocity,
			new Color(248, 255, 56) * 0.5f, Main.rand.NextFloat(0.25f, 0.45f), 260, 8, p =>
			{
				p.Velocity *= .98f;
				p.Scale = 0.25f + 0.15f * (float)Math.Sin(p.TimeActive * 0.05f);

			}));
	}

	//Oganesson: like Xenon, but lasts much shorter, up faster, and splits into 2 smaller ones
	private static void OganessonFloorParticles(Player player)
	{
		Vector2 start = player.BottomLeft + new Vector2(Main.rand.Next(player.width), 0);
		Vector2 velocity = new Vector2(0f, Main.rand.NextFloat(-2f, -1f));

		ParticleHandler.SpawnParticle(new GlowParticle(start, velocity,
			new Color(255, 255, 255) * 0.75f, Main.rand.NextFloat(0.25f, 0.45f), 60, 8, p =>
			{
				p.Velocity.Y *= Main.rand.NextFloat(.96f, .99f);
				if (p.TimeActive == 50)
					for (int i = 0; i < 2; i++)
						ParticleHandler.SpawnParticle(new GlowParticle(p.Position, new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f)), new Color(255, 255, 255) * 0.25f, Main.rand.NextFloat(0.25f, 0.3f), 30, 8));
			}));
	}
	#endregion
}

public class FloatingMoss(int style, Color color, Color outlineColor) : ABasicParticle
{
	public const int FRAME_COUNT = 4;
	public static readonly Asset<Texture2D> Texture = DrawHelpers.RequestLocal<FloatingMoss>("FloatingMoss", false);

	public Rectangle Hitbox => new((int)LocalPosition.X - 2, (int)LocalPosition.Y - 2, 4, 4);

	public Color color = color;
	public Color outlineColor = outlineColor;
	public bool drawOutline;

	protected readonly int _style = style;
	protected int _timeActive;
	protected float _opacity;

	public override void Update(ref ParticleRendererSettings settings)
	{
		const int time_left = 2000;
		const int fade_out_time = 20;

		bool fadeIn;
		if (Collision.WetCollision(Hitbox.TopLeft(), Hitbox.Width, Hitbox.Height))
		{
			Velocity.Y = Math.Max(Velocity.Y - 0.01f, -1); //Float
			fadeIn = false;
		}
		else if (Collision.WetCollision(Hitbox.TopLeft(), Hitbox.Width, Hitbox.Height + 2))
		{
			Velocity.Y *= 0.7f; //Settle at the top of liquid
			fadeIn = true;
		}
		else
		{
			Velocity.Y += 0.02f; //Sink outside of liquid
			fadeIn = true;
		}

		if (_timeActive % 60 == 0) //Randomly set acceleration at intervals
			AccelerationPerFrame = new Vector2(Main.rand.NextFloat(-0.001f, 0.001f), 0);

		Velocity.X = MathHelper.Clamp(Velocity.X, -0.05f, 0.05f); //Limit maximum horizontal velocity

		if (fadeIn)
			_opacity = Math.Min(_opacity + 0.03f, 1);
		else
			_opacity = Math.Max(_opacity - 0.03f, 0);

		if (Collision.SolidCollision(Hitbox.TopLeft(), Hitbox.Width, Hitbox.Height) && _timeActive < time_left - fade_out_time)
			_timeActive = time_left - fade_out_time; //Fade out on collision with a solid tile

		if (++_timeActive >= time_left)
			ShouldBeRemovedFromRenderer = true;

		if (_timeActive > time_left - fade_out_time)
			Scale *= 1f - 1f / fade_out_time;

		base.Update(ref settings);
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
	{
		Texture2D texture = Texture.Value;
		Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
		Rectangle source = texture.Frame(FRAME_COUNT, 2, _style, drawOutline ? 1 : 0, 0, -2);
		Color variableColor = color.Additive(200);

		if (_opacity > 0)
		{
			Lighting.AddLight(LocalPosition, color.ToVector3() * 0.3f * _opacity);

			Point16 coords = LocalPosition.ToTileCoordinates16();
			MossAmbience.SetChunk(coords.X, coords.Y, color); //Add a color chunk
		}

		if (drawOutline)
			variableColor = outlineColor;

		spritebatch.Draw(bloom, LocalPosition + settings.AnchorPosition - Main.screenPosition, null, color.Additive() * _opacity * 0.1f, Rotation, bloom.Size() / 2, Scale * 0.2f, default, 0);
		spritebatch.Draw(texture, LocalPosition + settings.AnchorPosition - Main.screenPosition, source, variableColor * _opacity, Rotation, source.Size() / 2, Scale, default, 0);
	}
}