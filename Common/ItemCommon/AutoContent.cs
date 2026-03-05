namespace SpiritReforged.Common.ItemCommon;

public static class AutoContent
{
	public const string Suffix = "Item";

	/// <summary> Attempts to find the autoloaded ModItem associated with the given Type. Throws exceptions on failure. </summary>
	public static ModItem ModItem<T>(string prepend = "") where T : ModType => SpiritReforgedMod.Instance.Find<ModItem>(ModContent.GetInstance<T>().Name + prepend + Suffix);

	/// <inheritdoc cref="ModItem{T}(string)"/>
	public static ModItem AutoModItem<T>(this T t, string prepend = "") where T : ModType => SpiritReforgedMod.Instance.Find<ModItem>(t.Name + prepend + Suffix);

	/// <summary> Attempts to find the autoloaded item associated with the given Type. Throws exceptions on failure. </summary>
	public static Item Item<T>(string prepend = "") where T : ModType => ModItem<T>(prepend).Item;

	/// <inheritdoc cref="Item{T}(string)"/>
	public static Item AutoItem<T>(this T t, string prepend = "") where T : ModType => t.AutoModItem(prepend).Item;

	/// <summary> Attempts to find the autoloaded item type associated with the given Type. Throws exceptions on failure. </summary>
	public static int ItemType<T>(string prepend = "") where T : ModType => ModItem<T>(prepend).Type;

	/// <inheritdoc cref="ItemType{T}(string)"/>
	public static int AutoItemType<T>(this T t, string prepend = "") where T : ModType => t.AutoModItem(prepend).Type;
}