using System.Reflection;
using System.Runtime.CompilerServices;

namespace SpiritReforged.Common.ItemCommon;

// This is a really ugly edit. Too bad!
internal class SmartCursorEdits : ILoadable
{
	private static FieldInfo GetItem = null;
	private static FieldInfo GetScreenTargetX = null;
	private static FieldInfo GetScreenTargetY = null;
	private static FieldInfo GetReachableStartX = null;
	private static FieldInfo GetReachableStartY = null;
	private static FieldInfo GetReachableEndX = null;
	private static FieldInfo GetReachableEndY = null;
	private static FieldInfo GetMouse = null;

	void ILoadable.Load(Mod mod)
	{
		On_SmartCursorHelper.Step_PlanterBox += CustomPlanterBoxes;
		On_SmartCursorHelper.Step_Platforms += StopPlanterBoxCursor;

		Type nested = typeof(SmartCursorHelper).GetNestedType("SmartCursorUsageInfo", BindingFlags.NonPublic);
		GetItem = nested.GetField("item");
		GetScreenTargetX = nested.GetField("screenTargetX");
		GetScreenTargetY = nested.GetField("screenTargetY");
		GetReachableStartX = nested.GetField("reachableStartX");
		GetReachableStartY = nested.GetField("reachableStartY");
		GetReachableEndX = nested.GetField("reachableEndX");
		GetReachableEndY = nested.GetField("reachableEndY");
		GetMouse = nested.GetField("mouse");
	}

	private void StopPlanterBoxCursor(On_SmartCursorHelper.orig_Step_Platforms orig, object providedInfo, ref int focusedX, ref int focusedY)
	{
		Item item = (Item)GetItem.GetValue(providedInfo);

		if (item.createTile < TileID.Dirt || SpiritSets.IsPlanterBox[item.createTile] || focusedX != -1 || focusedY != -1)
			return;

		orig(providedInfo, ref focusedX, ref focusedY);
	}

	[UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "_targets")]
	public static extern ref List<Tuple<int, int>> GetTargets(SmartCursorHelper helper);

	private void CustomPlanterBoxes(On_SmartCursorHelper.orig_Step_PlanterBox orig, object providedInfo, ref int focusedX, ref int focusedY)
	{
		// Run vanilla method,
		int oldX = focusedX;
		int oldY = focusedY;
		orig(providedInfo, ref focusedX, ref focusedY);

		if (oldX != focusedX || oldY != focusedY)
			return;

		// If it doesn't succeed, run ours instead.
		Item item = (Item)GetItem.GetValue(providedInfo);

		if (item.createTile < TileID.Dirt || !SpiritSets.IsPlanterBox[item.createTile] || focusedX != -1 || focusedY != -1)
			return;

		ref var _targets = ref GetTargets(null);
		_targets.Clear();
		bool flag = false;
		int screenTargetX = (int)GetScreenTargetX.GetValue(providedInfo);
		int screenTargetY = (int)GetScreenTargetY.GetValue(providedInfo);
		int reachableStartX = (int)GetReachableStartX.GetValue(providedInfo);
		int reachableStartY = (int)GetReachableStartY.GetValue(providedInfo);
		int reachableEndX = (int)GetReachableEndX.GetValue(providedInfo);
		int reachableEndY = (int)GetReachableEndY.GetValue(providedInfo);

		if (Main.tile[screenTargetX, screenTargetY].HasTile && SpiritSets.IsPlanterBox[Main.tile[screenTargetX, screenTargetY].TileType])
			flag = true;

		if (!flag)
		{
			for (int i = reachableStartX; i <= reachableEndX; i++)
			{
				for (int j = reachableStartY; j <= reachableEndY; j++)
				{
					Tile tile = Main.tile[i, j];

					if (tile.HasTile && SpiritSets.IsPlanterBox[tile.TileType])
					{
						if (!Main.tile[i - 1, j].HasTile || Main.tileCut[Main.tile[i - 1, j].TileType] || TileID.Sets.BreakableWhenPlacing[Main.tile[i - 1, j].TileType])
							_targets.Add(new Tuple<int, int>(i - 1, j));

						if (!Main.tile[i + 1, j].HasTile || Main.tileCut[Main.tile[i + 1, j].TileType] || TileID.Sets.BreakableWhenPlacing[Main.tile[i + 1, j].TileType])
							_targets.Add(new Tuple<int, int>(i + 1, j));
					}
				}
			}
		}

		if (_targets.Count > 0)
		{
			float num = -1f;
			Tuple<int, int> tuple = _targets[0];

			for (int k = 0; k < _targets.Count; k++)
			{
				float num2 = Vector2.Distance(new Vector2(_targets[k].Item1, _targets[k].Item2) * 16f + Vector2.One * 8f, (Vector2)GetMouse.GetValue(providedInfo));
				if (num == -1f || num2 < num)
				{
					num = num2;
					tuple = _targets[k];
				}
			}

			if (Collision.InTileBounds(tuple.Item1, tuple.Item2, reachableStartX, reachableStartY, reachableEndX, reachableEndY) && num != -1f)
			{
				focusedX = tuple.Item1;
				focusedY = tuple.Item2;
			}
		}

		_targets.Clear();
	}

	void ILoadable.Unload() { }
}
