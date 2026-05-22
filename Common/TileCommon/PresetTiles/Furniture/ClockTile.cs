using SpiritReforged.Common.ModCompat;
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

		AddMapEntry(CommonColor, Language.GetText("ItemName.GrandfatherClock"));
		AdjTiles = [TileID.GrandfatherClocks];
		DustType = -1;
	}

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

	public override bool RightClick(int x, int y)
	{
		//Post the time
		double time = Main.time;

		if (!Main.dayTime)
			time += 54000.0;

		time = time / 86400.0 * 24.0;
		time = time - 7.5 - 12.0;

		if (time < 0.0)
			time += 24.0;
		if (time >= 24.0)
			time -= 24.0;

		int hours = (int)time;
		int minutes = (int)((time - hours) * 60.0);

		bool use24Hour = CrossMod.RussianTranslate.Enabled;

		string timeText;

		if (use24Hour)
		{
			timeText = $"{hours}:{minutes:00}";
		}
		else
		{
			string period = Language.GetTextValue(hours >= 12 ? "GameUI.TimePastMorning" : "GameUI.TimeAtMorning");
			int displayHours = hours % 12;
			if (displayHours == 0)
				displayHours = 12;

			timeText = $"{displayHours}:{minutes:00} {period}";
		}

		Main.NewText(Language.GetTextValue("CLI.Time", timeText), 255, 240, 20);
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
