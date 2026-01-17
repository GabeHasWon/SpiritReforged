using SpiritReforged.Content.Ziggurat.Tiles;
using System.Diagnostics;
using System.Reflection;

namespace SpiritReforged.Common.TileCommon.TileMerging;

public sealed class TileMerger : ModSystem
{
	private const int FullFrameWidth = 108;

	/// <summary> All possible merge tile types. </summary>
	public static int[] All { get; private set; }
	private static readonly Dictionary<int, string> _texturePatchByType = [];
	private static readonly Dictionary<GarbagePaintHackSystem.Key, GarbagePaintHackSystem.RtHolder> _paintCache = [];

	private static readonly Point[] _offsets = [
		new(-1, -1), new(18, 0),  new(18, 36), new(54, 0),
		new(0, 18),  new(0, 0),   new(0, 36),  new(90, 0),
		new(36, 18), new(36, 0),  new(36, 36), new(90, 18),
		new(54, 18), new(72, 0),  new(72, 18), new(18, 18)
	];

	public override void SetStaticDefaults()
	{
		GarbagePaintHackSystem.ClearRenderTargets += _paintCache.Clear;

		Add(TileID.Sand, "Sand");
		Add(TileID.Dirt, "Dirt");
		AddRange("RedSandstone", ModContent.TileType<RedSandstoneBrick>(), ModContent.TileType<RedSandstoneBrickCracked>(), ModContent.TileType<RedSandstoneSlab>());
		AddRange("Hive", ModContent.TileType<PaleHive>(), ModContent.TileType<GooeyHive>());
		All = [.. _texturePatchByType.Keys];

		return;

		static void Add(int type, string name)
		{
			string path = "SpiritReforged/Common/TileCommon/TileMerging/Textures/" + name + "Merge";
			_texturePatchByType[type] = path;
			ModContent.Request<Texture2D>(path);
		}

		static void AddRange(string name, params int[] types)
		{
			foreach (int type in types)
				Add(type, name);
		}
	}

	public override void Unload() => GarbagePaintHackSystem.ClearRenderTargets -= _paintCache.Clear;

	/// <summary> Draws merge overlays according to <paramref name="types"/>. </summary>
	public static void DrawMerge(SpriteBatch spriteBatch, int i, int j, params int[] types) => DrawMerge(spriteBatch, i, j, Lighting.GetColor(i, j), TileExtensions.TileOffset, types);

	/// <summary><inheritdoc cref="DrawMerge(SpriteBatch, int, int, int[])"/>
	/// <br/>See the overload for a simpler method approach. </summary>
	public static void DrawMerge(SpriteBatch spriteBatch, int i, int j, Color color, Vector2 offset, params int[] types)
	{
		Tile tile = Main.tile[i, j];
		int frameNumber = tile.Get<TileWallWireStateData>().TileFrameNumber;
		Vector2 pos = new Vector2(i * 16, j * 16) - Main.screenPosition + offset;

		foreach (int type in types)
		{
			(int mask, int paint) = GetMergeData(i, j, type);
			if (mask <= 0 || !_texturePatchByType.TryGetValue(type, out string path))
				continue;

			Texture2D texture = null;
			Color finalColor = color;

			if (paint > PaintID.None)
				texture = GarbagePaintHackSystem.TryGetTexturePaintAndRequestIfNotReady(_paintCache, type, paint, path);

			if (texture == null)
			{
				texture = ModContent.Request<Texture2D>(path).Value;

				if (paint > PaintID.None)
					finalColor = finalColor.MultiplyRGBA(WorldGen.paintColor(paint));
			}

			Point p = _offsets[mask];
			spriteBatch.Draw(texture, pos, new Rectangle(p.X + frameNumber * FullFrameWidth, p.Y, 16, 16), finalColor);
		}
	}

	private static (int mask, int shaderIndex) GetMergeData(int i, int j, int type)
	{
		Tile center = Main.tile[i, j];
		int mask = 0;
		int shaderIdx = 0;

		Check(i, j - 1, 1, true);
		Check(i, j + 1, 2, false);
		Check(i - 1, j, 4, false);
		Check(i + 1, j, 8, false);

		return (mask, shaderIdx);

		void Check(int x, int y, int bit, bool isUp)
		{
			Tile neighbor = Main.tile[x, y];
			if (!neighbor.HasTileType(type))
				return;

			bool canMerge = isUp
				? !center.IsHalfBlock && (center.BottomSlope || center.Slope == 0) &&
				  (neighbor.TopSlope || neighbor.Slope == 0)
				: !center.IsHalfBlock;

			if (canMerge)
			{
				mask |= bit;
				if (shaderIdx == 0)
					shaderIdx = neighbor.TileColor;
			}
		}
	}
}

/* adapted from: https://github.com/Trivaxy/JadeFables/blob/main/Helpers/DrawHelper.cs */

internal sealed class GarbagePaintHackSystem : ModSystem
{
	internal readonly record struct Key(int ThingType, int PaintColor);

	internal class RtHolder(Key key, string texturePath, int copySettingsFrom = -1) : TilePaintSystemV2.ARenderTargetHolder
	{
		public Key Key = key;
		public TreePaintingSettings PaintSettings = TreePaintSystemData.GetTileSettings(copySettingsFrom, 0);
		public string TexturePath = texturePath;

		public override void Prepare()
		{
			Asset<Texture2D> asset = ModContent.Request<Texture2D>(TexturePath);
			asset.Wait?.Invoke();
			PrepareTextureIfNecessary(asset.Value);
		}

		public override void PrepareShader() => PrepareShader(Key.PaintColor, PaintSettings);
	}

	private static IList<TilePaintSystemV2.ARenderTargetHolder> _paintSystemRequests;

	public override void Load()
	{
		FieldInfo field = typeof(TilePaintSystemV2).GetField("_requests", BindingFlags.NonPublic | BindingFlags.Instance);

		Debug.Assert(field != null);

		_paintSystemRequests = field.GetValue(Main.instance.TilePaintSystem) as IList<TilePaintSystemV2.ARenderTargetHolder>;
	}

	public static event Action ClearRenderTargets;
	public override void Unload() => Main.QueueMainThreadAction(() => ClearRenderTargets?.Invoke());

	public static Texture2D TryGetTexturePaintAndRequestIfNotReady(
		Dictionary<Key, RtHolder> textureDict,
		int type,
		int paintColor,
		string texturePath,
		int copySettingsFrom = -1)
	{
		Key key = new(type, paintColor);

		if (textureDict.TryGetValue(key, out RtHolder holder))
		{
			if (holder.IsReady)
				return holder.Target;
		}
		else
		{
			var newHolder = new RtHolder(key, texturePath, copySettingsFrom);
			textureDict[key] = newHolder;
			_paintSystemRequests?.Add(newHolder);
		}

		return null;
	}
}