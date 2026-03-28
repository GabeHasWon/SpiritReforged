using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Desert.ScarabBoss.Boss;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;
using Terraria.GameInput;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items;

internal class SpaceHeater : ModItem
{
	public class SpaceHeaterPacketData : PacketData
	{
		private readonly int _id;
		private readonly float _value;
		private readonly int _from;

		public SpaceHeaterPacketData()
		{
		}

		public SpaceHeaterPacketData(int id, float value, int from)
		{
			_id = id;
			_value = value;
			_from = from;
		}

		public override void OnSend(ModPacket modPacket)
		{
			modPacket.Write((short)_id);
			modPacket.Write((Half)_value);

			if (Main.netMode != NetmodeID.Server)
				modPacket.Write((byte)_from);
		}

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			short id = reader.ReadInt16();
			float value = (float)reader.ReadHalf();

			if (TileEntity.ByID[id] is not SpaceHeaterEntity entity)
				return;

			entity.Strength = value;

			if (Main.netMode == NetmodeID.Server)
			{
				byte from = reader.ReadByte();
				Send(-1, from);
			}
		}
	}

	public sealed class SpaceHeaterEntity : ModTileEntity
	{
		/// <summary>
		/// Strength value, 0-1, for the heat shader.
		/// </summary>
		public float Strength = 0f;

		public override bool IsTileValidForEntity(int x, int y)
		{
			Tile tile = Main.tile[x, y];
			return tile.HasTile && tile.TileType == ModContent.TileType<SpaceHeaterTile>() && tile.TileFrameX == 0 && tile.TileFrameY == 0;
		}

		public override int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternate) => Generic_Hook_AfterPlacement(i, j, type, style, direction, alternate);

		public override void SaveData(TagCompound tag) => tag.Add("str", Strength);
		public override void LoadData(TagCompound tag) => Strength = tag.GetFloat("str");
	}

	public sealed class SpaceHeaterTile : ModTile
	{
		private bool LastFrameModified = false;

		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileObsidianKill[Type] = true;

			TileID.Sets.HasOutlines[Type] = true;
			TileID.Sets.DisableSmartCursor[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
			TileObjectData.newTile.Height = 4;
			TileObjectData.newTile.CoordinateHeights = [16, 16, 16, 18];
			TileObjectData.newTile.Origin = new Point16(0, 2);
			TileObjectData.newTile.LavaDeath = false;
			var tileEntity = ModContent.GetInstance<SpaceHeaterEntity>();
			TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(tileEntity.Hook_AfterPlacement, -1, 0, true);
			TileObjectData.addTile(Type);

			RegisterItemDrop(ModContent.ItemType<SpaceHeater>());
			AddMapEntry(new Color(135, 135, 136), Language.GetText("Mods.SpiritReforged.Items.SpaceHeater.DisplayName"));

			DustType = DustID.Iron;
		}

		public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

		public override void MouseOver(int i, int j)
		{
			Point16 topLeft = TileObjectData.TopLeft(i, j);
			Player player = Main.LocalPlayer;

			if (Main.mouseRight && TileEntity.ByPosition[topLeft] is SpaceHeaterEntity heater)
			{
				float factor = Utils.GetLerpValue(topLeft.Y * 16 + 8, (topLeft.Y + 4) * 16 - 16, Main.MouseWorld.Y, true);
				heater.Strength = 1 - factor;

				if (Main.netMode == NetmodeID.MultiplayerClient)
					SyncHeater(heater.ID, heater.Strength);

				Main.blockMouse = true;
				LastFrameModified = true;
			}
			else
			{
				if (LastFrameModified)
					SoundEngine.PlaySound(SoundID.MenuTick);

				LastFrameModified = false;

				player.noThrow = 2;
				player.cursorItemIconEnabled = true;
				player.cursorItemIconID = ModContent.ItemType<SpaceHeater>();
			}
		}

		public override bool RightClick(int i, int j)
		{
			Point16 topLeft = TileObjectData.TopLeft(i, j);
			TileObjectData data = TileObjectData.GetTileData(Main.tile[i, j]);

			if (TileEntity.ByPosition[topLeft] is not SpaceHeaterEntity heater)
				return false;

			bool smartContainsX = topLeft.X >= Main.SmartCursorX && Main.SmartCursorX < topLeft.X + data.Width;
			bool smartContainsY = topLeft.Y >= Main.SmartCursorY && Main.SmartCursorY < topLeft.Y + data.Height;

			if (smartContainsX && smartContainsY) // "Hijack" smart cursor functionality to move from 0% -> 25 -> 50 -> 75 -> 100 -> 0%
			{
				heater.Strength = heater.Strength switch
				{
					< 0.25f => 0.25f,
					< 0.5f => 0.5f,
					< 0.75f => 0.75f,
					< 1 => 1,
					_ => 0,
				};

				if (Main.netMode == NetmodeID.MultiplayerClient)
					SyncHeater(heater.ID, heater.Strength);

				SoundEngine.PlaySound(SoundID.MenuTick);
			}

			return true;
		}

		private static void SyncHeater(int id, float strength) => new SpaceHeaterPacketData(id, strength, Main.myPlayer).Send();

		public override void NearbyEffects(int i, int j, bool closer)
		{
			Point16 topLeft = TileObjectData.TopLeft(i, j);

			if (TileEntity.ByPosition[topLeft] is SpaceHeaterEntity heater)
				HeaterSystem.HeatEffect = heater.Strength;
		}

		public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
		{
			TileObjectData data = TileObjectData.GetTileData(Main.tile[i, j]);
			bool isBottomRight = TileObjectData.IsTopLeft(i - data.Width + 1, j - data.Height + 1);
			Point16 topLeft = TileObjectData.TopLeft(i, j);
			bool hideSlider = !TileExtensions.GetVisualInfo(i, j, out Color color, out Texture2D texture);

			if (!isBottomRight || TileEntity.ByPosition[topLeft] is not SpaceHeaterEntity heater)
				return;

			Vector2 position = TileExtensions.DrawPosition(i - data.Width + 1, j - data.Height + 1, new Vector2(-10, heater.Strength * 50 - 54));

			if (!hideSlider)
			{
				Rectangle button = new(0, 74, 12, 8);
				spriteBatch.Draw(texture, position, button, color);
			}

			if (heater.Strength > 0)
			{
				Rectangle glow = new(36, 0, 16, 16);

				for (int x = 0; x < data.Width; ++x)
				{
					for (int y = 0; y < data.Height; ++y)
					{
						if (TileExtensions.GetVisualInfo(topLeft.X + x, topLeft.Y + y, out Color tileColor, out Texture2D individualTileTexture))
						{
							glow.X = 36 + 18 * x;
							glow.Y = 0 + 18 * y;
							spriteBatch.Draw(individualTileTexture, TileExtensions.DrawPosition(i + x, j + y, new Vector2(16, 48)), glow, tileColor * heater.Strength);
						}
					}
				}
			}

			if (hideSlider)
				return;

			Point mouse = Main.MouseWorld.ToPoint();
			Rectangle tileBounds = new(topLeft.X * 16, topLeft.Y * 16, 16 * 2, 16 * 4);

			if (tileBounds.Contains(mouse))
				spriteBatch.Draw(texture, position - new Vector2(2, 2), new Rectangle(14, 72, 16, 12), color);
		}
	}

	public class HeaterSystem : ModSystem
	{
		public static float? HeatEffect = null;

		public override void ResetNearbyTileEffects() => HeatEffect = null;

		public override void PostUpdateDusts()
		{
			if (HeatEffect is { } value)
			{
				// Opacity goes from 0 -> 1 at value = 0 .. 0.5, then Intensity goes from 0 -> 1 at value = 0.5 .. 1
				ScarabHeatHazeShaderData.HeatHazeTargetIntensity = MathF.Max(0, (value - 0.5f) * 2f);
				ScarabHeatHazeShaderData.HeatHazeTargetOpacity = MathF.Min(value * 2, 1);
			}
		}
	}

	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<SpaceHeaterTile>());
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.Green;
	}
}
