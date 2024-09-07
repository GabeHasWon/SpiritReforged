namespace SpiritReforged.Content.Visuals.FrostBreath;

public class SnowBreathGlobalNPC : GlobalNPC
{
	public override void PostAI(NPC npc)
	{
		Player closest = Main.player[Player.FindClosest(npc.position, npc.width, npc.height)];

		if (closest.ZoneSnow || closest.ZoneSkyHeight)
			if (npc.townNPC && Main.rand.NextBool(27))
			{
				var spawnPos = new Vector2(npc.position.X + 8 * npc.direction, npc.Center.Y - 13f);
				int d = Dust.NewDust(spawnPos, npc.width, 10, ModContent.DustType<Dusts.FrostBreath>(), 1.5f * npc.direction, 0f, 100, default, Main.rand.NextFloat(.20f, 0.75f));
				Main.dust[d].velocity.Y *= 0f;
			}
	}
}