using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WorldGeneration;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underground.Tiles;

/// <summary> Mimics vanilla pots. </summary>
public class StackablePots : ModTile
{
	/// <summary> Tile coordinates to offset. </summary>
	[WorldBound]
	private static readonly Dictionary<Point16, Point16> Offsets = [];
	public override string Texture => Pots.PotTexture;

	/// <summary> Returns final coordinates from <see cref="TileExtensions.GetTopLeft"/>. </summary>
	private static Point16 Get(int i, int j)
	{
		TileExtensions.GetTopLeft(ref i, ref j);
		return new Point16(i, j);
	}

	public override void SetStaticDefaults()
	{
		const int row = 3;

		Main.tileSolid[Type] = false;
		Main.tileBlockLight[Type] = false;
		Main.tileCut[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileSpelunker[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.AlternateTile, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.AnchorAlternateTiles = [Type];
		TileObjectData.newTile.StyleWrapLimit = row;
		TileObjectData.newTile.RandomStyleRange = row;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.UsesCustomCanPlace = true;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(146, 76, 77), Language.GetText(Pots.NameKey));
		DustType = -1;
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		const int maxStackHeight = 2;
		Point offset = Point.Zero;

		for (int a = 0; a < maxStackHeight; a++)
		{
			if (Stacked(i, j, a))
			{
				offset.Y += 8;
				if (a == maxStackHeight - 1) //Max height exceded
				{
					WorldGen.KillTile(i, j);
					return false;
				}
			}
		}

		if (Framing.GetTileSafely(i + 2, j).TileType == Type)
			offset.X += 4;

		if (Framing.GetTileSafely(i - 2, j).TileType == Type)
			offset.X -= 4;

		if (offset != Point.Zero)
		{
			var pt = Get(i, j);

			Offsets.Remove(pt);
			Offsets.Add(pt, new Point16(offset.X, offset.Y));
		}

		return true;

		bool Stacked(int x, int y, int length) => Framing.GetTileSafely(x, y + 2 * (length + 1)).TileType == Type;
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		int x = frameX / (18 * 2);
		int y = frameY / (18 * 2);

		int style = x % 3 + y * 3;
		var velocity = (Vector2.UnitY * -Main.rand.NextFloat(3f, 5f)).RotatedByRandom(1f);

		Projectile.NewProjectile(new EntitySource_TileBreak(i, j), new Vector2(i, j).ToWorldCoordinates(16, 16), velocity, ModContent.ProjectileType<FallingPot>(), 10, 0, ai0: style);

		Offsets.Remove(new Point16(i, j));
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		var tile = Main.tile[i, j];
		var texture = TextureAssets.Tile[Type].Value;
		var source = new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 16);

		var zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
		var position = new Vector2(i, j) * 16 - Main.screenPosition + zero + new Vector2(0, 2);

		Offsets.TryGetValue(Get(i, j), out var offset);
		position += offset.ToVector2(); //Handle stack offset

		var color = Lighting.GetColor(i, j);

		if (Main.LocalPlayer.findTreasure)
			color = TileExtensions.GetSpelunkerTint(color);

		spriteBatch.Draw(texture, position, source, color, 0f, new Vector2(0, 0), 1f, SpriteEffects.None, 0f);
		return false;
	}
}

internal class FallingPot : ModProjectile
{
	public ref float Style => ref Projectile.ai[0];

	public override LocalizedText DisplayName => Language.GetText(Pots.NameKey);
	public override string Texture => Pots.PotTexture;

	public override void SetDefaults()
	{
		Projectile.Size = new Vector2(16);
		Projectile.friendly = true;
		Projectile.hostile = true;
	}

	public override void AI()
	{
		Projectile.velocity.Y += .3f;
		Projectile.rotation += Projectile.velocity.Length() * .05f;
	}

	public override bool? CanCutTiles() => false;

	public override void OnKill(int timeLeft)
	{
		SoundEngine.PlaySound(SoundID.Shatter with { PitchVariance = .5f, MaxInstances = -1 }, Projectile.Center);

		for (int i = 0; i < 10; i++)
		{
			var velocity = new Vector2(0, -Main.rand.NextFloat(2f)).RotatedByRandom(.25f);
			Dust.NewDustDirect(Projectile.BottomLeft, Projectile.width, 4, DustID.Pot, velocity.X, velocity.Y, Scale: Main.rand.NextFloat() + .25f);
		}

		ParticleHandler.SpawnParticle(new Particles.SmokeCloud(Projectile.Bottom, Vector2.UnitY * -.5f, Color.SandyBrown * .5f, .15f, Common.Easing.EaseFunction.EaseQuarticInOut, 150));

		var pos = Projectile.Center.ToTileCoordinates();
		BreakPot(pos.X, pos.Y);
	}

	/// <summary> Mimics pot break effects. </summary>
	private void BreakPot(int i, int j)
	{
		var t = Framing.GetTileSafely(i, j);

		short oldX = t.TileFrameX;
		short oldY = t.TileFrameY;

		t.TileFrameX = (short)(Style % 3 * 36);
		t.TileFrameY = (short)(Style / 3 * 36);

		WorldGen.CheckPot(i, j); //Trick this method by modifying tile frame to match the correct pot type

		t.TileFrameX = oldX;
		t.TileFrameY = oldY;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Unify4x4Sheet(Projectile.Center, Projectile.GetAlpha(lightColor), TileID.Pots, new Point(6, 74), (int)Style, Projectile.rotation, Projectile.scale);
		return false;
	}

	public static void Unify4x4Sheet(Vector2 center, Color color, int tileType, Point numFrames, int styleX = 0, float rotation = 0, float scale = 1)
	{
		const int tileFrame = 18;

		int framesX = numFrames.X;
		int framesY = numFrames.Y;

		var texture = TextureAssets.Tile[tileType].Value;
		var source = texture.Frame(framesX, framesY, styleX % (framesX / 2) * 2, styleX / (framesX / 2) * 2); //All tile frames

		for (int i = 0; i < 4; i++)
		{
			var newSource = source with { X = source.X + i % 2 * tileFrame, Y = source.Y + i / 2 * tileFrame };
			var origin = i switch
			{
				1 => new Vector2(0, 16),
				2 => new Vector2(16, 0),
				3 => new Vector2(0, 0),
				_ => new Vector2(16, 16)
			};

			Main.EntitySpriteDraw(texture, center - Main.screenPosition, newSource, color, rotation, origin, scale, default);
		}
	}
}