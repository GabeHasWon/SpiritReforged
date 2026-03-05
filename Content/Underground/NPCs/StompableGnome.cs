using Terraria.Audio;

namespace SpiritReforged.Content.Underground.NPCs;

public class StompableGnome : ModNPC
{
	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 2;
		NPCID.Sets.CountsAsCritter[Type] = true;
		NPCID.Sets.ShimmerTransformToNPC[Type] = NPCID.Shimmerfly;

		NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new()
		{ Hide = true };

		NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);
	}

	public override void SetDefaults()
	{
		NPC.CloneDefaults(NPCID.Grubby);
		NPC.Size = new Vector2(8);
	}

	public override void AI()
	{
		var p = Main.player[Player.FindClosest(NPC.Center, 0, 0)];
		if (!p.dontHurtCritters && p.getRect().Contains(NPC.Center.ToPoint()) && (int)p.velocity.X != 0)
		{
			SoundEngine.PlaySound(SoundID.NPCDeath1 with { Pitch = .75f }, NPC.Center);
			NPC.DeathSound = null;

			if (Main.netMode != NetmodeID.MultiplayerClient)
				NPC.StrikeInstantKill();
		}

		if (NPC.velocity.Y == 0)
		{
			if (Main.rand.NextBool(25))
				NPC.velocity.Y = -2.5f;

			NPC.velocity.X *= 3f;
		}

		NPC.spriteDirection = NPC.direction;

		foreach (var other in Main.ActiveNPCs) //Don't group up
		{
			if (other.whoAmI == NPC.whoAmI || other.type != Type || other.DistanceSQ(NPC.Center) > 16 * 16)
				continue;

			NPC.velocity.X = Vector2.Lerp(NPC.velocity, NPC.DirectionFrom(other.Center), .1f).X;
		}
	}

	public override void ModifyHoverBoundingBox(ref Rectangle boundingBox) => boundingBox = Rectangle.Empty;

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (NPC.life <= 0 && !Main.dedServ && ChildSafety.Disabled)
		{
			for (int k = 0; k < 5; k++)
			{
				var d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.BlueMoss, 1.05f * hit.HitDirection, -1.95f);
				d.velocity *= .5f;
			}
		}
	}

	public override void FindFrame(int frameHeight)
	{
		if (NPC.velocity != Vector2.Zero || NPC.IsABestiaryIconDummy)
		{
			NPC.frameCounter += 0.12f;
			NPC.frameCounter %= Main.npcFrameCount[Type];
			int frame = (int)NPC.frameCounter;

			NPC.frame.Y = frame * frameHeight;
		}
	}
}
