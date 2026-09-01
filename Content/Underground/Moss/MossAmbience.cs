using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Moss.Oganesson;
using SpiritReforged.Content.Underground.Moss.Radon;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Content.Underground.Moss;

[Autoload(Side = ModSide.Client)]
public sealed class MossAmbience : GlobalTile
{
	public static readonly ParticleRenderer OverPlayers = new();
	public static readonly Dictionary<int, Color> ColorByMoss = [];

	public static Color LerpedWaterTint { get; private set; }
	public static Color ActiveWaterTint { get; private set; }

	private static bool TryFindColorChunk(int x, int y, int depth, out Color topColor, out Color bottomColor)
	{
		bool success = false;
		int iDepth = 0;

		for (int i = 0; i <= depth; i++)
		{
			if (i == 0 && WorldGen.SolidOrSlopedTile(x, y))
			{

			}
			else if (Main.tile[x, y].LiquidAmount < 255)
			{
				success = true;
				iDepth = i;
				break;
			}

			y--;
		}

		if (success)
		{
			topColor = Color.Lerp(LerpedWaterTint, Color.Transparent, (iDepth - 1f) / depth);
			bottomColor = Color.Lerp(LerpedWaterTint, Color.Transparent, iDepth / (float)depth);

			return true;
		}

		topColor = bottomColor = default;
		return false;
	}

	public override void Load()
	{
		On_Main.UpdateParticleSystems += UpdateVisuals;
		On_Main.DrawInfernoRings += DrawAbovePlayer;
		On_SceneMetrics.ScanAndExportToMain += ExportWaterTint;

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

	private static void UpdateVisuals(On_Main.orig_UpdateParticleSystems orig, Main self)
	{
		OverPlayers.Update();
		LerpedWaterTint = Color.Lerp(LerpedWaterTint, ActiveWaterTint, 0.05f); //Lerp to the new color

		orig(self);
	}

	public override void NearbyEffects(int i, int j, int type, bool closer)
	{
		if (!closer || Main.gamePaused || !NeonMossScene.InNeonMoss)
			return;

		Tile tileAbove = Main.tile[i, j - 1];
		if (!WorldGen.SolidTile(tileAbove) && tileAbove.LiquidAmount > 0 && tileAbove.WallType == WallID.None && Main.rand.NextBool(180))
		{
			if (ColorByMoss.TryGetValue(type, out _)) //Check if this is a registered moss tile
			{
				Vector2 position = new Vector2(i, j - 1).ToWorldCoordinates(Main.rand.NextFloat(16), 8);
				OverPlayers.Add(new FloatingMoss(Main.rand.Next(FloatingMoss.FRAME_COUNT))
				{
					LocalPosition = position,
					Scale = Vector2.One * 0.8f,
					Rotation = Main.rand.NextFloat(MathHelper.Pi)
				});
			}
		}

		if (Main.rand.NextBool(2400) && ColorByMoss.TryGetValue(type, out Color floatingColor))
			SpawnAirParticles(i, j, floatingColor);
	}

	private static void DrawAbovePlayer(On_Main.orig_DrawInfernoRings orig, Main self)
	{
		OverPlayers.Draw(Main.spriteBatch);
		orig(self);
	}

	private static void ExportWaterTint(On_SceneMetrics.orig_ScanAndExportToMain orig, SceneMetrics self, SceneMetricsScanSettings settings)
	{
		orig(self, settings);

		if (NeonMossScene.InNeonMoss)
		{
			SceneTileCounter.Survey survey = SceneTileCounter.GetSurvey<NeonMossScene>();
			var counts = survey.countByType;
			int highestCount = 0;
			int highestType = 0;

			foreach (int type in counts.Keys)
			{
				if (counts[type] > highestCount)
				{
					highestCount = counts[type];
					highestType = type; //Find the most prominent moss type
				}
			}

			if (ColorByMoss.TryGetValue(highestType, out Color sampleColor))
				ActiveWaterTint = sampleColor;
		}
		else
		{
			ActiveWaterTint = Color.Transparent;
		}
	}

	private static bool ApplyMossWaterAlpha(int x, int y, ref VertexColors colors, bool isPartial)
	{
		if (NeonMossScene.InNeonMoss && TryFindColorChunk(x, y, 2, out Color topColor, out Color bottomColor))
		{
			(topColor, bottomColor) = (topColor.Additive(), bottomColor.Additive());

			ClampColor(ref colors.TopLeftColor, topColor, x, y);
			ClampColor(ref colors.TopRightColor, topColor, x + 1, y);
			ClampColor(ref colors.BottomLeftColor, bottomColor, x, y + 1);
			ClampColor(ref colors.BottomRightColor, bottomColor, x + 1, y + 1);

			return false;
		}

		return true;

		static void ClampColor(ref Color color, Color tint, int x, int y)
		{
			tint *= Lighting.Brightness(x, y) * 4;
			(color.R, color.G, color.B) = ((byte)Math.Min(color.R + tint.R, 255), (byte)Math.Min(color.G + tint.G, 255), (byte)Math.Min(color.B + tint.B, 255));
		}
	}

	private static void SpawnAirParticles(int i, int j, Color mossColor)
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

public class FloatingMoss(int style) : ABasicParticle
{
	public const int FRAME_COUNT = 7;
	public static readonly Asset<Texture2D> Texture = DrawHelpers.RequestLocal<FloatingMoss>("FloatingMoss", false);

	public Rectangle Hitbox => new((int)LocalPosition.X - 2, (int)LocalPosition.Y - 2, 4, 4);

	public bool drawOutline;

	protected readonly int _style = style;
	protected int _timeActive;
	protected float _opacity;

	public override void Update(ref ParticleRendererSettings settings)
	{
		const int time_left = 2000;
		const int death_fade_out = 20;
		const int fade_out = 57;

		if (Collision.SolidCollision(Hitbox.TopLeft(), Hitbox.Width, Hitbox.Height) && _timeActive < time_left - death_fade_out)
		{
			//_timeActive = time_left - fade_out_time; //Fade out on collision with a solid tile
			Velocity = Vector2.Zero;
		}

		bool fadeIn;
		if (Collision.WetCollision(Hitbox.TopLeft(), Hitbox.Width, Hitbox.Height))
		{
			Velocity.Y = Math.Max(Velocity.Y - 0.1f, -5); //Float
			fadeIn = false;
		}
		else if (Collision.WetCollision(Hitbox.TopLeft(), Hitbox.Width, Hitbox.Height + 6))
		{
			Velocity.Y *= 0.5f; //Settle at the top of liquid
			fadeIn = true;

			if (Main.rand.NextBool(150))
				Velocity.Y += 0.1f;
		}
		else
		{
			Velocity.Y = Math.Min(Velocity.Y + 0.1f, 5); //Sink outside of liquid
			fadeIn = false;
		}

		if (Main.LocalPlayer.Hitbox.Intersects(Hitbox))
			Velocity += Main.LocalPlayer.velocity * 0.05f;

		Velocity.X *= 0.97f;

		if (fadeIn)
			_opacity = Math.Min(_opacity + 1f / fade_out, 1);
		else
			_opacity = Math.Max(_opacity - 1f / fade_out, 0);

		if (_timeActive > time_left - death_fade_out)
			Scale *= 1f - 1f / death_fade_out;

		if (++_timeActive >= time_left)
			ShouldBeRemovedFromRenderer = true;

		base.Update(ref settings);
	}

	public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
	{
		Color variableColor = MossAmbience.LerpedWaterTint;
		Texture2D texture = Texture.Value;
		Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;
		Rectangle source = texture.Frame(FRAME_COUNT, 1, _style, 0, -2, 0);
		Vector2 position = LocalPosition + settings.AnchorPosition - Main.screenPosition + new Vector2(0, 2);

		Vector3 hsl = Main.rgbToHsl(variableColor);

		if (MossAmbience.ActiveWaterTint != Color.White)
			(hsl.Y, hsl.Z) = (1, 0.5f);

		variableColor = Main.hslToRgb(hsl);

		spritebatch.Draw(texture, position, source, variableColor * _opacity, Rotation, source.Size() / 2, Scale, default, 0);
		spritebatch.Draw(bloom, position, null, variableColor.Additive() * _opacity * 0.1f, Rotation, bloom.Size() / 2, Scale * 0.2f, default, 0);
		Lighting.AddLight(LocalPosition, MossAmbience.LerpedWaterTint.ToVector3() * 0.3f * _opacity);
	}
}