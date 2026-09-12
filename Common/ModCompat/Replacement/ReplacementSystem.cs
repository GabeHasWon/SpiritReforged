using SpiritReforged.Content.Forest.ArcaneNecklace;
using SpiritReforged.Content.Underground.WayfarerSet;
using SpiritReforged.Content.Ziggurat.Scarab;

namespace SpiritReforged.Common.ModCompat.Replacement;

/// <summary> Denotes a replacement for items or NPCs.<para/>
/// Works for both modded and vanilla content. </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ReplaceContentAttribute : Attribute
{
	public ReplaceContentAttribute(string fullName) => Name = fullName;

	public ReplaceContentAttribute(int type) => Type = type;

	public readonly string Name;
	public readonly int Type;
}

internal class ReplacementSystem : ModSystem
{
	/// <summary> Reforged items and their replacements. </summary>
	public static readonly Dictionary<int, int> ItemToReplacement = [];
	/// <summary> Reforged NPCs and their replacements. </summary>
	public static readonly Dictionary<int, int> NPCToReplacement = [];

	public override void SetStaticDefaults()
	{
		foreach (ModItem item in Mod.GetContent<ModItem>()) //Populate counterpart item array
		{
			if (GetAttributeType(item, out int type, false))
				AddItemReplacement(type, item.Type);
		}

		foreach (ModNPC npc in Mod.GetContent<ModNPC>()) //Populate counterpart NPC array
		{
			if (GetAttributeType(npc, out int type, true))
				NPCToReplacement.Add(type, npc.Type);
		}

		static bool GetAttributeType(ModType modType, out int type, bool npc = false)
		{
			type = -1;
			if (npc) //NPC content
			{
				if (Attribute.GetCustomAttribute(modType.GetType(), typeof(ReplaceContentAttribute), false) is ReplaceContentAttribute attribute)
				{
					if (attribute.Type != ItemID.None)
						type = attribute.Type;
					else if (ModContent.TryFind(attribute.Name, out ModNPC modNPC))
						type = modNPC.Type;
				}
				else if (CrossMod.Classic.Enabled && CrossMod.Classic.TryFind(modType.Name, out ModNPC modNPC)) //If no attribute exists, try matching internal names with classic only
				{
					type = modNPC.Type;
				}
			}
			else //Item content
			{
				if (Attribute.GetCustomAttribute(modType.GetType(), typeof(ReplaceContentAttribute), false) is ReplaceContentAttribute attribute)
				{
					if (attribute.Type != ItemID.None)
						type = attribute.Type;
					else if (ModContent.TryFind(attribute.Name, out ModItem modItem))
						type = modItem.Type;
				}
				else if (CrossMod.Classic.Enabled && CrossMod.Classic.TryFind(modType.Name, out ModItem modItem)) //If no attribute exists, try matching internal names with classic only
				{
					type = modItem.Type;
				}
			}

			return type != -1;
		}
	}

	public override void PostAddRecipes()
	{
		string disLog = "Disabled recipes for item: ";
		string modLog = "Adjusted recipes for item: ";

		foreach (Recipe recipe in Main.recipe)
		{
			int resultType = recipe.createItem.type;
			if (ItemToReplacement.ContainsKey(resultType) && !ModifyClassicResult(recipe))
			{
				recipe.DisableRecipe();
				disLog += $"{ItemLoader.GetItem(resultType)?.Name ?? string.Empty} ({recipe.RecipeIndex}), ";
			} //Replace recipe result type
			else
			{
				bool modified = false;

				for (int i = recipe.requiredItem.Count - 1; i >= 0; i--)
				{
					Item ingredient = recipe.requiredItem[i];

					if (!ingredient.IsAir && ItemToReplacement.TryGetValue(ingredient.type, out int replacementType))
					{
						int stack = ingredient.stack;
						modified = true;

						recipe.requiredItem[i].ChangeItemType(replacementType);
						recipe.requiredItem[i].stack = stack;
					}
				}

				if (modified)
					modLog += $"{ItemLoader.GetItem(resultType)?.Name ?? string.Empty} ({recipe.RecipeIndex}), ";
			} //Replace ingredient type
		}

		SpiritReforgedMod.Instance.Logger.Info(disLog[..^2]);
		SpiritReforgedMod.Instance.Logger.Info(modLog[..^2]);
	}

	public override void PostWorldGen() //Replace chest contents
	{
		foreach (var c in Main.chest)
		{
			if (c is null)
				continue;

			for (int i = 0; i < c.item.Length; i++)
			{
				var item = c.item[i];

				if (!item.IsAir && ItemToReplacement.TryGetValue(item.type, out int replacementType))
					item.ChangeItemType(replacementType);
			}
		}
	}

	/// <summary> Manually adds a ModItem replacement entry to <see cref="ItemToReplacement"/>. </summary>
	public static void AddItemReplacement(string fullName, int reforgedType)
	{
		if (ModContent.TryFind(fullName, out ModItem item))
			AddItemReplacement(item.Type, reforgedType);
	}

	/// <summary> Manually adds a ModItem replacement entry to <see cref="ItemToReplacement"/>. </summary>
	private static void AddItemReplacement(int type, int reforgedType)
	{
		ItemToReplacement.Add(type, reforgedType);

		if (CrossMod.Classic.Enabled && ItemLoader.GetItem(type)?.Mod == (Mod)CrossMod.Classic) //Additional features for Classic
		{
			ItemID.Sets.ShimmerTransformToItem[type] = reforgedType; //Populate shimmer transformations
			((Mod)CrossMod.Classic).Call("AddItemDefinition", type, reforgedType);
		}
	}

	#region classic methods
	public override void AddRecipes() //Add recipes for Classic
	{
		if (CrossMod.Classic.Enabled)
		{
			if (CrossMod.Classic.TryFind("Chitin", out ModItem chitin))
				Recipe.Create(ModContent.ItemType<GildedScarab>()).AddRecipeGroup("GoldBars", 5).AddIngredient(chitin.Type, 8).AddTile(TileID.Anvils).Register();

			if (CrossMod.Classic.TryFind("SeraphimBulwark", out ModItem bulwark) && CrossMod.Classic.TryFind("ManaShield", out ModItem manaShield) && CrossMod.Classic.TryFind("SoulShred", out ModItem soul))
				Recipe.Create(bulwark.Type).AddIngredient(ModContent.ItemType<ArcaneNecklacePlatinum>()).AddIngredient(manaShield.Type).AddIngredient(soul.Type, 5).AddTile(TileID.TinkerersWorkbench).Register();
		}
	}

	private static bool ModifyClassicResult(Recipe recipe)
	{
		return Match<WayfarerHead>() || Match<WayfarerBody>() || Match<WayfarerLegs>(); //Conditionally modifies recipes added by Classic rather than disabling them

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
	#endregion
}