using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.Loot;
using SpiritReforged.Content.Underground.Tiles;
using System.Linq;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;
using static SpiritReforged.Common.TileCommon.Loot.ILootTile;

namespace SpiritReforged.Content.Underground.Pottery;

public interface IRecordTile : INamedStyles
{
	public void AddRecord(int type, StyleDatabase.StyleGroup group) => RecordHandler.Records.Add(new TileRecord(group.name, type, group.styles));
}

public class RecordHandler : ModSystem
{
	public static readonly HashSet<TileRecord> Records = [];

	/// <summary> Checks whether the tile at the given coordinates corresponds to a record. </summary>
	public static bool Matching(int i, int j, out string name)
	{
		name = null;
		foreach (var value in Records)
		{
			int type = value.type;

			if (type == ModContent.TileType<Pots>())
				type = TileID.Pots; //Pretend type is actually vanilla

			if (type == Main.tile[i, j].TileType && HasStyle(i, j, value))
			{
				name = value.key;
				return true;
			}
		}

		return false;

		static bool HasStyle(int i, int j, TileRecord record)
		{
			var t = Main.tile[i, j];
			//Vanilla pots can't use GetTileStyle because they don't have object data
			int style = (t.TileType is TileID.Pots) ? t.TileFrameX / 36 + t.TileFrameY / 36 * 3 : TileObjectData.GetTileStyle(t);

			return record.styles.Contains(style);
		}
	}

	public override void SetStaticDefaults()
	{
		foreach (int type in StyleDatabase.Groups.Keys)
		{
			if (TileLoader.GetTile(type) is not IRecordTile r)
				continue;

			foreach (var group in StyleDatabase.Groups[type])
				r.AddRecord(type, group);
		}

		TileEvents.OnKillTile += ValidateRecord;
	}

	private static void ValidateRecord(int i, int j, int type, ref bool fail, ref bool effectOnly)
	{
		const int maxDistance = 800;

		if (effectOnly || fail)
			return;

		if (Matching(i, j, out string name))
		{
			var world = new Vector2(i, j).ToWorldCoordinates();
			var p = Main.player[Player.FindClosest(world, 16, 16)];

			if (p.DistanceSQ(world) < maxDistance * maxDistance)
				p.GetModPlayer<RecordPlayer>().Validate(name);
		}
	}

	public static bool ManualAddRecord(object[] args)
	{
		if (args.Length < 3)
			throw new ArgumentException("AddPotstiaryRecord requires at least 3 parameters.");

		if (args[0] is not int or ushort)
			throw new ArgumentException("AddPotstiaryRecord parameter 1 should be an int or ushort.");

		int type = (int)args[0];

		if (args[1] is not int[] styles)
			throw new ArgumentException("AddPotstiaryRecord parameter 2 should be an int[].");

		if (args[2] is not string name)
			throw new ArgumentException("AddPotstiaryRecord parameter 3 should be a string.");

		var e = new TileRecord(name, type, styles);

		if (args.Length > 3 && args[3] is byte rating)
			e.AddRating(rating);

		if (args.Length > 4) //Hidden
		{
			if (args[4] is bool hidden && hidden)
			{
				e.Hide();
			}
			else if (args[4] is Func<bool> hiddenFunc)
			{
				e.Hide(hiddenFunc);
			}
		}

		if (args.Length > 5) //Add a loot pool
			ParseLootAction(type, args[5]);

		if (args.Length > 6 && args[6] is LocalizedText desc)
			e.AddDescription(desc);

		if (args.Length > 7 && args[7] is LocalizedText dispName)
			e.AddDisplayName(dispName);

		Records.Add(e);
		return true;

		static bool ParseLootAction(int type, object arg)
		{
			if (arg is bool hasBasicLoot && hasBasicLoot)
			{
				bool result = TileLootHandler.TryGetLootPool(ModContent.TileType<Pots>(), out LootDelegate pool);

				if (result)
					TileLootHandler.RegisterLoot(pool, type);

				return result;
			}
			else if (arg is Action<int, ILoot> dele)
			{
				TileLootHandler.RegisterLoot((context, loot) => dele.Invoke(context.Style, loot), type); //Nest delegates to avoid using a .dll reference because of ILootTile.Context
				return true;
			}
			else if (arg is Action<int, Point16, ILoot> dele2)
			{
				TileLootHandler.RegisterLoot((context, loot) => dele2.Invoke(context.Style, context.Coordinates, loot), type);
				return true;
			}

			return false;
		}
	}
}

internal sealed class RecordPlayer : ModPlayer
{
	/// <summary> The list of unlocked entries saved per player. </summary>
	private IList<string> _validated = [];
	/// <summary> The list of entries newly discovered by the player. Not saved. </summary>
	private readonly HashSet<string> _newAndShiny = [];

	public bool IsNew(string name) => _newAndShiny.Contains(name);
	public bool RemoveNew(string name) => _newAndShiny.Remove(name);

	/// <summary> Unlocks the entry of <paramref name="name"/> for this player. </summary>
	public void Validate(string name)
	{
		if (!IsValidated(name))
			_newAndShiny.Add(name);

		_validated.Add(name);
	}

	/// <returns> Whether the entry of <paramref name="name"/> is unlocked for this player. </returns>
	public bool IsValidated(string name) => _validated.Contains(name);

	public override void SaveData(TagCompound tag) => tag[nameof(_validated)] = _validated;
	public override void LoadData(TagCompound tag) => _validated = tag.GetList<string>(nameof(_validated));
}