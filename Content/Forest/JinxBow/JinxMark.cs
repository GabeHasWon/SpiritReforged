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
		if (!npc.HasBuff<JinxMark>() || projectile.DamageType != DamageClass.Ranged)
			return;

		var owner = Main.player[projectile.owner];
		owner.ClearBuff(ModContent.BuffType<JinxMark>());

		if (owner.ownedProjectileCounts[ModContent.ProjectileType<JinxBowMinion>()] < 1)
			return;

		foreach (Projectile proj in Main.ActiveProjectiles)
		{
			if (proj.ModProjectile is not JinxBowMinion jinxBow)
				continue;

			jinxBow.EmpoweredShotTarget = npc.whoAmI; //Queue up a target
			projectile.netUpdate = true;

			break;
		}
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
		var color = Color.White.Additive() * compoundSineEase.Ease(buffTimeProgress);
		float scale = MathHelper.Lerp(0.8f, 1.1f, compoundSineEase.Ease(buffTimeProgress));
		scale += (EaseFunction.EaseSine.Ease(Main.GlobalTimeWrappedHourly * 1.4f % 1) - 0.5f) * 0.03f;

		spriteBatch.Draw(Icon.Value, npc.Center - Main.screenPosition + new Vector2(0, npc.gfxOffY), source, color, 0, source.Size() / 2, scale, default, 0);
	}
}