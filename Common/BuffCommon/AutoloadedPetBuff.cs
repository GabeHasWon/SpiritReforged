namespace SpiritReforged.Common.BuffCommon;

internal class AutoPetProjectile : GlobalProjectile
{
	/// <summary> Automatically sets pet timeLeft according to current buffs. </summary>
	public static void PetFlag(Projectile projectile)
	{
		var player = Main.player[projectile.owner];

		if (player.HasBuff(AutoloadedPetBuff.PetBuff[projectile.type]))
			projectile.timeLeft = 2;
	}

	public override bool PreAI(Projectile projectile)
	{
		if (AutoloadedPetBuff.ProjectileHasBuff(projectile.type)) //Is an autoloaded pet
			PetFlag(projectile);

		return true;
	}
}

[ReinitializeDuringResizeArrays]
internal sealed class AutoloadedPetBuff(string fullName, bool lightPet = false) : AutoloadedBuff(fullName)
{
	public static readonly int[] PetBuff = ProjectileID.Sets.Factory.CreateNamedSet(SpiritReforgedMod.Instance, "PetBuff").Description("Maps projectile ID to buff ID")
		.RegisterIntSet(defaultState: -1);

	public int PetType { get; private set; }

	/// <summary>
	/// Returns whether the given projectile has a buff associated with it.
	/// </summary>
	public static bool ProjectileHasBuff(int projectileId) => PetBuff[projectileId] != -1;

	public override void SetStaticDefaults()
	{
		Main.buffNoTimeDisplay[Type] = true;

		if (lightPet)
			Main.lightPet[Type] = true;
		else
			Main.vanityPet[Type] = true;

		if (Mod.TryFind(SourceName, out ModProjectile p))
		{
			PetType = p.Type;
			PetBuff[PetType] = Type;
		}
	}

	public override void Update(Player player, ref int buffIndex)
	{
		player.buffTime[buffIndex] = 18000;

		if (player.whoAmI == Main.myPlayer && player.ownedProjectileCounts[PetType] == 0)
			Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.Center, Vector2.Zero, PetType, 0, 0f, player.whoAmI);
	}
}

[AttributeUsage(AttributeTargets.Class)]
internal class AutoloadPetBuffAttribute : AutoloadBuffAttribute
{
	public bool LightPet = false;

	public override void AddContent(Type type, Mod mod)
	{
		var buff = new AutoloadedPetBuff(type.FullName, LightPet);
		mod.AddContent(buff);

		BuffAutoloader.SourceToType.Add(type, buff.Type);
	}
}