using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.PlayerCommon;

namespace SpiritReforged.Content.Forest.Safekeeper;

public class UndeadNPC : GlobalNPC
{
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
			case 2:
				{
					if (args[0] is int customType)
						return UndeadTypes.Add(customType);
					else
						throw new ArgumentException("AddUndead parameter 0 should be an int!");
				}
			case 3:
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