using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.VerletChains;
using SpiritReforged.Common.Visuals;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.Tiles;

public class FlagRing : EntityTile<FlagRing.FlagRingEntity>, IAutoloadTileItem
{
	public class FlagRingEntity : ModTileEntity, IEntityUpdate, IEntityDraw
	{
		public Chain chain;

		public override bool IsTileValidForEntity(int x, int y)
		{
			Tile tile = Main.tile[x, y];
			return tile.HasTile && tile.TileType == ModContent.TileType<FlagRing>() && tile.TileFrameY == SlopeFrame;
		}

		public override int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternate)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				NetMessage.SendTileSquare(Main.myPlayer, i, j);
				NetMessage.SendData(MessageID.TileEntityPlacement, number: i, number2: j, number3: Type);

				return -1;
			}

			return Place(i, j);
		}

		public void GlobalUpdate()
		{
			if (Main.dedServ || !OnScreen())
			{
				chain = null;
				return;
			}

			if (chain == null)
			{
				const int segments = 8;
				int length = FlagTrail.Value.Height;

				chain = new Chain(length / segments - 2, segments + 1, Position.ToWorldCoordinates(), new ChainPhysics(), stiffness: 2);
			}

			float wind = Main.WindForVisuals;
			float windOffsetY = (float)Math.Sin((Main.timeForVisualEffects + Position.X * 2 + Position.Y * 10) / 20f) * 8f * wind;

			Vector2 endOffset = new Vector2(FlagTrail.Value.Height, windOffsetY).RotatedBy(-MathHelper.PiOver2 * (Math.Clamp(wind * 1.5f, -1, 1) - 1));
			chain?.Update(Position.ToWorldCoordinates(), Position.ToWorldCoordinates() + endOffset);
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			if (chain != null && OnScreen())
			{
				SpriteBatch sb = Main.spriteBatch;
				Texture2D texture = FlagTrail.Value;
				int style = Framing.GetTileSafely(Position).TileFrameX / 18;

				for (int c = 0; c < chain.Segments.Count; c++)
				{
					ChainSegment segment = chain.Segments[c];
					segment.Draw(sb, texture, texture.Frame(3, chain.Segments.Count, style, c, -2, 0), 1);
				}
			}
		}

		public bool OnScreen()
		{
			Rectangle screen = new((int)Main.screenPosition.X, (int)Main.screenPosition.Y, Main.screenWidth, Main.screenHeight);
			return screen.Contains(new Point(Position.X * 16 + 8, Position.Y * 16 + 8));
		}

		/// <summary> Returns whether this entity will remain active. </summary>
		public bool CheckActive(int x, int y)
		{
			if (!IsTileValidForEntity(x, y))
			{
				Kill(x, y);
				return false;
			}

			return true;
		}

		public override void OnNetPlace() => NetMessage.SendData(MessageID.TileEntitySharing, number: ID, number2: Position.X, number3: Position.Y);
	}

	public const int SlopeFrame = 18;
	public static readonly Asset<Texture2D> FlagTrail = DrawHelpers.RequestLocal<FlagRing>("FlagTrail", false);

	public void AddItemRecipes(ModItem item) => item.CreateRecipe(5).AddRecipeGroup("CopperBars").AddIngredient(ItemID.Silk).AddTile(TileID.Anvils).Register();

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		Main.tileNoFail[Type] = true;
		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;
		TileID.Sets.CanBeSloped[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.AnchorBottom = new(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.AlternateTile, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.CoordinateHeights = [18];
		TileObjectData.newTile.AnchorAlternateTiles = [Type];
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(165, 85, 55));
		RegisterItemDrop(this.AutoItemType());
		DustType = -1;
	}

	public override bool Slope(int i, int j)
	{
		Tile tile = Main.tile[i, j];
		if (tile.TileFrameY == SlopeFrame)
		{
			tile.TileFrameY = 0;
			WorldGen.TileFrame(i, j);

			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendTileSquare(-1, i, j);
		}
		else
		{
			tile.TileFrameY = SlopeFrame;
			LocalEntity.Hook_AfterPlacement(i, j, LocalEntity.Type, 0, 0, 0);
		}

		Vector2 position = new Vector2(i, j).ToWorldCoordinates();
		for (int g = 0; g < 3; g++)
		{
			int type = Main.rand.NextFromList(GoreID.Smoke1, GoreID.Smoke2, GoreID.Smoke3);
			var gore = Gore.NewGoreDirect(new EntitySource_TileInteraction(Main.LocalPlayer, i, j), position, Vector2.Zero, type);
			gore.position -= new Vector2(20);
			gore.velocity *= Main.rand.NextFloat(0.25f, 0.75f);

			Dust.NewDust(position - new Vector2(8), 16, 16, DustID.Copper);
		}

		return false;
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		Tile tile = Main.tile[i, j];

		if (resetFrame)
			tile.TileFrameX = (short)(18 * Main.rand.Next(3));

		if (tile.TileFrameY != SlopeFrame)
		{
			if (!InStack(i, j, -1))
				tile.TileFrameY = 0;
			//else if (!InStack(i, j, -2))
			//	tile.TileFrameY = 36; //Special ribbon frames
			else
				tile.TileFrameY = 54;
		}

		if (Entity(i, j, false) is FlagRingEntity entity)
			entity.CheckActive(i, j);

		return true;

		static bool InStack(int x, int y, int length)
		{
			Tile tile = Framing.GetTileSafely(x, y + length);
			return tile.HasTile && tile.TileType == ModContent.TileType<FlagRing>();
		}
	}

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (!effectOnly && !fail && Entity(i, j, false) is FlagRingEntity entity)
			entity.Kill(i, j);
	}
}