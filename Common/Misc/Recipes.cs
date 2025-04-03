namespace SpiritReforged.Common.Misc;

internal class Recipes : ModSystem
{
	private static Dictionary<int, int> groupEntries = [];

	/// <summary> Add the given item <paramref name="type"/> to the existing recipe group of <paramref name="groupID"/>. </summary>
	/// <param name="groupID"> The recipe group to add to. </param>
	/// <param name="type"> The item type to add. </param>
	public static void AddToGroup(int groupID, int type) => groupEntries.Add(type, groupID);

	public override void AddRecipeGroups()
	{
		foreach (var pair in groupEntries)
		{
			var group = RecipeGroup.recipeGroups[pair.Value];
			group.ValidItems.Add(pair.Key);
		}

		groupEntries = null;

		RecipeGroup BaseGroup(object GroupName, int[] Items)
		{
			string Name = "";
			Name += GroupName switch
			{
				//modcontent items
				int i => Lang.GetItemNameValue((int)GroupName),
				//vanilla item ids
				short s => Lang.GetItemNameValue((short)GroupName),
				//custom group names
				_ => GroupName.ToString(),
			};
			return new RecipeGroup(() => Language.GetTextValue("LegacyMisc.37") + " " + Name, Items);
		}

		RecipeGroup.RegisterGroup("CopperBars", BaseGroup(ItemID.CopperBar, [ItemID.CopperBar, ItemID.TinBar]));
		RecipeGroup.RegisterGroup("SilverBars", BaseGroup(ItemID.SilverBar, [ItemID.SilverBar, ItemID.TungstenBar]));
		RecipeGroup.RegisterGroup("GoldBars", BaseGroup(ItemID.GoldBar, [ItemID.GoldBar, ItemID.PlatinumBar]));
		RecipeGroup.RegisterGroup("Tier3HMBar", BaseGroup(ItemID.AdamantiteBar, [ItemID.AdamantiteBar, ItemID.TitaniumBar]));
		RecipeGroup.RegisterGroup("PHMEvilMaterial", BaseGroup(ItemID.ShadowScale, [ItemID.ShadowScale, ItemID.TissueSample]));
		RecipeGroup.RegisterGroup("EvilMaterial", BaseGroup(ItemID.CursedFlame, [ItemID.CursedFlame, ItemID.Ichor]));
		RecipeGroup.RegisterGroup("Shells", BaseGroup(ItemID.Seashell, [ItemID.Seashell, ItemID.TulipShell, ItemID.JunoniaShell, ItemID.LightningWhelkShell]));
		RecipeGroup.RegisterGroup("ClayAndMud", BaseGroup(ItemID.ClayBlock, [ItemID.ClayBlock, ItemID.MudBlock]));
	}
}
