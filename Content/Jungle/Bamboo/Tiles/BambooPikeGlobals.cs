using SpiritReforged.Content.Jungle.Bamboo.Buffs;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Jungle.Bamboo.Tiles;

public class BambooPikePlayer : ModPlayer
{
	public static PlayerDeathReason GetDeathReason(Player player)
	{
		var text = NetworkText.FromKey("Mods.SpiritReforged.DamageSources.Impaling." + Main.rand.Next(2), player.name);
		return PlayerDeathReason.ByCustomReason(text);
	}

	public override void PostUpdate()
	{
		var tile = Framing.GetTileSafely(Player.Bottom - new Vector2(8));

		if (Player.velocity.Y >= 1.25f && tile.HasTile && tile.TileType == ModContent.TileType<BambooPike>() && tile.TileFrameY == 0)
			BambooPike.Strike(Player);
	}

	public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
	{
		if (damageSource.SourceOtherIndex == 8 && Player.HasBuff<Impaled>())
			damageSource = GetDeathReason(Player);

		return true;
	}
}

public class BambooPikeNPC : GlobalNPC
{
	public override void PostAI(NPC npc)
	{
		var tile = Framing.GetTileSafely(npc.Bottom - new Vector2(8));

		if (npc.velocity.Y >= 1.25f && tile.HasTile && tile.TileType == ModContent.TileType<BambooPike>() && tile.TileFrameY == 0)
			BambooPike.Strike(npc);
	}
}
