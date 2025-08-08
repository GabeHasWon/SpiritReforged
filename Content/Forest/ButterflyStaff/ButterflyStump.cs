using RubbleAutoloader;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Common.WorldGeneration;
using System.Linq;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;

namespace SpiritReforged.Content.Forest.ButterflyStaff;

[AutoloadGlowmask("100,100,100,0")]
public class ButterflyStump : ModTile, IAutoloadRubble
{
	private const int FrameHeight = 18 * 4;

	protected static int ItemType => ModContent.ItemType<ButterflyStaff>();
	public IAutoloadRubble.RubbleData Data => new(ItemType, IAutoloadRubble.RubbleSize.Large);

	private static bool HasItem(int i, int j) => Framing.GetTileSafely(i, j).TileFrameY < FrameHeight;
	private static bool TopHalf(int i, int j) => Framing.GetTileSafely(i, j).TileFrameY % FrameHeight < 18 * 2;

	public override void SetStaticDefaults()
	{
		Main.tileLighted[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileWaterDeath[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileID.Sets.HasOutlines[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;
		TileID.Sets.InteractibleByNPCs[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new Point16(1, 3);
		TileObjectData.newTile.Height = 4;
		TileObjectData.newTile.CoordinateHeights = [16, 16, 16, 16];
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(212, 125, 93));
		RegisterItemDrop(ItemType);

		AnimationFrameHeight = FrameHeight;
		DustType = DustID.WoodFurniture;
	}

	public override bool CreateDust(int i, int j, ref int type) => !TopHalf(i, j);
	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => HasItem(i, j);
	public override bool CanKillTile(int i, int j, ref bool blockDamaged) => !TopHalf(i, j) || HasItem(i, j);
	public override bool CanDrop(int i, int j) => HasItem(i, j);

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		var overlap = MicrobiomeSystem.Microbiomes.Where(x => x is ButterflyShrineBiome e && e.Rectangle.Contains(new Point(i, j)));
		bool removedAny = false;

		foreach (var biome in overlap)
			removedAny |= MicrobiomeSystem.Microbiomes.Remove(biome); //Remove any overlapping microbiomes if it is destroyed

		if (removedAny && Main.netMode == NetmodeID.Server)
			NetMessage.SendData(MessageID.WorldData); //Sync the changes
	}

	public override void MouseOver(int i, int j)
	{
		if (!HasItem(i, j) || Autoloader.IsRubble(Type))
			return;

		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = ItemType;
	}

	public override bool RightClick(int i, int j)
	{
		if (HasItem(i, j) && !Autoloader.IsRubble(Type))
		{
			TileExtensions.GetTopLeft(ref i, ref j);

			for (int x = i; x < i + 2; x++)
				for (int y = j; y < j + 4; y++)
					Main.tile[x, y].TileFrameY += FrameHeight;

			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendTileSquare(-1, i, j, 2, 4);

			ItemMethods.NewItemSynced(new EntitySource_TileBreak(i, j), ItemType, new Vector2(i, j).ToWorldCoordinates(16, 0), true);
			NPC.NewNPCDirect(null, (i + 1) * 16, (j + 1) * 16, ModContent.NPCType<ButterflyCritter>()).netUpdate = true;

			return true;
		}

		return false;
	}

	public override void RandomUpdate(int i, int j) //Randomly generate butterflies
	{
		const int tries = 20;

		if (!Autoloader.IsRubble(Type) && WorldGen.PlayerLOS(i, j) && NPC.CountNPCS(ModContent.NPCType<ButterflyCritter>()) < 5)
		{
			var world = new Vector2(i, j).ToWorldCoordinates();
			Vector2 pos = world;

			for (int t = 0; t < tries; t++)
			{
				pos = world + Main.rand.NextVector2Unit() * Main.rand.NextFloat(16 * 10);
				if (!Collision.SolidCollision(pos, 8, 8) && Collision.CanHitLine(pos, 0, 0, world, 0, 0))
					break;
			}

			NPC.NewNPCDirect(null, pos, ModContent.NPCType<ButterflyCritter>());
		}
	}

	public override void AnimateTile(ref int frame, ref int frameCounter)
	{
		if (++frameCounter >= 4)
		{
			frameCounter = 0;
			frame = ++frame % 8;
		}
	}

	public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
	{
		if (HasItem(i, j))
			frameYOffset = Main.tileFrame[type] * FrameHeight;
		else
			frameYOffset = FrameHeight * 7; //Don't animate
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		if (!HasItem(i, j))
			return;

		var color = new Vector3(255, 125, 255) * .001f;
		(r, g, b) = (color.X, color.Y, color.Z);
	}
}