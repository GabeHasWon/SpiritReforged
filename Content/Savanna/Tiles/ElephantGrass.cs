using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.Conversion;
using SpiritReforged.Common.TileCommon.TileSway;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Savanna.Tiles;

[DrawOrder(DrawOrderAttribute.Layer.NonSolid, DrawOrderAttribute.Layer.OverPlayers)]
public class ElephantGrass : ModTile, ICutAttempt, ISetConversion
{
	public const int FullFrameHeight = 54;
	public const int Styles = 8;

	public ConversionHandler.Set ConversionSet => new()
	{
		{ ModContent.TileType<SavannaGrassCorrupt>(), ModContent.TileType<ElephantGrassCorrupt>() },
		{ ModContent.TileType<SavannaGrassCrimson>(), ModContent.TileType<ElephantGrassCrimson>() },
		{ ModContent.TileType<SavannaGrassHallow>(), ModContent.TileType<ElephantGrassHallow>() },
		{ ModContent.TileType<SavannaGrass>(), ModContent.TileType<ElephantGrass>() }
	};

	public static readonly SoundStyle[] SwishSounds = 
		[new("SpiritReforged/Assets/SFX/Tile/SavannaGrass1"), 
		new("SpiritReforged/Assets/SFX/Tile/SavannaGrass2"),
		new("SpiritReforged/Assets/SFX/Tile/SavannaGrass3")];

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

		DustType = DustID.JungleGrass;
		HitSound = SoundID.Grass;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 1, 0);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 5;

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.RandomStyleRange = 3;

		PreAddObjectData();

		TileObjectData.addAlternate(5);
		TileObjectData.addTile(Type);
	}

	public virtual void PreAddObjectData()
	{
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaGrass>()];
		AddMapEntry(new(104, 156, 70));
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

			var style = Main.rand.NextFromList(SwishSounds) with { MaxInstances = -1, PitchVariance = 0.2f, Pitch = pitch, Volume = volume };
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

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		if (ConversionHandler.FindSet(nameof(ElephantGrass), Framing.GetTileSafely(i, j + 1).TileType, out int newType) && Type != newType)
			WorldGen.ConvertTile(i, j, newType);

		return true;
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

		var source = new Rectangle(frame.X, frame.Y, 16, 16);
		var effects = (i % 3 == 0) ? default : SpriteEffects.FlipHorizontally;
		Vector2 position = new Vector2(i, j) * 16 - Main.screenPosition + offset + new Vector2(0, 2);

		spriteBatch.Draw(texture, position, source, color, rotation, origin, 1, effects, 0f);
	}

	public virtual void DrawBack(int i, int j, SpriteBatch spriteBatch, Vector2 offset, float rotation, Vector2 origin, Point frame)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out var color, out var texture))
			return;

		var source = new Rectangle(frame.X, frame.Y + FullFrameHeight, 16, 16);
		var effects = (i % 3 == 0) ? SpriteEffects.FlipHorizontally : default;
		Vector2 position = new Vector2(i, j) * 16 - Main.screenPosition + offset + new Vector2(0, 2);

		spriteBatch.Draw(texture, position, source, color, rotation * 0.5f, origin, 1, effects, 0f);
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
	public override void PreAddObjectData()
	{
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaGrassCorrupt>()];

		TileID.Sets.AddCorruptionTile(Type);
		TileID.Sets.Corrupt[Type] = true;

		AddMapEntry(new(109, 106, 174));
		DustType = DustID.Corruption;
	}
}

[DrawOrder(DrawOrderAttribute.Layer.NonSolid, DrawOrderAttribute.Layer.OverPlayers)]
public class ElephantGrassCrimson : ElephantGrass
{
	public override void PreAddObjectData()
	{
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaGrassCrimson>()];

		TileID.Sets.AddCrimsonTile(Type);
		TileID.Sets.Crimson[Type] = true;

		AddMapEntry(new(183, 69, 68));
		DustType = DustID.CrimsonPlants;
	}
}

[DrawOrder(DrawOrderAttribute.Layer.NonSolid, DrawOrderAttribute.Layer.OverPlayers)]
public class ElephantGrassHallow : ElephantGrass
{
	public override void PreAddObjectData()
	{
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaGrassHallow>()];

		TileID.Sets.Hallow[Type] = true;
		TileID.Sets.HallowBiome[Type] = 1;

		AddMapEntry(new(78, 193, 227));
		DustType = DustID.HallowedPlants;
	}
}
