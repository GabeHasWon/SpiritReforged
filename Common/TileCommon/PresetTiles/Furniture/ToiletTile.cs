using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;

namespace SpiritReforged.Common.TileCommon.PresetTiles;

public abstract class ToiletTile : FurnitureTile
{
	private static bool WithinRange(int i, int j, Player player) => player.IsWithinSnappngRangeToTile(i, j, PlayerSittingHelper.ChairSittingMaxDistance);

	public override void SetItemDefaults(ModItem item) => item.Item.value = Item.sellPrice(copper: 30);

	public override void AddItemRecipes(ModItem item)
	{
		if (Info.Material != ItemID.None)
			item.CreateRecipe().AddIngredient(Info.Material, 6).AddTile(TileID.Sawmill).Register();
	}

	public override void StaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileID.Sets.HasOutlines[Type] = true;
		TileID.Sets.CanBeSatOnForNPCs[Type] = true;
		TileID.Sets.CanBeSatOnForPlayers[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.Origin = new Point16(0, 1);
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
		TileObjectData.addAlternate(1);
		TileObjectData.addTile(Type);

		AddToArray(ref TileID.Sets.RoomNeeds.CountsAsChair);
		AddMapEntry(new Color(100, 100, 60), Language.GetText("MapObject.Toilet"));
		AdjTiles = [TileID.Toilets];
		DustType = -1;
	}

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => WithinRange(i, j, settings.player);

	public override bool RightClick(int i, int j)
	{
		Player player = Main.LocalPlayer;
		if (WithinRange(i, j, player))
		{
			player.GamepadEnableGrappleCooldown();
			player.sitting.SitDown(player, i, j);
			return true;
		}

		return false;
	}

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		if (WithinRange(i, j, player))
		{
			player.noThrow = 2;
			player.cursorItemIconID = Info.Item.Type;
			player.cursorItemIconEnabled = true;

			if (Framing.GetTileSafely(i, j).TileFrameX / 18 < 1)
				player.cursorItemIconReversed = true;
		}
	}

	public override void ModifySittingTargetInfo(int i, int j, ref TileRestingInfo info)
	{
		info.TargetDirection = (Framing.GetTileSafely(i, j).TileFrameX == 0) ? -1 : 1;
		info.ExtraInfo.IsAToilet = true;
	}

	public override void HitWire(int i, int j)
	{
		var tile = Framing.GetTileSafely(i, j);
		j -= tile.TileFrameY % (18 * 2) / 18;

		Wiring.SkipWire(i, j);
		Wiring.SkipWire(i, j + 1);

		if (Wiring.CheckMech(i, j, 60))
		{
			var position = new Vector2(i, j) * 16 + new Vector2(8, 12);
			Projectile.NewProjectile(Wiring.GetProjectileSource(i, j), position, Vector2.Zero, ProjectileID.ToiletEffect, 0, 0, Main.myPlayer);
		}
	}
}
