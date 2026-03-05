using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.SimpleEntity;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileSway;
using SpiritReforged.Common.WorldGeneration;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.Utilities;

namespace SpiritReforged.Content.Savanna.Tiles;

public class HangingBaobabFruit : ModTile, ISwayTile
{
	/// <summary> Grows a baobab vine from the given coordinates including one-time framing logic. </summary>
	internal static void GrowVine(int i, int j, int length = 1)
	{
		int maxLength = 3 + (int)(i / 1.5f % 4); //Variable max lengths based on x position
		int type = ModContent.TileType<HangingBaobabFruit>();

		if (!WorldMethods.AreaClear(i, j, 1, 3))
			return; //Check for adequate space

		for (int l = 0; l < length; l++)
		{
			if (Placer.GrowVine(i, j, type, maxLength, sync: false))
			{
				int y = 0;
				while (Main.tile[i, j + y].TileType == type)
				{
					ModContent.GetInstance<HangingBaobabFruit>().ResetFrame(i, j + y);
					y++;
				}
			}
		}

		if (Main.netMode != NetmodeID.SinglePlayer)
			NetMessage.SendTileSquare(-1, i, j, 1, length);
	}

	public int Style => (int)TileDrawing.TileCounterType.Vine;

	public override void SetStaticDefaults()
	{
		Main.tileBlockLight[Type] = true;
		Main.tileCut[Type] = true;
		Main.tileNoFail[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileID.Sets.VineThreads[Type] = true;
		TileID.Sets.ReplaceTileBreakDown[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
		TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile | AnchorType.AlternateTile, 1, 0);
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<LivingBaobabLeaf>()];
		TileObjectData.newTile.AnchorAlternateTiles = [Type];
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 2;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(140, 140, 100));
		DustType = DustID.t_PearlWood;
		HitSound = SoundID.Grass;
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;

	public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
	{
		var above = Main.tile[i, j - 1];

		if (above.TileType != ModContent.TileType<LivingBaobabLeaf>() && above.TileType != Type)
		{
			WorldGen.KillTile(i, j);
			return false;
		}

		if (resetFrame)
			ResetFrame(i, j);

		return false; //True results in the tile being invisible in most cases
	}

	private void ResetFrame(int i, int j)
	{
		for (int x = 0; x < 2; x++)
			Main.tile[i, j].TileFrameY = (short)(Main.tile[i, j + 1].TileType == Type ? 0 : 18);
	}

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		var tile = Main.tile[i, j];

		if (tile.TileFrameY == 18) //This is a fruit frame
		{
			var position = new Vector2(i, j) * 16 + new Vector2(8);

			PreNewProjectile.New(new EntitySource_TileBreak(i, j), position, Vector2.Zero, ModContent.ProjectileType<FallingBaobabFruit>(), 10, 0f, ai0: tile.TileFrameX / 18, preSpawnAction: delegate (Projectile p)
			{
				(p.ModProjectile as FallingBaobabFruit).PickDrop();
			}).netUpdate = true;
		}
	}
}

public class FallingBaobabFruit : ModProjectile
{
	/// <summary> Determines our frame in PreDraw. </summary>
	public int Style { get => (int)Projectile.ai[0]; set => Projectile.ai[0] = value; }
	public override string Texture => base.Texture.Replace("Falling", "Hanging");

	private enum DropType : byte
	{
		Fruit,
		Acorn,
		Worm
	}
	private DropType _drop;

	public override void SetDefaults()
	{
		Projectile.Size = new Vector2(10);
		Projectile.friendly = Projectile.hostile = true;
	}

	/// <summary> Selects an item drop for this fruit. </summary>
	public void PickDrop()
	{
		var dropType = new WeightedRandom<DropType>();
		dropType.Add(DropType.Fruit, 1f);
		dropType.Add(DropType.Acorn, .9f);
		dropType.Add(DropType.Worm, .001f);

		_drop = (DropType)dropType;
	}

	public override void AI()
	{
		Projectile.rotation += Projectile.velocity.Length() * .08f;
		Projectile.velocity.Y += .5f;
	}

	public override void OnKill(int timeLeft)
	{
		switch (_drop)
		{
			case DropType.Fruit:
				ItemMethods.NewItemSynced(Projectile.GetSource_Death(), ModContent.ItemType<Items.Food.BaobabFruit>(), Projectile.Center, true);
				break;

			case DropType.Acorn:
				ItemMethods.NewItemSynced(Projectile.GetSource_Death(), ItemID.Acorn, Projectile.Center, true);
				break;

			case DropType.Worm:
				SimpleEntitySystem.NewEntity(SimpleEntitySystem.Types[typeof(NPCs.DevourerOfSoil)], Projectile.Center + new Vector2(0, 16));
				break;
		}

		if (!Main.dedServ)
		{
			SoundEngine.PlaySound(SoundID.NPCHit1, Projectile.Center);
			if (_drop != DropType.Fruit)
			{
				SoundEngine.PlaySound(SoundID.NPCDeath1 with { Pitch = 1f }, Projectile.Center);
				for (int i = 0; i < 20; i++)
				{
					var velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f);
					Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.SnowBlock).velocity = velocity;
				}

				for (int i = 1; i < 3; i++)
					Gore.NewGore(Projectile.GetSource_Death(), Projectile.Center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(),
						Mod.Find<ModGore>("BaobabFruit" + i).Type);
			} //Break open

			if (_drop == DropType.Worm)
				SoundEngine.PlaySound(SoundID.Roar, Projectile.Center);
		}
	}

	public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
	{
		fallThrough = false;
		return true;
	}

	public override bool? CanCutTiles() => false;
	public override void SendExtraAI(BinaryWriter writer) => writer.Write((byte)_drop);
	public override void ReceiveExtraAI(BinaryReader reader) => _drop = (DropType)reader.Read();

	public override bool PreDraw(ref Color lightColor)
	{
		var texture = TextureAssets.Projectile[Type].Value;
		var frame = texture.Frame(2, 2, Style, 1, -2, -2);

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), frame,
			Projectile.GetAlpha(lightColor), Projectile.rotation, frame.Size() / 2, Projectile.scale, SpriteEffects.None);

		return false;
	}
}