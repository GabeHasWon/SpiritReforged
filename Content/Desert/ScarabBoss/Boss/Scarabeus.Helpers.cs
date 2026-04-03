using SpiritReforged.Common.Easing;
using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Particles;
using System.Linq;
using Terraria.Utilities;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

public partial class Scarabeus : ModNPC
{
	private float GoBackToIdle()
	{
		SetFrame(0, 0, phaseTwo ? SimulatedProfile : PhaseOneProfile);
		ChangeState(FindAppropriateIdleState(), true);
		return 0f;
	}

	private AIState FindAppropriateIdleState()
	{
		float playerDistanceX = Math.Abs(NPC.Center.X - Target.Center.X);

		//Get some distance
		if (playerDistanceX < 110f)
			return AIState.IdleAwayFromPlayer;
		return AIState.IdleTowardsPlayer;
	}

	private AIState SelectAttack()
	{
		// return AIState.Dig;

		if (!Main.dayTime || Target.dead)
			return AIState.Despawn;

		WeightedRandom<AIState> state = new();

		float distanceToTargetX = Math.Abs(Target.Center.X - NPC.Center.X);

		if (!phaseTwo)
		{
			float targetFloorY = FindGroundFromPositionIgnorePlatforms(Target.Center).Y;
			float myFloorY = FindGroundFromPositionIgnorePlatforms(NPC.Center).Y;

			float distanceToTargetY = myFloorY - targetFloorY;

			Add(AIState.Shockwave, 1 - Utils.GetLerpValue(600f, 1000f, distanceToTargetX, true) * 0.3f - Utils.GetLerpValue(200f, 600f, distanceToTargetY, true) * 0.5f);
			Add(AIState.GroundPound, 1 + Utils.GetLerpValue(700f, 1000f, distanceToTargetX, true) * 0.4f);
			Add(AIState.Dig, 1 + Utils.GetLerpValue(700f, 1000f, distanceToTargetX, true) * 0.4f);
			Add(AIState.Roll, 1 - Utils.GetLerpValue(300f, 500f, distanceToTargetY, true) * 0.5f);
		}
		else
		{
			Add(AIState.SwoopDash, 1 - Utils.GetLerpValue(100f, 30f, distanceToTargetX, true) * 0.2f);
			Add(AIState.GroundPound, 1);
			Add(AIState.Dig, 1);
			Add(AIState.Swarm, 0.85f);
		}

		AIState selectedState = (state.elements.Count == 0) ? FindAppropriateIdleState() : state;
		LastAttack = selectedState;
		ShiftUpToFloorLevel();
		NPC.velocity.Y = Math.Min(NPC.velocity.Y, 0);
		if (!phaseTwo)
			NPC.rotation = 0f;
		return selectedState;

		void Add(AIState element, double weight) //Adds to state and automatically avoids duplicates
		{
			if (weight <= 0)
				return;

			float weightMult = LastAttack == element ? 0.1f : 1f;
			state.Add(element, weight * weightMult);
		}
	}

	/// <summary> Finds the nearest surface tile to the provided world coordinates, <b>in world coordinates</b> <br/>
	/// If the given input is inside the ground, instead moves upwards until reaching the surface. </summary>
	public static Vector2 FindGroundFromPosition(Vector2 input)
	{
		const int dimensions = 8;

		while (!CollisionChecks.Tiles(new((int)input.X - dimensions / 2, (int)input.Y - dimensions / 2, dimensions, dimensions), CollisionChecks.AnySurface))
			input.Y += dimensions;

		while (CollisionChecks.Tiles(new((int)input.X - dimensions / 2, (int)input.Y - dimensions / 2, dimensions, dimensions), CollisionChecks.AnySurface))
			input.Y -= dimensions;

		return input + new Vector2(0, dimensions);
	}

	public Vector2 FindGroundFromPositionIgnorePlatforms(Vector2 input, float? platformIgnoreHeight = null)
	{
		const int dimensions = 8;
		platformIgnoreHeight ??= Target.Top.Y - 40;

		if (input.X < 0 || input.X >= Main.maxTilesX * 16)
			return input;

		while (!CollisionChecks.Tiles(new((int)input.X - dimensions / 2, (int)input.Y - dimensions / 2, dimensions, dimensions), input.Y < platformIgnoreHeight ? CollisionChecks.SolidOnly : CollisionChecks.AnySurface))
			input.Y += dimensions;

		while (CollisionChecks.Tiles(new((int)input.X - dimensions / 2, (int)input.Y - dimensions / 2, dimensions, dimensions), input.Y < platformIgnoreHeight ? CollisionChecks.SolidOnly : CollisionChecks.AnySurface))
			input.Y -= dimensions;

		return input + new Vector2(0, dimensions);
	}

	public static Color[] GetTilePalette(Vector2 input)
	{
		Point tilePosition = input.ToTileCoordinates();
		Tile tile = Framing.GetTileSafely(tilePosition);

		if (!tile.HasTile || !Main.tileSolid[tile.TileType] || TileID.Sets.Platforms[tile.TileType] || tile.TileType == TileID.Sand)
			return [new Color(253, 239, 167) * 0.7f, new Color(148, 138, 90) * 0.7f, new Color(118, 116, 66) * 0.7f];

		var material = TileMaterial.FindMaterial(tile.TileType);
		return [material.Color, (material.Color * 0.8f).Additive(255) * 1.33f, (material.Color * 0.25f).Additive(255) * 0.5f];
	}

	private void GroundImpactVFX(float strength = 10f)
	{
		if (Main.dedServ)
			return;

		for (int i = 0; i < 22; i++)
		{
			Vector2 particlePos = NPC.Bottom + Vector2.UnitY * 4;
			particlePos += Main.rand.NextFloat(-64, 64) * Vector2.UnitX;

			Vector2 particleVel = -Vector2.UnitY * Main.rand.NextFloat(4, 7) * strength;
			Color[] colors = GetTilePalette(FindGroundFromPosition(NPC.Center));

			ParticleHandler.SpawnParticle(new SmokeCloud(particlePos, particleVel, colors[0], Main.rand.NextFloat(0.08f, 0.12f), EaseFunction.EaseCircularOut, Main.rand.Next(30, 40))
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

		for (int i = 0; i < 20; i++)
		{
			Vector2 dustPosition = NPC.Bottom + Vector2.UnitY * 4f;
			Point tilePosition = dustPosition.ToTileCoordinates();
			int dustIndex = WorldGen.KillTile_MakeTileDust(tilePosition.X, tilePosition.Y, Framing.GetTileSafely(tilePosition));

			Dust dust = Main.dust[dustIndex];
			dust.position = dustPosition + Vector2.UnitX * Main.rand.NextFloat(-NPC.width * 1.5f, NPC.width * 1.5f) - Vector2.UnitY * Main.rand.NextFloat(0f, 20f);
			dust.velocity.Y = -Main.rand.NextFloat(1.5f, 8f) * strength;
			dust.velocity.X *= 0.3f;
			dust.noLightEmittence = true;
			dust.scale = Main.rand.NextFloat(0.5f, 1.2f);
		}

		BouncingTileWave(5, Main.rand.NextFloat(10, 20), Main.rand.Next(30, 40));
	}

	private void BouncingTileWave(int numTiles, float maxHeight, int totalTime = 60, Vector2? offset = null)
	{
		if (Main.dedServ)
			return;

		for (int j = -1; j <= 1; j += 2)
			BouncingTileWave(j, numTiles, maxHeight, totalTime, offset);

		ParticleHandler.SpawnParticle(new MovingBlockParticle(FindGroundFromPosition(NPC.Center + (offset ?? Vector2.Zero)), totalTime / 2, maxHeight));
	}

	private void BouncingTileWave(int direction, int numTiles, float maxHeight, int totalTime = 60, Vector2? offset = null)
	{
		if (Main.dedServ)
			return;

		for (float i = 0; i < numTiles; i++)
		{
			float height = MathHelper.Lerp(maxHeight, 0, EaseFunction.EaseQuadIn.Ease(i / numTiles));
			int delay = (int)MathHelper.Lerp(0, totalTime / 2, (i + 1) / numTiles);
			ParticleHandler.SpawnQueuedParticle(new MovingBlockParticle(FindGroundFromPosition(NPC.Center + (offset ?? Vector2.Zero) + direction * Vector2.UnitX * 16 * (i + 1)), totalTime / 2, height), delay);
		}
	}

	/// <summary>
	/// Moves scarabeus up to be entirely outside of tiles if it is partially clipped into the floor. This can happen if it ground pounds too hard
	/// </summary>
	private void ShiftUpToFloorLevel(int maxTileShiftUp = 2)
	{
		bool shifted = false;
		Vector2 collisionPosition = NPC.position;
		int collisionWidth = NPC.width;
		int collisionHeight = NPC.height;
		ShrinkTileHitbox(NPC, ref collisionPosition, ref collisionWidth, ref collisionHeight);

		for (int i = 0; i < maxTileShiftUp * 8; i++)
		{
			bool freeSpaceAbove = !Collision.SolidCollision(collisionPosition - Vector2.UnitY * 8, collisionWidth, 1, !IgnorePlatforms);
			if (!freeSpaceAbove)
				freeSpaceAbove = !Collision.SolidCollision(collisionPosition - Vector2.UnitY * 16, collisionWidth, 1, !IgnorePlatforms);

			if (Collision.SolidCollision(collisionPosition, collisionWidth, collisionHeight, !IgnorePlatforms) && freeSpaceAbove)
			{
				shifted = true;
				NPC.position.Y -= 1f;
				collisionPosition.Y--;
			}
			else
				break;
		}

		if (shifted)
			NPC.netUpdate = true;
	}

	public bool GetClosestDesertPlayer(float maxDistance)
	{
		IEnumerable<Player> potentialTargets = Main.player.Where(p => p.active && !p.dead && p.ZoneDesert && p.Distance(NPC.Center) < maxDistance);
		if (potentialTargets.Count() == 0)
			return false;

		Player targetChoice = potentialTargets.OrderBy(p => p.Distance(NPC.Center) - p.aggro).FirstOrDefault();
		if (targetChoice == null)
			return false;

		int oldTarget = NPC.target;
		NPC.target = targetChoice.whoAmI;
		NPC.targetRect = targetChoice.Hitbox;
		if (oldTarget != NPC.target)
			NPC.netUpdate = true;
		return true;
	}
}