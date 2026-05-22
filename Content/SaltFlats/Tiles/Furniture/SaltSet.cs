using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Content.SaltFlats.Tiles.Furniture;

public class SaltSet : FurnitureSet
{
	public override string Name => "Salt";
	public override FurnitureTile.IFurnitureData GetInfo(FurnitureTile tile) => new FurnitureTile.LightedInfo(tile.AutoModItem(), AutoContent.ItemType<SaltPanel>(), new(0.75f, 0.75f, 0.95f), 
		DustID.BubbleBurst_White, false, SaltBlock.Break);
	public override bool Autoload(FurnitureTile tile) => Excluding(tile, Types.Barrel, Types.Bench, Types.Clock, Types.Chandelier, Types.Candelabra);
}

public class SaltClock : ClockTile
{
	private const int FrameHeight = 90;
	public override IFurnitureData Info => ModContent.GetInstance<SaltSet>().GetInfo(this);

	public override void StaticDefaults()
	{
		base.StaticDefaults();
		AnimationFrameHeight = FrameHeight;
	}

	public override void AnimateTile(ref int frame, ref int frameCounter)
	{
		if (++frameCounter >= 4)
		{
			frameCounter = 0;
			frame = ++frame % 5;
		}
	}
}

public class SaltChandelier : ChandelierTile
{
	public override IFurnitureData Info => ModContent.GetInstance<SaltSet>().GetInfo(this);
	public override float Physics(Point16 topLeft) => 0;
}

[AutoloadGlowmask("", ForceUnset = true)]
public class SaltCandelabra : CandelabraTile
{
	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		var tile = Main.tile[i, j];
		if (!TileDrawing.IsVisible(tile))
			return;

		if (TileExtensions.GetVisualInfo(i, j, out Color color, out Texture2D texture))
			return;

		var data = TileObjectData.GetTileData(tile);
		int height = data.CoordinateHeights[tile.TileFrameY / data.CoordinateFullHeight];
		var source = new Rectangle(tile.TileFrameX, tile.TileFrameY + 32, 16, height);

		if (Info is LightedInfo l && l.Blur)
		{
			ulong randSeed = Main.TileFrameSeed ^ (ulong)((long)j << 32 | (uint)i);
			for (int c = 0; c < 7; c++) //Draw our glowmask with a randomized position
			{
				float shakeX = Utils.RandomInt(ref randSeed, -10, 11) * 0.15f;
				float shakeY = Utils.RandomInt(ref randSeed, -10, 1) * 0.35f;
				var offset = new Vector2(shakeX, shakeY);

				var position = new Vector2(i, j) * 16 - Main.screenPosition + offset + TileExtensions.TileOffset;
				spriteBatch.Draw(texture, position, source, color * 0.25f, 0, Vector2.Zero, 1, SpriteEffects.None, 0f);
			}
		}
		else
		{
			var position = new Vector2(i, j) * 16 - Main.screenPosition + TileExtensions.TileOffset;
			spriteBatch.Draw(texture, position, source, color, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
		}
	}
}