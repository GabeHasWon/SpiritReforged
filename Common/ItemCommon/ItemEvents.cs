using System.Linq;
using Terraria.ModLoader.Core;

namespace SpiritReforged.Common.ItemCommon;

internal sealed class ItemEvents : GlobalItem
{
	public interface IQuickRecipeNPC
	{
		public void AddRecipes();
	}

	public delegate void DefaultsDelegate(Item item);
	/// <returns> True to continue vanilla behaviour. </returns>
	public delegate bool PrefixDelegate(Item item, int prefix);

	public static event PrefixDelegate OnPrefix;

	public override void Load() => On_Item.Prefix += PreventPrefix;

	private static bool PreventPrefix(On_Item.orig_Prefix orig, Item self, int prefixWeWant)
	{
		if (OnPrefix?.Invoke(self, prefixWeWant) == false)
			return false;

		orig(self, prefixWeWant);
		return true;
	}

	#region defaults
	internal static readonly Dictionary<int, DefaultsDelegate> DefaultByType = [];

	/// <summary> Binds <paramref name="dele"/> to the provided <paramref name="itemType"/> and invokes it whenever <see cref="GlobalType{TEntity, TGlobal}.SetDefaults(TEntity)"/> is called. </summary>
	public static void CreateItemDefaults(int itemType, DefaultsDelegate dele) => DefaultByType.Add(itemType, dele);
	/// <inheritdoc cref="CreateItemDefaults(int, DefaultsDelegate)"/>
	public static void CreateItemDefaults(DefaultsDelegate dele, params int[] itemTypes)
	{
		foreach (int type in itemTypes)
			CreateItemDefaults(type, dele);
	}

	public override void SetDefaults(Item entity)
	{
		if (DefaultByType.TryGetValue(entity.type, out var dele))
			dele.Invoke(entity);
	}
	#endregion

	public override void AddRecipes()
	{
		var types = AssemblyManager.GetLoadableTypes(GetType().Assembly).Where(x => typeof(IQuickRecipeNPC).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract);

		foreach (Type type in types)
		{
			var npc = (ModNPC)Activator.CreateInstance(type);

			if (npc is IQuickRecipeNPC recipe)
				recipe.AddRecipes();
		}
	}
}