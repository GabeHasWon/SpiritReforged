using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.TileCommon;
using Terraria.Audio;
using Terraria.DataStructures;
using TileHelper.Common;

namespace SpiritReforged.Content.Crossmod.Spooky.SpookyForest.Plants;

internal abstract class StarGourd : ModTile
{
	public interface IGourdInfo
	{
		public int ItemType { get; }
		public bool HasGlow { get; }
		public void CreateCarvedType(Mod mod);
	}

	public readonly record struct GourdInfo<TSelf>(string ItemName, bool HasGlow = true) : IGourdInfo where TSelf : StarGourd, new()
	{
		public int ItemType => CrossMod.Spooky.CheckFind(ItemName, out ModItem item) ? item.Type : throw null;
		
		public void CreateCarvedType(Mod mod)
		{
			CarvedStarGourd<TSelf> gourd = new();
			mod.AddContent(gourd);
		}
	}

	protected static SoundStyle CarveSound;

	protected abstract IGourdInfo Info { get; }
	protected virtual bool HasOriginalObjectData => true;

	public override bool IsLoadingEnabled(Mod mod) => CrossMod.Spooky.Enabled;
	public sealed override void Load() => Info.CreateCarvedType(Mod);
	public sealed override void SetStaticDefaults() => StaticDefaults(this, this, true);

	public static void StaticDefaults(ModTile tile, StarGourd copyInstance, bool addEntry)
	{
		CarveSound = new("Spooky/Content/Sounds/PumpkinCarve", SoundType.Sound);

		Main.tileSolid[tile.Type] = false;
		Main.tileFrameImportant[tile.Type] = true;
		Main.tileNoAttach[tile.Type] = true;

		TileID.Sets.BreakableWhenPlacing[tile.Type] = true;

		tile.DustType = DustID.DesertWater2;

		if (copyInstance.HasOriginalObjectData)
		{
			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
			TileObjectData.newTile.Origin = new Point16(1, 1);
			TileObjectData.newTile.DrawYOffset = 2;
		}

		if (copyInstance.ModifyObjectData(tile, TileObjectData.newTile) && addEntry)
			tile.AddMapEntry(new Color(25, 197, 87));

		TileObjectData.addTile(tile.Type);
	}

	protected virtual bool ModifyObjectData(ModTile tile, TileObjectData newTile) => true;

	public override void MouseOver(int i, int j)
	{
		if (!CrossMod.Spooky.CheckFind("PumpkinCarvingKit", out ModItem carving))
			return;

		Player player = Main.LocalPlayer;
		bool hasCarving = player.HeldItem.type == carving.Type;

		player.cursorItemIconEnabled = hasCarving;
		player.cursorItemIconID = hasCarving ? carving.Type : 0;
		player.cursorItemIconText = "";
	}

	public override void MouseOverFar(int i, int j)
	{
		MouseOver(i, j);
		Player player = Main.LocalPlayer;
		player.cursorItemIconEnabled = false;
		player.cursorItemIconID = ItemID.None;
	}

	public override bool RightClick(int i, int j)
	{
		if (!CrossMod.Spooky.CheckFind("PumpkinCarvingKit", out ModItem carving))
			return false;

		Player player = Main.LocalPlayer;

		if (player.HeldItem.type == carving.Type)
		{
			SoundEngine.PlaySound(CarveSound, new Vector2(i * 16, j * 16));

			var data = TileObjectData.GetTileData(Main.tile[i, j]);
			int left = i - Framing.GetTileSafely(i, j).TileFrameX / 18 % data.Width;
			int top = j - Framing.GetTileSafely(i, j).TileFrameY / 18 % data.Height;

			for (int x = left; x < left + data.Width; x++)
			{
				for (int y = top; y < top + data.Height; y++)
				{
					Tile tile = Framing.GetTileSafely(x, y);
					tile.TileType = Mod.Find<ModTile>(Name + "Carved").Type;
				}
			}

			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendTileSquare(-1, left, top, Math.Max(data.Width, data.Height));
		}

		return true;
	}

	public override void KillMultiTile(int i, int j, int fX, int fY) => Item.NewItem(new EntitySource_TileBreak(i, j), i * 16, j * 16, 32, 16, Info.ItemType, Main.rand.Next(15, 26));

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (!Info.HasGlow || !TileMethods.GetVisualInfo(i, j, out _, out Texture2D tex))
			return;

		Tile tile = Main.tile[i, j];
		var data = TileObjectData.GetTileData(tile);
		Color color = Color.White * 0.7f * MathF.Sin(i * 0.6f + j * 0.6f + (float)Main.timeForVisualEffects * 0.03f) * 0.3f;
		spriteBatch.Draw(tex, Helpers.GetTilePosition(i, j) + new Vector2(0, -2), new Rectangle(tile.TileFrameX, tile.TileFrameY + data.Height * 18, 16, 16), color);
	}
}

[Autoload(false)]
internal class CarvedStarGourd<T>() : ModTile, ILoadItem where T : StarGourd, new()
{
	private T Instance = new();

	public override string Name => new T().Name + "Carved";

	public override void SetStaticDefaults()
	{
		Instance = new T();
		StarGourd.StaticDefaults(this, Instance, false);

		Main.tileLighted[Type] = true;
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		var data = TileObjectData.GetTileData(Type, 0);

		if (Main.tile[i, j].TileFrameY > data.Height * 18)
		{
			Vector3 glow = new Vector3(247, 211, 134f) / 250f;
			(r, g, b) = (glow.X, glow.Y, glow.Z);
		}
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		var data = TileObjectData.GetTileData(Type, 0);

		if (!CrossMod.Spooky.CheckFind("CandleItem", out ModItem candle) && frameY > data.Height * 18)
			Item.NewItem(new EntitySource_TileBreak(i, j), new Rectangle(i * 16, j * 16, data.Width * 16, data.Height * 16), candle.Type);
	}

	public override void MouseOver(int i, int j)
	{
		Tile tile = Main.tile[i, j];
		var data = TileObjectData.GetTileData(tile);

		if (!CrossMod.Spooky.CheckFind("CandleItem", out ModItem candle) && tile.TileFrameY < data.Height * 18)
			return;
		
		Player player = Main.LocalPlayer;
		bool hasCandle = player.HeldItem.type == candle.Type;

		player.cursorItemIconEnabled = hasCandle;
		player.cursorItemIconID = hasCandle ? candle.Type : 0;
		player.cursorItemIconText = "";
	}

	public override void MouseOverFar(int i, int j)
	{
		MouseOver(i, j);
		Player player = Main.LocalPlayer;
		player.cursorItemIconEnabled = false;
		player.cursorItemIconID = ItemID.None;
	}

	public override bool RightClick(int i, int j)
	{
		Player player = Main.LocalPlayer;

		if (!CrossMod.Spooky.CheckFind("CandleItem", out ModItem candle))
			return false;

		Tile anchor = Main.tile[i, j];
		var data = TileObjectData.GetTileData(anchor);

		if (player.HeldItem.type == candle.Type && player.ConsumeItem(candle.Type) && anchor.TileFrameY < data.Height * 18)
		{
			SoundEngine.PlaySound(SoundID.Dig, new Vector2(i * 16, j * 16));

			int left = i - Framing.GetTileSafely(i, j).TileFrameX / 18 % data.Width;
			int top = j - Framing.GetTileSafely(i, j).TileFrameY / 18 % data.Height;

			for (int x = left; x < left + data.Width; x++)
			{
				for (int y = top; y < top + data.Height; y++)
				{
					Tile tile = Framing.GetTileSafely(x, y);
					tile.TileFrameY += (short)(18 * data.Height);
				}
			}

			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendTileSquare(-1, left, top, Math.Max(data.Width, data.Height));
		}

		return true;
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch) 
	{
		if (!TileMethods.GetVisualInfo(i, j, out Color color, out Texture2D tex))
			return false;

		Tile tile = Main.tile[i, j];
		int frameY = tile.TileFrameY;
		var data = TileObjectData.GetTileData(tile);

		if (tile.TileFrameY >= data.Height * 18)
			frameY -= data.Height * 18;

		Vector2 position = Helpers.GetTilePosition(i, j) + new Vector2(0, -2);
		spriteBatch.Draw(tex, position, new Rectangle(tile.TileFrameX, frameY, 16, 16), color);
		spriteBatch.Draw(tex, position, new Rectangle(tile.TileFrameX, tile.TileFrameY + data.Height * 18, 16, 16), Color.White);
		return false;
	}
}
