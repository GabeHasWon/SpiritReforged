using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals;
using System.Linq;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Content.Desert.Tiles.Amber;

/// <summary> A placeable amber tile that also generates naturally. </summary>
public class PolishedAmber : ShiningAmber, IAutoloadTileItem
{
	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		this.AutoItem().ResearchUnlockCount = 100;
	}
}

/// <summary> A naturally-generating amber fossil with no associated item. </summary>
public class FossilAmber : ShiningAmber
{
	public override string Texture => DrawHelpers.RequestLocal(GetType(), "PolishedAmber");
	public static int[] ContainedItems { get; private set; }
	/// <summary> Preserves the number of styles over <see cref="ShiningAmber.FullFrameHeight"/> after framing. </summary>
	private static int Overload;

	public static int GetOverload(int i, int j) => Main.tile[i, j].TileFrameY / FullFrameHeight;
	public static int GetContainedItem(int i, int j) => ContainedItems[GetOverload(i, j)];

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		RegisterItemDrop(AutoContent.ItemType<PolishedAmber>());
		ContainedItems = [ItemID.Frog, ItemID.BlueDragonfly, ItemID.RedDragonfly, ItemID.Grasshopper, .. Recipes.GetTypesFromGroup(RecipeGroupID.Fireflies)];
	}

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		var original = base.GetItemDrops(i, j) ?? [];
		return original.Concat([new Item(GetContainedItem(i, j))]); //Drop the contained item in addition to the original
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		Overload = GetOverload(i, j);
		return base.TileFrame(i, j, ref resetFrame, ref noBreak);
	}

	public override void PostTileFrame(int i, int j, int up, int down, int left, int right, int upLeft, int upRight, int downLeft, int downRight)
	{
		var t = Main.tile[i, j];
		if (TileEvents.ResetFrame)
			t.TileFrameY = (short)(t.TileFrameY % FullFrameHeight + FullFrameHeight * Main.rand.Next(ContainedItems.Length));
		else
			t.TileFrameY = (short)(t.TileFrameY + FullFrameHeight * Overload);
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

/// <summary> A placeable amber fossil. </summary>
public class FossilAmberSafe : FossilAmber, IAutoloadTileItem
{
	public void AddItemRecipes(ModItem item)
	{
		StartRecipe().AddRecipeGroup(RecipeGroupID.Fireflies).Register();
		StartRecipe().AddRecipeGroup(RecipeGroupID.Dragonflies).Register();
		StartRecipe().AddIngredient(ItemID.Grasshopper).Register();
		StartRecipe().AddIngredient(ItemID.Frog).Register();

		Recipe StartRecipe() => item.CreateRecipe(10).AddIngredient(AutoContent.ItemType<PolishedAmber>(), 10);
	}

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		RegisterItemDrop(this.AutoItemType());
		this.AutoItem().ResearchUnlockCount = 100;
	}

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		int type = TileLoader.GetItemDropFromTypeAndStyle(Type);
		return [new Item(type)];
	}
}