using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.Visuals.Glowmasks;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items;

public sealed class ScarabMusicBox : ModItem
{
	[AutoloadGlowmask("255,255,255")]
	public sealed class ScarabMusicBoxTile : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileObsidianKill[Type] = true;

			TileID.Sets.HasOutlines[Type] = true;
			TileID.Sets.DisableSmartCursor[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
			TileObjectData.newTile.Origin = new Point16(0, 1);
			TileObjectData.newTile.LavaDeath = false;
			TileObjectData.newTile.DrawYOffset = 2;
			TileObjectData.addTile(Type);

			RegisterItemDrop(ModContent.ItemType<ScarabRadio>()); //Register this drop for all styles
			AddMapEntry(new Color(191, 142, 111), Language.GetText("ItemName.MusicBox"));
			DustType = -1;
		}

		public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

		public override bool RightClick(int i, int j)
		{
			HitWire(i, j);
			return true;
		}

		public override void HitWire(int i, int j)
		{
			var data = TileObjectData.GetTileData(Type, 0);
			int width = data.CoordinateFullWidth;
			float pitch = 0;

			TileExtensions.GetTopLeft(ref i, ref j);

			for (int y = 0; y < data.Width; y++)
			{
				for (int x = 0; x < data.Height; x++)
				{
					Tile tile = Framing.GetTileSafely(i + x, j + y);
					tile.TileFrameX += (short)((tile.TileFrameX >= width * 2) ? -(width * 2) : width);

					Wiring.SkipWire(i + x, j + y);
					pitch = tile.TileFrameX / 72f;
				}
			}

			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendTileSquare(-1, i, j, data.Width, data.Height);

			SoundEngine.PlaySound(SoundID.Mech with { Pitch = pitch }, new Vector2(i, j).ToWorldCoordinates());
		}

		public override void EmitParticles(int i, int j, Tile tile, short tileFrameX, short tileFrameY, Color tileLight, bool visible)
		{
			if (visible && (tile.TileFrameX == 36 || tile.TileFrameX == 72) && tile.TileFrameY == 0 && MusicBoxTile.SpawnNote)
				MusicBoxTile.SpawnMusicNote(i, j);
		}

		public override void NearbyEffects(int i, int j, bool closer)
		{
			if (!closer)
				return;

			Tile tile = Main.tile[i, j];
			if (tile.TileFrameX >= 36)
			{
				Main.SceneMetrics.ActiveMusicBox = (tile.TileFrameX >= 72) ? MusicSlotPhase2 : MusicSlot;

				if (tile.TileFrameY == 0 && Main.rand.NextFloat() < ((tile.TileFrameX >= 72) ? 0.3f : 0.1f))
				{
					var dust = Dust.NewDustDirect(new Vector2(i, j) * 16, 16, 16, DustID.Torch);
					dust.noGravity = true;
					dust.velocity = -Vector2.UnitY;
					dust.fadeIn = 1.1f;
				}
			}
		}

		public override void MouseOver(int i, int j)
		{
			Player player = Main.LocalPlayer;
			player.noThrow = 2;
			player.cursorItemIconEnabled = true;
			player.cursorItemIconID = ModContent.ItemType<ScarabMusicBox>();
		}
	}

	public static int MusicSlot { get; private set; }
	public static int MusicSlotPhase2 { get; private set; }

	public override void SetStaticDefaults()
	{
		MusicSlot = MusicLoader.GetMusicSlot(Mod, "Assets/Music/Scarabeus");
		MusicSlotPhase2 = MusicLoader.GetMusicSlot(Mod, "Assets/Music/ScarabeusP2");

		ItemID.Sets.CanGetPrefixes[Type] = false;
		ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<ScarabRadio>();
	}

	public override void SetDefaults() => Item.DefaultToMusicBox(ModContent.TileType<ScarabMusicBoxTile>());
}