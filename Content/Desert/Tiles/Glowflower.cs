using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.WorldGeneration.Microbiomes.Biomes;
using SpiritReforged.Content.Forest.Stargrass.Tiles;
using SpiritReforged.Content.Particles;
using System.Linq;
using Terraria.DataStructures;
using Terraria.GameContent.Metadata;
using TileHelper.Common;

namespace SpiritReforged.Content.Desert.Tiles;

public class Glowflower : ModTile
{
	public const int StyleRange = 3;
	public const int TileHeight = 22;

	public override void Load() => TileEvents.OnRandomUpdate += Regrow;

	/// <summary> Causes Glowflower to regrow inside of underground oasis microbiomes. </summary>
	private static void Regrow(int i, int j, int type)
	{
		if (type == TileID.Sand && j > Main.worldSurface && Main.rand.NextBool(10) && WorldGen.InWorld(i, j - 1) && !Main.tile[i, j - 1].HasTile && Main.tile[i, j - 1].LiquidAmount < 100)
		{
			Point pt = new(i, j);
			int tileType = ModContent.TileType<Glowflower>();

			if (Placer.CanPlaceHerb(i, j, tileType) && UndergroundOasisBiome.OasisAreas.Any(x => x.Contains(pt)))
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

		TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);
		TileID.Sets.SwaysInWindBasic[Type] = true;
		TileHelperSets.TileGlowmask[Type] = Helpers.RequestGlowmask(this, static (i, j) =>
		{
			const float max_distance = 140;

			Point coords = new(i, j);
			float distance = Main.player[Player.FindClosest(coords.ToWorldCoordinates(0, 0), 16, 16)].DistanceSQ(coords.ToWorldCoordinates());

			return StargrassTile.GetGlowColor(coords.X, coords.Y) * MathHelper.Clamp(1f - distance / (max_distance * max_distance), 0.4f, 1f);
		});

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
		if (closer && !Main.gamePaused && Main.rand.NextBool(100))
		{
			Vector2 position = new Vector2(i, j).ToWorldCoordinates();
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

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool())
		{
			Vector2 position = new Vector2(i, j).ToWorldCoordinates();
			int whoAmI = NPC.NewNPC(new EntitySource_TileBreak(i, j), (int)position.X, (int)position.Y, NPCID.Firefly);

			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendData(MessageID.SyncNPC, number: whoAmI);
		}
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 2;

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) => (r, g, b) = (0.25f, 0.15f, 0.05f);
}