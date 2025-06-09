using Terraria.DataStructures;

namespace SpiritReforged.Common.PlayerCommon;

public class PlayerEvents : ModPlayer
{
	public static event Action<Player> OnKill;
	public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource) => OnKill?.Invoke(Player);
}