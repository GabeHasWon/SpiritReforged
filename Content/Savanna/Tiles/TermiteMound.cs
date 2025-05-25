using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Savanna.Tiles;

public class TermiteMoundLarge : ModTile
{
	public override string Texture => base.Texture.Replace("Large", string.Empty);

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoFail[Type] = true;

		TileID.Sets.BreakableWhenPlacing[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
		TileObjectData.newTile.Height = 5;
		TileObjectData.newTile.CoordinateHeights = [16, 16, 16, 16, 16];
		TileObjectData.newTile.Origin = new(2, 4);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaGrass>(), ModContent.TileType<SavannaGrassCorrupt>(), ModContent.TileType<SavannaGrassCrimson>(), 
			ModContent.TileType<SavannaGrassHallow>(), ModContent.TileType<SavannaDirt>(), ModContent.TileType<SavannaGrassHallowMowed>(), ModContent.TileType<SavannaGrassMowed>()];
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(100, 92, 50));
		DustType = DustID.DesertPot;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		if (!Main.dedServ)
		{
			for (int x = 0; x < 3; x++)
				ParticleHandler.SpawnParticle(new Particles.SmokeCloud(new Vector2(i, j) * 16 + new Vector2(24, 70), Vector2.UnitY * -.5f, Color.SandyBrown, .2f, Common.Easing.EaseFunction.EaseQuarticInOut, 120));
		}

		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;

		for (int x = 0; x < 5; x++)
		{
			var position = new Vector2(i, j) * 16 + Main.rand.NextVector2FromRectangle(new Rectangle(0, 0, 3 * 16, 5 * 16));
			var velocity = (Vector2.UnitY * -Main.rand.NextFloat(6f)).RotatedByRandom(2f);

			var npc = NPC.NewNPCDirect(new EntitySource_TileBreak(i, j), position, ModContent.NPCType<NPCs.Termite.Termite>());
			npc.velocity = velocity;
			npc.netUpdate = true;
		}
	}

	public override bool KillSound(int i, int j, bool fail)
	{
		if (!fail)
			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Tile/StoneCrack2") { Pitch = .5f, PitchVariance = .4f }, new Vector2(i, j) * 16 + new Vector2(24));

		return true;
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		DoDraw(i, j, spriteBatch);
		return false;
	}

	/// <summary> Helper for drawing all termite mound variants. </summary>
	public static void DoDraw(int i, int j, SpriteBatch spriteBatch, int frameXOffset = 0, int frameYOffset = 0)
	{
		var tile = Framing.GetTileSafely(i, j);
		var texture = TextureAssets.Tile[tile.TileType].Value;

		var frame = new Point(tile.TileFrameX + 18 * frameXOffset, tile.TileFrameY + 18 * frameYOffset);
		var source = new Rectangle(frame.X, frame.Y, 16, 16);
		var position = new Vector2(i, j) * 16 - Main.screenPosition + TileExtensions.TileOffset + new Vector2(0, 2);

		spriteBatch.Draw(texture, position, source, Lighting.GetColor(i, j), 0, Vector2.Zero, 1, SpriteEffects.None, 0);
	}
}

public class TermiteMoundMedium : ModTile
{
	public override string Texture => base.Texture.Replace("Medium", string.Empty);

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoFail[Type] = true;

		TileID.Sets.BreakableWhenPlacing[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
		TileObjectData.newTile.Height = 4;
		TileObjectData.newTile.CoordinateHeights = [16, 16, 16, 16];
		TileObjectData.newTile.Origin = new(2, 3);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 3, 0);
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaGrass>(), ModContent.TileType<SavannaGrassCorrupt>(), ModContent.TileType<SavannaGrassCrimson>(),
			ModContent.TileType<SavannaGrassHallow>(), ModContent.TileType<SavannaDirt>(), ModContent.TileType<SavannaGrassHallowMowed>(), ModContent.TileType<SavannaGrassMowed>()];
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 2;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(100, 92, 50));
		DustType = DustID.DesertPot;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		if (!Main.dedServ)
		{
			for (int x = 0; x < 2; x++)
				ParticleHandler.SpawnParticle(new Particles.SmokeCloud(new Vector2(i, j) * 16 + new Vector2(24, 54), Vector2.UnitY * -.5f, Color.SandyBrown, .2f, Common.Easing.EaseFunction.EaseQuarticInOut, Main.rand.Next(80, 140)));
		}

		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;

		for (int x = 0; x < 3; x++)
		{
			var position = new Vector2(i, j) * 16 + Main.rand.NextVector2FromRectangle(new Rectangle(0, 0, 2 * 16, 4 * 16));
			var velocity = (Vector2.UnitY * -Main.rand.NextFloat(6f)).RotatedByRandom(2f);

			var npc = NPC.NewNPCDirect(new EntitySource_TileBreak(i, j), position, ModContent.NPCType<NPCs.Termite.Termite>());
			npc.velocity = velocity;
			npc.netUpdate = true;
		}
	}

	public override bool KillSound(int i, int j, bool fail)
	{
		if (!fail)
			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Tile/StoneCrack2") { Pitch = .5f, PitchVariance = .4f }, new Vector2(i, j) * 16 + new Vector2(24));

		return true;
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		TermiteMoundLarge.DoDraw(i, j, spriteBatch, 3, 1);
		return false;
	}
}

public class TermiteMoundSmall : ModTile
{
	public override string Texture => base.Texture.Replace("Small", string.Empty);

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = false;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoFail[Type] = true;

		TileID.Sets.BreakableWhenPlacing[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x1);
		TileObjectData.newTile.CoordinateHeights = [16];
		TileObjectData.newTile.Origin = new(1, 0);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, 2, 0);
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<SavannaGrass>(), ModContent.TileType<SavannaGrassCorrupt>(), ModContent.TileType<SavannaGrassCrimson>(),
			ModContent.TileType<SavannaGrassHallow>(), ModContent.TileType<SavannaDirt>(), ModContent.TileType<SavannaGrassHallowMowed>(), ModContent.TileType<SavannaGrassMowed>()];
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 3;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(100, 92, 50));
		DustType = DustID.DesertPot;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		if (!Main.dedServ)
			ParticleHandler.SpawnParticle(new Particles.SmokeCloud(new Vector2(i, j) * 16 + new Vector2(16, 16), Vector2.UnitY * -.25f, Color.SandyBrown, .12f, Common.Easing.EaseFunction.EaseQuarticInOut, Main.rand.Next(80, 140)));

		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;

		for (int x = 0; x < 2; x++)
		{
			var position = new Vector2(i, j) * 16 + Main.rand.NextVector2FromRectangle(new Rectangle(0, 0, 2 * 16, 1 * 16));
			var velocity = (Vector2.UnitY * -Main.rand.NextFloat(6f)).RotatedByRandom(2f);

			var npc = NPC.NewNPCDirect(new EntitySource_TileBreak(i, j), position, ModContent.NPCType<NPCs.Termite.Termite>());
			npc.velocity = velocity;
			npc.netUpdate = true;
		}
	}

	public override bool KillSound(int i, int j, bool fail)
	{
		if (!fail)
			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Tile/StoneCrack2") { Pitch = .5f, PitchVariance = .4f }, new Vector2(i, j) * 16 + new Vector2(18));

		return true;
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		TermiteMoundLarge.DoDraw(i, j, spriteBatch, 9, 4);
		return false;
	}
}
