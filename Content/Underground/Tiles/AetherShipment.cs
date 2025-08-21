using RubbleAutoloader;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.TileCommon.TileSway;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Pottery;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underground.Tiles;

public class AetherShipment : PotTile, ISwayTile, ILootable, ICutAttempt
{
	public override Dictionary<string, int[]> TileStyles => new() { { string.Empty, [0, 1, 2] } };

	private const int FullHeight = 36;
	private static Color GlowColor => Main.DiscoColor; //Color.Lerp(Color.Magenta, Color.CadetBlue, (float)(Math.Sin(Main.timeForVisualEffects / 40f) / 2f) + .5f);

	public override void AddRecord(int type, StyleDatabase.StyleGroup group)
	{
		var desc = Language.GetText(TileRecord.DescKey + ".Aether");
		RecordHandler.Records.Add(new TileRecord(group.name, type, group.styles).AddDescription(desc).AddRating(6));
	}

	public override void AddItemRecipes(ModItem modItem, StyleDatabase.StyleGroup group, Condition condition) => modItem.CreateRecipe()
		.AddRecipeGroup("ClayAndMud", 3).AddIngredient(ItemID.StoneBlock, 5).AddIngredient(ItemID.ShimmerTorch)
		.AddTile(ModContent.TileType<PotteryWheel>()).AddCondition(condition).Register();

	public override void AddObjectData()
	{
		Main.tileOreFinderPriority[Type] = 575;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.StyleWrapLimit = 3;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.UsesCustomCanPlace = true;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		DustType = DustID.ShimmerTorch;
		AnimationFrameHeight = FullHeight;
	}

	public override void AddMapData() => AddMapEntry(new Color(225, 174, 252), Language.GetText("Mods.SpiritReforged.Items.AetherShipmentItem.DisplayName"));

	public override void NearbyEffects(int i, int j, bool closer)
	{
		const int distance = 200;

		if (!closer || Main.gamePaused || !TileObjectData.IsTopLeft(i, j) || Autoloader.IsRubble(Type))
			return;

		var world = new Vector2(i, j) * 16;
		float strength = Main.LocalPlayer.DistanceSQ(world) / (distance * distance);

		if (strength < 1 && Main.rand.NextFloat(16f) < 1f - strength)
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
		if (effectOnly || !fail || Autoloader.IsRubble(Type) || WorldMethods.Generating)
			return;

		fail = AdjustFrame(i, j);
		ISwayTile.SetInstancedRotation(i, j, Main.rand.NextFloat(-1f, 1f) * 4f, fail);
	}

	public override void AnimateTile(ref int frame, ref int frameCounter)
	{
		if (++frameCounter >= 6)
		{
			frameCounter = 0;
			frame = ++frame % 8;
		}
	}

	public bool OnCutAttempt(int i, int j)
	{
		if (ClubQuickKill(i, j))
			return true;

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
			SoundEngine.PlaySound(BiomePots.Break with { Volume = 0.12f, PitchRange = (0.4f, 0.8f), }, pos);
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
		const int fullWidth = FullHeight;

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
		var source = new Rectangle(tile.TileFrameX, tile.TileFrameY + Main.tileFrame[Type] * FullHeight, data.CoordinateWidth, data.CoordinateHeights[tile.TileFrameY / 18]);
		var dataOffset = new Vector2(data.DrawXOffset, data.DrawYOffset);

        var color = Lighting.GetColor(i, j);

        if (Main.LocalPlayer.findTreasure)
            color = TileExtensions.GetSpelunkerTint(color);

        spriteBatch.Draw(TextureAssets.Tile[tile.TileType].Value, drawPos + origin + dataOffset, source, color, rotation, origin, 1, SpriteEffects.None, 0);

		if (tile.TileFrameX % 36 == 18 && tile.TileFrameY % 36 == 18) //Bottom right frame
		{
			var bloom = TextureAssets.Extra[60].Value;

			float value = Main.LocalPlayer.DistanceSQ(new Vector2(i, j) * 16) / (200 * 200);
			Color glow = GlowColor.Additive() * (1f - value) * .5f;

			spriteBatch.Draw(bloom, drawPos + new Vector2(0, 16), null, glow, rotation, bloom.Size() / 2, new Vector2(1, .5f) * .4f, SpriteEffects.None, 0);
		}
	}

	public void AddLoot(ILoot loot)
	{
		loot.AddOneFromOptions(1, ItemID.AegisCrystal, ItemID.ArcaneCrystal, ItemID.AegisFruit, ItemID.Ambrosia, ItemID.GummyWorm, ItemID.GalaxyPearl);

		if (Main.expertMode || Main.masterMode)
			loot.AddCommon(ItemID.ShimmerTorch, 1, 5, 18);
		else
			loot.AddCommon(ItemID.ShimmerTorch, 1, 4, 14);

		loot.AddCommon(ItemID.ShimmerArrow, 1, 10, 20);

		if (CrossMod.Thorium.Enabled)
		{
			if (CrossMod.Thorium.TryFind("InspirationGem", out ModItem inspirationGem) && CrossMod.Thorium.TryFind("AstralWave", out ModItem astralWave))
				loot.AddOneFromOptions(1, inspirationGem.Type, astralWave.Type);
		}
	}
}