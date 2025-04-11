using RubbleAutoloader;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.TileCommon.TileSway;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Pottery;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underground.Tiles;

public class AetherShipment : PotTile, ISwayTile, ILootTile, ICutAttempt
{
	public override Dictionary<string, int[]> TileStyles => new() { { string.Empty, [0, 1, 2] } };

	private static Color GlowColor => Color.Lerp(Color.Magenta, Color.CadetBlue, (float)(Math.Sin(Main.timeForVisualEffects / 40f) / 2f) + .5f);
	public override void AddRecord(int type, StyleDatabase.StyleGroup group)
	{
		var desc = Language.GetText("Mods.SpiritReforged.Tiles.Records.Aether");
		RecordHandler.Records.Add(new TileRecord(group.name, type, group.styles).AddDescription(desc).AddRating(5));
	}

	public override void AddObjectData()
	{
		Main.tileCut[Type] = !Autoloader.IsRubble(Type);

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.StyleWrapLimit = 3;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.UsesCustomCanPlace = true;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		DustType = DustID.ShimmerTorch;
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		const int distance = 200;

		if (!closer || Main.gamePaused || !TileObjectData.IsTopLeft(i, j) || Autoloader.IsRubble(Type))
			return;

		var world = new Vector2(i, j) * 16;
		float strength = Main.LocalPlayer.DistanceSQ(world) / (distance * distance);

		if (strength < 1 && Main.rand.NextFloat(10f) < 1f - strength)
		{
			var spawn = Main.rand.NextVector2FromRectangle(new Rectangle(i * 16, (j + 2) * 16, 32, 2));
			float scale = Main.rand.NextFloat(.5f, 1.25f);
			var velocity = -Vector2.UnitY * Main.rand.NextFloat();
			float rotation = Main.rand.NextFloat();

			ParticleHandler.SpawnParticle(new ShimmerStar(spawn, GlowColor * (1f - strength), scale, 60, velocity) { Rotation = rotation });
			ParticleHandler.SpawnParticle(new ShimmerStar(spawn, Color.White * (1f - strength), scale * .8f, 60, velocity) { Rotation = rotation });
		}
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (effectOnly || !fail || Autoloader.IsRubble(Type))
			return;

		fail = AdjustFrame(i, j);
		ISwayTile.SetInstancedRotation(i, j, Main.rand.NextFloat(-1f, 1f) * 4f, fail);
	}

	public bool OnCutAttempt(int i, int j)
	{
		bool fail = AdjustFrame(i, j);
		ISwayTile.SetInstancedRotation(i, j, Main.rand.NextFloat(-1f, 1f) * 4f, fail);

		var cache = Main.tile[i, j];
		WorldGen.KillTile_MakeTileDust(i, j, cache);
		WorldGen.KillTile_PlaySounds(i, j, true, cache);

		return !fail;
	}

	public override bool KillSound(int i, int j, bool fail)
	{
		if (Autoloader.IsRubble(Type))
			return true;

		var pos = new Vector2(i, j).ToWorldCoordinates(16, 16);

		if (!fail)
		{
			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Tile/PotBreak") with { Volume = .12f, PitchRange = (.4f, .8f), }, pos);
		}
		else
		{
			int phase = Main.tile[i, j].TileFrameX / 36;
			SoundEngine.PlaySound(SoundID.Shatter with { Pitch = phase / 3f }, pos);
		}

		return false;
	}

	private static bool AdjustFrame(int i, int j)
	{
		const int fullWidth = 36;

		TileExtensions.GetTopLeft(ref i, ref j);

		if (Main.tile[i, j].TileFrameX > fullWidth)
			return false; //Frame has already been adjusted

		for (int x = i; x < i + 2; x++)
		{
			for (int y = j; y < j + 2; y++)
			{
				var t = Main.tile[x, y];
				t.TileFrameX += fullWidth;
			}
		}

		return true;
	}

	public override void DeathEffects(int i, int j, int frameX, int frameY)
	{
		var source = new EntitySource_TileBreak(i, j);

		for (int g = 1; g < 7; g++)
		{
			int goreType = Mod.Find<ModGore>("Aether" + g).Type;
			Gore.NewGore(source, new Vector2(i, j) * 16, Vector2.Zero, goreType);
		}
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (TileObjectData.IsTopLeft(i, j))
		{
			GlowTileHandler.AddGlowPoint(new Rectangle(i, j + 1, 32, 16), GlowColor, 200);
			Lighting.AddLight(new Vector2(i, j).ToWorldCoordinates(16, 16), new Vector3(.1f, .075f, .1f));
		}
	}

	public void DrawSway(int i, int j, SpriteBatch spriteBatch, Vector2 offset, float rotation, Vector2 origin)
	{
		var tile = Main.tile[i, j];
		var data = TileObjectData.GetTileData(tile);

		var drawPos = new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y);
		var source = new Rectangle(tile.TileFrameX, tile.TileFrameY, data.CoordinateWidth, data.CoordinateHeights[tile.TileFrameY / 18]);
		var dataOffset = new Vector2(data.DrawXOffset, data.DrawYOffset);

		spriteBatch.Draw(TextureAssets.Tile[tile.TileType].Value, drawPos + origin + dataOffset,
			source, Lighting.GetColor(i, j), rotation, origin, 1, SpriteEffects.None, 0);
	}

	public LootTable AddLoot(int objectStyle)
	{
		var loot = new LootTable();
		loot.AddOneFromOptions(1, ItemID.AegisCrystal, ItemID.ArcaneCrystal, ItemID.AegisFruit, ItemID.Ambrosia, ItemID.GummyWorm, ItemID.GalaxyPearl);

		if (Main.expertMode || Main.masterMode)
			loot.AddCommon(ItemID.ShimmerTorch, 1, 5, 18);
		else
			loot.AddCommon(ItemID.ShimmerTorch, 1, 4, 14);

		loot.AddCommon(ItemID.ShimmerArrow, 1, 10, 20);

		return loot;
	}
}