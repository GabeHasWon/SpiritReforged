using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Content.Savanna.Biome;
using SpiritReforged.Content.Savanna.Tiles;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Savanna.NPCs.PeevedTumbler;

[AutoloadBanner]
public class PeevedTumbler : ModNPC
{
	public ref float Counter => ref NPC.ai[0];

	private bool _hasItem;

	public override void SetStaticDefaults()
	{
		NPCID.Sets.TrailCacheLength[Type] = 5;
		NPCID.Sets.TrailingMode[Type] = 3;

		MoRHelper.AddElement(NPC, MoRHelper.Earth);
		MoRHelper.AddElement(NPC, MoRHelper.Wind);
		MoRHelper.AddNPCToElementList(Type, MoRHelper.NPCType_Hot);
		MoRHelper.AddNPCToElementList(Type, MoRHelper.NPCType_Inorganic);
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "Sandstorm");

	public override void SetDefaults()
	{
		NPC.CloneDefaults(NPCID.Tumbleweed);
		NPC.Size = new Vector2(24);
		NPC.lifeMax = 35;
		NPC.damage = 24;
		NPC.knockBackResist = .8f;
		NPC.aiStyle = -1;

		SpawnModBiomes = [ModContent.GetInstance<SavannaBiome>().Type];
	}

	public override void OnSpawn(IEntitySource source)
	{
		if (source is not EntitySource_Parent { Entity: Player })
		{
			_hasItem = Main.rand.NextBool(3);
			NPC.netUpdate = true;
		}
	}

	public override void AI()
	{
		NPC.TargetClosest(false);
		var target = Main.player[NPC.target];
		bool inDustStorm = target.GetModPlayer<DustStorm.DustStormPlayer>().ZoneDustStorm;
		float maxSpeed = inDustStorm ? 5f : 3.5f; //The maximum horizontal speed of the NPC (excludes factors like wind)
		const float timeBeforeRun = 60 * 2; //The time it takes before the NPC starts running under specific conditions

		bool RunningFromTarget() => Counter >= timeBeforeRun;

		if (RunningFromTarget())
		{
			if (NPC.Distance(target.Center) > 16 * 40 || ++Counter >= timeBeforeRun * 3)
				Counter = 0; //Stop running

			NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, Math.Sign(NPC.Center.X - target.Center.X) * maxSpeed, .01f);
		}
		else
		{
			if ((int)NPC.velocity.X == 0)
				if (NPC.velocity.Y == 0 && ++Counter % 60 == 0)
					NPC.velocity.Y = -5; //Jump over tall terrain if stuck
				else
					Counter = 0;

			NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, Math.Sign(target.Center.X - NPC.Center.X) * maxSpeed, .01f);
		}

		NPC.rotation += NPC.velocity.X * 0.05f;
		if (NPC.velocity.Y == 0 && Math.Abs(NPC.velocity.X) > 2.5f) //Hop when moving quickly
		{
			NPC.velocity.Y = -(Math.Abs(NPC.velocity.X) * .8f);
			SoundEngine.PlaySound(NPC.HitSound, NPC.Center);
		}

		Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
		if (NPC.collideX && (int)NPC.velocity.X != 0)
			NPC.velocity.X *= -1f;

		if (inDustStorm) //Get pushed around by the wind
		{
			float windPush = MathHelper.Lerp(0.6f, 1f, Math.Abs(Main.windSpeedTarget)) * Math.Sign(Main.windSpeedTarget) * .01f;
			NPC.velocity.X += windPush;
		}

		if (_hasItem && Main.rand.NextBool(26)) //Sparkle when carrying an item
			Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.TreasureSparkle, Scale: Main.rand.NextFloat(.25f, 1f)).velocity = Vector2.Zero;
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (Main.dedServ)
			return;

		bool dead = NPC.life <= 0;

		if (NPC.life <= 0)
		{
			for (int i = 1; i < 6; i++)
				Gore.NewGoreDirect(NPC.GetSource_Death(), Main.rand.NextVector2FromRectangle(NPC.getRect()),
					NPC.velocity * Main.rand.NextFloat(.5f, 1f), Mod.Find<ModGore>("PeevedTumbler" + i).Type);
		}

		for (int i = 0; i < (dead ? 20 : 4); i++)
			Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.WoodFurniture).velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(.5f, 1f);
	}

	//Only damage NPCs and players when moving quickly enough
	public override bool CanHitPlayer(Player target, ref int cooldownSlot) => Math.Abs(NPC.velocity.X) > 1.5f;
	public override bool CanHitNPC(NPC target) => Math.Abs(NPC.velocity.X) > 1.5f;

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		Texture2D texture = TextureAssets.Npc[Type].Value;
		var color = NPC.GetAlpha(NPC.GetNPCColorTintedByBuffs(drawColor));

		//Draw normally
		Main.EntitySpriteDraw(texture, NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY), null, color, NPC.rotation, texture.Size() / 2, NPC.scale, SpriteEffects.None, 0);
		//Draw trail
		for (int i = NPC.oldPos.Length - 1; i >= 0; i--)
		{
			var position = NPC.oldPos[i] + NPC.Size / 2 - screenPos + new Vector2(0, NPC.gfxOffY);
			Main.EntitySpriteDraw(texture, position, null, color * .25f * (1f - (float)i / NPC.oldPos.Length), NPC.oldRot[i], texture.Size() / 2, NPC.scale, SpriteEffects.None, 0);
		}
		//Draw eye
		for (int i = 0; i < 2; i++)
		{
			var eyeColor = NPC.GetAlpha(i == 0 ? new Color(249, 140, 0) : new Color(192, 86, 0));
			int offsetX = i * -2;

			Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, NPC.Center - screenPos + new Vector2(offsetX, NPC.gfxOffY),
				new Rectangle(0, 0, 2, 2), eyeColor, 0, new Vector2(.5f), NPC.scale, SpriteEffects.None, 0);
		}

		return false;
	}

	public override void SendExtraAI(BinaryWriter writer) => writer.Write(_hasItem);
	public override void ReceiveExtraAI(BinaryReader reader) => _hasItem = reader.ReadBoolean();

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		var rule = new LeadingConditionRule(new HasItem());

		rule.OnSuccess(ItemDropRule.Common(ItemID.Nachos, 26));
		rule.OnSuccess(ItemDropRule.Common(AutoContent.ItemType<Drywood>(), minimumDropped: 5, maximumDropped: 15));
		rule.OnSuccess(ItemDropRule.OneFromOptions(25, ModContent.ItemType<Items.Tools.LivingBaobabWand>(), ModContent.ItemType<Items.Tools.LivingBaobabLeafWand>()));
		rule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Items.WrithingSticks.WrithingSticks>(), 20));

		npcLoot.Add(rule);
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo)
	{
		bool zoneDustStorm = spawnInfo.Player.GetModPlayer<DustStorm.DustStormPlayer>().ZoneDustStorm;
		if (spawnInfo.Player.InModBiome<SavannaBiome>() && !spawnInfo.PlayerInTown && zoneDustStorm && !spawnInfo.Water)
			return .16f;

		return 0;
	}

	private class HasItem : IItemDropRuleCondition, IProvideItemConditionDescription
	{
		public bool CanDrop(DropAttemptInfo info) => info.npc.ModNPC is PeevedTumbler tumblr && tumblr._hasItem;
		public bool CanShowItemDropInUI() => true;
		public string GetConditionDescription() => Language.GetTextValue("Mods.SpiritReforged.Conditions.PeevedTumbler");
	}
}