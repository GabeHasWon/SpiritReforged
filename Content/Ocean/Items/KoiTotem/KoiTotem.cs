using SpiritReforged.Common.ItemCommon.FloatingItem;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.GameContent.Drawing;
using TileHelper.Common;

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

public class KoiTotemTile : ModTile
{
	public static readonly SoundStyle Feedback = new("SpiritReforged/Assets/SFX/Ambient/MagicFeedback", 2);

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
		AddMapEntry(new Color(107, 90, 64), ModContent.GetInstance<KoiTotem>().DisplayName);
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		Player player = Main.LocalPlayer;

		if (!closer)
		{
			if (!player.dead)
				player.AddBuff(ModContent.BuffType<KoiTotemBuff>(), 12);
		}
		else if (TileObjectData.IsTopLeft(i, j) && KoiTotemBuff.CursorOpacity > 0) //Create fancy visuals when bait is replenished
		{
			int height = TileObjectData.GetTileData(Main.tile[i, j])?.Height ?? 0;
			Vector2 position = new Vector2(i, j).ToWorldCoordinates(0, height * 16);

			if (Main.rand.NextBool())
			{
				var color = Color.Lerp(Color.LightBlue, Color.Cyan, Main.rand.NextFloat());
				float magnitude = Main.rand.NextFloat();

				ParticleHandler.SpawnParticle(new GlowParticle(position + new Vector2(Main.rand.NextFloat(32), 0), Vector2.UnitY * -magnitude, color, (1f - magnitude) * .25f, Main.rand.Next(30, 120), 5, extraUpdateAction: delegate (Particle p)
					{ p.Velocity = p.Velocity.RotatedBy(Main.rand.NextFloat(-0.1f, 0.1f)); }));
			}

			if (KoiTotemBuff.CursorOpacity > 0.9f)
			{
				ParticleHandler.SpawnParticle(new DissipatingImage(position + new Vector2(18, 0), Color.Cyan * 0.15f, 0, 0.25f, 1f, "Bloom", 120));

				SoundEngine.PlaySound(Feedback with { Volume = 0.3f, PitchRange = (-1, -0.75f) }, position);
				SoundEngine.PlaySound(Feedback with { Volume = 0.4f, PitchRange = (-0.65f, -0.35f) }, position);
			}
		}
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		Tile tile = Main.tile[i, j];

		if (!TileDrawing.IsVisible(tile) || TileObjectData.GetTileData(tile) is not TileObjectData tileObjectData)
			return false;

		int offset = (tile.TileFrameX % tileObjectData.CoordinateFullWidth == 0) ? -2 : 0;
		Rectangle source = new(tile.TileFrameX, tile.TileFrameY, 18, (tile.TileFrameY == 54) ? 18 : 16);
		Vector2 position = new Vector2(i, j) * 16 - Main.screenPosition + TileMethods.TileOffset + new Vector2(offset, 0);

		spriteBatch.Draw(Helpers.GetTileTextureValue(tile), position, source, Lighting.GetColor(i, j), 0, Vector2.Zero, 1, 0, 0);
		return false;
	}

	public override bool PreDrawPlacementPreview(int i, int j, SpriteBatch spriteBatch, ref Rectangle frame, ref Vector2 position, ref Color color, bool validPlacement, ref SpriteEffects spriteEffects)
	{
		if (TileObjectData.GetTileData(Type, 0) is TileObjectData tileObjectData)
		{
			int offset = (frame.X % tileObjectData.CoordinateFullWidth == 0) ? -2 : 0;
			position += Vector2.UnitX * (offset + 1);
		}

		return true;
	}
}