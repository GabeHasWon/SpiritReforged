using SpiritReforged.Content.Desert.GildedScarab;
using SpiritReforged.Content.Forest.ArcaneNecklace;
using SpiritReforged.Content.Ocean.Hydrothermal;

namespace SpiritReforged.Common.ModCompat.Classic;

/// <summary> Handles cross mod compatibility with Spirit Classic. Only loads when Spirit Classic is also loaded. </summary>
internal class SpiritClassic : ModSystem
{
	private const string ClassicName = "SpiritMod";

	/// <summary> Whether Spirit Classic is enabled. </summary>
	public static bool Enabled => Loaded || ModLoader.HasMod(ClassicName);
	/// <summary> The loaded Spirit Classic mod instance. Check <see cref="Enabled"/> before using. </summary>
	internal static Mod ClassicMod = null;
	/// <summary> Spirit Classic items and their Reforged replacements. </summary>
	internal static readonly Dictionary<int, int> ClassicToReforged = [];

	private static bool Loaded = false;

	public override bool IsLoadingEnabled(Mod mod)
	{
		if (!Enabled)
			return false;

		ClassicMod = ModLoader.GetMod(ClassicName);
		Loaded = true;

		mod.AddContent((ObsoleteItem)Activator.CreateInstance(typeof(ObsoleteItem)));
		mod.AddContent((ModifyNPCData)Activator.CreateInstance(typeof(ModifyNPCData)));

		return true;
	}

	public override void SetStaticDefaults()
	{
		foreach (var item in Mod.GetContent<ModItem>()) //Populate Classic/Reforged counterpart item array
		{
			int id = ClassicItem(item);
			if (id == -1)
				continue;

			ClassicToReforged.Add(id, item.Type);
			ItemID.Sets.ShimmerTransformToItem[id] = item.Type; //Populate shimmer transformations

			ClassicMod.Call("AddItemDefinition", id, item.Type);
		}

		if (ClassicMod.TryFind("SulfurDeposit", out ModItem sulfur)) //Add Sulfur to our Hydrothermal vent pool
			HydrothermalVentPlume.DropPool.Add(sulfur.Type, 3);

		static int ClassicItem(ModItem modItem)
		{
			string name; //Match the provided attribute name, otherwise, match internal names
			var attr = (FromClassicAttribute)Attribute.GetCustomAttribute(modItem.GetType(), typeof(FromClassicAttribute), false);

			if (attr != null)
				name = attr.name;
			else
				name = modItem.Name;

			return ClassicMod.TryFind(name, out ModItem item) ? item.Type : -1;
		}
	}

	public override void AddRecipes()
	{
		if (ClassicMod.TryFind("Chitin", out ModItem chitin))
			Recipe.Create(ModContent.ItemType<GildedScarab>()).AddRecipeGroup("GoldBars", 5).AddIngredient(chitin.Type, 8).AddTile(TileID.Anvils).Register();

		if (ClassicMod.TryFind("SeraphimBulwark", out ModItem bulwark) && ClassicMod.TryFind("ManaShield", out ModItem manaShield) && ClassicMod.TryFind("SoulShred", out ModItem soul))
			Recipe.Create(bulwark.Type).AddIngredient(ModContent.ItemType<ArcaneNecklacePlatinum>()).AddIngredient(manaShield.Type).AddIngredient(soul.Type, 5).AddTile(TileID.TinkerersWorkbench).Register();
	}

	public override void PostAddRecipes()
	{
		string disLog = "Disabled recipes from Classic: ";
		string modLog = "Adapted recipes from Classic: ";

		foreach (var recipe in Main.recipe)
		{
			if (recipe.Mod == null || recipe.Mod.Name != ClassicName)
				continue;

			int cType = recipe.createItem?.type ?? 0;
			if (ModifyRecipe(recipe, out bool modified))
			{
				if (modified)
					modLog += $"{ItemLoader.GetItem(cType)?.Name ?? string.Empty} ({recipe.RecipeIndex}), ";

				continue;
			}

			recipe.DisableRecipe();
			disLog += $"{ItemLoader.GetItem(cType)?.Name ?? string.Empty} ({recipe.RecipeIndex}), ";
		}

		SpiritReforgedMod.Instance.Logger.Info(disLog.Remove(disLog.Length - 2, 2));
		SpiritReforgedMod.Instance.Logger.Info(modLog.Remove(modLog.Length - 2, 2));
	}

	/// <summary> Modifies recipes added by Classic if the result isn't contained in <see cref="ClassicToReforged"/> but ingredients are. </summary>
	private static bool ModifyRecipe(Recipe recipe, out bool modified)
	{
		modified = false;

		if (ClassicToReforged.ContainsKey(recipe.createItem.type))
			return false; //The result is obsolete

		for (int i = recipe.requiredItem.Count - 1; i >= 0; i--)
		{
			var ingredient = recipe.requiredItem[i];

			if (!ingredient.IsAir && ClassicToReforged.TryGetValue(ingredient.type, out int reforgedType))
			{
				int stack = ingredient.stack;

				recipe.requiredItem[i].ChangeItemType(reforgedType);
				recipe.requiredItem[i].stack = stack;

				modified = true;
			}
		}

		return true;
	}

	public override void PostWorldGen()
	{
		foreach (var c in Main.chest)
		{
			if (c is null)
				continue;

			EditContents(c.item);
		}
	}

	/// <summary> Converts all Classic chest loot to their Reforged counterparts. </summary>
	private static void EditContents(Item[] items)
	{
		for (int i = 0; i < items.Length; i++)
		{
			var item = items[i];

			if (!item.IsAir && ClassicToReforged.TryGetValue(item.type, out int reforgedType))
				item.ChangeItemType(reforgedType);
		}
	}

	/// <summary> Manually adds a ModItem replacement entry to <see cref="ClassicToReforged"/>, if loaded. </summary>
	public static void AddReplacement(string classicName, int reforgedType)
	{
		if (Enabled && ClassicMod.TryFind(classicName, out ModItem item))
			ClassicToReforged.Add(item.Type, reforgedType);
	}
}

/// <summary> Denotes an item replacement for Spirit Classic. </summary>
/// <param name="classicName"> The internal name of item to replace. </param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class FromClassicAttribute(string classicName) : Attribute
{
	public string name = classicName;
}