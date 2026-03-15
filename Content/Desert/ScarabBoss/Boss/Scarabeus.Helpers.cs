using SpiritReforged.Common.Easing;
using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Content.Particles;
using Terraria.Utilities;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

public partial class Scarabeus : ModNPC
{
	private float GoBackToIdle()
	{
		SetFrame(phaseTwo ? 3 : 0, 0, phaseTwo ? PhaseTwoProfile : PhaseOneProfile);
		ChangeState(FindAppropriateIdleState());
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
		WeightedRandom<AIState> state = new();

		if (!phaseTwo)
		{
			Add(AIState.Shockwave, 1);
			Add(AIState.GroundPound, 1);
			Add(AIState.Dig, 1);
			Add(AIState.Roll, 1);
		}
		else
		{
			Add(AIState.SwoopDash, 1);
			Add(AIState.GroundPound, 1);
			Add(AIState.Dig, 1);
			Add(AIState.Swarm, 122223);
		}

		AIState selectedState = (state.elements.Count == 0) ? FindAppropriateIdleState() : state;
		LastAttack = selectedState;
		ShiftUpToFloorLevel();
		NPC.velocity.Y = Math.Min(NPC.velocity.Y, 0);
		return selectedState;

		void Add(AIState element, double weight) //Adds to state and automatically avoids duplicates
		{
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

	public Vector2 FindGroundFromPositionIgnorePlatforms(Vector2 input)
	{
		const int dimensions = 8;

		if (input.X < 0 || input.X >= Main.maxTilesX * 16)
			return input;

		while (!CollisionChecks.Tiles(new((int)input.X - dimensions / 2, (int)input.Y - dimensions / 2, dimensions, dimensions), input.Y < Target.Top.Y - 40 ? CollisionChecks.SolidOnly : CollisionChecks.AnySurface))
			input.Y += dimensions;

		while (CollisionChecks.Tiles(new((int)input.X - dimensions / 2, (int)input.Y - dimensions / 2, dimensions, dimensions), input.Y < Target.Top.Y - 40 ? CollisionChecks.SolidOnly : CollisionChecks.AnySurface))
			input.Y -= dimensions;

		return input + new Vector2(0, dimensions);
	}

	public static Color[] GetTilePalette(Vector2 input)
	{
		Point tilePosition = input.ToTileCoordinates();
		Tile tile = Framing.GetTileSafely(tilePosition);

		if (!tile.HasTile || !Main.tileSolid[tile.TileType] || tile.TileType == TileID.Sand)
			return [new Color(223, 219, 147) * 2f, new Color(188, 170, 86) * 1.33f, new Color(58, 49, 18) * 0.5f];

		var material = TileMaterial.FindMaterial(tile.TileType);
		return [material.Color, (material.Color * 0.8f).Additive(255) * 1.33f, (material.Color * 0.25f).Additive(255) * 0.5f];
	}

	private void BouncingTileWave(int numTiles, float maxHeight, int totalTime = 60, Vector2? offset = null)
	{
		for (int j = -1; j <= 1; j += 2)
			BouncingTileWave(j, numTiles, maxHeight, totalTime, offset);

		ParticleHandler.SpawnParticle(new MovingBlockParticle(FindGroundFromPosition(NPC.Center + (offset ?? Vector2.Zero)), totalTime / 2, maxHeight));
	}

	private void BouncingTileWave(int direction, int numTiles, float maxHeight, int totalTime = 60, Vector2? offset = null)
	{
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
}