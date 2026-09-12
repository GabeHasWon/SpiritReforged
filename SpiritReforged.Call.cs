using MonoMod.Utils;
using SpiritReforged.Common.DebuffOverhaul;
using SpiritReforged.Common.ItemCommon.Backpacks;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.UI.PotCatalogue;
using SpiritReforged.Common.WorldGeneration.Ecotones;
using SpiritReforged.Content.Desert.ScarabBoss.Boss;
using SpiritReforged.Content.Forest.Botanist.Items;
using SpiritReforged.Content.Forest.Safekeeper;
using SpiritReforged.Content.SaltFlats;
using SpiritReforged.Content.Savanna.Ecotone;
using SpiritReforged.Content.Savanna.Tiles.AcaciaTree;
using SpiritReforged.Content.Underground.Tiles;
using SpiritReforged.Content.Underground.Tiles.Potion;
using System.Linq;
using System.Reflection;
using Terraria.DataStructures;
using static SpiritReforged.Common.DebuffOverhaul.BuffExtension;
using static SpiritReforged.Common.TileCommon.Conversion.ConversionHandler;

namespace SpiritReforged;

#nullable enable

public partial class SpiritReforgedMod : Mod
{
	#region system
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	private class ModCallAttribute(params string[] aliases) : Attribute
	{
		public readonly string[] Aliases = aliases;
	}

	/// <summary> All mod call methods registered by name. </summary>
	private static readonly Dictionary<string, MethodInfo> CallMethods = [];

	public override object? Call(params object[] arguments)
	{
		try
		{
			if (CallMethods.Count == 0) //Initialize local methods attributed by [ModCall]
			{
				foreach (MethodInfo methodInfo in GetType().GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
				{
					if (methodInfo.GetCustomAttribute<ModCallAttribute>() is ModCallAttribute attr)
					{
						CallMethods.Add(methodInfo.Name, methodInfo);

						if (attr.Aliases is null)
							continue;

						foreach (string alias in attr.Aliases)
							CallMethods.Add(alias, methodInfo);
					}
				}
			}

			if (arguments.Length == 0)
				throw new ArgumentException("Call has no arguments.");

			if (arguments[0] is not string name)
				throw new ArgumentException($"The leading argument must be a {typeof(string).Name} corresponding to a call.");

			if (name == "fablescrossmod.kaiju")
				return Scarabeus.HandleModCall(arguments); //Handle the Fables crossmod special case

			if (CallMethods.TryGetValue(name, out MethodInfo? info))
			{
				arguments = arguments[1..];

				ParameterInfo[] parameters = info.GetParameters();
				int optionalCount = parameters.Where(x => x.IsOptional).Count(); //The number of optional parameters of this method
				object[] namedObjects = new object[parameters.Length];

				if (arguments.Length > parameters.Length)
					throw new ArgumentException(name + ((optionalCount > 0)
						? $" requires at least {parameters.Length - optionalCount} arguments."
						: $" requires exactly {parameters.Length} arguments."));

				for (int c = 0; c < arguments.Length; c++)
				{
					object argument = arguments[c];
					Type argumentType = argument.GetType();

					if (argument.GetType() == argumentType)
						namedObjects[c] = argument;
					else
					{
						throw new ArgumentException(name + (parameters[c].IsOptional
							? $" argument {c} ({parameters[c].Name}) optionally requires an object of type {argumentType.Name}."
							: $" argument {c} ({parameters[c].Name}) requires an object of type {argumentType.Name}."));
					}
				}

				object? value = info.Invoke(null, namedObjects);
				return value;
			}
			else
			{
				throw new ArgumentException($"Call '{name}' is invalid.");
			}
		}
		catch (Exception e)
		{
			Logger.Error("Call Error: " + e.Message + "\n" + e.StackTrace);
		}

		return null;
	}
	#endregion

	//A list of all mod calls accessible by method name
	#region calls
	[ModCall]
	private static void AddCustomDoT(int buffType, int category, Action<SpriteBatch, NPC, Color, Vector2, float, float>? onPostDraw = null)
		=> BuffHandler.Register(new CustomDoT((DoTExtension.Category)category, onPostDraw), buffType);

	[ModCall]
	private static bool WorldHasEcotone(string ecotoneName) => EcotoneSurfaceMapping.ContainsEcotone(ecotoneName);

	[ModCall]
	private static bool AddHerb(int type, bool customDrawing = false)
	{
		HerbSet.IsHerb[type] = true;
		HerbSet.CustomBotanistDisplay[type] = customDrawing;
		return true;
	}

	[ModCall]
	private static bool AddUndead(int type, bool noDeathAnimation = false)
	{
		UndeadNPC.UndeadTypes.Add(type);

		if (noDeathAnimation)
			UndeadNPC.NoDeathAnim.Add(type);

		return true;
	}

	[ModCall]
	private static void SetSavannaArea(Rectangle area)
	{
		if (!WorldGen.generatingWorld)
			throw new Exception(nameof(SetSavannaArea) + " is unused outside of worldgen.");

		if (SavannaEcotone.SavannaAreas.Count == 0)
			SavannaEcotone.SavannaAreas.Add(area);
		else
			SavannaEcotone.SavannaAreas[0] = area;
	}

	[ModCall]
	private static List<Rectangle> GetSavannaAreas() => SavannaEcotone.SavannaAreas;

	[ModCall]
	private static List<Rectangle> GetSaltFlatsAreas() => SaltFlatsEcotone.SaltFlatsAreas;

	[ModCall]
	private static bool AddPotionVat(int item, Color color, bool decorative)
	{
		if (decorative)
			PotionColorDatabase.DecorativeBrewColors.Add(item, color);
		else
			PotionColorDatabase.NaturalBrewColors.Add(item, color);

		return true;
	}

	[ModCall]
	private static bool HasBackpack(Player player) => player.GetModPlayer<BackpackPlayer>().backpack.ModItem is BackpackItem;

	[ModCall]
	private static bool ManualAddRecord(int type, int[] styles, string recordName, byte rating = byte.MaxValue, Func<bool>? hidden = null, Action<int, Point16, ILoot>? lootPool = null, LocalizedText? description = null, LocalizedText? displayName = null)
	{
		TileRecord tileRecord = new(recordName, type, styles);

		if (rating != byte.MaxValue)
			tileRecord.AddRating(rating);

		if (hidden != null)
			tileRecord.Hide(hidden);

		if (lootPool != null) //Register a loot pool, default if null
			TileLootSystem.RegisterLoot((loot) =>
			{
				if (loot is TileLootTable t)
					lootPool.Invoke(t.Style, t.Coordinates, loot);
			});
		else if (TileLootSystem.TryGetLootPool(ModContent.TileType<Pots>(), out LootTable.LootDelegate pool))
			TileLootSystem.RegisterLoot(pool, type);

		if (description != null)
			tileRecord.AddDescription(description);

		if (displayName != null)
			tileRecord.AddDescription(displayName);

		RecordHandler.Records.Add(tileRecord);
		return true;
	}

	/// <summary>
	/// Backwards compatible method required to not hard crash other mods.
	/// </summary>
	[ModCall]
	private static bool AddPotstiaryRecord(int type, int[] styles, string recordName, byte rating = byte.MaxValue, bool hidden = false, Action<int, ILoot>? lootPool = null, LocalizedText? description = null, LocalizedText? displayName = null)
	{
		TileRecord tileRecord = new(recordName, type, styles);

		if (rating != byte.MaxValue)
			tileRecord.AddRating(rating);

		if (hidden != false)
			tileRecord.Hide();

		if (lootPool != null) //Register a loot pool, default if null
			TileLootSystem.RegisterLoot((loot) =>
			{
				if (loot is TileLootTable t)
					lootPool.Invoke(t.Style, loot);
			});
		else if (TileLootSystem.TryGetLootPool(ModContent.TileType<Pots>(), out LootTable.LootDelegate pool))
			TileLootSystem.RegisterLoot(pool, type);

		if (description != null)
			tileRecord.AddDescription(description);

		if (displayName != null)
			tileRecord.AddDescription(displayName);

		RecordHandler.Records.Add(tileRecord);
		SpiritReforgedMod.Instance.Logger.Debug("[Mod.Call] Consider using the new overload: ManualAddRecord(int type, int[] styles, string recordName, byte rating = byte.MaxValue, Func<bool>? hidden = null, Action<int, Point16, ILoot>? lootPool = null, LocalizedText? description = null, LocalizedText? displayName = null)");
		return true;
	}

	[ModCall]
	private static bool PotDiscovered(string name, Player player) => player.GetModPlayer<RecordPlayer>().IsValidated(name);

	[ModCall]
	private static bool PlayerBotanist(Player player) => BotanistHat.SetActive(player);

	[ModCall]
	private static bool RegisterConversionSet(string setName, Dictionary<int, int> dict)
	{
		var set = new Set();
		set.AddRange(dict);
		CreateSet(setName, set);
		return true;
	}

	[ModCall]
	internal static (bool, int) AddSavannaTree(string texturePath, string tileName, Func<int[]> getAnchor, Mod mod)
		=> (mod.AddContent(new AcaciaTreeCrossmod(texturePath, tileName, getAnchor))) ? (true, mod.Find<ModTile>(tileName).Type) : (false, -1);
	#endregion
}