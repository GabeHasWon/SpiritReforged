using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileSway;
using SpiritReforged.Content.Savanna.Items.Food;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Savanna.Tiles;

public class BaobabPod : ModTile, ISwayTile
{
	private const int numStages = 3;

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileMergeDirt[Type] = false;
		Main.tileBlockLight[Type] = false;
		Main.tileFrameImportant[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		TileObjectData.newTile.Origin = new(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 2, 0);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(121, 92, 19));
		DustType = DustID.WoodFurniture;
		HitSound = SoundID.Dig;
	}

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (!ProgressStage(i, j, out int stage))
			return;

		fail = true;
		TileExtensions.GetTopLeft(ref i, ref j);

		if (!Main.dedServ)
		{
			ISwayTile.SetInstancedRotation(i, j, Main.rand.NextFloat(-1f, 1f), fail);

			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/NPCHit/HardNaturalHit") with { Pitch = stage - 1 }, new Vector2(i + 1, j + 1) * 16);
			for (int d = 0; d < 10; d++)
			{
				Dust.NewDustDirect(new Vector2(i, j) * 16, 32, 32, DustType, Scale: Main.rand.NextFloat())
					.velocity = (Vector2.UnitY * -Main.rand.NextFloat(2f)).RotatedByRandom(MathHelper.Pi);
			}
		}

		if (stage == numStages - 1) //Break open
		{
			if (!Main.dedServ)
			{
				var source = new EntitySource_TileBreak(i, j);

				SoundEngine.PlaySound(SoundID.NPCHit7 with { Pitch = -1 }, new Vector2(i + 1, j + 1) * 16);
				for (int g = 1; g < 4; g++)
				{
					Gore.NewGore(source, Main.rand.NextVector2FromRectangle(new Rectangle(i * 16, j * 16, 32, 16)),
						(Vector2.UnitY * -Main.rand.NextFloat(1f, 4f)).RotatedByRandom(1.5f), Mod.Find<ModGore>("BaobabPod" + g).Type);
				}

				for (int g = 1; g < 4; g++)
				{
					var gore = Gore.NewGoreDirect(source, new Vector2(i + 1, j + 1) * 16,
						Vector2.Zero, GoreID.Smoke1);

					gore.velocity = Vector2.UnitX * Main.rand.NextFloat(-1f, 1f);
					gore.alpha = 200;
					gore.position -= new Vector2(gore.Width, gore.Height) / 2;
				}
			}

			DropItem(i, j, ModContent.ItemType<Items.Tools.LivingBaobabLeafWand>());
			DropItem(i, j, ModContent.ItemType<Items.Tools.LivingBaobabWand>());
			DropItem(i, j, ItemID.Waterleaf, Main.rand.Next(2) + 1);
			DropItem(i, j, ModContent.ItemType<Items.SavannaGrassSeeds>(), Main.rand.Next(3) + 1);
			DropItem(i, j, ModContent.ItemType<BaobabFruit>());

			if (Main.rand.NextBool(3))
				DropItem(i, j, ItemID.Vine);

			ItemMethods.SplitCoins(Main.rand.Next(500, 800), delegate (int type, int stack)
			{ DropItem(i, j, type, stack); });
		}
		else
		{
			ItemMethods.SplitCoins(Main.rand.Next(150, 200), delegate (int type, int stack)
			{ DropItem(i, j, type, stack); });
		}
	}

	private static bool ProgressStage(int i, int j, out int stage)
	{
		var tile = Framing.GetTileSafely(i, j);
		var data = TileObjectData.GetTileData(tile);

		if (tile.TileFrameX / data.CoordinateFullWidth >= numStages - 1)
		{
			stage = numStages;
			return false;
		}

		TileExtensions.GetTopLeft(ref i, ref j);

		for (int frameX = 0; frameX < data.Width; frameX++)
			for (int frameY = 0; frameY < data.Height; frameY++)
				Framing.GetTileSafely(i + frameX, j + frameY).TileFrameX += (short)data.CoordinateFullWidth;

		if (Main.netMode == NetmodeID.Server)
			NetMessage.SendTileSquare(-1, i, j, 2, 2);

		stage = tile.TileFrameX / data.CoordinateFullWidth;
		return true;
	}

	private static void DropItem(int i, int j, int type, int stack = 1)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;

		var source = new EntitySource_TileBreak(i, j);

		int id = Item.NewItem(source, new Rectangle(i * 16, j * 16, 32, 16), type, stack, true);
		Main.item[id].velocity = (Vector2.UnitY * -Main.rand.NextFloat(1f, 4f)).RotatedByRandom(1.5f);
		Main.item[id].noGrabDelay = 100;

		if (Main.netMode == NetmodeID.Server)
			NetMessage.SendData(MessageID.SyncItem, number: id, number2: 100f);
	}

	public void DrawSway(int i, int j, SpriteBatch spriteBatch, Vector2 offset, float rotation, Vector2 origin)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out var color, out var texture))
			return;

		var t = Main.tile[i, j];
		var data = TileObjectData.GetTileData(t);

		var position = new Vector2(i, j) * 16 - Main.screenPosition;
		var source = new Rectangle(t.TileFrameX, t.TileFrameY, 16, data.CoordinateHeights[t.TileFrameY / 18]);

		spriteBatch.Draw(texture, position + origin, source, color, ISwayTile.GetInstancedRotation(i, j), origin, 1, SpriteEffects.None, 0);

		if (t.TileFrameY > 0)
			DrawGrassOverlay(i, j, spriteBatch, offset, rotation, origin);
	}

	private static void DrawGrassOverlay(int i, int j, SpriteBatch spriteBatch, Vector2 offset, float rotation, Vector2 origin)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out var color, out var texture))
			return;

		var t = Main.tile[i, j];
		var data = TileObjectData.GetTileData(t);
		int frameX = t.TileFrameX % data.CoordinateFullWidth;

		Vector2 position = new Vector2(i, j) * 16 - Main.screenPosition + new Vector2((frameX == 0) ? -2 : 0, 0);
		var source = new Rectangle(18 * 6, frameX, 18, 18);

		spriteBatch.Draw(texture, position + offset, source, color, rotation, origin, 1, SpriteEffects.None, 0);
	}

	public float Physics(Point16 topLeft)
	{
		var data = TileObjectData.GetTileData(Framing.GetTileSafely(topLeft));
		float rotation = Main.instance.TilesRenderer.GetWindCycle(topLeft.X, topLeft.Y, TileSwaySystem.Instance.GrassWindCounter);

		if (!WorldGen.InAPlaceWithWind(topLeft.X, topLeft.Y, data.Width, data.Height))
			rotation = 0f;

		return (rotation + TileSwayHelper.GetHighestWindGridPushComplex(topLeft.X, topLeft.Y, data.Width, data.Height, 20, 3f, 1, true)) * 1f;
	}
}
