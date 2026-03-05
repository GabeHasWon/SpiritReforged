using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Content.Particles;
using Terraria.Audio;
using Terraria.GameContent.Drawing;

namespace SpiritReforged.Content.Forest.ArcaneNecklace;

internal class ArcaneNecklaceGlobalNPC : GlobalNPC
{
	public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
	{
		if (item.CountsAsClass(DamageClass.Magic))
			TryDropStar(player, npc);
	}

	public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
	{
		if (projectile.CountsAsClass(DamageClass.Magic))
			TryDropStar(Main.player[projectile.owner], npc);
	}

	private static void TryDropStar(Player player, NPC target)
	{
		const int scans = 20;

		if (target.type == NPCID.TargetDummy)
			return;

		if ((player.HasEquip<ArcaneNecklaceGold>() || player.HasEquip<ArcaneNecklacePlatinum>()) && player.statMana < player.statManaMax2 && Main.rand.NextBool(15))
		{
			var pos = target.Center;

			for (int i = 0; i < scans; i++)
			{
				if (!Collision.SolidCollision(pos = RandomPos(), 2, 2))
					break;
			}

			SoundEngine.PlaySound(SoundID.DD2_LightningBugZap with { PitchRange = (.65f, .8f) }, target.Center);
			SoundEngine.PlaySound(SoundID.Item158 with { Pitch = 1f });
			SoundEngine.PlaySound(SoundID.DD2_DarkMageHealImpact with { Pitch = 1f });

			ParticleHandler.SpawnParticle(new TexturedPulseCircle(pos, Color.RoyalBlue.Additive() * .75f, 3, 50, 40, "supPerlin", new Vector2(2), Common.Easing.EaseFunction.EaseCircularOut));
			for (int i = 0; i < 3; i++)
				ParticleOrchestrator.SpawnParticlesDirect(ParticleOrchestraType.StardustPunch, new ParticleOrchestraSettings() { PositionInWorld = pos });

			target.DropItemInstanced(pos, Vector2.Zero, ItemID.Star);
		}

		Vector2 RandomPos()
		{
			float length = target.Hitbox.Size().Length();
			return target.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(length - 20, length + 10);
		}
	}
}
