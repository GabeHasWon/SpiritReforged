using static SpiritReforged.Common.NPCCommon.SlowdownGlobalNPC;

namespace SpiritReforged.Content.Snow.Frostbite;

public class Frozen : ModBuff
{
	public override string Texture => "Terraria/Images/Buff";

	public override void SetStaticDefaults()
	{
		Main.debuff[Type] = true;
		Main.buffNoSave[Type] = true;
	}

	public override void Update(NPC npc, ref int buffIndex)
	{
		if (npc.knockBackResist <= 0f)
			return;

		ApplySlow(npc, .3f);
		if (Main.rand.NextBool(25))
		{
			Vector2 position = npc.position + new Vector2(npc.width * Main.rand.NextFloat(), npc.height * Main.rand.NextFloat());
			Projectile.NewProjectile(npc.GetSource_Buff(buffIndex), position, Vector2.UnitX.RotatedByRandom(5f), ModContent.ProjectileType<FrozenFragment>(), 0, 0, ai0: npc.whoAmI);
		}
	}
}