namespace SpiritReforged.Common.DebuffOverhaul;

public abstract class DoTExtension : BuffExtension
{
	public const string VanillaTextures = SpiritReforgedMod.ModName + "/Common/DebuffOverhaul/Textures/";

	/// <param name="Scalability"> Determines how well this buff scales from weapon damage. </param>
	/// <param name="DamageLimit"> The maximum amount of damage this buff can deal per second. </param>
	public readonly record struct Settings(float Scalability, int DamageLimit);

    public abstract Settings LocalSettings { get; }

    protected float _damagePerSecond;
    private bool _justApplied;

    //NPC.lastInteraction is not set before OnApply, so instead of calling CountPlayerDamage here, delay the task to just before the value is used in UpdateLifeRegen
    protected override void OnApply(bool reApplied) => _justApplied = true;

    public override void UpdateLifeRegen(ref int damage)
    {
        if (_justApplied && NPC.AnyInteractions())
        {
            CountPlayerDamage();
            _justApplied = false;
        }

        NPC.lifeRegen -= (int)(_damagePerSecond * 2);
    }

    private float CountPlayerDamage()
    {
        Player player = Main.player[NPC.lastInteraction];
        float increase = Main.DamageVar(player.HeldItem.damage, player.luck) * LocalSettings.Scalability;

        return _damagePerSecond = Math.Min(_damagePerSecond + increase, LocalSettings.DamageLimit);
    }
}