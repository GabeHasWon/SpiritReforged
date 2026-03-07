using SpiritReforged.Common.Misc;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.Visuals;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items;

public sealed class ScarabRadio : ModItem
{
	public sealed class ScarabRadioTile : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLavaDeath[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
			TileObjectData.newTile.Origin = new(0, 1);
			TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
			TileObjectData.newTile.DrawYOffset = 2;
			TileObjectData.newTile.StyleHorizontal = true;

			TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
			TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
			TileObjectData.addAlternate(1);
			TileObjectData.addTile(Type);

			DustType = -1;
			AddMapEntry(FurnitureTile.CommonColor, CreateMapEntryName());
			RegisterItemDrop(ModContent.ItemType<ScarabRadio>());
		}

		public override void NearbyEffects(int i, int j, bool closer)
		{
			if (!closer && !Main.dedServ)
			{
				Tile tile = Main.tile[i, j];
				if (tile.TileFrameX % 36 == 0 && tile.TileFrameY == 0)
					ChooseMusic.SetMusic(MusicSlot);
			}
		}

		public override void EmitParticles(int i, int j, Tile tile, short tileFrameX, short tileFrameY, Color tileLight, bool visible)
		{
			if (visible && tile.TileFrameX % 36 == 0 && tile.TileFrameY == 0 && MusicBoxTile.SpawnNote)
				MusicBoxTile.SpawnMusicNote(i, j);
		}

		public override void MouseOver(int i, int j)
		{
			Player Player = Main.LocalPlayer;
			Player.noThrow = 2;
			Player.cursorItemIconEnabled = true;
			Player.cursorItemIconID = ModContent.ItemType<ScarabRadio>();
		}

		public override bool RightClick(int i, int j)
		{
			HitWire(i, j);
			return true;
		}

		public override void HitWire(int i, int j)
		{
			const int height = 36;
			TileExtensions.GetTopLeft(ref i, ref j);

			for (int y = 0; y < 2; y++)
			{
				for (int x = 0; x < 2; x++)
				{
					Tile tile = Framing.GetTileSafely(i + x, j + y);
					tile.TileFrameY += (short)((tile.TileFrameY < height) ? height : -height);

					Wiring.SkipWire(i + x, j + y);
				}
			}

			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendTileSquare(-1, i, j, 2, 2);

			if (!Main.dedServ)
				SoundEngine.PlaySound(SoundID.Mech);
		}
	}

	public static int MusicSlot { get; private set; }
	private static readonly Asset<Texture2D> HeldTexture = DrawHelpers.RequestLocal<ScarabRadio>("ScarabRadioHeld", false);

	public override void Load() => On_PlayerDrawLayers.DrawPlayer_27_HeldItem += DrawHeldItem;

	private static void DrawHeldItem(On_PlayerDrawLayers.orig_DrawPlayer_27_HeldItem orig, ref PlayerDrawSet drawinfo)
	{
		int heldType = drawinfo.drawPlayer.HeldItem.type;
		if (heldType == ModContent.ItemType<ScarabRadio>())
		{
			Texture2D texture = HeldTexture.Value;
			Rectangle source = texture.Frame(1, 3, 0, (int)(Main.timeForVisualEffects / 8f) % 3, 0, -2);
			Vector2 origin = source.Size() / 2;

			float sine = (float)Math.Min(Math.Sin(Main.timeForVisualEffects / 5f), 0);
			Vector2 offset = new(4 * drawinfo.drawPlayer.direction, -22 + sine * 10);

			Vector2 location = (drawinfo.drawPlayer.Center - Main.screenPosition + offset + new Vector2(0, drawinfo.drawPlayer.gfxOffY)).Floor();
			Color color = drawinfo.drawPlayer.HeldItem.GetAlpha(Lighting.GetColor(drawinfo.ItemLocation.ToTileCoordinates()));
			float rotation = drawinfo.drawPlayer.itemRotation + (float)Math.Sin(Main.timeForVisualEffects / 10f) * sine * 0.3f + MathHelper.PiOver4 * drawinfo.drawPlayer.direction;

			drawinfo.DrawDataCache.Add(new DrawData(texture, location, source, color, rotation, origin, 1, drawinfo.itemEffect));
			return; //Skips orig
		}

		orig(ref drawinfo);
	}

	public override void SetStaticDefaults() => MusicSlot = MusicLoader.GetMusicSlot(Mod, "Assets/Music/RadioScarab");

	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<ScarabRadioTile>());
		Item.width = Item.height = 14;
		Item.maxStack = 1;
		Item.holdStyle = ItemHoldStyleID.HoldUp;
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.Orange;
	}

	public override void HoldItem(Player player)
	{
		if (!Main.dedServ && Main.LocalPlayer.DistanceSQ(player.Center) < 250 * 250)
		{
			ChooseMusic.SetMusic(MusicSlot);

			if (MusicBoxTile.SpawnNote)
			{
				var gore = Gore.NewGoreDirect(player.GetSource_FromThis("HeldItem"), player.Top - new Vector2(0, 20), new Vector2(0, -0.5f), Main.rand.Next(570, 573), 0.8f);
				gore.position.X -= gore.Width / 2;
			}
		}
	}
}