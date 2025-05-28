using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.Audio;

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
	public override bool InstancePerEntity => true;

	private static Asset<Texture2D> icon;

	private int _storedPlayer;

	public void SetMark(NPC npc, Projectile projectile)
	{
		_storedPlayer = projectile.owner;
		if (!npc.HasBuff<JinxMark>())
			npc.AddBuff(ModContent.BuffType<JinxMark>(), (int)(JinxBowMinion.MARK_COOLDOWN * JinxBowMinion.MARK_LINGER_RATIO));

		else
		{
			int index = npc.FindBuffIndex(ModContent.BuffType<JinxMark>());
			if (index > -1)
				npc.buffTime[index] = JinxBowMinion.MARK_COOLDOWN;
		}
	}

	public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
	{
		Player markPlayer = Main.player[_storedPlayer];
		bool playerHasBow = markPlayer.ownedProjectileCounts[ModContent.ProjectileType<JinxBowMinion>()] == 1;
		bool isSameOwner = projectile.owner == _storedPlayer;
		if (npc.HasBuff<JinxMark>() && projectile.DamageType == DamageClass.Ranged && playerHasBow && isSameOwner)
		{
			int index = npc.FindBuffIndex(ModContent.BuffType<JinxMark>());
			if (index > -1)
				npc.DelBuff(index);

			foreach(Projectile proj in Main.ActiveProjectiles)
			{
				if(proj.owner == _storedPlayer)
				{
					if(proj.ModProjectile is JinxBowMinion jinxBow)
					{
						jinxBow.DoEmpoweredShot(npc);

						if(!Main.dedServ)
						{
							ParticleHandler.SpawnParticle(new ImpactLinePrim(npc.Center, Vector2.Zero, Color.MediumPurple.Additive(), new(0.75f, 3), 12, 1));
							ParticleHandler.SpawnParticle(new LightBurst(npc.Center, Main.rand.NextFloatDirection(), Color.MediumPurple.Additive(), 0.66f, 20));

							for (int i = 0; i < 10; i++)
							{
								Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(0.5f, 4);
								float scale = Main.rand.NextFloat(0.3f, 0.7f);
								int lifeTime = Main.rand.Next(12, 40);
								static void DelegateAction(Particle p) => p.Velocity *= 0.9f;

								ParticleHandler.SpawnParticle(new GlowParticle(npc.Center, velocity, Color.MediumPurple.Additive(), scale, lifeTime, 1, DelegateAction));
								ParticleHandler.SpawnParticle(new GlowParticle(npc.Center, velocity, Color.White.Additive(), scale, lifeTime, 1, DelegateAction));
							}
						}

						break;
					}
				}
			}

			npc.netUpdate = true;
		}
	}

	public override void Load() => icon = DrawHelpers.RequestLocal(GetType(), "JinxMark", false);

	public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) //Draw the mark icon
	{
		if (!npc.dontTakeDamage && npc.HasBuff<JinxMark>())
		{
			const int maxBuffTime = (int)(JinxBowMinion.MARK_COOLDOWN * JinxBowMinion.MARK_LINGER_RATIO);

			var source = icon.Frame();

			int index = npc.FindBuffIndex(ModContent.BuffType<JinxMark>());
			int time = (index == -1) ? 0 : npc.buffTime[index];
			var compoundSineEase = EaseFunction.CompoundEase([EaseFunction.EaseQuadIn, EaseFunction.EaseSine, EaseFunction.EaseCircularOut, EaseFunction.EaseQuadOut]);
			float buffTimeProgress = time / (float)maxBuffTime;
			var color = Color.White.Additive() * compoundSineEase.Ease(buffTimeProgress);
			float scale = MathHelper.Lerp(0.8f, 1.1f, compoundSineEase.Ease(buffTimeProgress));
			scale += (EaseFunction.EaseSine.Ease((Main.GlobalTimeWrappedHourly * 1.4f) % 1) - 0.5f) * 0.03f;

			spriteBatch.Draw(icon.Value, npc.Center - Main.screenPosition + new Vector2(0, npc.gfxOffY), source, color, 0, source.Size() / 2, scale, default, 0);
		}
	}
}