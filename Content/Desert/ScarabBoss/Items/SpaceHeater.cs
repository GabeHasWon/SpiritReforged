using SpiritReforged.Content.Desert.ScarabBoss.Boss;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items;

internal class SpaceHeater : ModItem
{
	public sealed class SpaceHeaterEntity : ModTileEntity
	{
		/// <summary>
		/// Strength value, 0-1, for the heat shader.
		/// </summary>
		public float Strength = 0f;

		public override bool IsTileValidForEntity(int x, int y)
		{
			Tile tile = Main.tile[x, y];
			return tile.HasTile && tile.TileType == ModContent.TileType<SpaceHeaterTile>() && tile.TileFrameX == 0 && tile.TileFrameY == 0;
		}

		public override int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternate) => Generic_Hook_AfterPlacement(i, j, type, style, direction, alternate);
	}

	public sealed class SpaceHeaterTile : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileObsidianKill[Type] = true;

			TileID.Sets.HasOutlines[Type] = true;
			TileID.Sets.DisableSmartCursor[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
			TileObjectData.newTile.Height = 3;
			TileObjectData.newTile.CoordinateHeights = [16, 16, 18];
			TileObjectData.newTile.Origin = new Point16(0, 2);
			TileObjectData.newTile.LavaDeath = false;
			var tileEntity = ModContent.GetInstance<SpaceHeaterEntity>();
			TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(tileEntity.Hook_AfterPlacement, -1, 0, true);
			TileObjectData.addTile(Type);

			RegisterItemDrop(ModContent.ItemType<SpaceHeater>());
			AddMapEntry(new Color(135, 135, 136), this.GetLocalization("MapEntry"));

			DustType = DustID.Iron;
		}

		public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

		public override void MouseOver(int i, int j)
		{
			Player player = Main.LocalPlayer;
			player.noThrow = 2;
			player.cursorItemIconEnabled = true;
			player.cursorItemIconID = ModContent.ItemType<SpaceHeater>();
		}

		public override void NearbyEffects(int i, int j, bool closer)
		{
			Point16 topLeft = TileObjectData.TopLeft(i, j);

			if (TileEntity.ByPosition[topLeft] is SpaceHeaterEntity heater)
				HeaterSystem.HeatEffect = heater.Strength;
		}
	}

	public class HeaterSystem : ModSystem
	{
		public static float? HeatEffect = null;

		public override void ResetNearbyTileEffects() => HeatEffect = null;

		public override void PostUpdateDusts()
		{
			if (HeatEffect is { } value)
			{
				ScarabHeatHazeShaderData.HeatHazeTargetIntensity = value;
				ScarabHeatHazeShaderData.HeatHazeTargetOpacity = value;
			}
		}
	}

	public class HeaterUI : UIState
	{
		public static Point16 WorldAnchor = Point16.Zero;

		private static Asset<Texture2D> UI = null;
		private static float Progress = 0;

		public override void OnInitialize()
		{
			UI ??= ModContent.Request<Texture2D>("SpiritReforged/Contents/Desert/ScarabBoss/Items/SpaceHeater/SpaceHeaterUI");

			UIImageFramed back = new(UI, new Rectangle(0, 0, 8, 48));
			Append(back);

			UIImageFramed slider = new(UI, new Rectangle(10, 0, 12, 12));
			slider.OnUpdate += HoverSliderUpdate;

		}

		private void HoverSliderUpdate(UIElement affectedElement)
		{
			UIImageFramed slider = affectedElement as UIImageFramed;
			slider.SetFrame(slider.IsMouseHovering ? new Rectangle(10, 14, 12, 12) : new Rectangle(10, 0, 12, 12));

			if (IsMouseHovering && Main.mouseLeft)
			{
				Progress = 0;
			}
		}
	}

	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<SpaceHeaterTile>());
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.Green;
	}
}
