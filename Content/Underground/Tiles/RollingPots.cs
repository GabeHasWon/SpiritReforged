using RubbleAutoloader;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Underground.Pottery;
using Terraria.Audio;
using Terraria.DataStructures;
using static SpiritReforged.Common.TileCommon.StyleDatabase;

namespace SpiritReforged.Content.Underground.Tiles;

public class RollingPots : PotTile, ILootTile
{
	public override Dictionary<string, int[]> TileStyles => new() { { string.Empty, [0, 1] } };

	public override void AddRecord(int type, StyleGroup group)
	{
		var desc = Language.GetText(TileRecord.DescKey + ".Boulder");
		RecordHandler.Records.Add(new TileRecord(group.name, type, group.styles).AddDescription(desc).AddRating(3));
	}

	public override void AddObjectData()
	{
		bool rubble = Autoloader.IsRubble(Type);

		Main.tileOreFinderPriority[Type] = 575;
		Main.tileNoFail[Type] = !rubble;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);

		if (rubble)
			TileObjectData.newTile.RandomStyleRange = 2;
		else
			HitSound = null;

		TileObjectData.newTile.Origin = new(0, 1);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.StyleWrapLimit = 2;
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.UsesCustomCanPlace = true;
		TileObjectData.newTile.DrawYOffset = 2;
		TileObjectData.addTile(Type);
	}

	public override void AddMapData() => AddMapEntry(new Color(180, 90, 95), Language.GetText("Mods.SpiritReforged.Items.RollingPotsItem.DisplayName"));

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		if (Autoloader.IsRubble(Type) || WorldMethods.Generating || Main.netMode == NetmodeID.MultiplayerClient)
			return;

		int style = frameX / 32;
		int damage = (style == 1) ? 80 : 3;

		Projectile.NewProjectile(new EntitySource_TileBreak(i, j), new Vector2(i, j).ToWorldCoordinates(16, 16), Vector2.Zero, ModContent.ProjectileType<PotBoulder>(), damage, 5, ai0: style);
	}

	public void AddLoot(ILootTile.Context context, ILoot loot) => RecordHandler.InvokeLootPool(ModContent.TileType<Pots>(), context, loot);
}

internal class PotBoulder : ModProjectile
{
	public ref float Style => ref Projectile.ai[1];

	public override LocalizedText DisplayName => Language.GetText(Pots.NameKey);
	public override string Texture => ModContent.GetInstance<RollingPots>().Texture;

	private bool _landed;

	public override void SetDefaults() => Projectile.CloneDefaults(ProjectileID.Boulder);
	public override void OnKill(int timeLeft)
	{
		if (!Main.dedServ)
		{
			for (int d = 0; d < 15; d++)
				Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Pot);

			for (int g = 1; g < 5; g++)
			{
				int goreType = Mod.Find<ModGore>("Rolling" + g).Type;
				Gore.NewGore(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero, goreType);
			}

			SoundEngine.PlaySound(BiomePots.Break, Projectile.Center);
			SoundEngine.PlaySound(SoundID.Shatter, Projectile.Center);
		}

		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			ItemMethods.SplitCoins(Main.rand.Next(30000, 50000), delegate (int type, int stack)
			{
				Item.NewItem(Projectile.GetSource_Death(), Projectile.Center, new Item(type, stack), noGrabDelay: true);
			});

			var table = new LootTable();
			ModContent.GetInstance<RollingPots>().AddLoot(new((int)Style, Projectile.Center.ToTileCoordinates16()), table);
			table.Resolve(Projectile.getRect(), Main.player[Player.FindClosest(Projectile.position, Projectile.width, Projectile.height)]);

			if (Main.rand.NextBool(50))
				Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.UnitY * -4f, ProjectileID.CoinPortal, 0, 0);
		}
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		if (Projectile.velocity.Y == 0 && Projectile.velocity.X == 0)
		{
			bool value = _landed;
			if (!_landed)
			{
				_landed = true;
				Projectile.Bounce(oldVelocity, 0.5f);
			}

			return value;
		}

		if (oldVelocity.Y > 2)
			SoundEngine.PlaySound(SoundID.Dig with { Pitch = 0.2f }, Projectile.Center);

		return false;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		FallingPot.Unify4x4Sheet(Projectile.Center, Projectile.GetAlpha(lightColor), ModContent.TileType<RollingPots>(), new Point(4, 2), (int)Style, Projectile.rotation, Projectile.scale);
		return false;
	}
}