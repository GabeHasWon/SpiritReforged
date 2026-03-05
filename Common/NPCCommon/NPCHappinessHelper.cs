using Terraria.GameContent.Personalities;

namespace SpiritReforged.Common.NPCCommon;

internal class NPCHappinessHelper : ILoadable
{
	private static readonly Dictionary<int, IShopPersonalityTrait> Traits = [];

	public void Load(Mod mod) => On_PersonalityDatabase.Register_int_IShopPersonalityTrait += RecordTraitRegistry;
	private static void RecordTraitRegistry(On_PersonalityDatabase.orig_Register_int_IShopPersonalityTrait orig, PersonalityDatabase self, int npcId, IShopPersonalityTrait trait)
	{
		orig(self, npcId, trait);
		Traits.Add(npcId, trait);
	}

	/// <summary> Sets all NPC affection for biome <paramref name="T"/> between that of <paramref name="a"/> and <paramref name="b"/>. </summary>
	public static void SetAverage<T>(AShoppingBiome a, AShoppingBiome b) where T : ModBiome
	{
		//Sorted by priority rather than biome A/B.
		var tempDict1 = new Dictionary<int, BiomePreferenceListTrait.BiomePreference>();
		var tempDict2 = new Dictionary<int, BiomePreferenceListTrait.BiomePreference>();

		foreach (int id in Traits.Keys)
		{
			if (Traits[id] is BiomePreferenceListTrait bTraits)
			{
				foreach (var bTrait in bTraits)
				{
					if (bTrait.Biome == a || bTrait.Biome == b)
					{
						if (!tempDict1.TryAdd(id, bTrait))
							tempDict2.TryAdd(id, bTrait);
					}
				}
			}
		}

		foreach (int id in tempDict1.Keys)
		{
			float affectionA = (float)tempDict1[id].Affection;
			float affectionB = tempDict2.TryGetValue(id, out var preferenceB) ? (float)preferenceB.Affection : affectionA;

			float finalAffection = MathHelper.Lerp(affectionA, affectionB, .5f);
			NPCHappiness.Get(id).SetBiomeAffection<T>((AffectionLevel)finalAffection);
		}
	}

	public void Unload() { }
}