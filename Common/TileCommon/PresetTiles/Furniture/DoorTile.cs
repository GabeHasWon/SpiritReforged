using SpiritReforged.Common.TileCommon.DrawPreviewHook;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;

namespace SpiritReforged.Common.TileCommon.PresetTiles;

public abstract class DoorTile : FurnitureTile, IDrawPreview
{
	public override void SetItemDefaults(ModItem item) => item.Item.value = Item.sellPrice(copper: 40);

	public override void AddItemRecipes(ModItem item)
	{
		if (CoreMaterial != ItemID.None)
			item.CreateRecipe()
			.AddIngredient(CoreMaterial, 6)
			.AddTile(TileID.WorkBenches)
			.Register();
	}

	/// <summary> Functions like <see cref="ModType.Load"/> and handles open door autoloading. </summary>
	public override void Load() => Mod.AddContent(new AutoloadedDoorOpen(Name + "Open", Texture, ModItem.Type));

	public override void StaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileBlockLight[Type] = true;
		Main.tileSolid[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileID.Sets.NotReallySolid[Type] = true;
		TileID.Sets.DrawsWalls[Type] = true;
		TileID.Sets.HasOutlines[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;
		TileID.Sets.OpenDoorID[Type] = Mod.Find<ModTile>(Name + "Open").Type;

		TileObjectData.newTile.Width = 1;
		TileObjectData.newTile.Height = 3;
		TileObjectData.newTile.Origin = new Point16(0, 0);
		TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.UsesCustomCanPlace = true;
		TileObjectData.newTile.LavaDeath = true;
		TileObjectData.newTile.CoordinateHeights = [16, 16, 16];
		TileObjectData.newTile.CoordinateWidth = 16;
		TileObjectData.newTile.CoordinatePadding = 2;

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Origin = new Point16(0, 1);
		TileObjectData.addAlternate(0);

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Origin = new Point16(0, 2);
		TileObjectData.addAlternate(0);

		TileObjectData.addTile(Type);

		AddToArray(ref TileID.Sets.RoomNeeds.CountsAsDoor);
		AddMapEntry(new Color(100, 100, 60), Language.GetText("MapObject.Door"));
		AdjTiles = [TileID.ClosedDoor];
		DustType = -1;
	}

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = ModItem.Type;
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out var color, out var texture))
			return false;

		var t = Main.tile[i, j];
		var source = new Rectangle(18 * 4, t.TileFrameY, 16, (t.TileFrameY > 18) ? 18 : 16);
		var position = new Vector2(i, j) * 16 - Main.screenPosition + TileExtensions.TileOffset;

		spriteBatch.Draw(texture, position, source, color, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

		if (Main.InSmartCursorHighlightArea(i, j, out bool actuallySelected))
			spriteBatch.Draw(TextureAssets.HighlightMask[Type].Value, position, source, 
				actuallySelected ? Color.Yellow : Color.Gray, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

		return false;
	}

	public virtual void DrawPreview(SpriteBatch spriteBatch, TileObjectPreviewData op, Vector2 position)
	{
		(int i, int j) = (op.Coordinates.X, op.Coordinates.Y);
		var texture = TextureAssets.Tile[Type].Value;
		var lightOffset = Lighting.LegacyEngine.Mode > 1 && Main.GameZoomTarget == 1 ? Vector2.Zero : Vector2.One * 12;

		for (int frameY = 0; frameY < 3; frameY++)
		{
			(int x, int y) = (i, j + frameY);
			var color = ((op[0, frameY + 1] == 1) ? Color.White : Color.Red * .7f) * .5f;

			var source = new Rectangle(18 * 4, frameY * 18, 16, (frameY == 2) ? 18 : 16);
			var drawPos = (new Vector2(x, y + 1) + lightOffset) * 16 - Main.screenPosition;

			spriteBatch.Draw(texture, drawPos, source, color, 0, Vector2.Zero, 1, SpriteEffects.None, 0f);
		}
	}
}

public sealed class AutoloadedDoorOpen(string name, string texture, int itemDrop) : ModTile
{
	public override string Name => name;

	public override string Texture => texture;

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileSolid[Type] = false;
		Main.tileLavaDeath[Type] = true;
		Main.tileNoSunLight[Type] = true;

		TileID.Sets.HousingWalls[Type] = true;
		TileID.Sets.HasOutlines[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;
		TileID.Sets.CloseDoorID[Type] = Mod.Find<ModTile>(Name.Replace("Open", string.Empty)).Type;

		TileObjectData.newTile.Width = 2;
		TileObjectData.newTile.Height = 3;
		TileObjectData.newTile.Origin = new Point16(0, 0);
		TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile, 1, 0);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 1, 0);
		TileObjectData.newTile.UsesCustomCanPlace = true;
		TileObjectData.newTile.LavaDeath = true;
		TileObjectData.newTile.CoordinateHeights = [16, 16, 16];
		TileObjectData.newTile.CoordinateWidth = 16;
		TileObjectData.newTile.CoordinatePadding = 2;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.StyleMultiplier = 2;
		TileObjectData.newTile.StyleWrapLimit = 2;
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceRight;

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Origin = new Point16(0, 1);
		TileObjectData.addAlternate(0);

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Origin = new Point16(0, 2);
		TileObjectData.addAlternate(0);

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Origin = new Point16(1, 0);
		TileObjectData.newAlternate.AnchorTop = new AnchorData(AnchorType.SolidTile, 1, 1);
		TileObjectData.newAlternate.AnchorBottom = new AnchorData(AnchorType.SolidTile, 1, 1);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceLeft;
		TileObjectData.addAlternate(1);

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Origin = new Point16(1, 1);
		TileObjectData.newAlternate.AnchorTop = new AnchorData(AnchorType.SolidTile, 1, 1);
		TileObjectData.newAlternate.AnchorBottom = new AnchorData(AnchorType.SolidTile, 1, 1);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceLeft;
		TileObjectData.addAlternate(1);

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Origin = new Point16(1, 2);
		TileObjectData.newAlternate.AnchorTop = new AnchorData(AnchorType.SolidTile, 1, 1);
		TileObjectData.newAlternate.AnchorBottom = new AnchorData(AnchorType.SolidTile, 1, 1);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceLeft;
		TileObjectData.addAlternate(1);

		TileObjectData.addTile(Type);

		RegisterItemDrop(itemDrop); //Prevents inconsistent item drops based on style
		AddToArray(ref TileID.Sets.RoomNeeds.CountsAsDoor);
		AddMapEntry(new Color(100, 100, 60), Language.GetText("MapObject.Door"));
		AdjTiles = [TileID.OpenDoor];
		DustType = -1;
	}

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = itemDrop;
	}
}
