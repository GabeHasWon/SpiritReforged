using SpiritReforged.Content.Desert.GildedScarab;
using SpiritReforged.Content.Forest.ArcaneNecklace;
using SpiritReforged.Content.Underground.WayfarerSet;

namespace SpiritReforged.Common.ModCompat.Classic;

/// <summary> Handles cross mod compatibility with Spirit Classic. Only loads when Spirit Classic is also loaded. </summary>
internal class SpiritClassic : ModSystem
{
	private const string ClassicName = "SpiritMod";

	/// <summary> Spirit Classic items and their Reforged replacements. </summary>
	internal static readonly Dictionary<int, int> ClassicItemToReforged = [];
	/// <summary> Spirit Classic NPCs and their Reforged replacements. </summary>
	internal static readonly Dictionary<int, int> ClassicNPCToReforged = [];

	public override bool IsLoadingEnabled(Mod mod) => CrossMod.Classic.Enabled;
	public override void SetStaticDefaults()
	{
		foreach (var item in Mod.GetContent<ModItem>()) //Populate Classic/Reforged counterpart item array
		{
			int id = ClassicItem(item);
			if (id == -1)
				continue;

			AddItemReplacement(id, item.Type);
		}

		foreach (var npc in Mod.GetContent<ModNPC>()) //Populate Classic/Reforged counterpart NPC array
		{
			int id = ClassicNPC(npc);
			if (id == -1)
				continue;

			ClassicNPCToReforged.Add(id, npc.Type);
		}

		#region local
		static int ClassicItem(ModItem modItem) => CrossMod.Classic.TryFind(GetName(modItem), out ModItem item) ? item.Type : -1;
		static int ClassicNPC(ModNPC modNPC) => CrossMod.Classic.TryFind(GetName(modNPC), out ModNPC npc) ? npc.Type : -1;

		static string GetName(ModType t)
		{
			string name; //Match the provided attribute name, otherwise, match internal names
			var attr = (FromClassicAttribute)Attribute.GetCustomAttribute(t.GetType(), typeof(FromClassicAttribute), false);

			if (attr != null)
				name = attr.name;
			else
				name = t.Name;

			return name;
		}
		#endregion
	}

	public override void AddRecipes()
	{
		if (CrossMod.Classic.TryFind("Chitin", out ModItem chitin))
			Recipe.Create(ModContent.ItemType<GildedScarab>()).AddRecipeGroup("GoldBars", 5).AddIngredient(chitin.Type, 8).AddTile(TileID.Anvils).Register();

		if (CrossMod.Classic.TryFind("SeraphimBulwark", out ModItem bulwark) && CrossMod.Classic.TryFind("ManaShield", out ModItem manaShield) && CrossMod.Classic.TryFind("SoulShred", out ModItem soul))
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
			if (ModifyIngredients(recipe, out bool modified))
			{
				if (modified)
					modLog += $"{ItemLoader.GetItem(cType)?.Name ?? string.Empty} ({recipe.RecipeIndex}), ";

				continue;
			}

			recipe.DisableRecipe();
			disLog += $"{ItemLoader.GetItem(cType)?.Name ?? string.Empty} ({recipe.RecipeIndex}), ";
		}

		SpiritReforgedMod.Instance.Logger.Info(disLog[..^2]);
		SpiritReforgedMod.Instance.Logger.Info(modLog[..^2]);
	}

	/// <summary> Modifies recipes added by Classic if the result isn't contained in <see cref="ClassicItemToReforged"/> but ingredients are. </summary>
	private static bool ModifyIngredients(Recipe recipe, out bool modified)
	{
		modified = false;

		if (ClassicItemToReforged.ContainsKey(recipe.createItem.type))
		{
			if (ModifyResult(recipe))
			{
				modified = true;
				return true;
			}

			return false; //The result is obsolete
		}

		for (int i = recipe.requiredItem.Count - 1; i >= 0; i--)
		{
			var ingredient = recipe.requiredItem[i];

			if (!ingredient.IsAir && ClassicItemToReforged.TryGetValue(ingredient.type, out int reforgedType))
			{
				int stack = ingredient.stack;

				recipe.requiredItem[i].ChangeItemType(reforgedType);
				recipe.requiredItem[i].stack = stack;

				modified = true;
			}
		}

		return true;
	}

	/// <summary> Conditionally modifies recipes added by Classic if the result is contained in <see cref="ClassicItemToReforged"/>. </summary>
	private static bool ModifyResult(Recipe recipe)
	{
		return Match<WayfarerHead>() || Match<WayfarerBody>() || Match<WayfarerLegs>();

		bool Match<T>() where T : ModItem //Only works if internal names match
		{
			if (CrossMod.Classic.TryFind(typeof(T).Name, out ModItem item) && item.Type == recipe.createItem.type)
			{
				recipe.createItem.ChangeItemType(ModContent.ItemType<T>());
				return true;
			}

			return false;
		}
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

			if (!item.IsAir && ClassicItemToReforged.TryGetValue(item.type, out int reforgedType))
				item.ChangeItemType(reforgedType);
		}
	}

	/// <summary> Manually adds a ModItem replacement entry to <see cref="ClassicItemToReforged"/>, if loaded. </summary>
	public static void AddItemReplacement(string classicName, int reforgedType)
	{
		if (CrossMod.Classic.Enabled && CrossMod.Classic.TryFind(classicName, out ModItem item))
			AddItemReplacement(item.Type, reforgedType);
	}

	/// <summary> Manually adds a ModItem replacement entry to <see cref="ClassicItemToReforged"/> <para/>
	/// Only use this method after checking if Classic is enabled. </summary>
	private static void AddItemReplacement(int classicType, int reforgedType)
	{
		ClassicItemToReforged.Add(classicType, reforgedType);
		ItemID.Sets.ShimmerTransformToItem[classicType] = reforgedType; //Populate shimmer transformations

		((Mod)CrossMod.Classic).Call("AddItemDefinition", classicType, reforgedType);
	}
}

/// <summary> Denotes a content replacement for Spirit Classic. </summary>
/// <param name="classicName"> The internal name of content to replace. </param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class FromClassicAttribute(string classicName) : Attribute
{
	public string name = classicName;
}