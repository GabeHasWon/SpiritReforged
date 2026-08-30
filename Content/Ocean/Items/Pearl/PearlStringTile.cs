using RubbleAutoloader;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using TileHelper.Common;

namespace SpiritReforged.Content.Ocean.Items.Pearl;

public class PearlStringTile : ModTile, IAutoloadRubble
{
	private static readonly int[] _sandyTypes = [TileID.Sand, TileID.Ebonsand, TileID.Crimsand, TileID.Pearlsand];

	public IAutoloadRubble.RubbleData Data => new(ModContent.ItemType<PearlString>(), IAutoloadRubble.RubbleSize.Small);

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoFail[Type] = true;

		TileID.Sets.CanDropFromRightClick[Type] = true;
		TileHelperSets.TileGlowmask[Type] = Helpers.RequestGlowmask(this, static (i, j) => Lighting.GetColor(i, j) * 2f);

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x1);
		TileObjectData.newTile.CoordinateHeights = [16];
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 2, 0);
		TileObjectData.newTile.AnchorValidTiles = _sandyTypes;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(100, 100, 120));
		RegisterItemDrop(ModContent.ItemType<PearlString>());
		TileMethods.Merge(Type, _sandyTypes);

		DustType = DustID.Sand;
	}

	public override void MouseOver(int i, int j)
	{
		if (Autoloader.IsRubble(Type))
			return;

		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = ModContent.ItemType<PearlString>();
	}

	public override bool CreateDust(int i, int j, ref int type)
	{
		Tile tile = Main.tile[i, j];
		type = (tile.TileFrameY / 18) switch
		{
			1 => DustID.Corruption,
			2 => DustID.Crimson,
			3 => DustID.Pearlsand,
			_ => DustID.Sand,
		};

		return true;
	}

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		Tile tile = Main.tile[i, j];
		Tile left = Framing.GetTileSafely(i - 1, j);
		Tile right = Framing.GetTileSafely(i + 1, j);
		Tile up = Framing.GetTileSafely(i, j - 1);
		Tile down = Framing.GetTileSafely(i, j + 1);

		tile.TileFrameX %= 36; //Reset frame
		tile.TileFrameY = 0;

		for (int x = 0; x < _sandyTypes.Length; x++) //Select a matching sand style
		{
			int mergeType = _sandyTypes[x];
			if (down.TileType == mergeType)
				tile.TileFrameY += (short)(18 * x);

			if (up.TileType == mergeType) //Upward frame adjustment
				tile.TileFrameX += 72;
			else if (left.TileType == mergeType || right.TileType == mergeType) //Side frame adjustment
				tile.TileFrameX += 36;
		}

		return true;
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (closer && !Main.gamePaused && TileDrawing.IsVisible(Main.tile[i, j]))
		{
			Rectangle region = new(i * 16, j * 16, 16, 16);
			if (Main.rand.NextBool(50) && Main.LocalPlayer.DistanceSQ(region.Center()) < 100 * 100)
			{
				Vector2 dustPos = Main.rand.NextVector2FromRectangle(region);

				var dust = Dust.NewDustPerfect(dustPos, DustID.SilverCoin, Scale: .2f);
				dust.noGravity = true;
				dust.velocity = Vector2.Zero;
				dust.noLightEmittence = true;

				ParticleHandler.SpawnParticle(new Particles.GlowParticle(dustPos, Vector2.Zero, Main.DiscoColor * 0.8f, Color.Black, 0.75f, 50));
			}
		}
	}
}