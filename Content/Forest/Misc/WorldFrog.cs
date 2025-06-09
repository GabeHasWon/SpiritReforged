using Humanizer;
using MonoMod.Utils;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Common.WorldGeneration.Micropasses.Passes;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Tiles;
using System.Linq;
using System.Reflection;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Forest.Misc;

[AutoloadHead]
public class WorldFrog : ModNPC
{
	public const string LocPath = "Mods.SpiritReforged.NPCs.WorldFrog.";

	private bool _updated;
	private float _glowIntensity;

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 10;
		NPCID.Sets.NoTownNPCHappiness[Type] = true;

		NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers()
		{ Hide = true });
	}

	public override void SetDefaults()
	{
		NPC.CloneDefaults(NPCID.TownSlimeBlue);
		NPC.townNPC = false;
	}

	public override void AI()
	{
		if (!_updated)
			return;

		var player = Main.player[Player.FindClosest(NPC.position, NPC.width, NPC.height)];
		if (player.talkNPC == -1)
		{
			VanishEffects(NPC.GetSource_FromAI());
			SoundEngine.PlaySound(NPC.DeathSound, NPC.Center);

			NPC.active = false;
		}

		_glowIntensity = Math.Max(_glowIntensity - 0.07f, 0);
	}

	public override void SetChatButtons(ref string button, ref string button2)
	{
		if (UpdaterSystem.Instance.AnyTask())
			button = Language.GetTextValue(LocPath + "Button");
	}

	public override void OnChatButtonClicked(bool firstButton, ref string shopName)
	{
		UpdaterSystem.RunFirstTask(out string report);
		Main.npcChatText = FrogifyText(Language.GetTextValue(LocPath + "Reports." + report + $"_{Main.rand.Next(2)}"));

		_updated = true;
		_glowIntensity = 1;

		for (int i = 0; i < 10; i++)
		{
			var position = NPC.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(20f);
			float scale = Main.rand.NextFloat(0.5f);
			int maxTime = Main.rand.Next(5, 30);
			var velocity = Vector2.UnitY * -Main.rand.NextFloat();

			ParticleHandler.SpawnParticle(new GlowParticle(position, velocity, Color.Lerp(Color.Cyan, Color.Goldenrod, Main.rand.NextFloat()).Additive(), scale, maxTime));
			ParticleHandler.SpawnParticle(new GlowParticle(position, velocity, Color.White.Additive(), scale * 0.75f, maxTime));
		}

		SoundEngine.PlaySound(SoundID.Item176, NPC.Center);
	}

	private static string FrogifyText(string dialogue)
	{
		string sounds = string.Empty;

		for (int i = 0; i < Main.rand.Next(1, 3); i++)
			sounds += Language.GetTextValue(LocPath + "Sounds." + Main.rand.Next(2)) + ", ";

		sounds = sounds.Remove(sounds.Length - 2, 2);
		sounds += " ({0})";

		return sounds.FormatWith(dialogue);
	}

	public override bool CheckActive() => false;
	public override bool CanChat() => true;
	public override string GetChat() => FrogifyText(Language.GetTextValue(LocPath + "Dialogue." + Main.rand.Next(3)));

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (!Main.dedServ && NPC.life <= 0)
			VanishEffects(NPC.GetSource_Death());
	}

	private void VanishEffects(IEntitySource source)
	{
		int goreType = Mod.Find<ModGore>(nameof(WorldFrog)).Type;
		Gore.NewGore(source, NPC.Top, NPC.velocity, goreType);

		for (int i = 0; i < 3; i++)
			Gore.NewGore(source, Main.rand.NextVector2FromRectangle(NPC.getRect()), Vector2.Zero, GoreID.Smoke1);
	}

	public override void FindFrame(int frameHeight)
	{
		const float rate = 0.2f;
		NPC.frame.Width = 36;

		bool moving = NPC.velocity != Vector2.Zero;
		int lastFrame = moving ? 5 : Main.npcFrameCount[Type];

		if (NPC.velocity.Y != 0) //Falling
		{
			NPC.frameCounter = 0;
		}
		else if (!moving)
		{
			if (NPC.frameCounter > rate || Main.rand.NextBool(50))
				NPC.frameCounter += rate;
		}
		else
		{
			NPC.frameCounter += rate;
		}

		NPC.frameCounter = NPC.frameCounter % lastFrame;

		NPC.frame.X = moving ? NPC.frame.Width : 0;
		NPC.frame.Y = (int)NPC.frameCounter * frameHeight;
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		NPC.spriteDirection = NPC.direction;

		var texture = TextureAssets.Npc[Type].Value;
		var effects = (NPC.spriteDirection == 1) ? SpriteEffects.FlipHorizontally : default;

		var frame = NPC.frame with { Width = NPC.frame.Width - 2, Height = NPC.frame.Height - 2 };
		var origin = new Vector2(frame.Width / 2, frame.Height);
		var position = NPC.Bottom - screenPos + new Vector2(0, NPC.gfxOffY + 2);

		spriteBatch.Draw(texture, position, frame, NPC.DrawColor(drawColor), NPC.rotation, origin, NPC.scale, effects, 0);

		if (_glowIntensity > 0)
			spriteBatch.Draw(TextureColorCache.ColorSolid(texture, Color.White), position, frame, NPC.GetAlpha(Color.White) * _glowIntensity, NPC.rotation, origin, NPC.scale, effects, 0);

		return false;
	}
}

internal class UpdaterSystem : ModSystem
{
	/// <summary> Stores the mod version that a particular delegate task belongs to. </summary>
	[AttributeUsage(AttributeTargets.Method)]
	private class VerAttribute(string Version) : Attribute
	{
		public string Version = Version;
	}

	private delegate void TaskDelegate(out string report);

	public static UpdaterSystem Instance { get; private set; }

	public static Version LastVersion { get; private set; }
	public static bool RunningTask { get; private set; }
	private static readonly Dictionary<Version, TaskDelegate> Tasks = [];

	#region tModLoader hooks
	public override void Load()
	{
		Instance = this;
		On_Player.Hooks.EnterWorld += SpawnFrog;
	}

	private static void SpawnFrog(On_Player.Hooks.orig_EnterWorld orig, int playerIndex)
	{
		orig(playerIndex);

		if (Main.netMode == NetmodeID.SinglePlayer && Instance.AnyTask()) //Only spawn in singleplayer to avoid potential complications
			NPC.NewNPC(new EntitySource_SpawnNPC(), Main.spawnTileX * 16, Main.spawnTileY * 16, ModContent.NPCType<WorldFrog>());
	}

	public override void PostWorldGen() => LastVersion = SpiritReforgedMod.Instance.Version;
	public override void ClearWorld()
	{
		if (!WorldGen.generatingWorld)
			LastVersion = null;
	}

	public override void LoadWorldData(TagCompound tag) => LastVersion = tag.Get<Version>(nameof(LastVersion));
	public override void SaveWorldData(TagCompound tag)
	{
		if (LastVersion != null)
			tag[nameof(LastVersion)] = LastVersion;
	}

	//public override void NetSend(BinaryWriter writer) => writer.Write(LastVersion.ToString());
	//public override void NetReceive(BinaryReader reader) => LastVersion = new(reader.ReadString());
	#endregion

	public bool AnyTask()
	{
		if (Tasks.Count == 0)
		{
			foreach (var method in GetType().GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
			{
				if (method.GetCustomAttribute<VerAttribute>() is VerAttribute ver && method.TryCreateDelegate<TaskDelegate>() is TaskDelegate dele)
					Tasks.Add(new Version(ver.Version), dele);
			}
		}

		return Tasks.Any(x => Lower(x.Key));
		static bool Lower(Version value) => LastVersion is null || LastVersion < value;
	}

	/// <summary> Runs the next update task and adjusts <see cref="LastVersion"/> accordingly.<br/>
	/// <see cref="AnyTask"/> must return true for this to be called safely. </summary>
	public static void RunFirstTask(out string report)
	{
		RunningTask = true;

		var task = Tasks.OrderBy(x => x.Key).First();
		task.Value.Invoke(out string reportKey);

		report = reportKey;
		LastVersion = task.Key;

		RunningTask = false;
	}

	[Ver("0.1.1")]
	private static void CavesAndClubs(out string report)
	{
		PotsMicropass.RunMultipliedTask(0.5f);
		WorldMethods.Generate(CreateCommon, (int)(Main.maxTilesX / (float)WorldGen.WorldSizeSmallX * 250), out _);

		report = "CavesAndClubs";

		static bool CreateCommon(int x, int y) //Manually place common pot variants because they normally happen as conversion
		{
			WorldMethods.FindGround(x, ref y);
			y--;

			if (y < Main.worldSurface || y > Main.UnderworldLayer)
				return false;

			int ground = Framing.GetTileSafely(x, y + 1).TileType;
			int type = ModContent.TileType<CommonPots>();

			switch (ground)
			{
				case TileID.MushroomGrass:
					Placer.Check(x, y, type, Main.rand.Next(3)).IsClear().Place();
					break;

				case TileID.Granite:
					Placer.Check(x, y, type, Main.rand.Next([3, 4, 5])).IsClear().Place();
					break;

				default:
					return false;
			}

			return Main.tile[x, y].TileType == type;
		}
	}
}