using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Common.TileCommon.PostDrawTreeHookSystem;

internal class PostDrawTreeHook : ModSystem
{
	public readonly Dictionary<Point, IPostDrawTree> TreePoints = [];

	public static void AddPoint(Point position, IPostDrawTree tree)
	{
		Dictionary<Point, IPostDrawTree> points = ModContent.GetInstance<PostDrawTreeHook>().TreePoints;

		if (!points.ContainsKey(position))
			points.Add(position, tree);
	}

	public override void Load()
	{
		IL_TileDrawing.DrawTrees += AddTreeHook;
		On_TileDrawing.DrawTrees += Trees;
		On_TileDrawing.PreDrawTiles += ResetPoints;
	}

	private void Trees(On_TileDrawing.orig_DrawTrees orig, TileDrawing self)
	{
		orig(self);

		Dictionary<Point, IPostDrawTree> points = ModContent.GetInstance<PostDrawTreeHook>().TreePoints;

		foreach ((Point position, IPostDrawTree tree) in points)
		{
			if (tree.DrawAfter)
				tree.PostDrawTree(position.X, position.Y);
		}
	}

	private void ResetPoints(On_TileDrawing.orig_PreDrawTiles orig, TileDrawing self, bool solidLayer, bool forRenderTargets, bool intoRenderTargets)
	{
		orig(self, solidLayer, forRenderTargets, intoRenderTargets);

		bool flag = intoRenderTargets || Lighting.UpdateEveryFrame;
		if (!solidLayer && flag)
		{
			PostDrawTreeHook instance = ModContent.GetInstance<PostDrawTreeHook>();
			instance.TreePoints.Clear();
		}
	}

	private void AddTreeHook(ILContext il)
	{
		ILCursor c = new(il);

		while (true)
		{
			if (!c.TryGotoNext(MoveType.After, x => x.MatchLdloc(10)))
				break;
		}

		c.Emit(OpCodes.Ldloc_S, (byte)7);
		c.Emit(OpCodes.Ldloc_S, (byte)8);
		c.EmitDelegate(PopTreeDraw);
	}

	public static void PopTreeDraw(int x, int y)
	{
		if (ModContent.GetInstance<PostDrawTreeHook>().TreePoints.TryGetValue(new Point(x, y), out IPostDrawTree tree) && !tree.DrawAfter)
		{
			tree.PostDrawTree(x, y);
		}
	}
}
