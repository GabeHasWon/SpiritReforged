namespace SpiritReforged.Common.ProjectileCommon;

[ReinitializeDuringResizeArrays]
internal class SpiritProjectileSets : ModSystem
{
	public static readonly bool?[] HeldProjectile = ProjectileID.Sets.Factory.CreateNamedSet(SpiritReforgedMod.Instance, "HeldProjectile")
		.Description("If a projectile is a held projectile.").RegisterCustomSet<bool?>(null);

	public override void PostSetupContent()
	{
		ScanIdRange(0, ProjectileID.Count, true);
		ScanIdRange(ProjectileID.Count, ProjectileLoader.ProjectileCount, false);
	}

	private static void ScanIdRange(int min, int max, bool createString)
	{
		if (createString)
		{
			string s = "";
			Player plr = Main.LocalPlayer;

			for (int i = min; i < max; ++i)
			{
				plr.heldProj = -1;

				Projectile proj = new Projectile();
				proj.whoAmI = 0;
				proj.SetDefaults(i);
				proj.AI();

				if (plr.heldProj != -1)
					s += $"ProjectileID.{ProjectileID.Search.GetName(i)}, true";
				else
					s += $"ProjectileID.{ProjectileID.Search.GetName(i)}, false, ";
			}

			s = s[..^2];
		}
		else
		{
			Player plr = Main.LocalPlayer;

			for (int i = min; i < max; ++i)
			{
				plr.heldProj = -1;

				Projectile proj = new Projectile();
				proj.whoAmI = 0;
				proj.SetDefaults(i);
				proj.AI();

				if (plr.heldProj != -1)
					HeldProjectile[i] = true;
				else
					HeldProjectile[i] = false;
			}
		}
	}
}
