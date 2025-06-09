using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.Corruption;
using SpiritReforged.Common.TileCommon.TileSway;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Savanna.Tiles;

[DrawOrder(DrawOrderAttribute.Layer.NonSolid, DrawOrderAttribute.Layer.OverPlayers)]
public class ElephantGrass : ModTile, IConvertibleTile, ICutAttempt
{
	protected virtual Color SubColor => Color.Goldenrod;

	/// <returns> Whether this <see cref="ElephantGrass"/> tile uses its short alternate style. </returns>
	public static bool IsShortgrass(int i, int j) => TileObjectData.GetTileStyle(Main.tile[i, j]) > 4;

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileMergeDirt[Type] = false;
		Main.tileBlockLight[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoFail[Type] = true;
		Main.tileCut[Type] = true;

		TileID.Sets.BreakableWhenPlacing[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 1, 0);
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaGrass>()];
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 5;

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.RandomStyleRange = 3;

		PreAddObjectData();
		TileObjectData.addAlternate(5);
		TileObjectData.addTile(Type);

		HitSound = SoundID.Grass;
	}

	public virtual void PreAddObjectData()
	{
		AddMapEntry(new(104, 156, 70));
		DustType = DustID.JungleGrass;
	}

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		if (Main.player[Player.FindClosest(new Vector2(i, j).ToWorldCoordinates(0, 0), 16, 16)].HeldItem.type == ItemID.Sickle)
			yield return new Item(ItemID.Hay, Main.rand.Next(3, 6));

		if (Main.player[Player.FindClosest(new Vector2(i, j).ToWorldCoordinates(0, 0), 16, 16)].HasItem(ItemID.Blowpipe))
			yield return new Item(ItemID.Seed, Main.rand.Next(1, 3));
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;

	public override void NearbyEffects(int i, int j, bool closer) //Play sounds when walking inside of grass patches
	{
		const string path = "SpiritReforged/Assets/SFX/Tile/SavannaGrass";

		if (Main.gamePaused)
			return;

		float length = Main.LocalPlayer.velocity.Length();
		float mag = MathHelper.Clamp(length / 10f, 0, 1);
		float chance = 1f - mag;

		if (Main.rand.NextFloat(chance) < .1f)
		{
			if (Main.LocalPlayer.velocity.Length() < 2 || !new Rectangle(i * 16, j * 16, 16, 16).Intersects(Main.LocalPlayer.getRect()))
				return;

			float pitch = 0;
			float volume = MathHelper.Lerp(.25f, .75f, mag);

			if (IsShortgrass(i, j))
			{
				volume -= .25f;
				pitch += .4f;
			}

			var style = new SoundStyle(path + Main.rand.Next(1, 4)) with { MaxInstances = -1, PitchVariance = .2f, Pitch = pitch, Volume = volume };
			SoundEngine.PlaySound(style, Main.LocalPlayer.Center);
		}
	}

	public override void RandomUpdate(int i, int j) //Grow up; spreading happens in SavannaGrass.RandomUpdate
	{
		if (!Main.rand.NextBool() || !IsShortgrass(i, j))
			return;

		if (GrassSurrounding())
		{
			Main.tile[i, j].TileFrameX = (short)(Main.rand.Next(5) * 18);
			NetMessage.SendTileSquare(-1, i, j, 1, 1);
		}

		bool GrassSurrounding() => Framing.GetTileSafely(i - 1, j).TileType == Type && Framing.GetTileSafely(i + 1, j).TileType == Type;
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch) //Imitates wind sway drawing but with unique dimensions
	{
		const int height = 3; //Pseudo tile height

		if (!TileExtensions.GetVisualInfo(i, j, out _, out _))
			return false;

		var t = Main.tile[i, j];
		float physics = Physics(new Point16(i, j - (height - 1)));

		for (int y = 0; y < 3; y++)
		{
			float swing = 1f - (float)(y + 1) / height + .5f;
			float rotation = physics * swing * .1f;

			var rotationOffset = new Vector2(0, Math.Abs(rotation) * 20f);
			var drawOrigin = new Vector2(8, (height - y) * 16);

			var frame = new Point(t.TileFrameX, t.TileFrameY + y * 18);
			var offset = drawOrigin + rotationOffset + new Vector2(0, y * 16 - 32);

			if (DrawOrderSystem.Order == DrawOrderAttribute.Layer.OverPlayers)
				DrawFront(i, j, spriteBatch, offset, rotation, drawOrigin, frame);
			else
				DrawBack(i, j, spriteBatch, offset, rotation, drawOrigin, frame);
		}

		return false;

		static float Physics(Point16 topLeft)
		{
			float rotation = Main.instance.TilesRenderer.GetWindCycle(topLeft.X, topLeft.Y, ModContent.GetInstance<TileSwaySystem>().GrassWindCounter * 2.25f);
			if (!WorldGen.InAPlaceWithWind(topLeft.X, topLeft.Y, 1, height))
				rotation = 0f;

			return (rotation + TileSwayHelper.GetHighestWindGridPushComplex(topLeft.X, topLeft.Y, 1, height, 20, 3f, 1, true)) * 1.9f;
		}
	}

	public virtual void DrawFront(int i, int j, SpriteBatch spriteBatch, Vector2 offset, float rotation, Vector2 origin, Point frame)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out var color, out var texture))
			return;

		float seed = (frame.X / 18f + i) * .8f % 1f;

		offset += new Vector2(MathHelper.Lerp(-2, 2, seed), 2);
		var source = new Rectangle(frame.X, frame.Y, 16, 16);
		var effects = ((int)(seed * 2) % 2 == 0) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		spriteBatch.Draw(texture, new Vector2(i, j) * 16 - Main.screenPosition + offset, source, color, rotation, origin, 1, effects, 0f);
	}

	public virtual void DrawBack(int i, int j, SpriteBatch spriteBatch, Vector2 offset, float rotation, Vector2 origin, Point frame)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out var color, out var texture))
			return;

		var data = Main.tile[i, j].SafelyGetData();
		int amount = i % 2 + 1;

		for (int x = 0; x < amount; x++)
		{
			float seed = (frame.X / 18f + i + x * .25f) * 1.3f % 1f;

			rotation += seed * .25f;
			offset += new Vector2(MathHelper.Lerp(-2, 2, seed), 2);

			int baseOffset = IsShortgrass(i, j) ? 5 : 0;
			int frameX = (baseOffset + (int)MathHelper.Lerp(0, data.RandomStyleRange, seed)) * 18;

			var source = new Rectangle(frameX, frame.Y, 16, 16);
			var effects = ((int)(seed * 2) % 2 == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			var subColor = Color.Lerp(Color.White, SubColor, .9f + (float)Math.Sin(i / 8f) * .3f);

			spriteBatch.Draw(texture, new Vector2(i, j) * 16 - Main.screenPosition + offset, source, color.MultiplyRGB(subColor), rotation * .4f, origin, 1, effects, 0f);
		}
	}

	public bool Convert(IEntitySource source, ConversionType type, int i, int j)
	{
		if (source is EntitySource_Parent { Entity: Projectile })
			return false;

		var tile = Main.tile[i, j];

		tile.TileType = (ushort)(type switch
		{
			ConversionType.Hallow => ModContent.TileType<ElephantGrassHallow>(),
			ConversionType.Crimson => ModContent.TileType<ElephantGrassCrimson>(),
			ConversionType.Corrupt => ModContent.TileType<ElephantGrassCorrupt>(),
			_ => ModContent.TileType<ElephantGrass>(),
		});

		return true;
	}

	public bool OnCutAttempt(int i, int j)
	{
		var p = Main.player[Player.FindClosest(new Vector2(i, j) * 16, 16, 16)];
		return p.HeldItem.type is ItemID.Sickle or ItemID.LawnMower; //Only allow this tile to be cut using a sickle or lawnmower
	}
}

[DrawOrder(DrawOrderAttribute.Layer.NonSolid, DrawOrderAttribute.Layer.OverPlayers)]
public class ElephantGrassCorrupt : ElephantGrass
{
	protected override Color SubColor => Color.Gray;

	public override void PreAddObjectData()
	{
		TileID.Sets.AddCorruptionTile(Type);
		TileID.Sets.Corrupt[Type] = true;

		AddMapEntry(new(109, 106, 174));

		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaGrassCorrupt>()];
		DustType = DustID.Corruption;
	}
}

[DrawOrder(DrawOrderAttribute.Layer.NonSolid, DrawOrderAttribute.Layer.OverPlayers)]
public class ElephantGrassCrimson : ElephantGrass
{
	protected override Color SubColor => new(190, 165, 0);

	public override void PreAddObjectData()
	{
		TileID.Sets.AddCrimsonTile(Type);
		TileID.Sets.Crimson[Type] = true;

		AddMapEntry(new(183, 69, 68));

		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaGrassCrimson>()];
		DustType = DustID.CrimsonPlants;
	}
}

[DrawOrder(DrawOrderAttribute.Layer.NonSolid, DrawOrderAttribute.Layer.OverPlayers)]
public class ElephantGrassHallow : ElephantGrass
{
	protected override Color SubColor => Color.LightPink;

	public override void PreAddObjectData()
	{
		TileID.Sets.Hallow[Type] = true;
		TileID.Sets.HallowBiome[Type] = 1;

		AddMapEntry(new(78, 193, 227));

		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaGrassHallow>()];
		DustType = DustID.HallowedPlants;
	}
}
