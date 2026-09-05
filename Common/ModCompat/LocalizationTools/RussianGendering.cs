using System.Reflection;

namespace SpiritReforged.Common.ModCompat.LocalizationTools;

/// <summary>
/// Helper class for gendering support. Only works for Russian through tRU at the moment.
/// </summary>
[ReinitializeDuringResizeArrays]
internal class RussianGendering : ModSystem
{
	internal static Dictionary<string, HashSet<int>> ItemGendersByGender = [];
	internal static string[] TypeMap = ItemID.Sets.Factory.CreateCustomSet("");

	/// <summary>
	/// Gets the gendering of an item ID. Options are "" (masculine/default), Feminine, Neuter (neutral), and Plural
	/// </summary>
	public static string GetGender(int id) => TypeMap[id];

	public override bool IsLoadingEnabled(Mod mod) => CrossMod.RussianTranslate.Enabled;

	public static void GlyphGendering()
	{
		if (!CrossMod.RussianLocalizable)
			return;

		// Get, find, remap and cache the gendering.
		Mod russ = CrossMod.RussianTranslate.Instance;
		Type prefixOverhaul = russ.Code.GetType("CalamityRuTranslate.Core.ItemGenderPrefixes.PrefixOverhaul");
		FieldInfo genderCollections = prefixOverhaul.GetField("_genderCollections", BindingFlags.Instance | BindingFlags.NonPublic);
		PropertyInfo instanceProp = prefixOverhaul.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
		object instance = instanceProp.GetValue(null, null);
		dynamic dict = genderCollections.GetValue(instance);

		foreach (dynamic pair in dict)
		{
			string name = pair.Key.ToString();
			HashSet<int> value = pair.Value;

			ItemGendersByGender.Add(name, value);
			
			foreach (int val in value)
				TypeMap[val] = name;
		}
	}
	public override void PostSetupContent()
	{
		RussianTranslateCompat.tRUSupport();
		RussianGendering.GlyphGendering();
	}
}
