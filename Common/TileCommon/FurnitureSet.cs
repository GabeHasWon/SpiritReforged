using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.Visuals;
using System.Linq;
using System.Reflection;
using Terraria.ModLoader.Core;

namespace SpiritReforged.Common.TileCommon;

/// <summary> Autoloads a complete array of furniture tiles based on <see cref="GetInfo"/>.<para/>
/// <see cref="Autoload"/> can be used to selectively disable items. Items with missing textures or conflicting internal names are automatically omitted. </summary>
public abstract class FurnitureSet : ILoadable
{
	public abstract string Name { get; }
	public virtual FurnitureTile.IFurnitureData GetInfo(FurnitureTile tile) => default;

	public void Load(Mod mod)
	{
		var loadable = AssemblyManager.GetLoadableTypes(GetType().Assembly);

		foreach (var t in typeof(FurnitureSet).GetNestedTypes(BindingFlags.Instance | BindingFlags.Public))
		{
			if (typeof(FurnitureTile).IsAssignableFrom(t))
			{
				var instance = (FurnitureTile)Activator.CreateInstance(t, [this]);

				if (!loadable.Any(x => x.Name == instance.Name) && Autoload(instance))
					mod.AddContent(instance);
			}
		}

		SpiritReforgedMod.OnSetupContent += OnPostSetupContent;
		OnLoad();
	}

	/// <returns> Whether this instance can be added to mod content. </returns>
	public virtual bool Autoload(FurnitureTile tile) => ModContent.RequestIfExists<Texture2D>(DrawHelpers.RequestLocal(GetType(), tile.Name), out _);
	public virtual void OnPostSetupContent() { }
	public virtual void OnLoad() { }
	public void Unload() { }

	#region types
	public sealed class AutoBathtubTile(FurnitureSet set) : BathtubTile
	{
		public const string Suffix = "Bathtub";

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Suffix;

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoBedTile(FurnitureSet set) : BedTile
	{
		public const string Suffix = "Bed";

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Suffix;

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoBookcaseTile(FurnitureSet set) : BookcaseTile
	{
		public const string Suffix = "Bookcase";

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Suffix;

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoCandelabraTile(FurnitureSet set) : CandelabraTile
	{
		public const string Suffix = "Candelabra";

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Suffix;

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoCandleTile(FurnitureSet set) : CandleTile
	{
		public const string Suffix = "Candle";

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Suffix;

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoChairTile(FurnitureSet set) : ChairTile
	{
		public const string Suffix = "Chair";

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Suffix;

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoChandelierTile(FurnitureSet set) : ChandelierTile
	{
		public const string Suffix = "Chandelier";

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Suffix;

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoChestTile(FurnitureSet set) : ChestTile
	{
		public const string Suffix = "Chest";

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Suffix;

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoBarrelTile(FurnitureSet set) : BarrelTile
	{
		public const string Suffix = "Barrel";

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Suffix;

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoClockTile(FurnitureSet set) : ClockTile
	{
		public const string Suffix = "Clock";

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Suffix;

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoDoorTile(FurnitureSet set) : DoorTile
	{
		public const string Suffix = "Door";

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Suffix;

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoDresserTile(FurnitureSet set) : DresserTile
	{
		public const string Suffix = "Dresser";

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Suffix;

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoLampTile(FurnitureSet set) : LampTile
	{
		public const string Suffix = "Lamp";

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Suffix;

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoLanternTile(FurnitureSet set) : LanternTile
	{
		public const string Suffix = "Lantern";

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Suffix;

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoPianoTile(FurnitureSet set) : PianoTile
	{
		public const string Suffix = "Piano";

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Suffix;

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoSinkTile(FurnitureSet set) : SinkTile
	{
		public const string Suffix = "Sink";

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Suffix;

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoSofaTile(FurnitureSet set) : SofaTile
	{
		public const string Suffix = "Sofa";

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Suffix;

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoBenchTile(FurnitureSet set) : BenchTile
	{
		public const string Suffix = "Bench";

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Suffix;

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoTableTile(FurnitureSet set) : TableTile
	{
		public const string Suffix = "Table";

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Suffix;

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoToiletTile(FurnitureSet set) : ToiletTile
	{
		public const string Suffix = "Toilet";

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Suffix;

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}

	public sealed class AutoWorkBenchTile(FurnitureSet set) : WorkBenchTile
	{
		public const string Suffix = "WorkBench";

		private readonly FurnitureSet _set = set;
		private readonly string _name = set.Name + Suffix;

		public override string Name => _name;
		public override string Texture => DrawHelpers.RequestLocal(_set.GetType(), Name);
		public override IFurnitureData Info => _set.GetInfo(this);
	}
	#endregion
}