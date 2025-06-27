using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Content.Forest.JinxBow;

public class JinxMark : ModBuff
{
	public override string Texture => "Terraria/Images/Buff";

	public override void SetStaticDefaults()
	{
		Main.debuff[Type] = true;
		Main.buffNoSave[Type] = true;
	}
}

public class JinxMarkNPC : GlobalNPC
{
	private static readonly Asset<Texture2D> Icon = DrawHelpers.RequestLocal(typeof(JinxMarkNPC), "JinxMark", false);

	public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
	{
		//Stop if the npc doesn't have the mark
		if (!npc.HasBuff<JinxMark>())
			return;

		//If the npc is killed by the projectile, prevent further code from being run and reset cooldowns
		if (damageDone > npc.life)
		{
			ClearMarkBuff(npc);
			ResetBowCooldown();

			return;
		}

		//Finally, stop if the projectile isn't a ranged projectile or if the owner doesn't have a jinxbow active
		var owner = Main.player[projectile.owner];
		if (projectile.DamageType != DamageClass.Ranged || owner.ownedProjectileCounts[ModContent.ProjectileType<JinxBowMinion>()] < 1)
			return;

		ClearMarkBuff(npc);

		//Iterate through projectiles to find the owner's jinxbow, then set its target
		foreach (Projectile proj in Main.ActiveProjectiles)
		{
			if (proj.ModProjectile is not JinxBowMinion jinxBow || proj.owner != projectile.owner)
				continue;

			jinxBow.EmpoweredShotTarget = npc.whoAmI; //Queue up a target
			proj.netUpdate = true;

			break;
		}
	}

	private static void ClearMarkBuff(NPC npc)
	{
		int buffIndex = npc.FindBuffIndex(ModContent.BuffType<JinxMark>());
		npc.DelBuff(buffIndex);
	}

	private static void ResetBowCooldown()
	{
		foreach (Projectile proj in Main.ActiveProjectiles)
		{
			if (proj.ModProjectile is not JinxBowMinion jinxBow)
				continue;

			jinxBow.MarkCooldown = 0;
			proj.netUpdate = true;

			break;
		}
	}

	public override void OnKill(NPC npc)
	{
		if (!npc.HasBuff<JinxMark>())
			return;

		ResetBowCooldown();
	}

	public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) //Draw the mark icon
	{
		if (npc.dontTakeDamage || !npc.HasBuff<JinxMark>())
			return;

		const int maxBuffTime = (int)(JinxBowMinion.MARK_COOLDOWN * JinxBowMinion.MARK_LINGER_RATIO);

		var source = Icon.Frame();

		int index = npc.FindBuffIndex(ModContent.BuffType<JinxMark>());
		int time = (index == -1) ? 0 : npc.buffTime[index];
		var compoundSineEase = EaseFunction.CompoundEase([EaseFunction.EaseQuadIn, EaseFunction.EaseSine, EaseFunction.EaseCircularOut, EaseFunction.EaseQuadOut]);
		float buffTimeProgress = time / (float)maxBuffTime;
		var color = Color.White.Additive(150) * compoundSineEase.Ease(buffTimeProgress);
		float scale = MathHelper.Lerp(0.8f, 1.1f, compoundSineEase.Ease(buffTimeProgress));
		scale += (EaseFunction.EaseSine.Ease(Main.GlobalTimeWrappedHourly * 1.4f % 1) - 0.5f) * 0.03f;

		spriteBatch.Draw(Icon.Value, npc.Center - Main.screenPosition + new Vector2(0, npc.gfxOffY), source, color, 0, source.Size() / 2, scale, default, 0);
	}
}