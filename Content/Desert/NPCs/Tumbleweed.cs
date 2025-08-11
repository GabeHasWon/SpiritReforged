using SpiritReforged.Common.NPCCommon;
using System.IO;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.NPCs;

public class Tumbleweed : ModNPC
{
	public override void SetStaticDefaults()
	{
		NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers() { Hide = true });
		Main.npcFrameCount[Type] = 3;
	}

	public override void SetDefaults()
	{
		NPC.Size = new Vector2(26);
		NPC.aiStyle = -1;
		NPC.lifeMax = 5;
		NPC.value = 50;
		NPC.dontCountMe = true;
		NPC.npcSlots = 0.1f;
		NPC.noGravity = true;
		NPC.DeathSound = SoundID.Grass;
	}

	public override void OnSpawn(IEntitySource source)
	{
		NPC.scale = Main.rand.NextFloat(0.9f, 1.1f);
		NPC.netUpdate = true;
	}

	public override void AI()
	{
		NPC.rotation += NPC.velocity.X / 15f;

		NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, Main.windSpeedCurrent * 3, 0.05f);
		NPC.velocity.Y = Math.Min(NPC.velocity.Y + 0.1f, 5);

		if (NPC.collideY)
		{
			NPC.velocity.Y -= Math.Abs(NPC.velocity.X);
		}

		Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (Main.dedServ || NPC.life > 0)
			return;

		for (int i = 0; i < 10; i++)
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.PalmWood);
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo)
	{
		if (spawnInfo.Player.ZoneDesert && !spawnInfo.Water && Math.Abs(Main.windSpeedCurrent) > 0.3f)
		{
			int playerX = (int)(spawnInfo.Player.Center.X / 16);

			if (Math.Sign(playerX - spawnInfo.SpawnTileX) == Math.Sign(Main.windSpeedCurrent)) //Ensure the tumbleweed will actually cross the player
				return 0.5f;
		}

		return 0;
	}

	public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => false;
	public override void ModifyHoverBoundingBox(ref Rectangle boundingBox) => boundingBox = Rectangle.Empty;

	public override void FindFrame(int frameHeight)
	{
		if (NPC.frameCounter == 0)
			NPC.frameCounter = Main.rand.NextBool(100) ? 3 : Main.rand.Next(1, 3);

		NPC.frame.Y = ((int)NPC.frameCounter - 1) * frameHeight;
		NPC.frame.Height = frameHeight - 2;
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		var texture = TextureAssets.Npc[Type].Value;
		var center = NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY);

		spriteBatch.Draw(texture, center, NPC.frame, NPC.DrawColor(drawColor), NPC.rotation, NPC.frame.Size() / 2, NPC.scale, default, 0);
		return false;
	}

	public override void SendExtraAI(BinaryWriter writer) => writer.Write(NPC.scale);
	public override void ReceiveExtraAI(BinaryReader reader) => NPC.scale = reader.ReadSingle();
}