using SpiritReforged.Common.ConfigurationCommon;

namespace SpiritReforged.Common.ProjectileCommon;

[ReinitializeDuringResizeArrays]
internal class HeldProjectileSet : ModSystem
{
	public static readonly bool[] HeldProjectile = ProjectileID.Sets.Factory.CreateNamedSet(SpiritReforgedMod.Instance, "HeldProjectile")
		.Description("If a projectile is a held projectile.").RegisterBoolSet(false, ProjectileID.DarkLance, ProjectileID.Trident, ProjectileID.Spear, ProjectileID.CobaltChainsaw, 
		ProjectileID.MythrilChainsaw, ProjectileID.CobaltDrill, ProjectileID.MythrilDrill, ProjectileID.AdamantiteChainsaw, ProjectileID.AdamantiteDrill, ProjectileID.MythrilHalberd, 
		ProjectileID.AdamantiteGlaive, ProjectileID.CobaltNaginata, ProjectileID.Gungnir, ProjectileID.Hamdrax, ProjectileID.MushroomSpear, ProjectileID.TheRottedFork, 
		ProjectileID.PalladiumDrill, ProjectileID.PalladiumChainsaw, ProjectileID.OrichalcumHalberd, ProjectileID.OrichalcumDrill, ProjectileID.OrichalcumChainsaw, 
		ProjectileID.TitaniumDrill, ProjectileID.TitaniumChainsaw, ProjectileID.ChlorophytePartisan, ProjectileID.ChlorophyteDrill, ProjectileID.ChlorophyteChainsaw, 
		ProjectileID.ChlorophyteJackhammer, ProjectileID.NebulaChainsaw, ProjectileID.CorruptYoyo, ProjectileID.PalladiumPike, ProjectileID.TitaniumTrident, ProjectileID.JoustingLance,
		ProjectileID.NorthPoleWeapon, ProjectileID.ObsidianSwordfish, ProjectileID.Swordfish, ProjectileID.SawtoothShark, ProjectileID.VortexChainsaw, ProjectileID.VortexDrill, 
		ProjectileID.NebulaDrill, ProjectileID.SolarFlareChainsaw, ProjectileID.SolarFlareDrill, ProjectileID.LaserMachinegun, ProjectileID.ScutlixLaserCrosshair, 
		ProjectileID.DrillMountCrosshair, ProjectileID.ChargedBlasterCannon, ProjectileID.ButchersChainsaw, ProjectileID.Code1, ProjectileID.MedusaHead, ProjectileID.WoodYoyo, 
		ProjectileID.CrimsonYoyo, ProjectileID.JungleYoyo, ProjectileID.Cascade, ProjectileID.Chik, ProjectileID.Code2, ProjectileID.Rally, ProjectileID.Yelets, ProjectileID.RedsYoyo, 
		ProjectileID.ValkyrieYoyo, ProjectileID.Amarok, ProjectileID.HelFire, ProjectileID.Kraken, ProjectileID.TheEyeOfCthulhu, ProjectileID.FormatC, ProjectileID.Gradient,
		ProjectileID.Arkhalis, ProjectileID.PortalGun, ProjectileID.Terrarian, ProjectileID.StardustDrill, ProjectileID.StardustChainsaw, ProjectileID.SolarWhipSword, 
		ProjectileID.Phantasm, ProjectileID.LastPrism, ProjectileID.WireKite, ProjectileID.MonkStaffT1, ProjectileID.MonkStaffT2, ProjectileID.DD2PhoenixBow, ProjectileID.MonkStaffT3, 
		ProjectileID.MonkStaffT3_Alt, ProjectileID.Celeb2Weapon, ProjectileID.ThunderSpear, ProjectileID.Terragrim, ProjectileID.GladiusStab, ProjectileID.RulerStab, 
		ProjectileID.ShadowJoustingLance, ProjectileID.HallowJoustingLance, ProjectileID.PiercingStarlight, ProjectileID.CopperShortswordStab, ProjectileID.TinShortswordStab, 
		ProjectileID.IronShortswordStab, ProjectileID.LeadShortswordStab, ProjectileID.SilverShortswordStab, ProjectileID.TungstenShortswordStab, ProjectileID.GoldShortswordStab, 
		ProjectileID.PlatinumShortswordStab, ProjectileID.HiveFive, ProjectileID.VortexBeater, ProjectileID.Valor, ProjectileID.LaserDrill);

	public static readonly bool[] SkipAutoHeldCheck = ProjectileID.Sets.Factory.CreateNamedSet(SpiritReforgedMod.Instance, nameof(SkipAutoHeldCheck))
		.Description("Whether this projectile ID skips the auto-check for Reforged's automatic held projectile detection system.")
		.RegisterBoolSet(false);

	private static bool _populating = false;

	public override void Load()
	{
		// TODO: Autoload held proj
		//if (ModContent.GetInstance<ReforgedServerConfig>().AutoloadHeldProjectiles)
		//	On_Projectile.Kill += SkipKillForPopulation;
	}

	// Skip kills so that heldProj is set by the projectile without being dead, if possible
	private static void SkipKillForPopulation(On_Projectile.orig_Kill orig, Projectile self)
	{
		if (_populating) 
			return;

		orig(self);
	}

	public override void PostSetupContent()
	{
		// TODO: Autoload held proj
		//if (ModContent.GetInstance<ReforgedServerConfig>().AutoloadHeldProjectiles)
		//	ScanIdRange(ProjectileID.Count, ProjectileLoader.ProjectileCount, false);
	}

	/// <summary>
	/// Scans a range of projectile IDs to either create a string for copy-pasting/updating the default set or for automatically setting projectiles' held proj value.
	/// </summary>
	private static void ScanIdRange(int min, int max, bool createString)
	{
		_populating = true;

		if (createString)
		{
			string s = "";
			Player plr = Main.player[0];
			plr.channel = true;
			plr.dead = false;

			IterateProjectiles(min, max, type => s += type < ProjectileID.Count ? $"ProjectileID.{ProjectileID.Search.GetName(type)}, " : ModContent.GetModProjectile(type).Name + ", ");
		}
		else
		{
			Player plr = Main.player[0];
			plr.channel = true;
			plr.dead = false;

			IterateProjectiles(min, max, type => HeldProjectile[type] = true);
		}

		_populating = false;
	}

	private static void IterateProjectiles(int min, int max, Action<int> success)
	{
		for (int i = min; i < max; ++i)
		{
			if (HeldProjectile[i] || SkipAutoHeldCheck[i])
				continue;

			Main.player[0].heldProj = -1;

			Projectile proj = new Projectile();
			proj.whoAmI = 0;
			proj.active = true;
			proj.SetDefaults(i);
			proj.owner = 0;

			if (ProjectileID.Sets.IsAWhip[i])
			{
				HeldProjectile[i] = true;
				continue;
			}
			else
			{
				int aiStyle = ContentSamples.ProjectilesByType[i].aiStyle;

				if (aiStyle is ProjAIStyleID.Yoyo or ProjAIStyleID.Spear or ProjAIStyleID.Flail or ProjAIStyleID.ShortSword)
				{
					HeldProjectile[i] = true;
					continue;
				}
			}

			try
			{
				proj.AI();
			}
			catch (Exception e)
			{
				SpiritReforgedMod.Instance.Logger.Debug("Caught exception during Held Projectile autoloading.\nThis contributes to long loads.\n" +
					$"Report this to {(proj.ModProjectile.Mod is Mod mod ? mod.DisplayNameClean : "Spirit Reforged")} for compatibility work\n\n" + e.ToString());
			}

			if (Main.player[0].heldProj != -1)
				success.Invoke(i);
		}
	}
}
