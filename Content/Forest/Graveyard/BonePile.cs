using SpiritReforged.Common.TileCommon;
using Terraria.Audio;

namespace SpiritReforged.Content.Forest.Graveyard;

[DrawOrder(DrawOrderAttribute.Layer.OverPlayers)]
public class BonePile : ModTile
{
	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileBlockLight[Type] = true;

		TileID.Sets.GeneralPlacementTiles[Type] = false;
		TileID.Sets.CanBeClearedDuringOreRunner[Type] = false;

		AddMapEntry(new Color(150, 140, 110));
		DustType = DustID.Bone;
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out Color color, out Texture2D texture))
			return false;

		Tile tile = Main.tile[i, j];
		Rectangle source = new(tile.TileFrameX, tile.TileFrameY, 16, 16);
		Vector2 position = new Vector2(i, j) * 16 - Main.screenPosition + new Vector2(0, 2);

		spriteBatch.Draw(texture, position, source, color, 0, Vector2.Zero, 1, 0, 0f);

		if (!Main.gamePaused)
			SpawnDusts(new(i * 16, j * 16, 16, 16), Color.White);

		return false;
	}

	private static void SpawnDusts(Rectangle worldHitbox, Color tint)
	{
		if (Main.LocalPlayer.velocity.Y > 1 && Main.LocalPlayer.Hitbox.Intersects(worldHitbox))
		{
			Dust.NewDustDirect(worldHitbox.TopLeft(), worldHitbox.Width, worldHitbox.Height, DustID.Bone, 0, 0, 0, tint).velocity = (Main.LocalPlayer.velocity * -0.5f).RotatedByRandom(0.2f);
			SoundEngine.PlaySound(SoundID.DD2_SkeletonHurt with { MaxInstances = 1, Pitch = -0.2f, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew }, worldHitbox.Center());
		}
	}
}