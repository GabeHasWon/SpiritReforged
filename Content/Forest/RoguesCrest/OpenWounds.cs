using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.Audio;

namespace SpiritReforged.Content.Forest.RoguesCrest;

public class OpenWounds : ModBuff
{
	public override string Texture => "Terraria/Images/Buff";

	public override void SetStaticDefaults()
	{
		Main.debuff[Type] = true;
		Main.buffNoSave[Type] = true;
	}
}

internal class OpenWoundsNPC : GlobalNPC
{
	public override bool InstancePerEntity => true;

	private static Asset<Texture2D> icon;
	private short _bleedTime;

	/// <summary> Causes <paramref name="npc"/> to take custom bleed damage for <paramref name="time"/> and consumes <see cref="OpenWounds"/>. </summary>
	public static bool Proc(NPC npc, short time)
	{
		if (npc.TryGetGlobalNPC(out OpenWoundsNPC gNPC))
		{
			gNPC._bleedTime = time;

			int index = npc.FindBuffIndex(ModContent.BuffType<OpenWounds>());
			if (index > -1)
				npc.DelBuff(index);

			if (!Main.dedServ)
			{
				SoundEngine.PlaySound(SoundID.Item101 with { Pitch = .5f }, npc.Center);
				SoundEngine.PlaySound(SoundID.NPCHit2 with { Pitch = .1f }, npc.Center);

				for (int i = 0; i < 8; i++)
					ParticleHandler.SpawnParticle(new GlowParticle(npc.Center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(), Color.White, Color.Red, Main.rand.NextFloat(.3f, .75f), 30, 4));
			}

			return true;
		}

		return false;
	}

	public override void Load() => icon = DrawHelpers.RequestLocal(GetType(), "Wound_Icon", false);

	public override void UpdateLifeRegen(NPC npc, ref int damage)
	{
		const int damagePerTick = 10;

		if (_bleedTime > 0)
			npc.lifeRegen -= damagePerTick * 2;

		_bleedTime = (short)Math.Max(_bleedTime - 1, 0);

		if (!Main.dedServ && _bleedTime > 0 && Main.rand.NextBool(8))
			ParticleHandler.SpawnParticle(new RedBubble(npc.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(20f), Color.White, Main.rand.NextFloat(.5f, 1f), 20));
	}

	public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) //Draw the mark icon
	{
		const int fadeout = 20; //The number of ticks this effect fades out for

		if (!npc.dontTakeDamage && npc.HasBuff<OpenWounds>())
		{
			var source = icon.Frame();

			int index = npc.FindBuffIndex(ModContent.BuffType<OpenWounds>());
			int time = (index == -1) ? 0 : npc.buffTime[index];
			var color = (Color.White * .75f * Math.Min(time / (float)fadeout, 1)).Additive();

			spriteBatch.Draw(icon.Value, npc.Center - Main.screenPosition + new Vector2(0, npc.gfxOffY), source, color, MathHelper.Pi, source.Size() / 2, 1, default, 0);
		}
	}
}

internal class OpenWoundsPlayer : ModPlayer
{
	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (DoesMelee() && target.HasBuff<OpenWounds>())
		{
			short time = 60 * 5;

			if (OpenWoundsNPC.Proc(target, time) && Main.netMode == NetmodeID.MultiplayerClient)
				new BleedTimeData((short)target.whoAmI, time).Send();
		}

		bool DoesMelee() => hit.DamageType.CountsAsClass(DamageClass.Melee);
	}
}

/// <summary> Syncs <see cref="OpenWoundsNPC._bleedTime"/>. </summary>
internal class BleedTimeData : PacketData
{
	private readonly short _npc;
	private readonly short _time;

	public BleedTimeData() { }
	public BleedTimeData(short npc, short time)
	{
		_npc = npc;
		_time = time;
	}

	public override void OnReceive(BinaryReader reader, int whoAmI)
	{
		short npcIndex = reader.ReadInt16();
		short time = reader.ReadInt16();

		if (Main.netMode == NetmodeID.Server)
			new BleedTimeData(npcIndex, time).Send(ignoreClient: whoAmI); //Relay to other clients

		if (npcIndex > 0 && npcIndex < Main.maxNPCs)
			OpenWoundsNPC.Proc(Main.npc[npcIndex], time);
	}

	public override void OnSend(ModPacket modPacket)
	{
		modPacket.Write(_npc);
		modPacket.Write(_time);
	}
}