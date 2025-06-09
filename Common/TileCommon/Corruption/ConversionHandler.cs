using ILLogger;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Linq;
using Terraria.DataStructures;

namespace SpiritReforged.Common.TileCommon.Corruption;

internal class ConversionHandler : ModSystem
{
	/// <summary> Caches all ModTiles using <see cref="IConvertibleTile"/>. </summary>
	internal static readonly HashSet<int> ConversionTypes = [];

	public static IEntitySource ConversionSource = null;

	private static Hook ProjAIHook = null;

	public override void Load()
	{
		On_WorldGen.Convert += OnConvert;
		IL_WorldGen.GERunner += OnHardmodeEvils;

		ProjAIHook = new Hook(typeof(ProjectileLoader).GetMethod(nameof(ProjectileLoader.ProjectileAI)), AddConversionSource);
	}

	public override void SetStaticDefaults()
	{
		foreach (var t in Mod.GetContent<ModTile>())
		{
			if (t is IConvertibleTile)
				ConversionTypes.Add(t.Type);
		}
	}

	private void AddConversionSource(On_Projectile.orig_VanillaAI orig, Projectile self)
	{
		ConversionSource = self.GetSource_FromAI();
		orig(self);
		ConversionSource = null;
	}

	private static void OnHardmodeEvils(ILContext il)
	{
		ILCursor c = new(il);
		var p_good = c.Method.Parameters.Where(x => x.Name == "good").FirstOrDefault();

		if (p_good == default)
		{
			SpiritReforgedMod.Instance.LogIL("On Hardmode Evils", "Parameter 'good' not found.");
			return;
		}

		c.GotoNext(x => x.Match(OpCodes.Ldarg_S, p_good));

		c.Emit(OpCodes.Ldloc_S, (byte)15);
		c.Emit(OpCodes.Ldloc_S, (byte)16);
		c.Emit(OpCodes.Ldarg_S, p_good);
		c.EmitDelegate(RunnerConversion);
	}

	/// <summary> Add compatibility for our custom conversion types with hardmode evil gen. </summary>
	/// <param name="i"> The X tile coordinate. </param>
	/// <param name="j"> The Y tile coordinate. </param>
	/// <param name="hallow"> Whether this conversion is hallow or <see cref="WorldGen.crimson"/>. </param>
	private static void RunnerConversion(int i, int j, bool hallow)
	{
		var type = hallow ? ConversionType.Hallow : WorldGen.crimson ? ConversionType.Crimson : ConversionType.Corrupt;
		TileCorruptor.Convert(null, type, i, j);
	}

	/// <summary> Add compatibility for our custom conversion types when a tile is converted via various means, except for purity. </summary>
	/// <param name="orig"></param>
	/// <param name="i"> The X tile coordinate. </param>
	/// <param name="j"> The Y tile coordinate. </param>
	/// <param name="conversionType"> The conversion type corresponding to <see cref="BiomeConversionID"/>. </param>
	/// <param name="size"> The size (in tiles) of the conversion. </param>
	private static void OnConvert(On_WorldGen.orig_Convert orig, int i, int j, int conversionType, int size)
	{
		orig(i, j, conversionType, size);

		ConversionType type = conversionType switch //Translate BiomeConversionID
		{
			BiomeConversionID.Corruption => ConversionType.Corrupt,
			BiomeConversionID.Hallow => ConversionType.Hallow,
			BiomeConversionID.Crimson => ConversionType.Crimson,
			_ => ConversionType.Purify,
		};

		ConvertArea(new Point16(i, j), size, type, ConversionSource);
	}

	/// <summary> Converts an area of tiles using the selected <see cref="ConversionType"/>. </summary>
	/// <param name="point"> The center coordinates of the area to convert. </param>
	/// <param name="size"> Half the size of the conversion area. </param>
	/// <param name="type"> The conversion type. </param>
	public static void ConvertArea(Point16 point, int size, ConversionType type, IEntitySource source = null)
	{
		int i = point.X;
		int j = point.Y;

		for (int x = i - size; x <= i + size; x++)
		{
			for (int y = j - size; y <= j + size; y++)
			{
				if (!WorldGen.InWorld(i, j, 5))
					continue;

				int oldType = Main.tile[x, y].TileType;

				if (TileID.Sets.Hallow[oldType] && source is EntitySource_Parent { Entity: Projectile p } && p.type == ProjectileID.PurificationPowder)
					continue; //Purification powder can't cleanse the hallow

				TileCorruptor.Convert(source, type, x, y);
			}
		}
	}
}
