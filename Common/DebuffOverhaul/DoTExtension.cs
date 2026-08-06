using System.IO;

namespace SpiritReforged.Common.DebuffOverhaul;

public abstract class DoTExtension : BuffExtension
{
	public const float VanillaMaximum = 0.5f;
	public const float VanillaScaling = 0.25f;

	public const string VanillaTextures = SpiritReforgedMod.ModName + "/Common/DebuffOverhaul/Textures/";

	/// <param name="Scalability"> Determines how well this buff scales from weapon damage. </param>
	/// <param name="DamageLimit"> The maximum amount of damage this buff can deal per second. </param>
	/// <param name="Stackable"> Whether damage will stack per application. </param>
	/// <param name="ScalingBehaviour"> The action this buff takes to passively scale. null if none. </param>
	public readonly record struct BuffSettings(float Scalability, int DamageLimit, bool Stackable = false, Action ScalingBehaviour = null);

    public abstract BuffSettings Settings { get; }

    public float damagePerSecond;
    protected bool _reapplyDamage;
	protected int _timeActive;

	#region scaling behaviours
	public void FireScaling()
	{
		const int scaling_max = 600;
		const float scaling_markiplier = 1.5f;

		if (_timeActive <= scaling_max && _timeActive % (scaling_max / 2) == 0)
			damagePerSecond = Math.Min(damagePerSecond * scaling_markiplier, Settings.DamageLimit); //Increase damage in waves
	}

	public void PoisonScaling()
	{
		const int scaling_rate = 120;
		const float scaling_strength = 0.01f;

		damagePerSecond = Math.Min(damagePerSecond + NPC.lifeMax / (float)scaling_rate * scaling_strength, Settings.DamageLimit);
	}
	#endregion

	//NPC.lastInteraction is not set before OnApply, so instead of calling CountPlayerDamage here, delay the task to just before the value is used in UpdateLifeRegen
	protected override void OnApply(bool reApplied) => _reapplyDamage = Settings.Stackable || !reApplied;

    public override void UpdateLifeRegen(ref int damage)
    {
		if (_reapplyDamage && NPC.AnyInteractions())
		{
			CountPlayerDamage();
			_reapplyDamage = false;
		}

		NPC.lifeRegen -= (int)(damagePerSecond * 2);
		Settings.ScalingBehaviour?.Invoke();
	}

    protected float CountPlayerDamage()
    {
        Player player = Main.player[NPC.lastInteraction];
        float increase = Main.DamageVar(player.HeldItem.damage, player.luck) * Settings.Scalability;

        return damagePerSecond = Math.Min(damagePerSecond + increase, Settings.DamageLimit);
    }

	public override void NetSend(BinaryWriter writer) => writer.Write(damagePerSecond);

	public override void NetReceive(BinaryReader reader) => damagePerSecond = reader.ReadSingle();
}