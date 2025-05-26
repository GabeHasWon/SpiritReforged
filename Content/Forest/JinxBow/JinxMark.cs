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
		const int markTime = 60 * 30;
		_storedPlayer = projectile.owner;
		if (!npc.HasBuff<JinxMark>())
			npc.AddBuff(ModContent.BuffType<JinxMark>(), markTime);

		else
		{
			int index = npc.FindBuffIndex(ModContent.BuffType<JinxMark>());
			if (index > -1)
				npc.buffTime[index] = markTime;
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
		const int fadeout = 20; //The number of ticks this effect fades out for

		if (!npc.dontTakeDamage && npc.HasBuff<JinxMark>())
		{
			var source = icon.Frame();

			int index = npc.FindBuffIndex(ModContent.BuffType<JinxMark>());
			int time = (index == -1) ? 0 : npc.buffTime[index];
			var color = (Color.White * .75f * Math.Min(time / (float)fadeout, 1)).Additive();

			spriteBatch.Draw(icon.Value, npc.Center - Main.screenPosition + new Vector2(0, npc.gfxOffY), source, color, 0, source.Size() / 2, 1, default, 0);
		}
	}
}