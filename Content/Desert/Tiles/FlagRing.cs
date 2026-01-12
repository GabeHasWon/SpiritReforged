using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.VerletChains;
using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Content.Desert.Tiles;

public class FlagRing : EntityTile<FlagRing.FlagRingEntity>, IAutoloadTileItem
{
	public class FlagRingEntity : ModTileEntity, IEntityUpdate, IEntityDraw
	{
		public Chain chain;

		public override bool IsTileValidForEntity(int x, int y)
		{
			Tile tile = Main.tile[x, y];
			return tile.HasTileType(ModContent.TileType<FlagRing>()) && tile.TileFrameY == 0;
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
			float windOffsetY = (float)Math.Sin((Main.timeForVisualEffects + Position.X * 2) / 20f) * 8f * wind;

			Vector2 endOffset = new Vector2(FlagTrail.Value.Height, windOffsetY).RotatedBy(-MathHelper.PiOver2 * (Math.Clamp(wind * 1.5f, -1, 1) - 1));
			chain?.Update(Position.ToWorldCoordinates(), Position.ToWorldCoordinates() + endOffset);
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			if (chain != null && OnScreen())
				DrawFullChain(chain);
		}

		public bool OnScreen()
		{
			Rectangle screen = new((int)Main.screenPosition.X, (int)Main.screenPosition.Y, Main.screenWidth, Main.screenHeight);
			return screen.Contains(new Point(Position.X * 16 + 8, Position.Y * 16 + 8));
		}

		private static void DrawFullChain(Chain chain)
		{
			SpriteBatch sb = Main.spriteBatch;
			Texture2D texture = FlagTrail.Value;
			int style = (int)(chain.FirstVertex.Position / 16).Length() % 3;

			for (int c = 0; c < chain.Segments.Count; c++)
			{
				ChainSegment segment = chain.Segments[c];
				segment.Draw(sb, texture, texture.Frame(3, chain.Segments.Count, style, c, -2, 0), 1);
			}
		}

		public override void OnNetPlace() => NetMessage.SendData(MessageID.TileEntitySharing, number: ID, number2: Position.X, number3: Position.Y);
	}

	public static readonly Asset<Texture2D> FlagTrail = DrawHelpers.RequestLocal<FlagRing>("FlagTrail", false);

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		Main.tileNoFail[Type] = true;
		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.Height = 1;
		TileObjectData.newTile.AnchorBottom = new(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.AlternateTile, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.CoordinateHeights = [16];
		TileObjectData.newTile.AnchorAlternateTiles = [Type];
		TileObjectData.newTile.HookPostPlaceMyPlayer = Hook;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(150, 150, 150));
		RegisterItemDrop(this.AutoItemType());
		DustType = -1;
	}

	public override void PostTileFrame(int i, int j, int up, int down, int left, int right, int upLeft, int upRight, int downLeft, int downRight)
	{
		bool top = !Framing.GetTileSafely(i, j - 1).HasTileType(Type);
		Main.tile[i, j].TileFrameY = (short)(top ? 0 : 18);

		if (!top && Entity(i, j) is FlagRingEntity entity)
			entity.Kill(i, j);
	}
}