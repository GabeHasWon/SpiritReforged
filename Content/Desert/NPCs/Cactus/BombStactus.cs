using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.NPCCommon;
using Terraria.Audio;

namespace SpiritReforged.Content.Desert.NPCs.Cactus;

public class BombStactus : Stactus
{
	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 8;
		NPCID.Sets.CantTakeLunchMoney[Type] = true;
		NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new()
		{
			Hide = true
		});

		MoRHelper.AddNPCToElementList(Type, MoRHelper.NPCType_Plantlike);
	}

	public override void OnKill()
	{
		if (CrossMod.Fables.TryFind("WulfrumMortarExplosion", out ModProjectile projectile))
			Projectile.NewProjectile(NPC.GetSource_Death(), NPC.Center, Vector2.Zero, projectile.Type, 40, 20, Main.myPlayer);
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (!Main.dedServ)
		{
			for (int i = 0; i < 3; i++)
				Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, parameters.DustType, Scale: Main.rand.NextFloat(1f, 1.2f)).noGravity = Main.rand.NextBool();

			if (!falling)
				SoundEngine.PlaySound(SoundID.NPCHit1, NPC.Center);
		}
	}

	public override void FindFrame(int frameHeight)
	{
		NPC.frame.Height = frameHeight - 2;
		NPC.frame.Width = 40;

		NPC.frameCounter = (NPC.frameCounter + 0.15f) % Main.npcFrameCount[Type];
		NPC.frame.Y = (int)NPC.frameCounter * frameHeight;
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		Texture2D texture = TextureAssets.Npc[Type].Value;
		Vector2 center = NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY);
		Rectangle source = NPC.frame;
		Vector2 origin = source.Size() / 2;
		Color color = NPC.DrawColor(drawColor);

		Main.EntitySpriteDraw(texture, center, source, color, NPC.rotation, origin, NPC.scale, default);
		return false;
	}
}