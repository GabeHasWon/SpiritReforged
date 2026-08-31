using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;
using Terraria.GameContent.Drawing;
using TileHelper.Common;

namespace SpiritReforged.Common.TileCommon;

[ReinitializeDuringResizeArrays]
public sealed class WindTileRenderer : GlobalTile
{
	public interface IDrawInWind
	{
		public void DrawInWind(SpriteBatch spriteBatch, int i, int j, float rotation, Vector2 position, Vector2 origin)
		{
			Tile tile = Main.tile[i, j];
			Rectangle source = new(tile.TileFrameX, tile.TileFrameY, 16, 16);
			Color lightColor = Lighting.GetColor(i, j);

			spriteBatch.Draw(Helpers.GetTileTextureValue(tile), position + origin, source, lightColor, rotation, origin, 1, 0, 0);

			if (Helpers.TryGetGlowmask(i, j, out Texture2D glowmask, out Color glowColor)) //Draw glowmask
				spriteBatch.Draw(glowmask, position + origin, source, glowColor, rotation, origin, 1, 0, 0);
		}

		public float GetWindStrength(int i, int j) => WindGrid.GetWind(i, j);
	}

	public readonly record struct SimpleWindGrid()
	{
		public float GetWind(int i, int j) => _grid.TryGetValue((i, j), out float value) ? value : 0f;

		public void SetWind(int i, int j, float value)
		{
			if (_grid.ContainsKey((i, j)))
				_grid[(i, j)] = value;
			else
				_grid.Add((i, j), value);
		}

		public void Update()
		{
			const float reduction = 0.9f;

			foreach ((int x, int y) key in _grid.Keys)
			{
				if (Math.Abs(_grid[key] *= reduction) < 0.1f)
				{
					_grid.Remove(key);
					break;
				}
			}
		}

		private readonly Dictionary<(int x, int y), float> _grid = [];
	}

	static WindTileRenderer() => TileDrawInWind.CollectionChanged += OnSetChanged;

	/// <summary> A secondary wind grid. Useful for applying wind visuals independently from <see cref="TileDrawing.Wind"/>. </summary>
	public static readonly SimpleWindGrid WindGrid = new();

	/// <summary> Denotes a tile type with wind drawing behaviour.<para/>
	/// All <see cref="TileDrawing.TileCounterType"/>s are automatically handled when set. </summary>
	public static readonly ObservableCollection<TileDrawing.TileCounterType?> TileDrawInWind = new(TileID.Sets.Factory.CreateCustomSet<TileDrawing.TileCounterType?>(null));

	public static double TreeWindCounter { get; private set; }
	public static double GrassWindCounter { get; private set; }
	public static double SunflowerWindCounter { get; private set; }

	public override void Load() => On_TileDrawing.Update += UpdateClients;

	private static void UpdateClients(On_TileDrawing.orig_Update orig, TileDrawing self)
	{
		orig(self);

		if (!Main.dedServ)
		{
			WindGrid.Update();

			double num = Math.Abs(Main.WindForVisuals);
			num = Utils.GetLerpValue(0.08f, 1.2f, (float)num, clamped: true);

			TreeWindCounter += 0.0041666666666666666 + 0.0041666666666666666 * num * 2.0;
			GrassWindCounter += 0.0055555555555555558 + 0.0055555555555555558 * num * 4.0;
			SunflowerWindCounter += 0.002380952380952 + 0.0023809523809523810 * num * 5.0;
		}
	}

	private static void OnSetChanged(object sender, NotifyCollectionChangedEventArgs arguments)
	{
		if (arguments.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Move)
		{
			throw new NotSupportedException($"Collection {nameof(TileDrawInWind)} can not be added to, removed from, or have elements move.");
		}

		if (arguments.Action == NotifyCollectionChangedAction.Replace && arguments.NewItems[0] is TileDrawing.TileCounterType counter)
		{
			int type = arguments.NewStartingIndex;

			if (counter is TileDrawing.TileCounterType.MultiTileVine or TileDrawing.TileCounterType.MultiTileGrass) //Assign to required sets
				TileID.Sets.MultiTileSway[type] = true;
			else if (counter == TileDrawing.TileCounterType.Vine)
				TileID.Sets.VineThreads[type] = true;
			else if (counter == TileDrawing.TileCounterType.ReverseVine)
				TileID.Sets.ReverseVineThreads[type] = true;
		}
	}

	public override void SetStaticDefaults()
	{
		foreach (ModTile modTile in Mod.GetContent<ModTile>())
		{
			if (modTile is IDrawInWind) //Automatically register all Mod IDrawInWind instances to the set
				TileDrawInWind[modTile.Type] = TileDrawing.TileCounterType.CustomNonSolid;
		}
	}

	public override bool PreDraw(int i, int j, int type, SpriteBatch spriteBatch)
	{
		if (TileDrawInWind[type] is TileDrawing.TileCounterType counter)
		{
			if (counter == TileDrawing.TileCounterType.Vine)
			{
				Main.instance.TilesRenderer.CrawlToTopOfVineAndAddSpecialPoint(j, i);
			}
			else if (counter == TileDrawing.TileCounterType.ReverseVine)
			{
				Main.instance.TilesRenderer.CrawlToBottomOfReverseVineAndAddSpecialPoint(j, i);
			}
			else if (TileObjectData.IsTopLeft(i, j))
			{
				Main.instance.TilesRenderer.AddSpecialPoint(i, j, counter);
			}

			return false;
		}

		return true;
	}

	public override void SpecialDraw(int i, int j, int type, SpriteBatch spriteBatch)
	{
		if (TileDrawInWind[type] == TileDrawing.TileCounterType.CustomNonSolid && TileLoader.GetTile(type) is IDrawInWind iDrawInWind)
		{
			if (TileObjectData.GetTileData(Main.tile[i, j]) is not TileObjectData tileObjectData)
				return;

			(int width, int height) = (tileObjectData.Width, tileObjectData.Height);
			Vector2 objectOffset = new(tileObjectData.DrawXOffset, tileObjectData.DrawYOffset);
			float physics = iDrawInWind.GetWindStrength(i, j);
			//bool flipped = tileObjectData.Origin.Y == 0; ADD FLIPPED BEHAVIOUR IF NEEDED

			for (int x = i; x < i + width; x++)
			{
				for (int y = j; y < j + height; y++)
				{
					(int gridX, int gridY) = (x - i, y - j);
					float rotation = (1.5f - gridY / Math.Max(tileObjectData.Origin.Y, 1f)) * physics * 0.1f;

					Vector2 position = new Vector2(x, y) * 16 - Main.screenPosition + new Vector2(0, Math.Abs(rotation) * 20f);
					Vector2 origin = (tileObjectData.Origin.ToVector2() + Vector2.One * 0.5f - new Vector2(gridX, gridY)) * 16;

					iDrawInWind.DrawInWind(spriteBatch, x, y, rotation, position + origin + objectOffset, origin);
				}
			}
		}
	}

	#region helpers
	//Use reflection to access SetWindTime
	internal static void SetWindTime(int i, int j, Vector2 direction)
	{
		WindGrid windGrid = Main.instance.TilesRenderer.Wind;
		windGrid.GetType().InvokeMember("SetWindTime", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic, null, windGrid, [i, j, (int)direction.X, (int)direction.Y]);
	}

	//Adapted from vanilla - should be used for multitiles
	internal static float GetHighestWindGridPushComplex(int topLeftX, int topLeftY, int sizeX, int sizeY, int totalPushTime, float pushForcePerFrame, int loops, bool swapLoopDir) //Adapted from vanilla
	{
		float result = 0f;
		int num = int.MaxValue;

		for (int i = 0; i < sizeX; i++)
		{
			for (int j = 0; j < sizeY; j++)
			{
				Main.instance.TilesRenderer.Wind.GetWindTime(topLeftX + i, topLeftY + j, totalPushTime, out int windTimeLeft, out _, out _);
				float windGridPushComplex = Main.instance.TilesRenderer.GetWindGridPushComplex(topLeftX + i, topLeftY + j, totalPushTime, pushForcePerFrame, loops, swapLoopDir);

				if (windTimeLeft < num && windTimeLeft != 0)
				{
					result = windGridPushComplex;
					num = windTimeLeft;
				}
			}
		}

		return result;
	}

	/*public delegate void WindDelegate(int x, int y, float rotation, Vector2 position, Vector2 origin);

	public static void DrawWithWind(SpriteBatch spriteBatch, int i, int j, float physics) => DrawWithWind(i, j, physics, (x, y, rotation, position, origin) =>
	{
		Tile tile = Main.tile[x, y];
		Rectangle source = new(tile.TileFrameX, tile.TileFrameY, 16, 16);
		Color lightColor = Lighting.GetColor(x, y);

		spriteBatch.Draw(Helpers.GetTileTextureValue(tile), position + origin, source, lightColor, rotation, origin, 1, 0, 0);
	});

	public static void DrawWithWind(int i, int j, float physics, WindDelegate action)
	{
		if (TileObjectData.GetTileData(Main.tile[i, j]) is not TileObjectData tileObjectData)
			return;

		(int width, int height) = (tileObjectData.Width, tileObjectData.Height);
		for (int x = i; x < i + width; x++)
		{
			for (int y = j; y < j + height; y++)
			{
				(int gridX, int gridY) = (x - i, y - j);
				float rotation = (1.5f - (float)gridY / tileObjectData.Origin.Y) * physics * 0.1f;

				Vector2 position = new Vector2(x, y) * 16 - Main.screenPosition + new Vector2(0, Math.Abs(rotation) * 20f);
				Vector2 origin = (tileObjectData.Origin.ToVector2() + Vector2.One * 0.5f - new Vector2(gridX, gridY)) * 16;

				action.Invoke(x, y, rotation, position, origin);
			}
		}
	}*/
	#endregion
}