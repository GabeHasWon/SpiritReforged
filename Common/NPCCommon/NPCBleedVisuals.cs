using System.IO;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Common.NPCCommon;

internal class NPCBleedVisuals : GlobalNPC
{
	public override bool InstancePerEntity => true;

	public Vector2? bleedDirection = null;

	public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => !entity.dontTakeDamage;

	public override void PostAI(NPC npc)
	{
		if (npc.HasBuff(BuffID.Bleeding) && Main.rand.NextFloat() < 0.45f)
		{
			if (bleedDirection is { } dir)
			{
				// Bleed in a direction
				Dust.NewDustPerfect(npc.Center + dir * MathF.Min(npc.width, npc.height) / 2f, DustID.Blood, dir.RotatedByRandom(0.3f) * Main.rand.NextFloat(1f, 3f), 0, default, Main.rand.NextFloat(1, 2f));
			}
			else
			{
				Rectangle hitbox = npc.Hitbox;
				hitbox.Inflate(hitbox.Width > 16 ? -8 : 0, hitbox.Height > 16 ? -8 : 0);
				Dust.NewDust(hitbox.Location.ToVector2(), hitbox.Width, hitbox.Height, DustID.Blood);
			}
		}
	}

	public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
	{
		bitWriter.WriteBit(bleedDirection.HasValue);

		if (bleedDirection is { } dir)
			binaryWriter.WriteVector2(dir);
	}

	public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
	{
		if (bitReader.ReadBit())
			bleedDirection = binaryReader.ReadVector2();
	}
}
