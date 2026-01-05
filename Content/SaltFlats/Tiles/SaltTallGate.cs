using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;

namespace SpiritReforged.Content.SaltFlats.Tiles;

public class SaltTallGate : EntityTile<SaltTallGateEntity>, ICustomDoor, IAutoloadTileItem
{
	public const int FrameWidth = 36;

	public void AddItemRecipes(ModItem item) => item.CreateRecipe().AddIngredient(AutoContent.ItemType<SaltPanel>(), 12).AddRecipeGroup(RecipeGroupID.IronBar, 4).AddTile(TileID.HeavyWorkBench).Register();

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileBlockLight[Type] = true;
		Main.tileSolid[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = false;
		Main.tileWaterDeath[Type] = false;

		TileID.Sets.NotReallySolid[Type] = true;
		TileID.Sets.DrawsWalls[Type] = true;
		TileID.Sets.HasOutlines[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;
		TileID.Sets.OpenDoorID[Type] = ModContent.TileType<SaltTallGateOpen>();

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Width = 2;
		TileObjectData.newTile.Height = 4;
		TileObjectData.newTile.CoordinateHeights = [18, 16, 16, 18];
		TileObjectData.newTile.Origin = new Point16(1, 3);
		TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.UsesCustomCanPlace = true;
		TileObjectData.newTile.LavaDeath = false;
		TileObjectData.newTile.HookPostPlaceMyPlayer = Hook;

		TileObjectData.addTile(Type);

		AddToArray(ref TileID.Sets.RoomNeeds.CountsAsDoor);
		AddMapEntry(FurnitureTile.CommonColor, Language.GetText("MapObject.Door"));
		AdjTiles = [TileID.ClosedDoor];
		DustType = -1;
	}

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => settings.player.InInteractionRange(i, j, TileReachCheckSettings.Simple);

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = AutoContent.ItemType<SaltTallGate>();
	}

	public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
	{
		if (Entity(i, j) is SaltTallGateEntity entity)
			frameXOffset = FrameWidth * (int)entity.frame;
	}
}

public class SaltTallGateOpen : EntityTile<SaltTallGateEntity>, ICustomDoor
{
	public override string Texture => ModContent.GetInstance<SaltTallGate>().Texture;

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileBlockLight[Type] = false;
		Main.tileSolid[Type] = false;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = false;
		Main.tileWaterDeath[Type] = false;

		TileID.Sets.NotReallySolid[Type] = true;
		TileID.Sets.DrawsWalls[Type] = true;
		TileID.Sets.HasOutlines[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;
		TileID.Sets.HousingWalls[Type] = true;
		TileID.Sets.DrawTileInSolidLayer[Type] = true;
		TileID.Sets.CloseDoorID[Type] = ModContent.TileType<SaltTallGate>();

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Width = 2;
		TileObjectData.newTile.Height = 4;
		TileObjectData.newTile.CoordinateHeights = [18, 16, 16, 18];
		TileObjectData.newTile.Origin = new Point16(1, 3);
		TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.UsesCustomCanPlace = true;
		TileObjectData.newTile.LavaDeath = false;
		TileObjectData.newTile.HookPostPlaceMyPlayer = Hook;

		TileObjectData.addTile(Type);

		RegisterItemDrop(AutoContent.ItemType<SaltTallGate>());
		AddToArray(ref TileID.Sets.RoomNeeds.CountsAsDoor);
		AddMapEntry(FurnitureTile.CommonColor, Language.GetText("MapObject.Door"));
		AdjTiles = [TileID.ClosedDoor];
		DustType = -1;
	}

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => settings.player.InInteractionRange(i, j, TileReachCheckSettings.Simple);

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = AutoContent.ItemType<SaltTallGate>();
	}

	public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
	{
		if (Entity(i, j) is SaltTallGateEntity entity)
			frameXOffset = SaltTallGate.FrameWidth * (int)entity.frame;
	}
}

public class SaltTallGateEntity : ModTileEntity, IEntityUpdate
{
	public float frame;

	public override bool IsTileValidForEntity(int x, int y)
	{
		Tile tile = Main.tile[x, y];
		return tile.HasTileType(ModContent.TileType<SaltTallGate>()) || tile.HasTileType(ModContent.TileType<SaltTallGateOpen>()) && TileObjectData.IsTopLeft(x, y);
	}

	public override int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternate)
	{
		TileExtensions.GetTopLeft(ref i, ref j);
		var d = TileObjectData.GetTileData(Main.tile[i, j]);
		var size = (d is null) ? new Point(1, 1) : new Point(d.Width, d.Height);

		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			NetMessage.SendTileSquare(Main.myPlayer, i, j, size.X, size.Y);
			NetMessage.SendData(MessageID.TileEntityPlacement, number: i, number2: j, number3: Type);

			return -1;
		}

		return Place(i, j);
	}

	public void GlobalUpdate()
	{
		float rate = Math.Max(EaseFunction.EaseCubicOut.Ease(1f - frame / 7f), 0.1f);
		bool closed = Framing.GetTileSafely(Position).HasTileType(ModContent.TileType<SaltTallGate>());
		float lastFrame = frame;

		if (closed)
			frame = Math.Max(0, frame - rate);
		else
			frame = Math.Min(7, frame + rate);

		if (Main.dedServ)
			return;

		Rectangle area = new(Position.X * 16, (Position.Y + 2) * 16, 32, 2);
		if (frame == 0 && lastFrame != 0) //Just closed
		{
			for (int i = 0; i < 8; i++)
			{
				var dust = Dust.NewDustPerfect(Main.rand.NextVector2FromRectangle(area), DustID.WhiteTorch, Vector2.UnitY * -Main.rand.NextFloat(), 150, default, 2);
				dust.noGravity = true;
				dust.noLightEmittence = true;
			}

			SoundEngine.PlaySound(SoundID.Item102 with { Pitch = 1 }, area.Center());
		}
		else if (frame != 0 && lastFrame == 0) //Just opened
		{
			SoundEngine.PlaySound(SoundID.Item101 with { Pitch = 1 }, area.Center());
			SoundEngine.PlaySound(SoundID.AbigailUpgrade with { Pitch = 1 }, area.Center());
		}
	}

	public override void OnNetPlace() => NetMessage.SendData(MessageID.TileEntitySharing, number: ID, number2: Position.X, number3: Position.Y);
}