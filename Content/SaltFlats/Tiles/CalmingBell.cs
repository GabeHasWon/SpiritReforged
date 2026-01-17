using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.TileSway;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;

namespace SpiritReforged.Content.SaltFlats.Tiles;

[AutoloadGlowmask("255,255,255")]
public class CalmingBell : ModTile, ISwayTile, IAutoloadTileItem
{
	/// <summary> Used to sync <see cref="CalmingBell"/> use effects by client-side methods. Should only be recieved by multiplayer clients. </summary>
	internal class BellUseData : PacketData
	{
		private readonly Point16 _coordinates;

		public BellUseData() { }
		public BellUseData(Point16 coordinates) => _coordinates = coordinates;

		public override void OnReceive(BinaryReader reader, int whoAmI)
		{
			Point16 coordinates = reader.ReadPoint16();
			OnUse(coordinates.X, coordinates.Y);
		}

		public override void OnSend(ModPacket modPacket) => modPacket.WritePoint16(_coordinates);
	}

	/// <summary> Gets opacity by local player distance. </summary>
	public static float GetOpacity(int x, int y)
	{
		float value = Math.Clamp(1f - Main.LocalPlayer.DistanceSQ(new Vector2(x, y).ToWorldCoordinates()) / (100f * 100f), 0, 1);
		return value * ((1.2f - Lighting.Brightness(x, y)) / 1.2f);
	}

	public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;
        Main.tileLavaDeath[Type] = false;
        Main.tileNoFail[Type] = true;

		TileID.Sets.HasOutlines[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
        TileObjectData.newTile.CoordinateHeights = new[] { 24 };
        TileObjectData.newTile.CoordinateWidth = 22;
        TileObjectData.newTile.Origin = new Point16(0, 0);
        TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
        TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
        TileObjectData.addTile(Type);

        DustType = DustID.Copper;
        AddMapEntry(new Color(80, 80, 80));
    }

    public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;
	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

    public override bool RightClick(int i, int j)
    {
		if (Main.netMode == NetmodeID.MultiplayerClient)
			new BellUseData(new(i, j)).Send();

		OnUse(i, j);
        return true;
    }

	public override void HitWire(int i, int j)
	{
		if (Main.netMode == NetmodeID.Server)
			new BellUseData(new(i, j)).Send();
		else
			OnUse(i, j);
	}

	public static void OnUse(int i, int j)
	{
		Main.LocalPlayer.AddBuff(BuffID.Calm, 60 * 60 * 3);

		if (!Main.dedServ)
		{
			TileSwayHelper.SetWindTime(i, j, Vector2.UnitX * 5);

			Vector2 worldPos = new Vector2(i, j).ToWorldCoordinates();

			SoundEngine.PlaySound(SoundID.Shatter with { Pitch = -0.5f }, worldPos);
			SoundEngine.PlaySound(SoundID.GuitarEm with { Pitch = -0.5f }, worldPos);
			SoundEngine.PlaySound(SoundID.Item80 with { Pitch = 0.1f }, worldPos);

			ParticleHandler.SpawnParticle(new PulseCircle(worldPos, Color.Cyan.Additive(), 0.2f, 200, 20, Common.Easing.EaseBuilder.EaseCircularOut));
			ParticleHandler.SpawnParticle(new PulseCircle(worldPos, Color.White.Additive(), 0.1f, 200, 20, Common.Easing.EaseBuilder.EaseCircularOut));

			ParticleHandler.SpawnParticle(new ImpactLinePrim(worldPos, Vector2.Zero, Color.DarkCyan.Additive(), new(0.5f, 1), 5, 0));
			ParticleHandler.SpawnParticle(new ImpactLinePrim(worldPos, Vector2.Zero, Color.Cyan.Additive(), new(1, 3), 10, 0)
			{
				Rotation = MathHelper.PiOver2
			});
			ParticleHandler.SpawnParticle(new ImpactLinePrim(worldPos, Vector2.Zero, Color.White.Additive(), new(0.5f, 3), 10, 0)
			{
				Rotation = MathHelper.PiOver2
			});

			for (int x = 0; x < 4; x++)
				ParticleHandler.SpawnParticle(new EmberParticle(worldPos, Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f), Color.Cyan, Color.IndianRed, Main.rand.NextFloat(0.25f, 0.5f), Main.rand.Next(60, 80), 5));
		}
	}

    public override void MouseOver(int i, int j)
    {
        Player player = Main.LocalPlayer;
        player.noThrow = 2;
        player.cursorItemIconEnabled = true;
        player.cursorItemIconID = this.AutoItemType();
    }

	public void DrawSway(int i, int j, SpriteBatch spriteBatch, Vector2 offset, float rotation, Vector2 origin)
	{
		if (!TileExtensions.GetVisualInfo(i, j, out Color color, out Texture2D texture))
			return;

		Vector2 position = new Vector2(i, j).ToWorldCoordinates(8, -2) - Main.screenPosition;
		Rectangle source = TextureAssets.Tile[Type].Frame();
		origin = new(source.Width / 2, 0);

		spriteBatch.Draw(texture, position, source, color, rotation, origin, 1, 0, 0);

		float opacity = GetOpacity(i, j);
		if (opacity > 0)
		{
			Texture2D glowmask = GlowmaskTile.TileIdToGlowmask[Type].Glowmask.Value;

			spriteBatch.Draw(glowmask, position, source, (new Color(0, 255, 190) * opacity).Additive(), rotation, origin, 1, 0, 0);
			spriteBatch.Draw(glowmask, position, source, (Color.White * opacity * 0.2f).Additive(), rotation, origin, 1, 0, 0);

			if (!Main.gamePaused && Main.rand.NextFloat() < GetOpacity(i, j) * 0.05f)
			{
				Vector2 emberPosition = Main.rand.NextVector2FromRectangle(new(i * 16, j * 16, 16, 16));
				ParticleHandler.SpawnParticle(new EmberParticle(emberPosition, Vector2.UnitY * -0.1f, Color.Cyan, Main.rand.NextFloat(0.1f, 0.3f), Main.rand.Next(60, 80), 5));
			}
		}

		if (Main.InSmartCursorHighlightArea(i, j, out bool actuallySelected))
			spriteBatch.Draw(TextureAssets.HighlightMask[Type].Value, position, source, actuallySelected ? Color.Yellow : Color.Gray, rotation, origin, 1, SpriteEffects.None, 0);
	}

	public float Physics(Point16 topLeft)
	{
		var data = TileObjectData.GetTileData(Framing.GetTileSafely(topLeft));
		float rotation = Main.instance.TilesRenderer.GetWindCycle(topLeft.X, topLeft.Y, TileSwaySystem.SunflowerWindCounter);

		if (!WorldGen.InAPlaceWithWind(topLeft.X, topLeft.Y, data.Width, data.Height))
			rotation = 0f;

		return rotation + Main.instance.TilesRenderer.GetWindGridPushComplex(topLeft.X, topLeft.Y, 30, 3f, 3, true);
	}
}