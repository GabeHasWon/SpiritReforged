using MonoMod.Cil;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.TileCommon.TileSway;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Content.Forest.Botanist.Tiles;

public class Scarecrow : SingleSlotTile<ScarecrowSlot>, IAutoloadTileItem, ISwayTile
{
	private bool IsTop(int i, int j, out ScarecrowSlot entity)
	{
		entity = Entity(i, j);
		return Framing.GetTileSafely(i, j).TileFrameY == 0 && entity is not null;
	}

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		Main.tileSolid[Type] = false;
		Main.tileMergeDirt[Type] = false;
		Main.tileBlockLight[Type] = false;
		Main.tileFrameImportant[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.CoordinateWidth = 46;
		TileObjectData.newTile.Height = 3;
		TileObjectData.newTile.CoordinateHeights = [16, 16, 22];
		TileObjectData.newTile.DrawYOffset = -4;
		TileObjectData.newTile.Origin = new(0, 2);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, 1, 0);
		TileObjectData.newTile.HookPostPlaceMyPlayer = Hook;
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
		TileObjectData.newTile.StyleHorizontal = true;

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
		TileObjectData.addAlternate(1);
		TileObjectData.addTile(Type);

		RegisterItemDrop(ItemType); //Register for all alternative styles
		AddMapEntry(new Color(21, 92, 19));
		DustType = DustID.Hay;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;

	public override bool RightClick(int i, int j)
	{
		if (IsTop(i, j, out _))
			return base.RightClick(i, j);

		return false;
	}

	public override void MouseOver(int i, int j)
	{
		if (IsTop(i, j, out _))
			base.MouseOver(i, j);
	}

	public void DrawSway(int i, int j, SpriteBatch spriteBatch, Vector2 offset, float rotation, Vector2 origin)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out var color, out var texture))
			return;

		var t = Main.tile[i, j];
		var data = TileObjectData.GetTileData(t);
		var dataOffset = new Vector2(data.DrawXOffset - 15, data.DrawYOffset);
		var drawPos = new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y) + dataOffset;
		var source = new Rectangle(t.TileFrameX, t.TileFrameY, data.CoordinateWidth, data.CoordinateHeights[t.TileFrameY / 18]);

		spriteBatch.Draw(texture, drawPos + offset, source, color, rotation, origin, 1, SpriteEffects.None, 0f);
	}

	public float Physics(Point16 topLeft)
	{
		var data = TileObjectData.GetTileData(Framing.GetTileSafely(topLeft));
		float rotation = Main.instance.TilesRenderer.GetWindCycle(topLeft.X, topLeft.Y, TileSwaySystem.TreeWindCounter);

		if (!WorldGen.InAPlaceWithWind(topLeft.X, topLeft.Y, data.Width, data.Height))
			rotation = 0f;

		return (rotation + TileSwayHelper.GetHighestWindGridPushComplex(topLeft.X, topLeft.Y, data.Width, data.Height, 20, 3f, 1, true)) * .5f;
	}
}

public class ScarecrowSlot : SingleSlotEntity
{
	private readonly Player dummy;

	/// <summary> Gets a <see cref="ScarecrowSlot"/> instance by tile position, and in a multiplayer friendly fashion. </summary>
	/// <returns> null if no entity is found. </returns>
	private static ScarecrowSlot GetMe(int i, int j)
	{
		TileExtensions.GetTopLeft(ref i, ref j);

		int id = ModContent.GetInstance<ScarecrowSlot>().Find(i, j);
		if (id == -1)
			return null;

		return (ScarecrowSlot)ByID[id];
	}

	/// <summary> Places <see cref="Scarecrow"/> in the world along with the associated tile entity, wearing a sunflower hat. </summary>
	public static void Generate(int i, int j)
	{
		WorldGen.PlaceObject(i, j, ModContent.TileType<Scarecrow>(), true);
		PlaceEntityNet(i, j - 2, ModContent.TileEntityType<ScarecrowSlot>());

		if (GetMe(i, j) is ScarecrowSlot sgaregrow)
			sgaregrow.item = new Item(ModContent.ItemType<Items.BotanistHat>());
	}

	public ScarecrowSlot()
	{
		dummy = new Player();
		dummy.hair = 15;
		dummy.skinColor = Color.White;
		dummy.skinVariant = 10;
	}

	public override void Load() => IL_TileDrawing.DrawEntities_HatRacks += static (ILContext il) =>
	{
		var c = new ILCursor(il);
		if (!c.TryGotoNext(x => x.MatchCallvirt<SpriteBatch>("End")))
			return;

		//Emit a delegate before the SpriteBatch ends so we don't have to start it again
		c.EmitDelegate(() =>
		{
			foreach (var entity in ByPosition.Values)
				if (entity is ScarecrowSlot scarecrow)
					scarecrow.DrawHat();
		});
	};

	public void DrawHat()
	{
		if (item.IsAir || item.headSlot < 0)
			return;

		//The base of the scarecrow
		var origin = new Vector2(8, 16 * 3);
		float rotation = 0;
		int direction = -1;

		if (TileLoader.GetTile(ModContent.TileType<Scarecrow>()) is ISwayTile sway && TileObjectData.GetTileData(Framing.GetTileSafely(Position)) != null)
			rotation = sway.Physics(Position) * 0.12f;

		if (TileObjectData.GetTileStyle(Framing.GetTileSafely(Position)) == 1)
			direction = 1;

		var position = new Vector2(Position.X * 16, Position.Y * 16) + new Vector2((direction == -1) ? -1 : -3, 32f * Math.Max(rotation, 0) - 6);
		if (Math.Abs(rotation) > .012f)
			position.Y++;

		dummy.direction = direction;
		dummy.Male = true;
		dummy.isDisplayDollOrInanimate = true;
		dummy.isHatRackDoll = true;
		dummy.armor[0] = item;
		dummy.ResetEffects();
		dummy.ResetVisibleAccessories();
		dummy.invis = true;
		dummy.UpdateDyes();
		dummy.DisplayDollUpdate();
		dummy.PlayerFrame();
		dummy.position = position;
		dummy.fullRotation = rotation;
		dummy.fullRotationOrigin = origin;

		//Draw our hat
		Main.PlayerRenderer.DrawPlayer(Main.Camera, dummy, dummy.position, dummy.fullRotation, dummy.fullRotationOrigin);
	}

	public override bool CanAddItem(Item item) => item.headSlot > -1;

	public override bool IsTileValidForEntity(int x, int y)
	{
		Tile tile = Framing.GetTileSafely(x, y);
		return tile.HasTile && tile.TileType == ModContent.TileType<Scarecrow>() && tile.TileFrameY == 0;
	}
}