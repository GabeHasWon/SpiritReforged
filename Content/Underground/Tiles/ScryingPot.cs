using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.UI.PotCatalogue;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Forest.Cartography.Maps;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underground.Tiles;

public class ScryingPot : PotTile, ILootable
{
	public override Dictionary<string, int[]> TileStyles => new() { { string.Empty, [0] } };

	public override TileRecord AddRecord(int type, NamedStyles.StyleGroup group)
	{
		var record = new TileRecord(group.name, type, group.styles);
		return record.AddRating(3).AddDescription(Language.GetText(TileRecord.DescKey + ".Scrying"));
	}

	public override void AddObjectData()
	{
		const int row = 1;

		Main.tileOreFinderPriority[Type] = 575;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Origin = new(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.StyleWrapLimit = row;
		TileObjectData.newTile.RandomStyleRange = row;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.UsesCustomCanPlace = true;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);

		DustType = IsRubble ? -1 : DustID.Pot;
	}

	public override void AddMapData() => AddMapEntry(new Color(146, 76, 77), Language.GetText("Mods.SpiritReforged.Items.ScryingPotItem.DisplayName"));

	public override bool KillSound(int i, int j, bool fail)
	{
		if (!fail)
		{
			var pos = new Vector2(i, j).ToWorldCoordinates(16, 16);

			SoundEngine.PlaySound(SoundID.Shatter, pos);
			SoundEngine.PlaySound(BiomePots.Break, pos);

			return false;
		}

		return true;
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		if (WorldMethods.Generating || IsRubble)
			return;

		var spawn = new Vector2(i, j).ToWorldCoordinates(16, 16);
		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			ItemMethods.SplitCoins(Main.rand.Next(6000, 9000), delegate (int type, int stack)
			{
				Item.NewItem(new EntitySource_TileBreak(i, j), spawn, new Item(type, stack), noGrabDelay: true);
			});

			TileLootSystem.Resolve(i, j, Type, frameX, frameY);
		}

		if (!Main.dedServ)
		{
			int pWhoAmI = Player.FindClosest(new Vector2(i, j) * 16, 32, 32);

			if (Main.myPlayer == pWhoAmI)
				TornMapPiece.LightMap(i, j, 280, out _, .5f); //Only reveal the map for the nearest player

			ParticleHandler.SpawnParticle(new TexturedPulseCircle(spawn, Color.MediumPurple * .15f, .25f, 400, 20, "supPerlin", Vector2.One, Common.Easing.EaseFunction.EaseQuadOut));
			SoundEngine.PlaySound(SoundID.NPCDeath6 with { Pitch = .5f }, spawn);

			for (int x = 0; x < 12; x++)
			{
				var newSpawn = spawn + Main.rand.NextVector2Unit() * Main.rand.NextFloat(20);
				int time = Main.rand.Next(20, 50);
				float speed = Main.rand.NextFloat(4f);

				ParticleHandler.SpawnParticle(new GlowParticle(newSpawn, spawn.DirectionTo(newSpawn) * speed, Color.Purple, .5f, time));
				ParticleHandler.SpawnParticle(new GlowParticle(newSpawn, spawn.DirectionTo(newSpawn) * speed, Color.White, .2f, time));
			}

			for (int x = 51; x < 54; x++)
				Gore.NewGore(new EntitySource_TileBreak(i, j), new Vector2(i, j) * 16, Vector2.Zero, x);
		}
	}

	public void AddLoot(ILoot loot) => loot.AddOneFromOptions(1, ItemID.NightOwlPotion, ItemID.ShinePotion, ItemID.BiomeSightPotion, ItemID.TrapsightPotion, ItemID.HunterPotion, ItemID.SpelunkerPotion);

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (TileObjectData.IsTopLeft(i, j))
			GlowTileHandler.AddGlowPoint(new Rectangle(i, j + 1, 32, 16), Color.Magenta * .5f, 200);

		Lighting.AddLight(new Vector2(i, j).ToWorldCoordinates(), Color.Purple.ToVector3() * .5f);
		return true;
	}
}