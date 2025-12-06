using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.Tiles;

public class OldTomb : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Width = 3;
		TileObjectData.newTile.Height = 2;
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		TileObjectData.newTile.Origin = new(1, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.RandomStyleRange = 2;
		TileObjectData.newTile.StyleMultiplier = 2;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(104, 44, 28), CreateMapEntryName());
		DustType = -1;
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (!closer && Main.LocalPlayer.velocity.Length() > 3)
			OpenUp(i, j);
	}

	public static void OpenUp(int i, int j)
	{
		const int fullWidth = 18 * 3;
		TileExtensions.GetTopLeft(ref i, ref j);

		Tile tile = Main.tile[i, j];
		if (tile.TileFrameX % (fullWidth * 2) == 0) //Closed styles
		{
			for (int x = i; x < i + 3; x++)
			{
				for (int y = j; y < j + 2; y++)
					Main.tile[x, y].TileFrameX += fullWidth;
			}

			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendTileSquare(-1, i, j, 3, 2);

			Rectangle topArea = new(i * 16, j * 16, fullWidth, 2);

			for (int g = 1; g < 5; g++)
				Gore.NewGore(new EntitySource_TileUpdate(i, j), Main.rand.NextVector2FromRectangle(topArea), -Vector2.UnitY, SpiritReforgedMod.Instance.Find<ModGore>("RedBrick" + g).Type, 1f);

			ParticleHandler.SpawnParticle(new SmokeCloud(topArea.Center() + new Vector2(0, 8), -Vector2.UnitY, Color.SandyBrown * 0.8f, 0.1f, Common.Easing.EaseFunction.EaseCircularOut, 120)
			{
				Pixellate = true,
				PixelDivisor = 3,
				TertiaryColor = Color.IndianRed
			});
		}
	}
}