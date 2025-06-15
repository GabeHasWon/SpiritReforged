using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Common.TileCommon.PresetTiles;

[AutoloadGlowmask("255,255,255")]
public abstract class TorchTile : ModTile
{
	public virtual Vector3 Light => new(0.9f);

	public override void SetStaticDefaults()
	{
		Main.tileLighted[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileSolid[Type] = false;
		Main.tileNoAttach[Type] = true;
		Main.tileNoFail[Type] = true;
		Main.tileWaterDeath[Type] = true;

		TileID.Sets.FramesOnKillWall[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;
		TileID.Sets.DisableSmartInteract[Type] = true;
		TileID.Sets.Torch[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.StyleTorch);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
		TileObjectData.newAlternate.CopyFrom(TileObjectData.StyleTorch);
		TileObjectData.newAlternate.AnchorLeft = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide | AnchorType.Tree | AnchorType.AlternateTile, TileObjectData.newTile.Height, 0);
		TileObjectData.newAlternate.AnchorAlternateTiles = [124, 561, 574, 575, 576, 577, 578];
		TileObjectData.addAlternate(1);
		TileObjectData.newAlternate.CopyFrom(TileObjectData.StyleTorch);
		TileObjectData.newAlternate.AnchorRight = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide | AnchorType.Tree | AnchorType.AlternateTile, TileObjectData.newTile.Height, 0);
		TileObjectData.newAlternate.AnchorAlternateTiles = [124, 561, 574, 575, 576, 577, 578];
		TileObjectData.addAlternate(2);
		TileObjectData.newAlternate.CopyFrom(TileObjectData.StyleTorch);
		TileObjectData.newAlternate.AnchorWall = true;
		TileObjectData.addAlternate(0);
		TileObjectData.addTile(Type);

		DustType = DustID.Torch;
		AdjTiles = [TileID.Torches];
		VanillaFallbackOnModDeletion = TileID.Torches;

		AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);
		AddMapEntry(new Color(200, 200, 200), Language.GetText("ItemName.Torch"));
	}

	public override void MouseOver(int i, int j)
	{
		var player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;

		int style = TileObjectData.GetTileStyle(Main.tile[i, j]);
		player.cursorItemIconID = TileLoader.GetItemDropFromTypeAndStyle(Type, style);
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = Main.rand.Next(1, 3);
	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		if (Main.tile[i, j].TileFrameX < 66)
			(r, g, b) = (Light.X, Light.Y, Light.Z);
	}

	public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY) => offsetY = WorldGen.SolidTile(i, j - 1) ? 4 : 0;

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		var tile = Main.tile[i, j];
		if (!TileDrawing.IsVisible(tile))
			return;

		int offsetY = WorldGen.SolidTile(i, j - 1) ? 4 : 0;
		ulong randSeed = Main.TileFrameSeed ^ (ulong)((long)j << 32 | (long)(uint)i);
		var frame = new Rectangle(tile.TileFrameX, tile.TileFrameY, 20, 20);

		for (int k = 0; k < 7; k++)
		{
			float xx = Utils.RandomInt(ref randSeed, -10, 11) * 0.15f;
			float yy = Utils.RandomInt(ref randSeed, -10, 1) * 0.35f;
			var position = new Vector2(i * 16 - (int)Main.screenPosition.X - 4 / 2f + xx, j * 16 - (int)Main.screenPosition.Y + offsetY + yy) + TileExtensions.TileOffset;

			spriteBatch.Draw(GlowmaskTile.TileIdToGlowmask[Type].Glowmask.Value, position, frame, new Color(100, 100, 100, 0), 0f, default, 1f, SpriteEffects.None, 0f);
		}
	}

	public override void EmitParticles(int i, int j, Tile tileCache, short tileFrameX, short tileFrameY, Color tileLight, bool visible)
	{
		if (visible && Main.rand.NextBool(40) && tileFrameX < 66)
		{
			var dust = Dust.NewDustDirect(new Vector2(i * 16 + 4, j * 16), 4, 4, DustID.Torch, 0f, 0f, 100);
			dust.noGravity = !Main.rand.NextBool(3);
			dust.velocity *= 0.3f;
			dust.velocity.Y -= 1.5f;
		}
	}
}