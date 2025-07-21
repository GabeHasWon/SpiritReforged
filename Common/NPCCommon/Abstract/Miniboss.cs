using Terraria.Audio;

namespace SpiritReforged.Common.NPCCommon.Abstract;

public abstract class Miniboss : ModNPC
{
	public static readonly SoundStyle Downed = new("SpiritReforged/Assets/SFX/NPCDeath/DownedMiniboss");

	/// <summary><inheritdoc cref="ModNPC.HitEffect"/><para/>
	/// Plays the miniboss death sound effect by default. </summary>
	public override void HitEffect(NPC.HitInfo hit)
	{
		if (NPC.life <= 0 && !Main.dedServ)
			SoundEngine.PlaySound(Downed, NPC.Center);
	}
}