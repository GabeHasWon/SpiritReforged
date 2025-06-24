using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.Conversion;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Content.Ocean.Tiles;

[DrawOrder(DrawOrderAttribute.Layer.NonSolid, DrawOrderAttribute.Layer.OverPlayers)]
public class OceanKelp : ModTile
{
	private const int ClumpX = 92;

	public static readonly Dictionary<int, int> Conversions = new()
	{
		{ TileID.Ebonsand, ModContent.TileType<OceanKelpCorrupt>() },
		{ TileID.Crimsand, ModContent.TileType<OceanKelpCrimson>() },
		{ TileID.Pearlsand, ModContent.TileType<OceanKelpHallowed>() },
		{ TileID.Sand, ModContent.TileType<OceanKelp>() },
	};

	private readonly static int[] ClumpOffsets = [0, -8, 8];

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileBlockLight[Type] = false;
		Main.tileFrameImportant[Type] = true;

		TileID.Sets.NotReallySolid[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.WaterPlacement = LiquidPlacement.OnlyInFullLiquid;
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.AlternateTile, 1, 0);

		TileObjectData.newTile.AnchorValidTiles = [TileID.Sand, TileID.Ebonsand, TileID.Crimsand, TileID.Pearlsand];
		TileObjectData.newTile.AnchorAlternateTiles = [Type];

		PreAddObjectData();
		TileObjectData.addTile(Type);

		RegisterItemDrop(ModContent.ItemType<Items.Kelp>());
		HitSound = SoundID.Grass;
	}

	public virtual void PreAddObjectData()
	{
		AddMapEntry(new(104, 156, 70));
		DustType = DustID.Grass;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		var drops = base.GetItemDrops(i, j);

		if (Main.rand.NextBool(100))
			drops = [new Item(ItemID.LimeKelp)];

		return drops;
	}

	public override void PostTileFrame(int i, int j, int up, int down, int left, int right, int upLeft, int upRight, int downLeft, int downRight)
	{
		Tile tile = Main.tile[i, j];
		Tile above = Main.tile[i, j - 1];
		Tile below = Main.tile[i, j + 1];

		// Gets the group frame (left or right) for the given tile
		short frameX = GetGroupFrameX(i, j);

		tile.TileFrameX = frameX;
		int oldFrameY = tile.TileFrameY;

		// Sets the tile as a clump randomly and if the conditions are right (tile above, no clumps below nearby)
		if (Main.rand.NextBool(12) && above.HasTile && above.TileType == Type && CanPlaceClump(i, j))
		{
			tile.TileFrameX = ClumpX;
			tile.TileFrameY = (short)Main.rand.Next(4);
			return;
		}

		SetFrameY(tile, above, below, Type);

		// Set to the same clump status as the old frame
		tile.TileFrameY += (short)(GetClumpNumber(oldFrameY) * 198);
	}

	private bool CanPlaceClump(int i, int j)
	{
		int y = j;

		while (Main.tile[i, y].HasTile && Main.tile[i, y].TileType == Type && Main.tile[i, y].TileFrameX != ClumpX)
		{
			y++;
		}

		y--;

		return y - j > 2;
	}

	/// <summary>
	/// Gets the "group" of the kelp's framing.<br/>
	/// The spritesheet is split into two groups - A and B, left and right - giving much needed variety to the sprite.<br/>
	/// This is used for normal framing and for layering clumps.
	/// </summary>
	/// <returns>The frame X of the current kelp.</returns>
	private static short GetGroupFrameX(int i, int j)
	{
		bool aGroup = (i + j) % 2 == 0;
		short frameX = (short)(aGroup ? 2 : 48);
		return frameX;
	}

	/// <summary>
	/// Sets the given tile's frame Y based on the conditions around it.
	/// </summary>
	public static void SetFrameY(Tile tile, Tile above, Tile below, int type)
	{
		if (above.TileType != type) // Prioritize using the top texture if the tile above isn't kelp
		{
			if (tile.TileFrameY >= 36 && tile.TileFrameY <= 54)
				return;

			tile.TileFrameY = (short)(Main.rand.Next(2) * 18);
			return;
		}

		if (below.TileType != type) // If the tile above is kelp, use bottom texture
		{
			tile.TileFrameY = (short)((Main.rand.Next(2) + 9) * 18);
			return;
		}

		tile.TileFrameY = (short)((Main.rand.Next(5) + 4) * 18);
	}

	public override void RandomUpdate(int i, int j)
	{
		Tile tile = Main.tile[i, j];

		if (tile.TileFrameY <= 18 && Main.rand.NextBool(10)) // Grow cut tops
		{
			tile.TileFrameY += 36;

			if (Main.netMode == NetmodeID.MultiplayerClient)
				NetMessage.SendTileSquare(-1, i, j);
		}
		else if (tile.TileFrameY > 18 && Main.rand.NextBool(35) && !Main.tile[i, j - 1].HasTile) // Grow kelp itself
		{
			WorldGen.PlaceTile(i, j - 1, Type, true);

			if (Main.netMode == NetmodeID.MultiplayerClient)
				NetMessage.SendTileSquare(-1, i, j - 1);
		}

		if (Main.rand.NextBool(40)) // Adds an additional "clump" to give depth & add a back layer
		{
			bool canAddClump = Main.tile[i, j + 1].TileType != Type || GetClumpNumber(i, j + 1) != GetClumpNumber(i, j);

			if (canAddClump && tile.TileFrameY < 198 * 2)
			{
				tile.TileFrameY += 198;

				if (Main.netMode == NetmodeID.MultiplayerClient)
					NetMessage.SendTileSquare(-1, i, j);
			}
		}
	}

	public static int GetClumpNumber(int i, int j) => GetClumpNumber(Main.tile[i, j]);
	public static int GetClumpNumber(Tile tile) => GetClumpNumber(tile.TileFrameY);
	public static int GetClumpNumber(int frameY) => (int)(frameY / 198f);

	public override void NearbyEffects(int i, int j, bool closer) //Dust effects
	{
		if (Main.rand.Next(1000) <= 1) //Spawns little bubbles
		{
			Vector2 pos = new Vector2(i * 16, j * 16) + new Vector2(2 + Main.rand.Next(12), Main.rand.Next(16));
			Dust.NewDustPerfect(pos, 34, new Vector2(Main.rand.NextFloat(-0.08f, 0.08f), Main.rand.NextFloat(-0.2f, -0.02f)));
		}
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		var t = Main.tile[i, j];
		if (!TileDrawing.IsVisible(t))
			return false;

		int clumpAmount = GetClumpNumber(t.TileFrameY) + 1;

		Rectangle frame = new(t.TileFrameX, t.TileFrameY % 198, 44, 16);
		Vector2 drawPos = new Vector2(i, j + 1) * 16 - Main.screenPosition + new Vector2(10, 2);

		for (int k = clumpAmount - 1; k >= 0; --k)
		{
			if (DrawOrderSystem.Order == DrawOrderAttribute.Layer.Default && k == 0)
				continue;
			else if (DrawOrderSystem.Order == DrawOrderAttribute.Layer.OverPlayers && k != 0)
				continue;

			bool useClump = (i + j) % 3 + j % 4 + i % 3 == 0; // Deterministic "random" for switching up the clump type
			int clump = k;
			Vector2 realPos = drawPos + new Vector2(ClumpOffsets[clump] + GetOffset(i, j), 0);

			if (useClump)
			{
				if (clump == 1)
					clump = 2;
				else if (clump == 2)
					clump = 1;
			}

			if (t.TileFrameX == ClumpX)
				DrawClump(i, j, spriteBatch, clumpAmount, frame, realPos, clump);
			else
				DrawSingleKelp(i, j, spriteBatch, clumpAmount, frame, realPos, clump, k);
		}

		return false;
	}

	private static void DrawClump(int i, int j, SpriteBatch spriteBatch, int clumpAmount, Rectangle frame, Vector2 drawPos, int clump)
	{
		TileExtensions.GetVisualInfo(i, j, out Color color, out Texture2D texture);

		var tint = color.MultiplyRGB(Color.Lerp(Color.White, Color.Black, clump / (float)clumpAmount));
		frame = new Rectangle(GetGroupFrameX(i, j) == 48 ? 168 : 94, frame.Y / 18 * 34, 72, 32);

		spriteBatch.Draw(texture, drawPos, frame, tint, 0f, new Vector2(36, 16), 1f, SpriteEffects.None, 0);
	}

	private static void DrawSingleKelp(int i, int j, SpriteBatch spriteBatch, int clumpAmount, Rectangle frame, Vector2 drawPos, int clump, int realClump)
	{
		TileExtensions.GetVisualInfo(i, j, out Color color, out Texture2D texture);

		var tint = color.MultiplyRGB(Color.Lerp(Color.White, Color.Black, clump / (float)clumpAmount));
		frame.X = GetGroupFrameX(i + clump, j);

		if (clump != 0 && GetClumpNumber(Main.tile[i, j - 1]) < realClump)
			frame.Y = 0;

		spriteBatch.Draw(texture, drawPos, frame, tint, 0f, new Vector2(23, 16), 1f, SpriteEffects.None, 0);
	}

	public float GetOffset(int i, int j, float sOffset = 0f)
	{
		float sin = (float)Math.Sin((Main.GameUpdateCount + i * 24 + j * 19) * (0.04f * (!Lighting.NotRetro ? 0f : 1)) + sOffset) * 2.3f;
		int y = j;

		while (Main.tile[i, y].HasTile && Main.tile[i, y].TileType == Type)
			y++;

		if (y - j < 4)
			sin *= (y - j) / 4f;

		return sin;
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		FrameConvert(i, j, Main.tile[i, j].TileType);
		return true;
	}

	public static void FrameConvert(int i, int j, int type)
	{
		if (Conversions.TryGetValue(Framing.GetTileSafely(i, j + 1).TileType, out int newType) && type != newType)
		{
			int top = j;
			while (WorldGen.InWorld(i, top, 2) && Main.tile[i, top].TileType == type)
				top--; //Iterate to the top of the stalk

			int height = j - top;
			ConversionHelper.ConvertTiles(i, top + 1, 1, height, newType);
		}
	}
}

[DrawOrder(DrawOrderAttribute.Layer.NonSolid, DrawOrderAttribute.Layer.OverPlayers)]
public class OceanKelpCorrupt : OceanKelp
{
	public override void PreAddObjectData()
	{
		AddMapEntry(new Color(129, 120, 143));

		TileID.Sets.AddCorruptionTile(Type);
		TileID.Sets.Corrupt[Type] = true;

		DustType = DustID.Corruption;
	}
}

[DrawOrder(DrawOrderAttribute.Layer.NonSolid, DrawOrderAttribute.Layer.OverPlayers)]
public class OceanKelpCrimson : OceanKelp
{
	public override void PreAddObjectData()
	{
		AddMapEntry(new Color(191, 107, 76));

		TileID.Sets.AddCrimsonTile(Type);
		TileID.Sets.Crimson[Type] = true;

		DustType = DustID.CrimsonPlants;
	}
}

[DrawOrder(DrawOrderAttribute.Layer.NonSolid, DrawOrderAttribute.Layer.OverPlayers)]
public class OceanKelpHallowed : OceanKelp
{
	public override void PreAddObjectData()
	{
		AddMapEntry(new Color(111, 183, 170));

		TileID.Sets.Hallow[Type] = true;
		TileID.Sets.HallowBiome[Type] = 1;

		DustType = DustID.HallowedPlants;
	}
}