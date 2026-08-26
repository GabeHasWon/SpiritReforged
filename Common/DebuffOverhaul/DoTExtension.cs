namespace SpiritReforged.Common.DebuffOverhaul;

public abstract class DoTExtension : BuffExtension
{
	public const string VanillaTextures = SpiritReforgedMod.ModName + "/Common/DebuffOverhaul/Textures/";

	// /// <param name="Scalability"> Determines how well this buff scales from weapon damage. </param>
	// /// <param name="DamageLimit"> The maximum amount of damage this buff can deal per second. </param>
	// /// <param name="Stackable"> Whether damage will stack per application. </param>
	/// <param name="Category"> Which categories this buff falls under, determining damage scaling behaviour. </param>
	public readonly record struct BuffSettings(/*float Scalability, int DamageLimit, bool Stackable = false,*/ Category Category = Category.None);

	[Flags]
	public enum Category
	{
		None = 0,
		Poison = 1,
		Fire = 2,
		Bleeding = 4,
		Electric = 8
	}

    public abstract BuffSettings Settings { get; }

    /*public float damagePerSecond;
    protected bool _reapplyDamage;
	protected int _timeActive;*/

	//NPC.lastInteraction is not set before OnApply, so instead of calling CountPlayerDamage here, delay the task to just before the value is used in UpdateLifeRegen
	//protected override void OnApply(bool reApplied) => _reapplyDamage = Settings.Stackable || !reApplied;

	/*public override void UpdateLifeRegen(ref int damage)
	{
		if (_reapplyDamage && NPC.AnyInteractions())
		{
			CountPlayerDamage();
			_reapplyDamage = false;
		}

		NPC.lifeRegen -= (int)(damagePerSecond * 2);
		_timeActive++;

		if (Settings.Category.HasFlag(Category.Fire))
			FireScaling(ref damage);

		if (Settings.Category.HasFlag(Category.Poison))
			PoisonScaling(ref damage);
	}

	protected float CountPlayerDamage()
	{
		Player player = Main.player[NPC.lastInteraction];
		float increase = Main.DamageVar(player.GetWeaponDamage(player.HeldItem), player.luck) * Settings.Scalability;

		return damagePerSecond = Math.Min(damagePerSecond + increase, Settings.DamageLimit);
	}

	public override void NetSend(BinaryWriter writer) => writer.Write(damagePerSecond);

	public override void NetReceive(BinaryReader reader) => damagePerSecond = reader.ReadSingle();*/

	/*#region scaling behaviours
	public void FireScaling(ref int damage)
	{
		const int scaling_max = 900;
		const float scaling_multiplier = 2f;

		if (_timeActive <= scaling_max && _timeActive % (scaling_max / 3) == 0)
			damagePerSecond = Math.Min(damagePerSecond * scaling_multiplier, Settings.DamageLimit); //Increase damage in waves

		damage = (int)(damagePerSecond * 2) + Main.rand.Next(10);
	}

	public void PoisonScaling(ref int damage)
	{
		const int scaling_rate = 240;
		const float scaling_strength = 0.01f;

		damagePerSecond = Math.Min(damagePerSecond + NPC.lifeMax / (float)scaling_rate * scaling_strength, Settings.DamageLimit);
		damage = NPC.lifeMax / 30 + Main.rand.Next(10);
	}
	#endregion*/
}