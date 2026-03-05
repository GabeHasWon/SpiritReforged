using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.WorldGeneration.Micropasses.Passes;
using Terraria.DataStructures;

namespace SpiritReforged.Common.TileCommon.PresetTiles;

public abstract class StatueTile : ModTile, IAutoloadTileItem
{
	public abstract int NPCType { get; }

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileObsidianKill[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;
		TileID.Sets.IsAMechanism[Type] = true;
		Main.tileSpelunker[Type] = true;

		DustType = DustID.Stone;
		AdjTiles = [TileID.Statues];

		AddObjectData();

		RegisterItemDrop(this.AutoItem().type);
		AddMapEntry(new Color(144, 148, 144), Language.GetText("MapObject.Statue"));
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

	/// <summary> Adds common tile object data for statues and automatically registers it for generation in <see cref="NewStatuesMicropass"/>. </summary>
	public virtual void AddObjectData()
	{
		NewStatuesMicropass.Statues.Add(Type);

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.newTile.StyleWrapLimit = 2;
		TileObjectData.newTile.StyleMultiplier = 2;
		TileObjectData.newTile.StyleHorizontal = true;

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
		TileObjectData.addAlternate(1);
		TileObjectData.addTile(Type);
	}

	public override void HitWire(int i, int j)
	{
		TileExtensions.GetTopLeft(ref i, ref j);
		var data = TileObjectData.GetTileData(Main.tile[i, j]);

		int width = data?.Height ?? 2;
		int height = data?.Height ?? 3;

		for (int x = i; x < i + width; x++)
		{
			for (int y = j; y < j + height; y++)
				Wiring.SkipWire(x, y);
		}

		Vector2 spawn = new Vector2(i, j).ToWorldCoordinates(width / 2f * 16 - 8, height * 16);
		StatueSpawn(i, j, spawn);
	}

	public virtual void StatueSpawn(int i, int j, Vector2 center)
	{
		int type = NPCType;

		if (Wiring.CheckMech(i, j, 30) && NPC.MechSpawn(center.X, center.Y, type))
		{
			var source = new EntitySource_TileUpdate(i, j, context: "Statue");
			var npc = NPC.NewNPCDirect(source, (int)center.X, (int)center.Y, type);

			npc.value = 0;
			npc.npcSlots = 0;
			npc.SpawnedFromStatue = true;
		}
	}
}