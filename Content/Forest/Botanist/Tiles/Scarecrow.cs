﻿using MonoMod.Cil;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.TileCommon.TileSway;
using SpiritReforged.Content.Jungle.Bamboo.Tiles;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Content.Forest.Botanist.Tiles;

public class Scarecrow : ModTile, IAutoloadTileItem, ISwayTile
{
	private static bool IsTop(int i, int j, out ScarecrowSlot entity)
	{
		entity = ScarecrowSlot.GetMe(i, j);
		return Framing.GetTileSafely(i, j).TileFrameY == 0 && entity is not null;
	}

	public override void SetStaticDefaults()
	{
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
		var entity = ModContent.GetInstance<ScarecrowSlot>();
		TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(entity.Hook_AfterPlacement, -1, 0, false);
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
		TileObjectData.newTile.StyleHorizontal = true;

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
		TileObjectData.addAlternate(1);
		TileObjectData.addTile(Type);

		RegisterItemDrop(Mod.Find<ModItem>(Name + "Item").Type); //Register for all alternative styles
		AddMapEntry(new Color(21, 92, 19));
		DustType = DustID.Hay;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (!effectOnly && !Main.dedServ && ScarecrowSlot.GetMe(i, j) is ScarecrowSlot slot && !slot.item.IsAir)
		{
			fail = true;
			TileExtensions.GetTopLeft(ref i, ref j);

			var pos = new Vector2(i, j).ToWorldCoordinates();

			ItemMethods.NewItemSynced(new EntitySource_TileBreak(i, j), slot.item, pos);
			slot.item.TurnToAir();
		}
	}

	public override bool RightClick(int i, int j)
	{
		if (!IsTop(i, j, out var entity))
			return false;

		entity.OnInteract(Main.LocalPlayer);
		return true;
	}

	public override void MouseOver(int i, int j)
	{
		if (!IsTop(i, j, out var entity))
			return;

		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = entity.item.IsAir ? Mod.Find<ModItem>(Name + "Item").Type : entity.item.type;
	}

	public void DrawSway(int i, int j, SpriteBatch spriteBatch, Vector2 offset, float rotation, Vector2 origin)
	{
		var tile = Framing.GetTileSafely(i, j);
		var data = TileObjectData.GetTileData(tile);
		var dataOffset = new Vector2(data.DrawXOffset - 15, data.DrawYOffset);
		var drawPos = new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y) + dataOffset;
		var source = new Rectangle(tile.TileFrameX, tile.TileFrameY, data.CoordinateWidth, data.CoordinateHeights[tile.TileFrameY / 18]);

		spriteBatch.Draw(TextureAssets.Tile[Type].Value, drawPos + offset, source, Lighting.GetColor(i, j), rotation, origin, 1, SpriteEffects.None, 0f);
	}

	public float Physics(Point16 topLeft)
	{
		var data = TileObjectData.GetTileData(Framing.GetTileSafely(topLeft));
		float rotation = Main.instance.TilesRenderer.GetWindCycle(topLeft.X, topLeft.Y, TileSwaySystem.Instance.TreeWindCounter);

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
	public static ScarecrowSlot GetMe(int i, int j)
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
				if (entity is not null and ScarecrowSlot scarecrow)
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

		if (TileLoader.GetTile(ModContent.TileType<Scarecrow>()) is ISwayTile sway)
			rotation = sway.Physics(Position) * .12f;

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