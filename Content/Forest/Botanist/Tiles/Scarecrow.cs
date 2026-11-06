using MonoMod.Cil;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using TileHelper.Common;

namespace SpiritReforged.Content.Forest.Botanist.Tiles;

public class Scarecrow : SingleSlotTile<ScarecrowSlot>, ILoadItem, WindTileRenderer.IDrawInWind
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

	void WindTileRenderer.IDrawInWind.DrawInWind(SpriteBatch spriteBatch, int i, int j, float rotation, Vector2 position, Vector2 origin)
	{
		Tile tile = Main.tile[i, j];
		if (TileObjectData.GetTileData(tile) is TileObjectData tileObjectData)
		{
			Vector2 offset = new(-15, 0);
			Rectangle source = new(tile.TileFrameX, tile.TileFrameY, tileObjectData.CoordinateWidth, tileObjectData.CoordinateHeights[tile.TileFrameY / 18]);
			Color lightColor = Lighting.GetColor(i, j);

			spriteBatch.Draw(Helpers.GetTileTextureValue(tile), position + offset, source, lightColor, rotation, origin, 1, SpriteEffects.None, 0f);
		}
	}

	float WindTileRenderer.IDrawInWind.GetWindStrength(int i, int j)
	{
		if (TileObjectData.GetTileData(Framing.GetTileSafely(i, j)) is TileObjectData tileObjectData)
		{
			float rotation = WorldGen.InAPlaceWithWind(i, j, tileObjectData.Width, tileObjectData.Height) ? Main.instance.TilesRenderer.GetWindCycle(i, j, WindTileRenderer.TreeWindCounter) : 0f;
			return (rotation + WindTileRenderer.GetHighestWindGridPushComplex(i, j, tileObjectData.Width, tileObjectData.Height, 20, 3f, 1, true)) * 0.5f;
		}

		return 0f;
	}
}

public class ScarecrowSlot : SingleSlotEntity
{
	private readonly Player dummy;

	/// <summary> Gets a <see cref="ScarecrowSlot"/> instance by tile position, and in a multiplayer friendly fashion. </summary>
	/// <returns> null if no entity is found. </returns>
	private static ScarecrowSlot GetMe(int i, int j)
	{
		(i, j) = Helpers.GetTopLeft(i, j);

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

		if (TileLoader.GetTile(ModContent.TileType<Scarecrow>()) is WindTileRenderer.IDrawInWind iDrawInWind && TileObjectData.GetTileData(Framing.GetTileSafely(Position)) != null)
			rotation = iDrawInWind.GetWindStrength(Position.X, Position.Y) * 0.12f;

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