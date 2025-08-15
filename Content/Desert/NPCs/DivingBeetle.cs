
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.NPCCommon;
using System.IO;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Desert.NPCs;

[AutoloadCritter]
public class DivingBeetle : ModNPC
{
	public ref float Counter => ref NPC.ai[0];
	/// <summary> A randomly-selected location to control passive swimming velocity. </summary>
	private Vector2 _targetPosition;

	public override void SetStaticDefaults()
	{
		CreateItemDefaults();
		Main.npcFrameCount[Type] = 3;
	}
	public override void SetDefaults()
	{

		NPC.lifeMax = 5;
		NPC.dontCountMe = true;
		NPC.npcSlots = 0.1f;
		NPC.noGravity = true;
	}

	public virtual void CreateItemDefaults() => 
		ItemEvents.CreateItemDefaults(
		this.AutoItemType(), 
		item =>
		{
			item.value = Item.sellPrice(0, 0, 0, 45);
			item.bait = 18;
		}
	);

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "Desert");

	public override void AI()
	{
		const int retargetInterval = 120;

		if (!NPC.HasPlayerTarget || Main.player[NPC.target] is not Player target || !target.active || target.dead || target.DistanceSQ(NPC.Center) > 500 * 500)
			NPC.TargetClosest();

		if (_targetPosition == Vector2.Zero)
		{
			_targetPosition = NPC.Center - new Vector2(0, 10); //Initialize _targetPosition
			NPC.scale = Main.rand.NextFloat(0.9f, 1.1f); //As a bonus, slightly randomize scale using an unsynced value
		}
		else if (Main.netMode != NetmodeID.MultiplayerClient && ++Counter % retargetInterval == 0)
		{
			Vector2 samplePosition = NPC.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(10, 80);

			if (Collision.WetCollision(samplePosition - NPC.Size / 2, NPC.width, NPC.height))
			{
				_targetPosition = samplePosition; //Allow this position if it collides with liquid
				NPC.netUpdate = true;
			}
			else
			{
				Counter = retargetInterval / 2; //Reduce the time to retarget otherwise
			}
		}

		var velocityTarget = NPC.DirectionTo(_targetPosition) * 2;

		if (Main.player[NPC.target].DistanceSQ(NPC.Center) < 150 * 150)
			velocityTarget += NPC.DirectionFrom(Main.player[NPC.target].Center);

		NPC.velocity = Vector2.Lerp(NPC.velocity, velocityTarget, 0.01f);
		NPC.rotation = NPC.velocity.ToRotation() + MathHelper.PiOver2;
		NPC.noGravity = Collision.WetCollision(NPC.position, NPC.width, NPC.height);
	}

	public override void FindFrame(int frameHeight)
	{
		float rate = MathHelper.Clamp(NPC.velocity.Length() / 8f, 0.1f, 0.25f);

		NPC.frameCounter = (NPC.frameCounter + rate) % Main.npcFrameCount[Type];
		NPC.frame.Y = (int)NPC.frameCounter * frameHeight;
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		var texture = TextureAssets.Npc[Type].Value;
		var source = NPC.frame with { Height = NPC.frame.Height - 2 };

		spriteBatch.Draw(texture, NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY), source, NPC.DrawColor(drawColor), NPC.rotation, source.Size() / 2, NPC.scale, default, 0);
		return false;
	}

	public override void SendExtraAI(BinaryWriter writer) => writer.WritePackedVector2(_targetPosition);
	public override void ReceiveExtraAI(BinaryReader reader) => _targetPosition = reader.ReadPackedVector2();

	public override float SpawnChance(NPCSpawnInfo spawnInfo) => spawnInfo.Player.ZoneDesert && spawnInfo.SpawnTileY < Main.worldSurface && !spawnInfo.Invasion && spawnInfo.Water 
		? (spawnInfo.PlayerInTown ? 0.5f : 0.25f) : 0;
}