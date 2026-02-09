using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using Terraria.Audio;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Content.Forest.Graveyard;

public class UndeadNPC : GlobalNPC
{
	/// <summary> Handles visuals and sounds for <see cref="UndeadNPC"/> death effects. See <see cref="StartEffect"/> for spawning. </summary>
	public sealed class UndeadDecay : ModProjectile
	{
		private const float DecayRate = 0.025f;

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
			Pitch = 0.75f,
			PitchVariance = 0.25f
		};

		public static readonly SoundStyle Fire = new("SpiritReforged/Assets/SFX/NPCDeath/Fire_1")
		{
			Pitch = 0.25f,
			PitchVariance = 0.2f
		};

		public static readonly SoundStyle LiquidExplosion = new("SpiritReforged/Assets/SFX/Projectile/Explosion_Liquid")
		{
			Pitch = 0.8f,
			PitchVariance = 0.2f
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
				if (!Main.dedServ)
					SoundEngine.PlaySound(DustScatter, Projectile.Center);

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
				ParticleHandler.SpawnParticle(new Particles.GlowParticle(Projectile.Center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f),
					Color.White, Color.Lerp(Color.Goldenrod, Color.Orange, Main.rand.NextFloat()), 1, Main.rand.Next(10, 20), 8));
		}

		public override bool PreDraw(ref Color lightColor) => ToDraw.Add(Projectile);
		private static void DrawQueue(On_Main.orig_DrawNPCs orig, Main self, bool behindTiles)
		{
			const float spotScale = 0.5f;

			orig(self, behindTiles);

			if (ToDraw.Count == 0)
				return; //Nothing to draw; don't restart the spritebatch

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, default, SamplerState.PointClamp, null, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);

			var effect = AssetLoader.LoadedShaders["NoiseFade"].Value;

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

	public override bool InstancePerEntity => true;

	private static readonly HashSet<int> NoDeathAnim = [];

	private static readonly HashSet<int> UndeadTypes = [NPCID.Zombie, NPCID.ZombieDoctor, NPCID.ZombieElf, NPCID.ZombieElfBeard, NPCID.ZombieElfGirl, NPCID.ZombieEskimo, 
		NPCID.ZombieMerman, NPCID.ZombieMushroom, NPCID.ZombieMushroomHat, NPCID.ZombiePixie, NPCID.ZombieRaincoat, NPCID.ZombieSuperman, NPCID.ZombieSweater, 
		NPCID.ZombieXmas, NPCID.ArmedTorchZombie, NPCID.ArmedZombie, NPCID.ArmedZombieCenx, NPCID.ArmedZombieEskimo, NPCID.ArmedZombiePincussion, NPCID.ArmedZombieSlimed, 
		NPCID.ArmedZombieSwamp, NPCID.ArmedZombieTwiggy, NPCID.BaldZombie, NPCID.BloodZombie, NPCID.FemaleZombie, NPCID.MaggotZombie, NPCID.PincushionZombie, 
		NPCID.TheGroom, NPCID.TheBride, NPCID.SlimedZombie, NPCID.SwampZombie, NPCID.TorchZombie, NPCID.TwiggyZombie, NPCID.Drippler, NPCID.Skeleton, NPCID.SkeletonAlien, 
		NPCID.SkeletonArcher, NPCID.SkeletonAstonaut, NPCID.SkeletonTopHat, NPCID.BoneThrowingSkeleton, NPCID.BoneThrowingSkeleton2, NPCID.BoneThrowingSkeleton3, 
		NPCID.BoneThrowingSkeleton4, NPCID.ArmoredSkeleton, NPCID.ArmoredViking, NPCID.BlueArmoredBones, NPCID.BlueArmoredBonesMace, NPCID.BlueArmoredBonesNoPants, 
		NPCID.BlueArmoredBonesSword, NPCID.HellArmoredBones, NPCID.HellArmoredBonesMace, NPCID.HellArmoredBonesSpikeShield, NPCID.HellArmoredBonesSword, 
		NPCID.RustyArmoredBonesAxe, NPCID.RustyArmoredBonesFlail, NPCID.RustyArmoredBonesSword, NPCID.RustyArmoredBonesSwordNoArmor, NPCID.Necromancer, 
		NPCID.NecromancerArmored, NPCID.SkeletonSniper, NPCID.SkeletonCommando, NPCID.RuneWizard, NPCID.Tim, NPCID.BoneLee, NPCID.AngryBones, NPCID.AngryBonesBig, 
		NPCID.AngryBonesBigHelmet, NPCID.AngryBonesBigMuscle, NPCID.UndeadMiner, NPCID.UndeadViking, NPCID.BoneSerpentBody, NPCID.BoneSerpentHead, NPCID.BoneSerpentTail, 
		NPCID.DemonEye, NPCID.DemonEyeOwl, NPCID.DemonEyeSpaceship, NPCID.ServantofCthulhu, NPCID.EyeofCthulhu, NPCID.SkeletronHand, NPCID.SkeletronHead, 
		NPCID.PossessedArmor, NPCID.Paladin, NPCID.DarkCaster, NPCID.RaggedCaster, NPCID.DiabolistRed, NPCID.DiabolistWhite, NPCID.Eyezor, NPCID.CursedSkull, 
		NPCID.GiantCursedSkull, NPCID.Frankenstein, NPCID.DD2SkeletonT1, NPCID.DD2SkeletonT3, NPCID.Poltergeist, NPCID.Wraith, NPCID.FloatyGross, NPCID.Mummy, 
		NPCID.BloodMummy, NPCID.DarkMummy, NPCID.LightMummy, NPCID.Ghost];

	private static bool TrackingGore;

	internal static bool AddCustomUndead(params object[] args)
	{
		switch (args.Length)
		{
			case 1:
				{
					if (args[0] is int customType)
						return UndeadTypes.Add(customType);
					else
						throw new ArgumentException("AddUndead parameter 0 should be an int!");
				}
			case 2:
				{
					if (args[0] is not int customType)
						throw new ArgumentException("AddUndead parameter 0 should be an int!");

					if (args[1] is not bool excludeDeathAnim)
						throw new ArgumentException("AddUndead parameter 1 should be a bool!");

					return UndeadTypes.Add(customType) && (!excludeDeathAnim || NoDeathAnim.Add(customType));
				}
		}

		return false;
	}

	/// <summary> Checks whether the NPC of the given type is considered "undead". </summary>
	internal static bool IsUndeadType(int type) => UndeadTypes.Contains(type) || NPCID.Sets.Zombies[type] || NPCID.Sets.Skeletons[type] || NPCID.Sets.DemonEyes[type];
	private static bool ShouldTrackGore(NPC self) => self.TryGetGlobalNPC(out UndeadNPC _) && Interaction(self).HasEquip<SafekeeperRing>();
	public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => IsUndeadType(entity.type);

	#region detours
	public override void Load()
	{
		On_NPC.HitEffect_HitInfo += TrackGore;
		On_Gore.NewGore_IEntitySource_Vector2_Vector2_int_float += StopGore;
	}

	/// <summary> Tracks on hit gores for removal according to <see cref="ShouldTrackGore"/>. </summary>
	private static void TrackGore(On_NPC.orig_HitEffect_HitInfo orig, NPC self, NPC.HitInfo hit)
	{
		TrackingGore = ShouldTrackGore(self);
		orig(self, hit);
		TrackingGore = false;
	}

	/// <summary> Deactivates the spawned gore according to <see cref="TrackGore"/>. </summary>
	private static int StopGore(On_Gore.orig_NewGore_IEntitySource_Vector2_Vector2_int_float orig, Terraria.DataStructures.IEntitySource source, Vector2 Position, Vector2 Velocity, int Type, float Scale)
	{
		int result = orig(source, Position, Velocity, Type, Scale);

		if (TrackingGore)
			Main.gore[result].active = false; //Instantly deactivate the spawned gore

		return result;
	}
	#endregion

	public override bool CheckDead(NPC npc)
	{
		bool value = base.CheckDead(npc);
		if (value && Main.netMode != NetmodeID.MultiplayerClient && Interaction(npc).HasEquip<SafekeeperRing>() && !NoDeathAnim.Contains(npc.type))
			UndeadDecay.StartEffect(npc);

		return value;
	}

	/// <summary> Returns the index <see cref="NPC.lastInteraction"/> in singleplayer and <see cref="NPC.FindClosestPlayer()"/> in multiplayer. </summary>
	private static Player Interaction(NPC npc)
	{
		//Resort to checking the closest player in mp because lastInteraction is only valid on the server, causing sync difficulties
		if (Main.netMode == NetmodeID.SinglePlayer)
			return Main.player[npc.lastInteraction];
		else
			return Main.player[npc.FindClosestPlayer()];
	}
}