using ILLogger;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Linq;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.IO;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

namespace SpiritReforged.Common.WorldGeneration.SecretSeeds;

internal class SecretSeedSystem : ModSystem
{
	/// <summary> Whether the current world has a Spirit secret seed. Abbreviated null check for <see cref="WorldSecretSeed"/>. </summary>
	public static bool HasSecretSeed => WorldSecretSeed != null && !Failed;
	/// <summary> The Spirit secret seed currently in use. Returns null if none. </summary>
	public static SecretSeed WorldSecretSeed { get; private set; }
	/// <summary> Whether setup has failed at some point. Prevents all functionality if so. </summary>
	private static bool Failed;

	private static readonly Dictionary<string, SecretSeed> SecretSeeds = [];

	//public static SecretSeed GetSeed<T>() where T : SecretSeed => SecretSeeds.Values.Where(x => x is T).FirstOrDefault();
	public static void RegisterSeed(SecretSeed seed) => SecretSeeds.Add(seed.Name, seed);

	public override void Load()
	{
		On_UIWorldCreation.ProcessSpecialWorldSeeds += ProcessCustomSeed;
		IL_WorldGen.GenerateWorld += InjectCustomSeed;
		On_AWorldListItem.GetIcon += GetCustomIcon;
	}

	private static void ProcessCustomSeed(On_UIWorldCreation.orig_ProcessSpecialWorldSeeds orig, string processedSeed)
	{
		orig(processedSeed);

		if (Failed)
			return;

		WorldSecretSeed = null;

		foreach (string key in SecretSeeds.Keys)
		{
			if (CompareFromArray(SecretSeeds[key]))
			{
				WorldSecretSeed = SecretSeeds[key];
				break;
			}
		}

		bool CompareFromArray(SecretSeed s)
		{
			foreach (string key in s.Keys)
				if (processedSeed.Equals(key, StringComparison.CurrentCultureIgnoreCase))
					return true;

			return false;
		}
	}

	private static void InjectCustomSeed(ILContext il)
	{
		ILCursor c = new(il);

		var p_seed = c.Method.Parameters.Where(x => x.Name == "seed").FirstOrDefault();
		if (p_seed == default)
		{
			SpiritReforgedMod.Instance.LogIL("Custom World Seeds", "Parameter 'seed' not found.");
			Failed = true;

			return;
		}

		if (!c.TryGotoNext(MoveType.After, x => x.MatchStsfld<Main>("zenithWorld")))
		{
			SpiritReforgedMod.Instance.LogIL("Custom World Seeds", "Member 'zenithWorld' not found.");
			Failed = true;

			return;
		}

		c.EmitLdarg0(); //seed
		c.EmitDelegate(ModifyFinalSeed);
		c.Emit(OpCodes.Starg_S, p_seed);
	}

	/// <summary> Ensure the world seed still gets randomized when using a custom secret seed, like vanilla does. </summary>
	private static int ModifyFinalSeed(int seed)
	{
		if (HasSecretSeed)
		{
			Main.rand = new UnifiedRandom();
			seed = Main.rand.Next(999999999);
		}

		return seed;
	}

	private static Asset<Texture2D> GetCustomIcon(On_AWorldListItem.orig_GetIcon orig, AWorldListItem self)
	{
		var value = orig(self);

		if (TryGetHeader(self.Data, out string name) && SecretSeeds.TryGetValue(name, out var seed))
			return seed.GetIcon(self.Data);

		return value;
	}

	public override void ClearWorld()
	{
		if (!WorldGen.generatingWorld)
			WorldSecretSeed = null;
	}

	public override void SaveWorldData(TagCompound tag)
	{
		if (HasSecretSeed)
			tag[nameof(WorldSecretSeed)] = WorldSecretSeed.Name;
	}

	public override void LoadWorldData(TagCompound tag)
	{
		string name = tag.GetString(nameof(WorldSecretSeed));

		if (SecretSeeds.TryGetValue(name, out var value))
			WorldSecretSeed = value;
	}

	public override void SaveWorldHeader(TagCompound tag)
	{
		if (HasSecretSeed)
			tag[nameof(WorldSecretSeed)] = WorldSecretSeed.Name;
	}

	private static bool TryGetHeader(WorldFileData fileData, out string name)
	{
		if (fileData.TryGetHeaderData(ModContent.GetInstance<SecretSeedSystem>(), out var data) && data.TryGet(nameof(WorldSecretSeed), out string seed))
		{
			name = seed;
			return true;
		}

		name = null;
		return false;
	}
}