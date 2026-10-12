using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using TileHelper.Common;

namespace SpiritReforged.Content.Forest.Graveyard;

public class GloomyStatue : ModTile, ILoadItem
{
	public sealed class GloomyLightingSystem : ModSystem
	{
		public static float? ForcedGraveyardIntensity { get; set; }

		public override void Load() => On_Player.UpdateGraveyard += On_Player_UpdateGraveyard;

		private static void On_Player_UpdateGraveyard(On_Player.orig_UpdateGraveyard orig, Player self, bool now)
		{
			orig(self, now);

			if (ForcedGraveyardIntensity is float value)
				UpdateGraveyardVisuals(value);
		}

		private static void UpdateGraveyardVisuals(float intensity)
		{
			if (Main.GraveyardVisualIntensity < intensity)
			{
				Main.GraveyardVisualIntensity += 0.02f;

				if (Main.GraveyardVisualIntensity > intensity)
					Main.GraveyardVisualIntensity = intensity;
			}
			else if (Main.GraveyardVisualIntensity > intensity)
			{
				Main.GraveyardVisualIntensity -= 0.01f;

				if (Main.GraveyardVisualIntensity < intensity)
					Main.GraveyardVisualIntensity = intensity;
			}
		}

		public override void ResetNearbyTileEffects() => ForcedGraveyardIntensity = null;
	}

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;
		Main.tileLighted[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
		TileObjectData.newTile.Width = 4;
		TileObjectData.newTile.Height = 2;
		TileObjectData.newTile.Origin = new Point16(2, 1);
		TileObjectData.newTile.CoordinateHeights = [16, 18];
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.Platform, 2, 1);
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(216, 216, 216), this.AutoModItem().DisplayName);
		DustType = -1;
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		Tile tile = Main.tile[i, j];
		if (tile.TileFrameY == 0 && TileDrawing.IsVisible(tile))
			(r, g, b) = (0.2f, 0.2f, 0.2f);
	}

	public override void NearbyEffects(int i, int j, bool closer) => GloomyLightingSystem.ForcedGraveyardIntensity = 1f;

	public override void EmitParticles(int i, int j, Tile tile, short tileFrameX, short tileFrameY, Color tileLight, bool visible)
	{
		if (visible && TileObjectData.IsTopLeft(i, j) && Main.rand.NextBool(8))
		{
			Vector2 position = new Vector2(i, j).ToWorldCoordinates() + Main.rand.NextVector2Circular(12, 12) + new Vector2(24, -24);
			var dust = Dust.NewDustPerfect(position, DustID.SilverCoin, null, 150, Scale: 0.5f);

			dust.noLightEmittence = true;
			dust.velocity = Vector2.Zero;
			dust.noGravity = true;
		}
	}

	public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
	{
		if (TileObjectData.IsTopLeft(i, j))
			Main.instance.TilesRenderer.AddSpecialPoint(i, j, TileDrawing.TileCounterType.CustomNonSolid);
	}

	public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
	{
		Texture2D texture = TextureAssets.Tile[Type].Value;
		Rectangle source = new(72, 0, 24, 24);

		float sine = EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 120f) * 4;
		Vector2 position = new Vector2(i, j).ToWorldCoordinates() - Main.screenPosition + new Vector2(24, -24 + sine);
		Color lightColor = Lighting.GetColor((position + Main.screenPosition).ToTileCoordinates());

		Graphics.DrawOutline((offset) =>
			spriteBatch.Draw(texture, position + offset, source, Color.White.Additive() * 0.5f, 0, source.Size() / 2, 1, 0, 0));

		spriteBatch.Draw(texture, position, source, Color.Lerp(lightColor, Color.White.Additive(), (EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 200f) + 1) / 2f * 0.1f), 0, source.Size() / 2, 1, 0, 0);
	}
}