using SpiritReforged.Common.PlayerCommon;
using System.Linq;

namespace SpiritReforged.Content.Underground.Items.ExplorerTreads;

internal class ExplorerTreadsPlayer : ModPlayer
{
	public override bool CanBeHitByProjectile(Projectile proj)
	{
		int[] trapTypes = [ProjectileID.PoisonDart, ProjectileID.PoisonDartTrap, ProjectileID.SporeTrap, ProjectileID.SporeTrap2, ProjectileID.SpearTrap, ProjectileID.GeyserTrap, ProjectileID.FlamethrowerTrap, ProjectileID.FlamesTrap, ProjectileID.SpikyBallTrap, ProjectileID.RollingCactus, ProjectileID.RollingCactusSpike, ProjectileID.Boulder];
		if (Player.HasEquip<ExplorerTreadsItem>() && trapTypes.Contains(proj.type))
		{
			if (ExplorerTreadsItem.DoDodgeEffect(Player, Player.GetSource_OnHurt(proj)))
				return false;
		}

		return true;
	}

	public override bool FreeDodge(Player.HurtInfo info)
	{
		if (Player.HasEquip<ExplorerTreadsItem>() && info.DamageSource.SourceOtherIndex == 3) //Spikes
		{
			if (ExplorerTreadsItem.DoDodgeEffect(Player, Player.GetSource_OnHurt(info.DamageSource)))
				return true;
		}

		return false;
	}
}