using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Ocean.Items;
using SpiritReforged.Content.Ocean.Items.Reefhunter.Particles;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Ocean.Hydrothermal.Tiles;

public class HydrothermalVent : ModTile
{
	public const int CooldownMax = (int)(Main.dayLength / 2);
	public const int EruptDuration = 600;

	public static readonly SoundStyle[] EruptionSounds =
		[new("SpiritReforged/Assets/SFX/Tile/StoneCrack1") { PitchVariance = 0.6f },
		new("SpiritReforged/Assets/SFX/Tile/StoneCrack2") { PitchVariance = 0.6f }];

	/// <summary> Cooldowns for all <see cref="HydrothermalVent"/> tiles in the world. Never read on multiplayer clients. </summary>
	private static readonly Dictionary<Point16, int> cooldowns = [];

	/// <summary> Precise texture top positions for all tile styles, used for visuals. </summary>
	private static readonly Point[] tops = [new Point(16, 16), new Point(16, 16), new Point(16, 24), new Point(12, 4), new Point(20, 4), new Point(16, 16), new Point(16, 16), new Point(16, 16)];

	public override void Load() => On_Wiring.UpdateMech += UpdateCooldowns;

	private static void UpdateCooldowns(On_Wiring.orig_UpdateMech orig)
	{
		orig();

		foreach (var entry in cooldowns)
		{
			var coords = entry.Key;

			if (!IsValid(coords.X, coords.Y))
			{
				cooldowns.Remove(coords);
				break;
			}

			if (cooldowns[coords] > 0)
				cooldowns[coords]--;
		}
	}

	/// <summary> Checks whether the given position is valid for a <see cref="cooldowns"/> entry to exist. </summary>
	/// <param name="i"> The X coordinate. </param>
	/// <param name="j"> The Y Coordinate.</param>
	/// <returns> Whether the given position is valid. </returns>
	private static bool IsValid(int i, int j) => TileObjectData.IsTopLeft(i, j) && Framing.GetTileSafely(i, j).TileType == ModContent.TileType<HydrothermalVent>();

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileSpelunker[Type] = true;
		Main.tileLighted[Type] = true;

		TileID.Sets.DisableSmartCursor[Type] = true;
		TileID.Sets.PreventsTileRemovalIfOnTopOfIt[Type] = true;
		TileID.Sets.PreventsTileReplaceIfOnTopOfIt[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Height = 4;
		TileObjectData.newTile.CoordinateHeights = [16, 16, 16, 16];
		TileObjectData.newTile.Origin = new(0, 3);
		TileObjectData.newTile.AnchorValidTiles = [ModContent.TileType<Gravel>(), ModContent.TileType<Magmastone>(), TileID.Sand];
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.RandomStyleRange = 8;
		TileObjectData.addTile(Type);

		DustType = DustID.Stone;
		MinPick = 50;
		AddMapEntry(new Color(64, 54, 66), CreateMapEntryName());
	}

	public override void NearbyEffects(int i, int j, bool closer) //Client effects
	{
		if (!TileObjectData.IsTopLeft(i, j) || Main.gamePaused)
			return;

		var t = Framing.GetTileSafely(i, j);
		int fullWidth = TileObjectData.GetTileData(t).CoordinateFullWidth;
		var position = new Vector2(i, j) * 16 + tops[t.TileFrameX / fullWidth].ToVector2();

		if (Main.rand.NextBool(5)) //Passive smoke effects
		{
			var velocity = new Vector2(0, -Main.rand.NextFloat(2f, 2.5f));
			var smoke = new SmokeCloud(position, velocity, new Color(40, 40, 50), Main.rand.NextFloat(0.1f, 0.15f), EaseFunction.EaseQuadOut, Main.rand.Next(50, 120), false)
			{
				SecondaryColor = Color.SlateGray,
				TertiaryColor = Color.Black,
				ColorLerpExponent = 0.5f,
				Intensity = 0.25f,
				Pixellate = true,
				PixelDivisor = 4
			};

			ParticleHandler.SpawnParticle(smoke);
		}

		if (Main.rand.NextBool(12))
			ParticleHandler.SpawnParticle(new BubbleParticle(position + Main.rand.NextVector2Unit() * Main.rand.NextFloat(4), -Vector2.UnitY, Main.rand.NextFloat(0.2f, 0.35f), 60));

		if (Main.rand.NextBool()) //Passive ash effects
		{
			float range = Main.rand.NextFloat();
			var velocity = new Vector2(0, -Main.rand.NextFloat(range * 8f)).RotatedByRandom((1f - range) * 1.5f);

			var dust = Dust.NewDustPerfect(position, DustID.Ash, velocity, Alpha: 180);
			dust.noGravity = true;
		}

		BubbleSoundPlayer.StartSound(new Vector2(i, j) * 16);
	}

	public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		if (Framing.GetTileSafely(i, j).TileFrameY == 0)
		{
			float pulse = Main.rand.Next(28, 42) * .001f;
			var col = Color.Orange.ToVector3() / 3.5f;

			(r, g, b) = (col.X + pulse, col.Y + pulse, col.Z + pulse);
		}
	}

	public override void MouseOver(int i, int j)
	{
		var player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = ModContent.ItemType<MineralSlag>();
	}

	public override void RandomUpdate(int i, int j) => TryErupt(i, j);
	private static bool TryErupt(int i, int j)
	{
		TileExtensions.GetTopLeft(ref i, ref j);

		var pt = new Point16(i, j);
		if (IsValid(i, j) && !cooldowns.ContainsKey(pt))
			cooldowns.Add(pt, CooldownMax); //Initialize cooldown counters on the server/singleplayer

		if (cooldowns[pt] == 0 && WorldGen.PlayerLOS(i, j))
		{
			Erupt(i, j);
			cooldowns[pt] = CooldownMax;

			if (Main.netMode != NetmodeID.SinglePlayer) //Sync vent eruption in multiplayer
				new EruptionData(new Point16(i, j)).Send();

			return true;
		}

		return false;
	}

	public static void Erupt(int i, int j)
	{
		var t = Framing.GetTileSafely(i, j);
		int fullWidth = TileObjectData.GetTileData(t).CoordinateFullWidth;
		var position = new Vector2(i, j) * 16 + tops[t.TileFrameX / fullWidth].ToVector2();

		if (Main.netMode != NetmodeID.MultiplayerClient)
			Projectile.NewProjectile(new EntitySource_Wiring(i, j), position, Vector2.UnitY * -4f, ModContent.ProjectileType<HydrothermalVentPlume>(), 5, 0f);

		if (!Main.dedServ)
		{
			for (int k = 0; k <= 20; k++)
				Dust.NewDustPerfect(position, ModContent.DustType<Dusts.BoneDust>(), new Vector2(0, 6).RotatedByRandom(1) * Main.rand.NextFloat(-1, 1));
			for (int k = 0; k <= 20; k++)
				Dust.NewDustPerfect(position, ModContent.DustType<Dusts.FireClubDust>(), new Vector2(0, 6).RotatedByRandom(1) * Main.rand.NextFloat(-1, 1));

			SoundEngine.PlaySound(Main.rand.Next(EruptionSounds), position);
			SoundEngine.PlaySound(SoundID.Drown with { Pitch = -.5f, PitchVariance = .25f, Volume = 1.5f }, position);

			ParticleHandler.SpawnParticle(new TexturedPulseCircle(position, Color.Yellow, 0.75f, 200, 20, "supPerlin",
				new Vector2(4, 0.75f), EaseFunction.EaseCubicOut).WithSkew(0.75f, MathHelper.Pi - MathHelper.PiOver2));

			ParticleHandler.SpawnParticle(new TexturedPulseCircle(position, Color.Red, 0.75f, 200, 20, "supPerlin",
				new Vector2(4, 0.75f), EaseFunction.EaseCubicOut).WithSkew(0.75f, MathHelper.Pi - MathHelper.PiOver2));

			for (int x = 0; x < 5; x++) //Large initial smoke plume
			{
				var smoke = new SmokeCloud(position, -Vector2.UnitY, new Color(40, 40, 50), Main.rand.NextFloat(0.15f, 0.25f), EaseFunction.EaseQuadOut, 150, false)
				{
					SecondaryColor = Color.SlateGray,
					TertiaryColor = Color.Black,
					ColorLerpExponent = 0.5f,
					Intensity = 0.25f,
					Pixellate = true,
					PixelDivisor = 4
				};

				ParticleHandler.SpawnParticle(smoke);
			}

			var player = Main.LocalPlayer;
			if (Collision.WetCollision(player.position, player.width, player.height))
				player.SimpleShakeScreen(2, 3, 90, 16 * 10);

			Magmastone.AddGlowPoint(i, j);
		}
	}
}

internal class EruptionData : PacketData
{
	private readonly Point16 _point;

	public EruptionData() { }
	public EruptionData(Point16 point) => _point = point;

	public override void OnReceive(BinaryReader reader, int whoAmI)
	{
		var point = reader.ReadPoint16();

		if (Main.netMode == NetmodeID.Server) //If received by the server, send to all clients
			new EruptionData(point).Send(ignoreClient: whoAmI);

		HydrothermalVent.Erupt(point.X, point.Y);
	}

	public override void OnSend(ModPacket modPacket) => modPacket.WritePoint16(_point);
}
