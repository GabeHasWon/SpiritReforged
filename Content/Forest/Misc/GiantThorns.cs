using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileSway;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Misc;

[DrawOrder(DrawOrderAttribute.Layer.NonSolid)]
public class GiantThorns : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileMerge[Type][TileID.Dirt] = true;
		Main.tileMerge[Type][TileID.Grass] = true;
		Main.tileNoFail[Type] = true;
		Main.tileCut[Type] = true;

		TileID.Sets.GeneralPlacementTiles[Type] = false;
		TileID.Sets.CanBeClearedDuringOreRunner[Type] = false;

		AddMapEntry(new Color(125, 160, 50));

		DustType = DustID.JunglePlants;
		HitSound = SoundID.Grass;
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (closer)
			return;

		Rectangle worldHitbox = new(i * 16, j * 16, 16, 16);
		if (Main.LocalPlayer.Hitbox.Intersects(worldHitbox))
		{
			Main.LocalPlayer.Hurt(PlayerDeathReason.ByOther(6), 10, 0, dodgeable: false);
			Main.LocalPlayer.AddBuff(BuffID.Poisoned, 180);
		}
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out Color color, out Texture2D texture))
			return false;

		Tile tile = Main.tile[i, j];
		Rectangle source = new(tile.TileFrameX, tile.TileFrameY, 18, 18);

		var renderer = Main.instance.TilesRenderer;
		float wind = (renderer.InAPlaceWithWind(i, j, 1, 1) ? renderer.GetWindCycle(i, j, TileSwaySystem.SunflowerWindCounter) : 0) + renderer.GetWindGridPush(i, j, 20, 0.1f);
		Vector2 position = new Vector2(i, j).ToWorldCoordinates() - Main.screenPosition + new Vector2(wind, wind);

		spriteBatch.Draw(texture, position, source, color, wind * 0.05f, new(8), 1, 0, 0f);

		return false;
	}
}