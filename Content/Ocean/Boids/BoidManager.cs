using Terraria.Utilities;

namespace SpiritReforged.Content.Ocean.Boids;

public class BoidManager : ModSystem
{
	/// <summary> int corresponds to the current world seed. </summary>
	internal static Action<int> OnAddBoids;

	/// <summary> The hash of potential boids for this world.<br/>
	/// Boids can have objects dynamically added and removed within a world, but the initial data always remains. </summary>
	internal static HashSet<Boid> Boids = [];

	/// <summary> Stores boid fish textures by load index (used as identifier 'type'). </summary>
	internal static readonly Dictionary<int, Asset<Texture2D>> Types = [];

	public override void Load()
	{
		const int numTypes = 14;

		if (Main.dedServ)
			return;

		for (int i = 0; i < numTypes; i++)
			Types.Add(i, ModContent.Request<Texture2D>("SpiritReforged/Content/Ocean/Boids/Textures/fish_" + i));

		On_Main.DrawWoF += static (On_Main.orig_DrawWoF orig, Main self) =>
		{
			if (!Main.gamePaused) //Update here because boids are largely visual
				Update();

			Draw(Main.spriteBatch);
			orig(self);
		};
	}

	public override void Unload() => OnAddBoids = null;

	public override void OnWorldLoad()
	{
		int seed = Main.ActiveWorldFileData.Seed;
		int boidCount = 8 + seed % 5; //The number of unique boids in this world

		for (int i = 0; i < boidCount; i++)
		{
			var boid = new WeightedRandom<Boid>();
			int[] selection = SelectTypes(seed + i * 300f);

			boid.Add(new Boid(Main.rand.Next(5, 20), 1, selection), 1);
			boid.Add(new Boid(Main.rand.Next(30, 60), .5f, selection), .2f);
			boid.Add(new Boid(Main.rand.Next(30, 60), .8f, 12), .14f); //Shrimp
			boid.Add(new Boid(60, .525f, 13), .19f); //Sardine

			Boids.Add((Boid)boid);
		}

		OnAddBoids?.Invoke(seed);
	}

	/// <summary> Used to select pseudo random types for a boid. </summary>
	internal static int[] SelectTypes(float seed)
	{
		int count = (int)(seed / 200f % 4f) + 1;
		List<int> result = [];

		for (int j = 0; j < count; ++j)
		{
			int toAdd = (int)((seed / 220f + j * 15.5f) / 8f) % 12;
			result.Add(toAdd);
		}

		return [.. result];
	}

	public override void OnWorldUnload() => Boids.Clear();

	public static void Draw(SpriteBatch spriteBatch)
	{
		foreach (Boid fishflock in Boids)
			fishflock.Draw(spriteBatch);
	}

	public static void Update()
	{
		foreach (Boid fishflock in Boids)
			fishflock.Update();

		bool nearLure = Main.LocalPlayer.GetModPlayer<OceanPlayer>().nearLure;
		int spawnRate = nearLure ? 28 : 34;

		if ((Main.LocalPlayer.ZoneBeach || nearLure) && Main.GameUpdateCount % spawnRate == 0)
		{
			var weightedBoid = new WeightedRandom<Boid>();

			foreach (var registered in Boids)
			{
				float w = registered.spawnWeight;

				if (w > 0)
					weightedBoid.Add(registered, w);
			}

			const int fluff = 1000;
			var spawnPos = Main.LocalPlayer.Center + new Vector2((Main.screenWidth / 2 + fluff) * Main.rand.NextFloat(-1f, 1f), (Main.screenHeight / 2 + fluff) * Main.rand.NextFloat(-1f, 1f));

			//Don't spawn on-screen unless spawned by a lure
			if (!nearLure && new Rectangle((int)Main.screenPosition.X, (int)Main.screenPosition.Y, Main.screenWidth, Main.screenHeight).Contains(spawnPos.ToPoint()))
				return;

			var tilePos = spawnPos.ToTileCoordinates();
			if (WorldGen.InWorld(tilePos.X, tilePos.Y, 10) && Framing.GetTileSafely(tilePos).LiquidAmount == 255)
				((Boid)weightedBoid).Populate(spawnPos, Main.rand.Next(20, 30), 50f);
		}
	}
}