using SpiritReforged.Common.Easing;
using SpiritReforged.Common.MathHelpers;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.TileCommon;
using Terraria.Utilities;

namespace SpiritReforged.Content.Desert.ScarabBoss.Boss;

public partial class Scarabeus : ModNPC
{
	private Action SelectWeightedState()
	{
		if (!phaseTwo && NPC.Center.Y - Target.Center.Y > 80)
			return Leap; //Prioritize leap if too far below Target

		WeightedRandom<Action> state = new();

		if (phaseTwo)
		{
			Add(FlyHover, 1);
			Add(FlyingDash, 1);
			Add(ChainGroundPound, 1);

			if (!Collision.SolidTiles(NPC.position, NPC.width, NPC.height))
				Add(LeapDig, 1);

			Add(ScarabSwarm, 1);
		}
		else
		{
			if (CurrentState != Array.IndexOf(_states, Skitter))
				Add(Walking, 1);

			if (NPC.DistanceSQ(Target.Center) > 160)
				Add(Leap, 0.5);

			if (Collision.SolidTiles(NPC.position + new Vector2(0, 4), NPC.width, NPC.height)) //This is different from checking whether the NPC is grounded
			{
				Add(Dig, 1);
				Add(GroundedSlam, 1);
			}

			if (Math.Abs(NPC.Center.Y - Target.Center.Y) < 64 && Math.Abs(NPC.Center.X - Target.Center.X) > 48)
				Add(RollDash, 1);
		}

		return (state.elements.Count == 0) ? Walking : state;

		bool Add(Action element, double weight) //Adds to state and automatically avoids duplicates
		{
			if (CurrentState != Array.IndexOf(_states, element))
			{
				state.Add(element, weight);
				return true;
			}

			return false;
		}
	}

	/// <summary> Finds the nearest surface tile to the provided world coordinates, <b>in world coordinates</b> <br/>
	/// If the given input is inside the ground, instead moves upwards until reaching the surface. </summary>
	private static Vector2 FindGroundFromPosition(Vector2 input)
	{
		const int dimensions = 8;

		while (!CollisionChecks.Tiles(new((int)input.X - dimensions / 2, (int)input.Y - dimensions / 2, dimensions, dimensions), CollisionChecks.AnySurface))
			input.Y += dimensions;

		while (CollisionChecks.Tiles(new((int)input.X - dimensions / 2, (int)input.Y - dimensions / 2, dimensions, dimensions), CollisionChecks.AnySurface))
			input.Y -= dimensions;

		return input + new Vector2(0, dimensions);
	}

	private static Color[] GetTilePalette(Vector2 input)
	{
		Point tilePosition = input.ToTileCoordinates();
		Tile tile = Framing.GetTileSafely(tilePosition);

		if (!tile.HasTile || tile.TileType == TileID.Sand)
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
}