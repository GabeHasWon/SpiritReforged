namespace SpiritReforged.Content.Ocean.Boids;

/// <summary> The blueprint for a group of <see cref="BoidObject"/>s. Does not represent individual groups as they exist in the world. </summary>
internal class Boid(float maxFlockSize = 60, float weight = 1, params int[] types)
{
	protected readonly List<BoidObject> Objects = [];

	public readonly HashSet<int> variants = [.. types];
	public readonly float maxFish = maxFlockSize;
	public readonly float spawnWeight = weight;

	private const int SimulationDistance = 2500;

	internal void Populate(Vector2 position, int amount, float spread)
	{
		for (int i = 0; i < amount; i++)
		{
			if (Objects.Count < maxFish)
			{
				var fish = new BoidObject(this)
				{
					position = position + new Vector2(Main.rand.NextFloat(-spread, spread), Main.rand.NextFloat(-spread, spread)),
					velocity = new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1))
				};

				Objects.Add(fish);
			}
		}
	}

	public void Update()
	{
		foreach (BoidObject fish in Objects.ToArray())
		{
			if (fish is null)
				continue;

			fish.Update();
			fish.AdjFish.Clear();

			foreach (BoidObject adjfish in Objects)
			{
				if (!fish.Equals(adjfish) && Vector2.DistanceSquared(fish.position, adjfish.position) < BoidObject.Vision * BoidObject.Vision)
					fish.AdjFish.Add(adjfish);
			}

			if (Vector2.DistanceSquared(fish.position, Main.LocalPlayer.Center) > SimulationDistance * SimulationDistance)
				Objects.Remove(fish);
		}
	}

	public void Draw(SpriteBatch spriteBatch)
	{
		foreach (BoidObject fish in Objects.ToArray())
			fish?.Draw(spriteBatch);
	}
}
