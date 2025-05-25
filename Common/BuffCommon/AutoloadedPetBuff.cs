namespace SpiritReforged.Common.BuffCommon;

internal class AutoPetProjectile : GlobalProjectile
{
	/// <summary> Automatically sets pet timeLeft according to current buffs. </summary>
	public static void PetFlag(Projectile projectile)
	{
		var player = Main.player[projectile.owner];

		if (player.HasBuff(AutoloadedPetBuff.Registered[projectile.type]))
			projectile.timeLeft = 2;
	}

	public override bool PreAI(Projectile projectile)
	{
		if (AutoloadedPetBuff.Registered.ContainsKey(projectile.type)) //Is an autoloaded pet
			PetFlag(projectile);

		return true;
	}
}

internal sealed class AutoloadedPetBuff(string fullName, bool lightPet = false) : AutoloadedBuff(fullName)
{
	/// <summary> Projectile type to buff type. Populated after <see cref="ModBuff.SetStaticDefaults"/>. </summary>
	public static readonly Dictionary<int, int> Registered = [];

	public int PetType { get; private set; }

	public override void SetStaticDefaults()
	{
		Main.buffNoTimeDisplay[Type] = true;

		if (lightPet)
			Main.lightPet[Type] = true;
		else
			Main.vanityPet[Type] = true;

		if (Mod.TryFind(SourceName, out ModProjectile p))
			Registered.Add(PetType = p.Type, Type);
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