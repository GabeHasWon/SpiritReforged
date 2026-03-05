namespace SpiritReforged.Content.Visuals.FrostBreath;

internal class FrostBreathPlayer : ModPlayer
{
	public override void PreUpdate()
	{
		if ((Player.ZoneSnow || Player.ZoneSkyHeight) && !Player.behindBackWall && Main.rand.NextBool(27))
		{
			var position = Player.RotatedRelativePoint(Player.MountedCenter + new Vector2(10 * Player.direction, -10));

			var d = Dust.NewDustDirect(position, Player.width, 10, ModContent.DustType<Dusts.FrostBreath>(), 1.5f * Player.direction, 0f, 100, default, Main.rand.NextFloat(.20f, 0.75f));
			d.velocity.Y = 0;
		}
	}
}

internal class SnowBreathGlobalNPC : GlobalNPC
{
	public override void PostAI(NPC npc)
	{
		if (!npc.townNPC || !Main.rand.NextBool(27))
			return;

		var closest = Main.player[Player.FindClosest(npc.position, npc.width, npc.height)];
		var tilePos = npc.Center.ToTileCoordinates();

		if (closest.ZoneSnow || closest.ZoneSkyHeight && Framing.GetTileSafely(tilePos).WallType == WallID.None)
		{
			var position = new Vector2(npc.position.X + 8 * npc.direction, npc.Center.Y - 13f);

			var d = Dust.NewDustDirect(position, npc.width, 10, ModContent.DustType<Dusts.FrostBreath>(), 1.5f * npc.direction, 0f, 100, default, Main.rand.NextFloat(.20f, 0.75f));
			d.velocity.Y = 0;
		}
	}
}