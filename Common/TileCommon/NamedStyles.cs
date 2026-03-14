using System.Linq;

namespace SpiritReforged.Common.TileCommon;

public static class NamedStyles
{
	public readonly record struct StyleGroup(string name, int[] styles)
	{
		public readonly string name = name;
		public readonly int[] styles = styles;
	}

	public static readonly Dictionary<int, HashSet<StyleGroup>> Groups = [];

	/// <inheritdoc cref="GetName(int, byte)"/>
	public static string GetName(int i, int j)
	{
		var t = Main.tile[i, j];
		int style = TileObjectData.GetTileStyle(t);

		return (style == -1) ? null : GetName(t.TileType, style);
	}

	/// <summary> Gets the registered style name of the tile at the given coordinates. Returns null if not <see cref="NamedStyles"/> or object data is invalid. </summary>
	public static string GetName(int type, byte style)
	{
		if (Groups.TryGetValue(type, out var value))
		{
			foreach (var group in value)
			{
				if (group.styles.Contains(style))
					return group.name;
			}
		}

		return null;
	}

	public static void AddStyle(int tileType, StyleGroup group)
	{
		if (!Groups.TryAdd(tileType, [group]))
			Groups[tileType].Add(group);
	}
}