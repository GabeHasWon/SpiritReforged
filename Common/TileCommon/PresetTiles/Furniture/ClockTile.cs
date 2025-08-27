using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;

namespace SpiritReforged.Common.TileCommon.PresetTiles;

public abstract class ClockTile : FurnitureTile
{
	public override void SetItemDefaults(ModItem item) => item.Item.value = Item.sellPrice(copper: 60);

	public override void AddItemRecipes(ModItem item)
	{
		if (Info.Material != ItemID.None)
			item.CreateRecipe().AddRecipeGroup(RecipeGroupID.IronBar, 3).AddIngredient(ItemID.Glass, 6).AddIngredient(Info.Material, 10).AddTile(TileID.Sawmill).Register();
	}

	public override void StaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileID.Sets.HasOutlines[Type] = true;
		TileID.Sets.Clock[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Height = 5;
		TileObjectData.newTile.Origin = new Point16(1, 4);
		TileObjectData.newTile.CoordinateHeights = [16, 16, 16, 16, 18];
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(100, 100, 60), Language.GetText("ItemName.GrandfatherClock"));
		AdjTiles = [TileID.GrandfatherClocks];
		DustType = -1;
	}

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

	public override bool RightClick(int x, int y)
	{
		//Post the time
		string text = "AM";
		double time = Main.time;

		if (!Main.dayTime)
			time += 54000.0;

		time = time / 86400.0 * 24.0;
		time = time - 7.5 - 12.0;

		if (time < 0.0)
			time += 24.0;

		if (time >= 12.0)
			text = "PM";

		int intTime = (int)time;
		double deltaTime = time - intTime;
		deltaTime = (int)(deltaTime * 60.0);
		string text2 = string.Concat(deltaTime);

		if (deltaTime < 10.0)
			text2 = "0" + text2;

		if (intTime > 12)
			intTime -= 12;

		if (intTime == 0)
			intTime = 12;

		string newText = string.Concat("Time: ", intTime, ":", text2, " ", text);
		Main.NewText(newText, 255, 240, 20);

		return true;
	}

	public override void MouseOver(int i, int j)
	{
		Player Player = Main.LocalPlayer;
		Player.noThrow = 2;
		Player.cursorItemIconEnabled = true;
		Player.cursorItemIconID = Info.Item.Type;
	}
}
