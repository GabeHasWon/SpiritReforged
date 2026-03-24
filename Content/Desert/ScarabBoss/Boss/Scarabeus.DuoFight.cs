using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.SaltFlats.Tiles.Salt;
using SpiritReforged.Content.Savanna.Tiles;
using System.Linq;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.UI;
using Terraria.Graphics.CameraModifiers;
using Terraria.ModLoader;
using static SpiritReforged.Common.Misc.AnimationSequence;
using static SpiritReforged.Common.PlayerCommon.DoubleTapPlayer;
using static tModPorter.ProgressUpdate;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

public partial class Scarabeus : ModNPC
{
	public bool FightingDScourge
	{
		get => desertScourge != null;
	}

	private int scourgeFightManagerIndex = -1;
	public ModNPC scourgeFightManager;
	private NPC _cachedDesertScourge;

	private static int duoFightManagerType = -1;
	private static int desertScourgeType = -1;

	public NPC desertScourge
	{
		get
		{
			if (!CrossMod.Fables.Enabled || scourgeFightManager == null)
				return null;

			if (_cachedDesertScourge == null || !_cachedDesertScourge.active)
				_cachedDesertScourge = (NPC)(CrossMod.Fables.Instance.Call("spiritCrossmod.kaiju", "getDesertScourge", scourgeFightManager));

			return _cachedDesertScourge;
		}
	}

	public static object HandleModCall(object[] args)
	{
		//???
		if (args.Length < 2 || args[1] is not string instruction)
			return null;

		switch (instruction)
		{
			case "doDigRumbleVFX":
				DuoFightSpawnRumbleVFX((Rectangle)args[2], (bool)args[3]);
				return true;
		}

		return null;
	}

	#region Setup
	private static LocalizedText DuoFightHoverText;
	private void LoadDuoFight()
	{
		if (!CrossMod.Fables.Enabled)
			return;

		if (CrossMod.Fables.Instance.TryFind("DesertScourge", out ModNPC dscourge))
			desertScourgeType = dscourge.Type;
		if (CrossMod.Fables.Instance.TryFind("ScourgeVsScarab", out ModNPC duel))
			duoFightManagerType = duel.Type;
		DuoFightHoverText = CrossMod.Fables.Instance.GetLocalization("Extras.ScourgeVsScarabHover");
	}

	public void CheckDuoFightStart(IEntitySource source)
	{
		//If we've been spawned by the duo fight manager, use it as the lifemax
		if (source is EntitySource_Parent parentSource && parentSource.Entity is NPC parentNPC && parentNPC.type == duoFightManagerType)
		{
			NPC.realLife = parentNPC.whoAmI;
			scourgeFightManagerIndex = parentNPC.whoAmI;
			scourgeFightManager = parentNPC.ModNPC;
			CurrentState = AIState.DuoFightSpawnAnim;
			scarabColorIndex = 0; //No recolored scarab because at points in the fight a different shader gets applied on scarab so we can't rely on the iridescence shader for it
		}
	}
	#endregion

	#region Spawn anim
	public float DuoFightSpawnAnimation(ref bool retarget)
	{
		retarget = false;
		NPC.direction = 1; //Scarab will always jump from the left

		if (scourgeFightManager == null || !scourgeFightManager.NPC.active)
		{
			NPC.Opacity = 1f;
			ChangeState(AIState.IdleAwayFromPlayer);
			return 0f;
		}

		float spawnAnimProgress = (float)CrossMod.Fables.Instance.Call("spiritCrossmod.kaiju", "spawnAnimPercent", scourgeFightManager);

		NPC.Opacity = 1f;
		Vector2 targetPosition = (Vector2)CrossMod.Fables.Instance.Call("spiritCrossmod.kaiju", "spawnAnimPos", scourgeFightManager);
		NPC.noTileCollide = true;
		NPC.noGravity = true;
		NPC.behindTiles = spawnAnimProgress < 0.5f;
		NPC.velocity = targetPosition - NPC.Center;
		SetFrame(RollFrame, PhaseTwoProfile);

		float slowdown = 1 - spawnAnimProgress * 0.7f;
		trailOpacity = MathF.Pow(1 - slowdown, 0.5f);

		NPC.rotation += NPC.direction * 0.55f * slowdown;
		return 1f;
	}

	public float DuoFightSpawnFallback(ref bool retarget)
	{
		NPC.direction = -1; //Bounce back to the right
		retarget = false;
		NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, 0f, 0.02f);
		NPC.velocity.Y += 0.35f;
		SetFrame(RollFrame, PhaseTwoProfile);
		NPC.rotation += NPC.direction * (0.3f + NPC.velocity.Y * 0.03f);

		NPC.noGravity = false;
		NPC.noTileCollide = true;

		//Hitting the ground
		if (NPC.velocity.Y > 0 && OnTopOfTiles)
		{
			NPC.rotation = 0;
			ShiftUpToFloorLevel();
			SoundEngine.PlaySound(BounceSound, NPC.Center);
			GroundImpactVFX(Math.Abs(NPC.velocity.Y) * 0.1f);
			NPC.velocity.Y *= 0;
			currentFrame.X = 0;
			Profile = PhaseOneProfile;
			ChangeState(AIState.IdleAwayFromPlayer);
			return 0f;
		}

		return 1f;
	}
	#endregion

	#region Giga Slam Attack
	public void DuoFightUnearthScourge(Vector2 position, float delay)
	{
		CrossMod.Fables.Instance.Call("spiritCrossmod.kaiju", "sendScourgeFlyingUp", scourgeFightManager, position, delay);
	}
	#endregion

	#region Visuals
	public static void DuoFightSpawnRumbleVFX(Rectangle rumbleArea, bool doTileWave)
	{
		if (Main.dedServ)
			return;

		Vector2 rumbleCenter = rumbleArea.Center.ToVector2();

		for (int i = 0; i < Main.rand.Next(4); i++)
		{
			Vector2 particleVel = -Vector2.UnitY * Main.rand.NextFloat(4, 7);
			Color[] colors = GetTilePalette(FindGroundFromPosition(rumbleCenter));

			ParticleHandler.SpawnParticle(new SmokeCloud(Main.rand.NextVector2FromRectangle(rumbleArea), particleVel, colors[0], Main.rand.NextFloat(0.08f, 0.12f), EaseFunction.EaseCircularOut, Main.rand.Next(30, 40))
			{
				Pixellate = true,
				DissolveAmount = 1,
				Intensity = 0.9f,
				SecondaryColor = colors[1],
				TertiaryColor = colors[2],
				PixelDivisor = 3,
				Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
				ColorLerpExponent = 0.5f,
				Layer = ParticleLayer.BelowSolid
			});
		}

		if (Main.rand.NextBool(3))
			Dust.NewDustPerfect(Main.rand.NextVector2FromRectangle(rumbleArea), DustID.Sand, new(0, -4), 0, default, Main.rand.NextFloat(0.7f, 1.2f));

		if (doTileWave)
			StaticBouncingTileWave(rumbleCenter, 5, Main.rand.NextFloat(4, 10), Main.rand.Next(30, 40), Main.rand.NextFloat(-rumbleArea.Width / 4, rumbleArea.Width / 4) * Vector2.UnitX);
	}

	private Effect GetElectricEffect(float electricityOpacity)
	{
		if (scourgeFightManager == null)
			return null;

		Texture2D texture = Profile.Texture.Value;
		if (currentFrame == RollFrame)
			texture = BallProfile.Texture.Value;

		return (Effect)CrossMod.Fables.Instance.Call("spiritCrossmod.kaiju", "setupElectricShader", scourgeFightManager, texture, NPC.frame, electricityOpacity);
	}

	private static void StaticBouncingTileWave(Vector2 rumblePos, int numTiles, float maxHeight, int totalTime = 60, Vector2? offset = null)
	{
		if (Main.dedServ)
			return;

		for (int j = -1; j <= 1; j += 2)
		{
			for (float i = 0; i < numTiles; i++)
			{
				float height = MathHelper.Lerp(maxHeight, 0, EaseFunction.EaseQuadIn.Ease(i / numTiles));
				int delay = (int)MathHelper.Lerp(0, totalTime / 2, (i + 1) / numTiles);
				ParticleHandler.SpawnQueuedParticle(new MovingBlockParticle(FindGroundFromPosition(rumblePos + (offset ?? Vector2.Zero) + j * Vector2.UnitX * 16 * (i + 1)), totalTime / 2, height), delay);
			}
		}

		ParticleHandler.SpawnParticle(new MovingBlockParticle(FindGroundFromPosition(rumblePos + (offset ?? Vector2.Zero)), totalTime / 2, maxHeight));
	}

	public override bool PreHoverInteract(bool mouseIntersects)
	{
		if (mouseIntersects && !Main.LocalPlayer.mouseInterface && FightingDScourge)
		{
			Main.LocalPlayer.cursorItemIconEnabled = false;
			string text = DuoFightHoverText.Format(NPC.GivenOrTypeName, desertScourge.GivenOrTypeName);
			Main.instance.MouseTextHackZoom(text);
			Main.mouseText = true;
			return false;
		}

		return true;
	}
	#endregion
}