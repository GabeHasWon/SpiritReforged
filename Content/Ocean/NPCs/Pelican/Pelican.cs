using SpiritReforged.Content.Ocean.Items;
using System.IO;
using Terraria.DataStructures;
using Terraria.UI;
using Terraria.Utilities;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI;
using System.Linq;
using Terraria.Audio;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.Ocean.Tiles;

namespace SpiritReforged.Content.Ocean.NPCs.Pelican;

[AutoloadCritter]
public class Pelican : ModNPC
{
	private static readonly int[] endFrames = [2, 6, 8, 5];

	private enum State : byte
	{
		Idle,
		Swim,
		Walk,
		Fly,
		Startle //Shares the same column as Idle, which similarly has no animation
	}

	public int AIState { get => (int)NPC.ai[0]; set => NPC.ai[0] = value; }
	public ref float Counter => ref NPC.ai[1];
	private static WeightedRandom<int> choice;
	private int heldItemType;

	public override void Unload() => choice = null;

	public override void SetStaticDefaults()
	{
		choice = new(Main.rand);
		choice.Add(ItemID.None, 6);
		choice.Add(ModContent.ItemType<Kelp>(), 0.25f);
		choice.Add(AutoContent.ItemType<Driftwood>(), 0.1f);
		choice.Add(ItemID.RedSnapper, 1f);
		choice.Add(ItemID.Shrimp, 0.5f);
		choice.Add(ItemID.Trout, 1.5f);
		choice.Add(ItemID.Tuna, 1f);
		choice.Add(ItemID.GoldenCarp, 0.01f);

		Main.npcFrameCount[Type] = 8; //Rows
		NPCID.Sets.CountsAsCritter[Type] = true;
		NPCID.Sets.ShimmerTransformToNPC[NPC.type] = NPCID.Shimmerfly;
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
	{
		bestiaryEntry.UIInfoProvider = new CritterUICollectionInfoProvider(ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[Type]);
		bestiaryEntry.AddInfo(this, "Ocean");
	}

	public override void SetDefaults()
	{
		NPC.Size = new Vector2(22);
		NPC.damage = 0;
		NPC.defense = 0;
		NPC.lifeMax = 5;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath1;
		NPC.knockBackResist = 1f;
		NPC.dontCountMe = true;
		NPC.npcSlots = 0;
		NPC.aiStyle = -1;
	}

	public override void OnSpawn(IEntitySource source)
	{
		if (source is not EntitySource_Parent { Entity: Player })
			heldItemType = choice;

		NPC.direction = -1;
		NPC.netUpdate = true;
	}

	public override void AI()
	{
		NPC.TargetClosest(false);
		var target = Main.player[NPC.target];

		if (AIState == (int)State.Startle)
		{
			NPC.velocity *= .9f;
			if (++Counter >= 30)
			{
				NPC.direction = Math.Sign(NPC.Center.X - target.Center.X);
				ChangeState(State.Fly);
			}

			return;
		}

		if (AIState == (int)State.Fly)
		{
			if (target.dead)
				return;

			if (Counter >= 300f) //Fall down and switch states when landing
			{
				if (NPC.velocity.Y == 0f || NPC.collideY || NPC.wet)
				{
					ChangeState(State.Idle);
					NPC.velocity = Vector2.Zero;
				}
				else
				{
					NPC.velocity.X *= 0.98f;
					NPC.velocity.Y = MathHelper.Min(NPC.velocity.Y + .15f, 2.5f);
				}

				return;
			}

			if (NPC.collideX)
				NPC.velocity.X = MathHelper.Clamp(NPC.oldVelocity.X * -0.5f, -2, 2);
			if (NPC.collideY)
				NPC.velocity.Y = MathHelper.Clamp(NPC.oldVelocity.Y * -0.5f, -1, 1);

			NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, 5 * NPC.direction, .04f);

			const int ScanCheck = 15;
			int tileX = (int)(NPC.Center.X / 16f) + NPC.direction;
			int tileY = (int)(NPC.Bottom.Y / 16f);
			bool closeGround = true;
			bool veryCloseGround = false;

			for (int y = tileY; y < tileY + ScanCheck; y++)
				if (Main.tile[tileX, y].HasUnactuatedTile && Main.tileSolid[Main.tile[tileX, y].TileType] || Main.tile[tileX, y].LiquidAmount > 0)
				{
					if (y < tileY + 5)
						veryCloseGround = true;

					closeGround = false;
					break;
				}

			NPC.velocity.Y += closeGround ? .15f : -.15f;
			if (veryCloseGround)
				NPC.velocity.Y -= 0.25f;

			NPC.velocity.Y = MathHelper.Clamp(NPC.velocity.Y, -4.5f, 4);
		}
		else
		{
			if (Main.rand.NextBool(750))
				SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Ambient/Pelican_Idle") with { PitchVariance = 0.6f, Pitch = .25f, Volume = .4f, MaxInstances = 2 }, NPC.Center);

			if (NPC.wet)
			{
				//Level with the liquid
				if (Collision.WetCollision(NPC.position, NPC.width, NPC.height - 4))
					NPC.velocity.Y = MathHelper.Max(NPC.velocity.Y - .75f, -5f);
				else if (!Collision.WetCollision(NPC.position, NPC.width, NPC.height + 4))
					NPC.velocity.Y += .1f;
				else
					NPC.velocity.Y *= .75f;

				if (Counter >= 200)
				{
					NPC.velocity.X *= .98f;
					if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(80))
					{
						NPC.netUpdate = true;
						Counter = 0;
						NPC.velocity.X = Main.rand.NextFloat(.5f, 1.5f) * (Main.rand.NextBool() ? -1 : 1);
					}
				}
			}
			else if (Counter >= 300 && Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(50)) //Periodically switch directions or idle
			{
				NPC.netUpdate = true;

				if (NPC.velocity.X == 0)
					NPC.velocity.X = Main.rand.NextFloat(.8f, 1.5f) * (Main.rand.NextBool() ? -1 : 1);
				else
					NPC.velocity.X = 0;
			}

			Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
			if (NPC.collideX)
				NPC.velocity.X *= -1;

			if (NPC.wet)
				ChangeState(State.Swim);
			else if (NPC.velocity.X != 0)
				ChangeState(State.Walk);
			else
				ChangeState(State.Idle);

			//Scare check
			if (target.active && !target.dead && target.DistanceSQ(NPC.Center) < 200 * 200 && target.velocity.LengthSquared() > 5 * 5)
			{
				NPC.velocity = new Vector2(-Math.Sign(target.Center.X - NPC.Center.X) * 8, -8);
				NPC.noGravity = true;

				ChangeState(State.Startle);
				NPC.frameCounter = 1; //Use the startle frame in Idle's column
				SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Ambient/Pelican_Surprise") with { PitchVariance = 0.4f, Pitch = .2f, Volume = 1f, MaxInstances = 2 }, NPC.Center);

				if (heldItemType != ItemID.None) //Startled - drop the held item
				{
					Item.NewItem(new EntitySource_Loot(NPC), NPC.Center, heldItemType);
					heldItemType = ItemID.None;
				}

				EmoteBubble.NewBubble(EmoteID.EmotionAlert, new WorldUIAnchor(NPC), 30);
				SoundEngine.PlaySound(SoundID.Item169 with { Pitch = .5f }, NPC.Center);
			}
		}

		Counter++;
		NPC.noGravity = AIState is (int)State.Swim or (int)State.Fly or (int)State.Startle;

		if (NPC.velocity.X != 0)
			NPC.spriteDirection = NPC.direction = (NPC.velocity.X > 0 ? 1 : -1) * (AIState == (int)State.Startle ? -1 : 1);
	}

	private void ChangeState(State toState)
	{
		if (AIState == (int)toState) //We switched to a new state
			return;

		NPC.frameCounter = 0;
		Counter = 0;
		AIState = (int)toState;
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (NPC.life > 0)
			return;

		if (Main.netMode != NetmodeID.MultiplayerClient && heldItemType != ItemID.None)
			Item.NewItem(NPC.GetSource_Death(), NPC.getRect(), heldItemType); //Drop the held item

		if (Main.netMode != NetmodeID.Server)
		{
			for (int i = 0; i < 20; i++)
			{
				var dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Blood);
				dust.velocity = dust.position.DirectionFrom(NPC.Center);
			}

			for (int i = 1; i < 4; i++)
				Gore.NewGore(Entity.GetSource_Death(), NPC.Center, NPC.velocity, Mod.Find<ModGore>("Pelican" + i).Type);
		}
	}

	public override void FindFrame(int frameHeight)
	{
		NPC.frame.Width = 62; //frameHeight = 60
		NPC.frame.X = NPC.frame.Width * (AIState % endFrames.Length);

		if (AIState is not ((int)State.Idle) and not ((int)State.Startle))
		{
			NPC.frameCounter += .2f;
			NPC.frameCounter %= endFrames[AIState];
		}

		NPC.frame.Y = (int)NPC.frameCounter * frameHeight;
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		var texture = TextureAssets.Npc[Type].Value;
		var source = NPC.frame with { Width = NPC.frame.Width - 2, Height = NPC.frame.Height - 2 }; //Remove padding
		var position = NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY);
		var effects = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		var color = NPC.GetAlpha(NPC.GetNPCColorTintedByBuffs(drawColor));

		Main.EntitySpriteDraw(texture, position, source, color, NPC.rotation, source.Size() / 2, NPC.scale, effects);
		return false;
	}

	public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) //Draws heldItemType
	{
		if (heldItemType == ItemID.None)
			return;

		Main.instance.LoadItem(heldItemType);

		var item = ContentSamples.ItemsByType[heldItemType];
		var value = TextureAssets.Item[item.type].Value;
		var frame = Main.itemAnimations[item.type] == null ? value.Frame() : Main.itemAnimations[item.type].GetFrame(value);
		frame.Height /= 2;

		const float SizeLimit = 20;
		float scale = item.scale;

		if (frame.Width > SizeLimit || frame.Height > SizeLimit)
			scale = frame.Width <= frame.Height ? SizeLimit / frame.Height : SizeLimit / frame.Width;

		int[] bobFrames = [1, 2, 5, 6]; //Extra vertical displacement on these frames
		float offY = AIState == (int)State.Swim ? -4 : bobFrames.Contains((int)NPC.frameCounter) ? -10 : -8;
		var pos = NPC.Center + new Vector2(14 * NPC.spriteDirection, offY + NPC.gfxOffY) - screenPos;

		var color = Lighting.GetColor(NPC.Center.ToTileCoordinates());
		float modScale = 1f;
		ItemSlot.GetItemLight(ref color, ref modScale, item);
		scale *= modScale;

		var effects = NPC.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		spriteBatch.Draw(value, pos, frame, color, MathHelper.Pi, frame.Size() / 2f, scale, effects, 0f);

		if (item.color != default)
			spriteBatch.Draw(value, pos, frame, item.GetColor(color), 0f, frame.Size() / 2, scale, SpriteEffects.FlipVertically, 0f);
	}

	public override void SendExtraAI(BinaryWriter writer) => writer.Write(heldItemType);

	public override void ReceiveExtraAI(BinaryReader reader) => heldItemType = reader.ReadInt32();

	public override float SpawnChance(NPCSpawnInfo spawnInfo)
	{
		if (spawnInfo.Player.ZoneBeach && Main.dayTime && !spawnInfo.Water)
			return spawnInfo.PlayerInTown ? 2 : 1;

		return 0;
	}
}
