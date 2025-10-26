using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.Visuals;
using System.Linq;
using System.Reflection;

namespace SpiritReforged.Common.TileCommon;

/// <summary> Autoloads a complete array of furniture tiles based on <see cref="GetInfo"/>.<para/>
/// <see cref="Autoload"/> can be used to selectively disable items. </summary>
public abstract class FurnitureSet : ILoadable
{
	public enum Types
	{
		Bathtub,
		Bed,
		Bookcase,
		Candelabra,
		Candle,
		Chair,
		Chandelier,
		Chest,
		Barrel,
		Clock,
		Door,
		Dresser,
		Lamp,
		Lantern,
		Piano,
		Sink,
		Sofa,
		Bench,
		Table,
		Toilet,
		WorkBench
	}

	/// <summary> The mod this instance was registered through. </summary>
	public Mod Mod { get; private set; }
	public abstract string Name { get; }
	public virtual FurnitureTile.IFurnitureData GetInfo(FurnitureTile tile) => default;

	/// <summary> Gets the tile type associated with the given <see cref="Type"/>. Throws exceptions on failure. </summary>
	public int GetTileType(Types type) => Mod.Find<ModTile>(Name + type.ToString()).Type;

	/// <summary> Tries to get the tile type associated with the given <see cref="Type"/>. </summary>
	public bool TryGetTileType(Types type, out int tileType)
	{
		tileType = -1;
		string fullName = Name + type.ToString();

		if (Mod.TryFind(fullName, out ModTile value))
			tileType = value.Type;

		return tileType != -1;
	}

	public void Load(Mod mod)
	{
		Mod = mod;

		foreach (var t in typeof(FurnitureSet).GetNestedTypes(BindingFlags.Instance | BindingFlags.Public))
		{
			if (typeof(FurnitureTile).IsAssignableFrom(t))
			{
				var instance = (FurnitureTile)Activator.CreateInstance(t, this);

				if (Autoload(instance))
					mod.AddContent(instance);
			}
		}

		SpiritReforgedSystem.OnSetupContent += OnPostSetupContent;
		OnLoad();
	}

	/// <returns> Whether this instance can be added to mod content. Use methods like <see cref="Including"/> and <see cref="Excluding"/> to simplify the process. </returns>
	public virtual bool Autoload(FurnitureTile tile) => true;
	public virtual void OnPostSetupContent() { }
	public virtual void OnLoad() { }
	public void Unload() { }

	#region enum
	public static bool Excluding(FurnitureTile tile, params Types[] values)
	{
		var c = GetEnumValue(tile);
		return !values.Contains(c);
	}

	public static bool Including(FurnitureTile tile, params Types[] values)
	{
		var c = GetEnumValue(tile);
		return values.Contains(c);
	}

	private static Types GetEnumValue(FurnitureTile tile) => (Types)tile.GetType().GetField("EnumValue", BindingFlags.Static | BindingFlags.Public).GetValue(null);
	#endregion

	#region types
	public sealed class AutoBathtubTile(FurnitureSet set) : BathtubTile
	{
		public const Types EnumValue = Types.Bathtub;

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Enum.GetName(EnumValue);

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoBedTile(FurnitureSet set) : BedTile
	{
		public const Types EnumValue = Types.Bed;

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Enum.GetName(EnumValue);

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoBookcaseTile(FurnitureSet set) : BookcaseTile
	{
		public const Types EnumValue = Types.Bookcase;

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Enum.GetName(EnumValue);

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoCandelabraTile(FurnitureSet set) : CandelabraTile
	{
		public const Types EnumValue = Types.Candelabra;

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Enum.GetName(EnumValue);

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoCandleTile(FurnitureSet set) : CandleTile
	{
		public const Types EnumValue = Types.Candle;

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Enum.GetName(EnumValue);

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoChairTile(FurnitureSet set) : ChairTile
	{
		public const Types EnumValue = Types.Chair;

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Enum.GetName(EnumValue);

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoChandelierTile(FurnitureSet set) : ChandelierTile
	{
		public const Types EnumValue = Types.Chandelier;

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Enum.GetName(EnumValue);

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoChestTile(FurnitureSet set) : ChestTile
	{
		public const Types EnumValue = Types.Chest;

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Enum.GetName(EnumValue);

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoBarrelTile(FurnitureSet set) : BarrelTile
	{
		public const Types EnumValue = Types.Barrel;

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Enum.GetName(EnumValue);

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoClockTile(FurnitureSet set) : ClockTile
	{
		public const Types EnumValue = Types.Clock;

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Enum.GetName(EnumValue);

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoDoorTile(FurnitureSet set) : DoorTile
	{
		public const Types EnumValue = Types.Door;

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Enum.GetName(EnumValue);

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoDresserTile(FurnitureSet set) : DresserTile
	{
		public const Types EnumValue = Types.Dresser;

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Enum.GetName(EnumValue);

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoLampTile(FurnitureSet set) : LampTile
	{
		public const Types EnumValue = Types.Lamp;

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Enum.GetName(EnumValue);

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoLanternTile(FurnitureSet set) : LanternTile
	{
		public const Types EnumValue = Types.Lantern;

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Enum.GetName(EnumValue);

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoPianoTile(FurnitureSet set) : PianoTile
	{
		public const Types EnumValue = Types.Piano;

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Enum.GetName(EnumValue);

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoSinkTile(FurnitureSet set) : SinkTile
	{
		public const Types EnumValue = Types.Sink;

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Enum.GetName(EnumValue);

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoSofaTile(FurnitureSet set) : SofaTile
	{
		public const Types EnumValue = Types.Sofa;

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Enum.GetName(EnumValue);

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoBenchTile(FurnitureSet set) : BenchTile
	{
		public const Types EnumValue = Types.Bench;

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Enum.GetName(EnumValue);

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoTableTile(FurnitureSet set) : TableTile
	{
		public const Types EnumValue = Types.Table;

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Enum.GetName(EnumValue);

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoToiletTile(FurnitureSet set) : ToiletTile
	{
		public const Types EnumValue = Types.Toilet;

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Enum.GetName(EnumValue);

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoWorkBenchTile(FurnitureSet set) : WorkBenchTile
	{
		public const Types EnumValue = Types.WorkBench;

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Enum.GetName(EnumValue);

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}
	#endregion
}