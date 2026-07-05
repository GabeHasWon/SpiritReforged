using SpiritReforged.Common.ModCompat;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Crossmod.SpookyForest;

internal abstract class StarGourd : ModTile
{
	public interface IGourdInfo
	{
		public int ItemType { get; }
		public int CarvedType { get; }
	}

	public readonly record struct GourdInfo<T>(string ItemName) : IGourdInfo where T : StarGourd
	{
		public int ItemType => CrossMod.Spooky.CheckFind(ItemName, out ModItem item) ? item.Type : throw null;
		public int CarvedType => ModContent.TileType<T>();
	}

	protected static SoundStyle CarveSound;

	protected abstract IGourdInfo Info { get; }

	public override bool IsLoadingEnabled(Mod mod) => CrossMod.Spooky.Enabled;

	public override void SetStaticDefaults()
	{
		CarveSound = new("Spooky/Content/Sounds/PumpkinCarve", SoundType.Sound);

		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;

		TileID.Sets.BreakableWhenPlacing[Type] = true;

		AddMapEntry(new Color(25, 197, 87));

		DustType = DustID.DesertWater2;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
		TileObjectData.newTile.Origin = new Point16(1, 1);
		TileObjectData.newTile.DrawYOffset = 2;

		ModifyObjectData(TileObjectData.newTile);
		TileObjectData.addTile(Type);
	}

	protected virtual void ModifyObjectData(TileObjectData newTile) { }

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

			int left = i - Framing.GetTileSafely(i, j).TileFrameX / 18 % 3;
			int top = j - Framing.GetTileSafely(i, j).TileFrameY / 18 % 2;

			for (int x = left; x < left + 3; x++)
			{
				for (int y = top; y < top + 2; y++)
				{
					Tile tile = Framing.GetTileSafely(x, y);
					tile.TileType = (ushort)Info.CarvedType;
				}
			}

			if (Main.netMode != NetmodeID.SinglePlayer)
			{
				NetMessage.SendTileSquare(-1, left, top, 12);
			}
		}

		return true;
	}

	public override void KillMultiTile(int i, int j, int fX, int fY) => Item.NewItem(new EntitySource_TileBreak(i, j), i * 16, j * 16, 32, 16, Info.ItemType, Main.rand.Next(15, 26));
}
