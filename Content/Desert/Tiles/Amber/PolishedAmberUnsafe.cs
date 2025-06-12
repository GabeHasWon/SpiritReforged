using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals;
using System.Linq;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Content.Desert.Tiles.Amber;

public partial class PolishedAmberUnsafe : PolishedAmber
{
	public override string Texture => DrawHelpers.RequestLocal(GetType(), "PolishedAmber");
	private static int[] ContainedItems;

	public static int GetContainedItem(int i, int j) => ContainedItems[Main.tile[i, j].TileFrameY / FullFrameHeight];

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		this.Merge(ModContent.TileType<PolishedAmber>());

		RegisterItemDrop(AutoContent.ItemType<PolishedAmber>());
		ContainedItems = [ItemID.Firefly, ItemID.Frog, ItemID.BlueDragonfly, ItemID.RedDragonfly];
	}

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		var original = base.GetItemDrops(i, j) ?? [];
		return original.Concat([new Item(GetContainedItem(i, j))]); //Drop the contained item in addition to the original
	}

	public override void PostTileFrame(int i, int j, int up, int down, int left, int right, int upLeft, int upRight, int downLeft, int downRight)
	{
		if (TileEvents.ResetFrame)
		{
			var t = Main.tile[i, j];
			t.TileFrameY = (short)(t.TileFrameY % FullFrameHeight + FullFrameHeight * Main.rand.Next(ContainedItems.Length));
		}
	}

	public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
	{
		int type = GetContainedItem(i, j);
		Main.instance.LoadItem(type);

		var texture = TextureAssets.Item[type].Value;
		var position = new Vector2(i, j).ToWorldCoordinates() - Main.screenPosition;
		float rotation = (i + j) * 0.25f % MathHelper.TwoPi;

		spriteBatch.Draw(texture, position, null, Lighting.GetColor(i, j) * 0.4f, rotation, texture.Size() / 2, 1, default, 0);
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (TileDrawing.IsVisible(Main.tile[i, j]))
			Main.instance.TilesRenderer.AddSpecialPoint(i, j, TileDrawing.TileCounterType.CustomSolid);

		return base.PreDraw(i, j, spriteBatch);
	}
}