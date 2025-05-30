using SpiritReforged.Common.Particle;
using Terraria.Audio;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Content.Forest.Safekeeper;

/// <summary> Handles visuals and sounds for <see cref="UndeadNPC"/> death effects. See <see cref="StartEffect"/> for spawning. </summary>
internal class UndeadDecay : ModProjectile
{
	private const float DecayRate = .025f;

	public override string Texture => AssetLoader.EmptyTexture;
	public override LocalizedText DisplayName => Language.GetText("Mods.SpiritReforged.Projectiles.Firespike.DisplayName");

	public float Progress => (float)Projectile.timeLeft / TimeLeftMax;

	/// <summary> The sample NPC instance corresponding to <see cref="NPCWhoAmI"/>. Should <b>NOT</b> be modified. </summary>
	public NPC NPC { get; private set; }
	public int NPCWhoAmI
	{
		get => (int)Projectile.ai[0];
		set => Projectile.ai[0] = value;
	}

	public static readonly SoundStyle DustScatter = new("SpiritReforged/Assets/SFX/NPCDeath/Dust_1")
	{
		Pitch = .75f,
		PitchVariance = .25f
	};

	public static readonly SoundStyle Fire = new("SpiritReforged/Assets/SFX/NPCDeath/Fire_1")
	{
		Pitch = .25f,
		PitchVariance = .2f
	};

	public static readonly SoundStyle LiquidExplosion = new("SpiritReforged/Assets/SFX/Projectile/Explosion_Liquid")
	{
		Pitch = .8f,
		PitchVariance = .2f
	};

	public static readonly int TimeLeftMax = (int)(1f / DecayRate);
	private static readonly HashSet<Projectile> ToDraw = [];

	public static void StartEffect(NPC npc) => Projectile.NewProjectileDirect(npc.GetSource_Death(), npc.position, npc.velocity, ModContent.ProjectileType<UndeadDecay>(), 0, 0, ai0: npc.whoAmI);

	public override void Load() => On_Main.DrawNPCs += DrawQueue;
	public override void SetDefaults()
	{
		Projectile.Size = Vector2.Zero;
		Projectile.penetrate = -1;
		Projectile.ignoreWater = true;
		Projectile.timeLeft = TimeLeftMax;
	}

	public override void AI()
	{
		if (Projectile.timeLeft == TimeLeftMax) //Just spawned
		{
			NPC = (NPC)Main.npc[NPCWhoAmI].Clone();
			Projectile.Size = NPC.Size;

			if (!Main.dedServ)
				StartIgnite();
		}

		if (Projectile.timeLeft == 1)
		{
			if (!Main.dedServ)
				SoundEngine.PlaySound(DustScatter, Projectile.Center);
		}

		if (!NPC.noGravity)
			Projectile.velocity.Y += 0.05f; //Pseudo-gravity

		Projectile.velocity *= 0.95f;

		if (!Main.dedServ)
		{
			var dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Asphalt, Scale: Main.rand.NextFloat(.5f, 1.5f));
			dust.velocity = -Projectile.velocity;
			dust.noGravity = true;
			dust.color = new Color(25, 20, 20);
		}
	}

	public override bool OnTileCollide(Vector2 oldVelocity) => false;
	public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac) => !(fallThrough = false);

	/// <summary> Visual and sound effects for burning away. </summary>
	private void StartIgnite()
	{
		SoundEngine.PlaySound(Fire, Projectile.Center);
		SoundEngine.PlaySound(LiquidExplosion, Projectile.Center);

		var pos = Projectile.Center;
		for (int i = 0; i < 3; i++)
			ParticleOrchestrator.SpawnParticlesDirect(ParticleOrchestraType.AshTreeShake, new ParticleOrchestraSettings() with { PositionInWorld = pos });

		ParticleHandler.SpawnParticle(new Particles.LightBurst(Projectile.Center, 0, Color.Goldenrod, Projectile.scale * .8f, 10));

		for (int i = 0; i < 15; i++)
		{
			ParticleHandler.SpawnParticle(new Particles.GlowParticle(Projectile.Center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f),
				Color.White, Color.Lerp(Color.Goldenrod, Color.Orange, Main.rand.NextFloat()), 1, Main.rand.Next(10, 20), 8));
		}
	}

	public override bool PreDraw(ref Color lightColor) => ToDraw.Add(Projectile);
	private static void DrawQueue(On_Main.orig_DrawNPCs orig, Main self, bool behindTiles)
	{
		const float spotScale = .5f;

		orig(self, behindTiles);

		if (ToDraw.Count == 0)
			return; //Nothing to draw; don't restart the spritebatch

		Main.spriteBatch.End();
		Main.spriteBatch.Begin(SpriteSortMode.Immediate, default, SamplerState.PointClamp, null, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);

		var effect = AssetLoader.LoadedShaders["NoiseFade"];

		foreach (var projectile in ToDraw) //Draw all shader-affected NPCs
		{
			var inst = projectile.ModProjectile as UndeadDecay;
			if (inst?.NPC is null)
				continue;

			float decayTime = inst.Progress;

			var npc = inst.NPC;
			npc.Center = projectile.Center;

			effect.Parameters["power"].SetValue(decayTime * 50f);
			effect.Parameters["size"].SetValue(new Vector2(1, Main.npcFrameCount[inst.NPC.type]) * spotScale);
			effect.Parameters["noiseTexture"].SetValue(AssetLoader.LoadedTextures["vnoise"].Value);
			effect.Parameters["tint"].SetValue(Color.Black.ToVector4());
			effect.CurrentTechnique.Passes[0].Apply();

			Main.instance.DrawNPCDirect(Main.spriteBatch, npc, npc.behindTiles, Main.screenPosition);
		}

		Main.spriteBatch.End();
		Main.spriteBatch.Begin();

		ToDraw.Clear();
	}
}