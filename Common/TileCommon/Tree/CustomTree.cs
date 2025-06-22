using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Common.WorldGeneration.Noise;
using System.Linq;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.Metadata;

namespace SpiritReforged.Common.TileCommon.Tree;

/// <summary> Follows palm tree logic by default. </summary>
public abstract class CustomTree : ModTile, IModifySmartTarget
{
	/// <summary> Common frame size for tree tiles. </summary>
	public const int FrameSize = 22;

	/// <summary> Controls growth height without the need to override <see cref="CreateTree"/>. </summary>
	public virtual int TreeHeight => WorldGen.genRand.Next(10, 21);

	private static readonly HashSet<Point16> drawPoints = [];
	private static readonly HashSet<Point16> treeShakes = [];

	private static bool AppliedDetours = false;

	#region grow methods
	/// <summary> Attempts to grow a custom tree from a sapling at the given coordinates. </summary>
	/// <returns> Whether the tree was successfully grown. </returns>
	public static bool GrowTree(int i, int j)
	{
		if (!CustomSapling.SaplingToCustomTree.TryGetValue(Main.tile[i, j].TileType, out ushort type) || TileLoader.GetTile(type) is not CustomTree instance)
			return false;

		while (!WorldGen.SolidOrSlopedTile(Framing.GetTileSafely(i, j + 1)))
			j++; //Find the ground

		return GrowTreeFromInstance(i, j, instance);
	}

	/// <summary> Attempts to grow a custom tree of <see cref="{T}"/> at the given coordinates. </summary>
	/// <returns> Whether the tree was successfully grown. </returns>
	public static bool GrowTree<T>(int i, int j) where T : CustomTree
	{
		while (!WorldGen.SolidOrSlopedTile(Framing.GetTileSafely(i, j + 1)))
			j++; //Find the ground

		var instance = ModContent.GetInstance<T>() as CustomTree;
		return GrowTreeFromInstance(i, j, instance);
	}

	private static bool GrowTreeFromInstance(int i, int j, CustomTree instance)
	{
		int height = instance.TreeHeight;

		if (WorldGen.InWorld(i, j) && WorldMethods.AreaClear(i, j - (height - 1), 1, height))
		{
			WorldGen.KillTile(i, j); //Kill the tile at origin, presumably a sapling
			instance.CreateTree(i, j, height);

			if (WorldGen.PlayerLOS(i, j))
				instance.GrowEffects(i, j);
		}

		return Framing.GetTileSafely(i, j).TileType == instance.Type;
	}
	#endregion

	#region detours
	/// <summary> <inheritdoc/><para/> Includes detours for tree drawing and logic. </summary>
	public override void Load()
	{
		if (Main.dedServ || AppliedDetours)
			return;

		On_TileDrawing.DrawTrees += DrawAllFoliage;
		On_TileDrawing.PreDrawTiles += ResetPoints;

		On_WorldGen.IsTileTypeFitForTree += MakeUnfit;
		On_Player.IsBottomOfTreeTrunkNoRoots += ConfirmTreeTrunk;

		AppliedDetours = true;
	}

	private static void DrawAllFoliage(On_TileDrawing.orig_DrawTrees orig, TileDrawing self)
	{
		orig(self);

		foreach (Point16 p in drawPoints)
		{
			if (TileLoader.GetTile(Main.tile[p.X, p.Y].TileType) is CustomTree custom)
				custom.DrawTreeFoliage(p.X, p.Y, Main.spriteBatch);
		}
	}

	private static void ResetPoints(On_TileDrawing.orig_PreDrawTiles orig, TileDrawing self, bool solidLayer, bool forRenderTargets, bool intoRenderTargets)
	{
		orig(self, solidLayer, forRenderTargets, intoRenderTargets);

		if ((intoRenderTargets || Lighting.UpdateEveryFrame) && !solidLayer)
			drawPoints.Clear();
	}

	/// <summary> Forces <see cref="CustomModTree"/>s to be unfit for gen. </summary>
	private static bool MakeUnfit(On_WorldGen.orig_IsTileTypeFitForTree orig, ushort type)
	{
		if (CustomSapling.CustomAnchorTypes.Contains(type))
			return false;

		return orig(type);
	}

	/// <summary> Allows custom trees to work with the Axe of Regrowth. </summary>
	private static bool ConfirmTreeTrunk(On_Player.orig_IsBottomOfTreeTrunkNoRoots orig, Player self, int x, int y)
	{
		var t = Main.tile[x, y];
		if (t.HasTile && TileLoader.GetTile(t.TileType) is CustomTree && t.TileType != Main.tile[x, y + 1].TileType)
			return true; //Skips orig

		return orig(self, x, y);
	}
	#endregion

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;
		Main.tileAxe[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.CoordinateWidth = FrameSize - 2;
		TileObjectData.newTile.CoordinateHeights = [FrameSize - 2];
		TileObjectData.newTile.RandomStyleRange = 3;
		TileObjectData.newTile.StyleMultiplier = 3;
		TileObjectData.newTile.StyleWrapLimit = 3 * 4;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.AlternateTile, 1, 0);
		TileObjectData.newTile.AnchorValidTiles = [TileID.Grass];
		TileObjectData.newTile.AnchorAlternateTiles = [Type];

		//TileID.Sets.IsATreeTrunk[Type] = true; //If true, allows torches to be placed on trunks regardless of tileNoAttach
		TileID.Sets.IsShakeable[Type] = true;
		TileID.Sets.PreventsTileRemovalIfOnTopOfIt[Type] = true;
		TileID.Sets.PreventsTileReplaceIfOnTopOfIt[Type] = true;
		TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);
		DustType = -1;

		PreAddObjectData();
		TileObjectData.addTile(Type);
	}

	/// <summary> Called during SetStaticDefaults before <see cref="TileObjectData.addTile"/> is called. </summary>
	public virtual void PreAddObjectData() { }

	/// <summary> Used for pseudo random logic, like branch positions. </summary>
	protected virtual float Noise(Vector2 position) => NoiseSystem.PerlinStatic(position.X, position.Y * 3f) * 10f;

	/// <returns> Whether the given tile has a treetop. </returns>
	public virtual bool IsTreeTop(int i, int j) => Framing.GetTileSafely(i, j - 1).TileType != Type;

	public void ShakeTree(int i, int j)
	{
		while (Framing.GetTileSafely(i, j - 1).TileType == Type)
			j--; //Move to the top of the tree

		var pt = new Point16(i, j);
		if (!treeShakes.Contains(pt) && IsTreeTop(i, j))
			OnShakeTree(i, j);

		treeShakes.Add(pt); //Prevent this tree from being shook again
	}

	protected virtual void OnShakeTree(int i, int j) => GrowEffects(i, j, true);

	public void GrowEffects(int i, int j, bool shake = false)
	{
		int height = 1;
		while (Framing.GetTileSafely(i, j - height).TileType == Type)
			height++; //Move to the top of the tree

		if (shake)
			height = 1;

		OnGrowEffects(i, j - (height - 1), height);
	}

	/// <summary> Used to create effects when the tree is grown, such as leaves. Doubles for shake effects by default. </summary>
	protected virtual void OnGrowEffects(int i, int j, int height) { }

	public override IEnumerable<Item> GetItemDrops(int i, int j)
	{
		var drops = base.GetItemDrops(i, j);

		if (IsTreeTop(i, j))
			drops = drops.Concat([new Item(ItemID.Acorn)]);

		return drops;
	}

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (!fail) //Switch to the 'chopped' frame
			Framing.GetTileSafely(i, j + 1).TileFrameX = (short)(WorldGen.genRand.Next(9, 12) * FrameSize);
		else
			ShakeTree(i, j);
	}

	public sealed override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		DrawTreeBody(i, j, spriteBatch);

		if (IsTreeTop(i, j) || (int)Noise(new Vector2(i, j)) == 0)
			drawPoints.Add(new Point16(i, j));

		return false;
	}

	public virtual void DrawTreeBody(int i, int j, SpriteBatch spriteBatch)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out Color color, out Texture2D texture))
			return;

		var t = Main.tile[i, j];
		var source = new Rectangle(t.TileFrameX % (FrameSize * 12), 0, FrameSize - 2, FrameSize - 2);
		var position = new Vector2(i, j) * 16 - Main.screenPosition + TileExtensions.TileOffset + TreeExtensions.GetPalmTreeOffset(i, j);

		spriteBatch.Draw(texture, position, source, color, 0f, new Vector2(0, 0), 1f, SpriteEffects.None, 0f);
		return;
	}

	/// <summary> Used to draw treetops and tree branches based on coordinates resulting from <see cref="Noise"/>. </summary>
	public virtual void DrawTreeFoliage(int i, int j, SpriteBatch spriteBatch) { }

	protected virtual void CreateTree(int i, int j, int height)
	{
		int variance = WorldGen.genRand.Next(-8, 9) * 2;
		short xOff = 0;

		for (int h = 0; h < height; h++)
		{
			int frameX = WorldGen.genRand.Next(0, 3);

			if (h == 0)
				frameX = 3;
			if (j == height - 1)
				frameX = WorldGen.genRand.Next(4, 7);

			WorldGen.PlaceTile(i, j - h, Type, true);
			var tile = Framing.GetTileSafely(i, j - h);

			if (tile.HasTile && tile.TileType == Type)
			{
				tile.TileFrameX = (short)(frameX * FrameSize);
				tile.TileFrameY = TreeExtensions.GetPalmOffset(j, variance, height, ref xOff);
			}
		}

		if (Main.netMode != NetmodeID.SinglePlayer)
			NetMessage.SendTileSquare(-1, i, j + 1 - height, 1, height, TileChangeType.None);
	}

	public void ModifyTarget(ref int x, ref int y)
	{
		while (Main.tile[x, y + 1].HasTile && Main.tile[x, y + 1].TileType == Type && WorldGen.InWorld(x, y + 1))
			y++;
	}
}