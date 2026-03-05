using SpiritReforged.Common.ItemCommon.FloatingItem;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.DrawPreviewHook;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Ocean.Items.KoiTotem;

public class KoiTotem : FloatingItem
{
	public override float SpawnWeight => 0.005f;
	public override float Weight => base.Weight * 0.9f;
	public override float Bouyancy => base.Bouyancy * 1.07f;

	public override void SetStaticDefaults() => ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<AncientKoiTotem>();

	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<KoiTotemTile>());
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.Blue;
	}
}

public class KoiTotemTile : ModTile, IDrawPreview
{
	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Height = 4;
		TileObjectData.newTile.Origin = new(0, 3);
		TileObjectData.newTile.CoordinateHeights = [16, 16, 16, 18];
		TileObjectData.newTile.CoordinateWidth = 18;
		TileObjectData.newTile.Direction = TileObjectDirection.PlaceRight;
		TileObjectData.newTile.StyleWrapLimit = 2; 
		TileObjectData.newTile.StyleMultiplier = 2; 
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
		TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceLeft; 
		TileObjectData.addAlternate(1); 
		TileObjectData.addTile(Type);

		DustType = DustID.Ash;
		AddMapEntry(new Color(107, 90, 64), CreateMapEntryName());
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		var player = Main.LocalPlayer;

		if (!closer)
		{
			if (!player.dead)
				player.AddBuff(ModContent.BuffType<KoiTotemBuff>(), 12);
		}
		else if (TileObjectData.IsTopLeft(i, j) && KoiTotemBuff.CursorOpacity > 0) //Create fancy visuals when bait is replenished
		{
			var t = Main.tile[i, j];
			int height = TileObjectData.GetTileData(t)?.Height ?? 0;
			var pos = new Vector2(i, j).ToWorldCoordinates(0, height * 16);

			if (Main.rand.NextBool())
			{
				var color = Color.Lerp(Color.LightBlue, Color.Cyan, Main.rand.NextFloat());
				float magnitude = Main.rand.NextFloat();

				ParticleHandler.SpawnParticle(new GlowParticle(pos + new Vector2(Main.rand.NextFloat(32), 0), Vector2.UnitY * -magnitude, color, (1f - magnitude) * .25f, Main.rand.Next(30, 120), 5, extraUpdateAction: delegate (Particle p)
					{ p.Velocity = p.Velocity.RotatedBy(Main.rand.NextFloat(-.1f, .1f)); }));
			}

			if (KoiTotemBuff.CursorOpacity > .9f)
			{
				const string path = "SpiritReforged/Assets/SFX/Ambient/MagicFeedback";

				ParticleHandler.SpawnParticle(new DissipatingImage(pos + new Vector2(18, 0), Color.Cyan * .15f, 0, .25f, 1f, "Bloom", 120));

				SoundEngine.PlaySound(new SoundStyle(path + 1) with { Volume = .3f, PitchRange = (-1, -.75f) }, pos);
				SoundEngine.PlaySound(new SoundStyle(path + 2) with { Volume = .4f, PitchRange = (-.65f, -.35f) }, pos);
			}
		}
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out var color, out var texture))
			return false;

		var t = Main.tile[i, j];
		var frame = new Point(t.TileFrameX, t.TileFrameY);
		var source = new Rectangle(frame.X, frame.Y, 18, (frame.Y == 54) ? 18 : 16);
		int offX = (frame.X % TileObjectData.GetTileData(Type, 0).CoordinateFullWidth == 0) ? -2 : 0;
		var position = new Vector2(i, j) * 16 - Main.screenPosition + TileExtensions.TileOffset + new Vector2(offX, 0);

		spriteBatch.Draw(texture, position, source, color, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

		return false;
	}

	public void DrawPreview(SpriteBatch spriteBatch, TileObjectPreviewData op, Vector2 position)
	{
		var texture = TextureAssets.Tile[op.Type].Value;
		var data = TileObjectData.GetTileData(op.Type, op.Style, op.Alternate);
		var color = ((op[0, 0] == 1) ? Color.White : Color.Red * .7f) * .5f;

		int style = data.CalculatePlacementStyle(op.Style, op.Alternate, op.Random);

		for (int frameX = 0; frameX < 2; frameX++)
		{
			for (int frameY = 0; frameY < 4; frameY++)
			{
				(int x, int y) = (op.Coordinates.X + frameX, op.Coordinates.Y + frameY);

				var source = new Rectangle(frameX * 20 + style * data.CoordinateFullWidth, frameY * 18, 18, (frameY == 3) ? 18 : 16);
				int offX = (frameX == 0) ? -2 : 0;
				var drawPos = new Vector2(x, y) * 16 - Main.screenPosition + TileExtensions.TileOffset + new Vector2(offX, 0);

				spriteBatch.Draw(texture, drawPos, source, color, 0, Vector2.Zero, 1, SpriteEffects.None, 0f);
			}
		}
	}
}