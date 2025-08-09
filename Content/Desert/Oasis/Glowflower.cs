using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileSway;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Forest.Stargrass.Tiles;
using SpiritReforged.Content.Particles;
using System.Linq;
using Terraria.DataStructures;
using Terraria.GameContent.Metadata;

namespace SpiritReforged.Content.Desert.Oasis;

[AutoloadGlowmask("255,255,255", false)]
public class Glowflower : ModTile, ISwayTile
{
	public const int StyleRange = 3;
	public const int TileHeight = 22;

	public override void Load() => TileEvents.OnRandomUpdate += Regrow;
	/// <summary> Causes Glowflower to regrow inside of underground oasis microbiomes. </summary>
	private static void Regrow(int i, int j, int type)
	{
		if (type == TileID.Sand && j > Main.worldSurface && WorldGen.genRand.NextBool(10) && WorldGen.InWorld(i, j - 1) && !Main.tile[i, j - 1].HasTile && Main.tile[i, j - 1].LiquidAmount < 100)
		{
			Point pt = new(i, j);
			int tileType = ModContent.TileType<Glowflower>();

			if (Placer.CanPlaceHerb(i, j, tileType) && MicrobiomeSystem.Microbiomes.Any(x => x is UndergroundOasisBiome o && o.Rectangle.Contains(pt)))
				Placer.PlaceTile(i, j - 1, tileType).Send();
		}
	}

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoFail[Type] = true;
		Main.tileCut[Type] = true;
		Main.tileLighted[Type] = true;

		TileID.Sets.SwaysInWindBasic[Type] = true;
		TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.LavaDeath = true;
		TileObjectData.newTile.WaterDeath = false;
		TileObjectData.newTile.CoordinatePadding = 2;
		TileObjectData.newTile.CoordinateWidth = 16;
		TileObjectData.newTile.CoordinateHeights = [TileHeight];
		TileObjectData.newTile.DrawYOffset = -(TileHeight - 18);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = StyleRange;
		TileObjectData.newTile.AnchorValidTiles = [TileID.Sand];
		TileObjectData.newTile.AnchorAlternateTiles = [TileID.ClayPot, TileID.PlanterBox];
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(200, 150, 50));
		DustType = DustID.Firefly;
		HitSound = SoundID.Grass;
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (closer)
		{
			if (!Main.gamePaused && Main.rand.NextBool(100))
			{
				var position = new Vector2(i, j).ToWorldCoordinates();
				ParticleHandler.SpawnParticle(new GlowParticle(position, Main.rand.NextVector2Unit(), Color.Lerp(Color.GreenYellow, Color.Goldenrod, Main.rand.NextFloat()), Main.rand.NextFloat(0.2f, 0.5f), 300, 2, (p) =>
				{
					p.Velocity = p.Velocity.RotatedByRandom(0.3f);

					if (p.Position.DistanceSQ(position) > 100 * 100)
						p.Velocity = Vector2.Lerp(p.Velocity, p.Position.DirectionTo(position), 0.05f);
					else if (Collision.SolidCollision(p.Position - new Vector2(2), 4, 4))
						p.Velocity.Y -= 0.05f;
				}));
			}
		}
		else
		{
			Main.SceneMetrics.HasSunflower = true;
		}
	}

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool())
		{
			var position = new Vector2(i, j).ToWorldCoordinates();
			int whoAmI = NPC.NewNPC(new EntitySource_TileBreak(i, j), (int)position.X, (int)position.Y, NPCID.Firefly);

			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendData(MessageID.SyncNPC, number: whoAmI);
		}
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 2;
	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) => (r, g, b) = (0.25f, 0.15f, 0.05f);

	public void DrawSway(int i, int j, SpriteBatch spriteBatch, Vector2 offset, float rotation, Vector2 origin)
	{
		var tile = Framing.GetTileSafely(i, j);
		int type = tile.TileType;
		var data = TileObjectData.GetTileData(tile);

		var drawPos = new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y);
		var source = new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, TileHeight);
		var dataOffset = new Vector2(data.DrawXOffset, data.DrawYOffset);

		spriteBatch.Draw(TextureAssets.Tile[type].Value, drawPos + offset + dataOffset, source, Lighting.GetColor(i, j), rotation, origin, 1, default, 0);

		var glowmask = GlowmaskTile.TileIdToGlowmask[Type].Glowmask.Value;
		spriteBatch.Draw(glowmask, drawPos + offset + dataOffset, source, GetGlow(new(i, j)), rotation, origin, 1, default, 0);

		static Color GetGlow(Point16 coords)
		{
			const float maxDistance = 140 * 140;

			float distance = Main.player[Player.FindClosest(coords.ToWorldCoordinates(0, 0), 16, 16)].DistanceSQ(coords.ToWorldCoordinates());
			return StargrassTile.Glow(new Point(coords.X, coords.Y)) * MathHelper.Clamp(1f - distance / maxDistance, 0.4f, 1f);
		}
	}

	public float Physics(Point16 coords)
	{
		var data = TileObjectData.GetTileData(Framing.GetTileSafely(coords));
		float rotation = Main.instance.TilesRenderer.GetWindCycle(coords.X, coords.Y, TileSwaySystem.Instance.GrassWindCounter);

		if (!WorldGen.InAPlaceWithWind(coords.X, coords.Y, data.Width, data.Height))
			rotation = 0f;

		return rotation + Main.instance.TilesRenderer.GetWindGridPush(coords.X, coords.Y, 20, 0.35f) * 1.5f;
	}
}