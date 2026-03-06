using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Relics;

/// <summary> Contains all Spirit boss relics in different styles. </summary>
public class Relics : ModTile
{
	public const int FrameWidth = 18 * 3;
	public const int FrameHeight = 18 * 4;
	public const int NumRelics = 1;

	public static readonly Asset<Texture2D> PropTexture = DrawHelpers.RequestLocal<Relics>("RelicProp", false);

	public override void SetStaticDefaults() {
		Main.tileShine[Type] = 400; 
		Main.tileFrameImportant[Type] = true; 
		TileID.Sets.InteractibleByNPCs[Type] = true; 

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x4); 
		TileObjectData.newTile.LavaDeath = false; 
		TileObjectData.newTile.DrawYOffset = 2; 
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
		TileObjectData.newTile.StyleHorizontal = false; 

		TileObjectData.newTile.StyleWrapLimitVisualOverride = 2;
		TileObjectData.newTile.StyleMultiplier = 2;
		TileObjectData.newTile.StyleWrapLimit = 2;
		TileObjectData.newTile.styleLineSkipVisualOverride = 0;

		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
		TileObjectData.addAlternate(1);

		TileObjectData.addTile(Type);

		AddMapEntry(new Color(233, 207, 94), Language.GetText("MapObject.Relic"));
		DustType = -1;
	}

	public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY) {
		tileFrameX %= FrameWidth; 
		tileFrameY %= FrameHeight * 2; 
	}

	public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData) {
		if (TileObjectData.IsTopLeft(i, j))
			Main.instance.TilesRenderer.AddSpecialLegacyPoint(i, j);
	}

	public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch) {
		if (Main.tile[i, j] is Tile tile && tile.HasTile)
		{
			Texture2D texture = PropTexture.Value;
			Rectangle frame = texture.Frame(NumRelics, NumRelics, 0, tile.TileFrameX / FrameWidth, 0, -2);
			Vector2 origin = frame.Size() / 2f;
			Vector2 worldPos = new Vector2(i, j).ToWorldCoordinates(24f, 64f);
			Color color = Lighting.GetColor(i, j);
			SpriteEffects effects = (tile.TileFrameY / FrameHeight != 0) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

			float offset = (float)MathF.Round(MathF.Sin(Main.GlobalTimeWrappedHourly * MathHelper.TwoPi / 5f) * 4);
			Vector2 drawPos = worldPos + TileExtensions.TileOffset - Main.screenPosition + new Vector2(0f, -40f) + new Vector2(0f, offset);

			spriteBatch.Draw(texture, drawPos, frame, color, 0f, origin, 1f, effects, 0f);

			float scale = (float)Math.Sin(Main.GlobalTimeWrappedHourly * MathHelper.TwoPi / 2f) * 0.3f + 0.7f;
			Color effectColor = color.Additive();
			effectColor = effectColor * 0.1f * scale;

			for (float num5 = 0f; num5 < 1f; num5 += 355f / (678f * (float)Math.PI))
				spriteBatch.Draw(texture, drawPos + (MathHelper.TwoPi * num5).ToRotationVector2() * (6f + offset / 4f * 2f), frame, effectColor, 0f, origin, 1f, effects, 0f);
		}
	}
}

public class ScarabRelic : ModItem
{
	public override void SetDefaults() => Item.SetRelicsDefaults(0);
}