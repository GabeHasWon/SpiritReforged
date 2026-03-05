using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.UI.PotCatalogue;
using SpiritReforged.Common.UI.System;
using Terraria.GameContent.ObjectInteractions;

namespace SpiritReforged.Content.Underground.Pottery;

public class PotteryWheel : ModTile, IAutoloadTileItem
{
	private const int FullFrameHeight = 18 * 3;

	public void AddItemRecipes(ModItem item) => item.CreateRecipe().AddIngredient(ItemID.StoneBlock, 10)
		.AddRecipeGroup(RecipeGroupID.Wood, 15).AddIngredient(ItemID.BottledWater).AddTile(TileID.WorkBenches).Register();

	public override void SetStaticDefaults()
	{
		Main.tileLighted[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileWaterDeath[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileID.Sets.InteractibleByNPCs[Type] = true;
		TileID.Sets.HasOutlines[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(191, 142, 111), this.AutoItem().ModItem.DisplayName);
		DustType = DustID.WoodFurniture;
		AnimationFrameHeight = FullFrameHeight;
	}

	public override bool RightClick(int i, int j)
	{
		Main.playerInventory = false;
		UISystem.SetActive<CatalogueUI>();

		return true;
	}

	public override void MouseOver(int i, int j)
	{
		if (UISystem.IsActive<CatalogueUI>())
			return;

		var p = Main.LocalPlayer;

		p.cursorItemIconEnabled = true;
		p.cursorItemIconID = this.AutoItem().type;
		p.noThrow = 2;
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (Main.dedServ || !TileObjectData.IsTopLeft(i, j))
			return;

		var world = new Vector2(i, j).ToWorldCoordinates(16, 16);

		if (UISystem.IsActive<CatalogueUI>() && Main.LocalPlayer.Distance(world) > 16 * 5)
			UISystem.SetInactive<CatalogueUI>();
	}

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
	public override void AnimateTile(ref int frame, ref int frameCounter)
	{
		if (++frameCounter >= 4)
		{
			frameCounter = 0;
			frame = ++frame % 4;
		}
	}
}