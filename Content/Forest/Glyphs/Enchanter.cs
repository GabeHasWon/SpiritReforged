using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.UI.Enchantment;
using SpiritReforged.Common.UI.System;
using SpiritReforged.Content.Forest.Glyphs.CharmcasterSet;
using SpiritReforged.Content.Forest.MagicPowder;
using SpiritReforged.Content.Particles;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Forest.Glyphs;

[AutoloadHead]
public class Enchanter : ModNPC
{
	/// <summary> Used to track whether <see cref="Enchanter"/> has spawned previously in this world. Not useable on multiplayer clients. </summary>
	public sealed class EnchanterSystem : ModSystem
	{
		public bool enchanterSpawned;

		public override void ClearWorld() => enchanterSpawned = false;

		public override void SaveWorldData(TagCompound tag) => tag[nameof(enchanterSpawned)] = enchanterSpawned;
		public override void LoadWorldData(TagCompound tag) => enchanterSpawned = tag.GetBool(nameof(enchanterSpawned));
	}

	/// <summary> Stores a shop value by item type. </summary>
	public static readonly Dictionary<int, int> SpecialShop = [];

	private static readonly Vector2[] TailOrigin = [
			new(54, 32),
			new(60, 34),
			new(58, 28),
			new(54, 32),
			new(52, 32),
			new(50, 32),
			new(50, 32),
			new(52, 32),
			new(54, 32),
			new(54, 32),
			new(54, 32),
			new(52, 32),
			new(50, 32),
			new(50, 32),
			new(50, 32),
			new(52, 32),
			new(54, 32),
			new(54, 32),
			new(50, 30),
			new(54, 32),
			new(54, 32),
			new(52, 32),
			new(52, 32),
			new(54, 32),
			new(54, 32),
			new(52, 32),
		];

	private static Profiles.StackedNPCProfile NPCProfile;

	public override void Load() => Mod.AddNPCHeadTexture(Type, Texture + "_Shimmer_Head");

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		Main.npcFrameCount[Type] = 26;

		NPCID.Sets.ExtraFramesCount[Type] = 6;
		NPCID.Sets.AttackFrameCount[Type] = 4;
		NPCID.Sets.DangerDetectRange[Type] = 600;
		NPCID.Sets.AttackType[Type] = 2;
		NPCID.Sets.AttackTime[Type] = 20;
		NPCID.Sets.HatOffsetY[Type] = 2;
		NPCID.Sets.IsTownChild[Type] = true;
		NPCID.Sets.ShimmerTownTransform[Type] = true;

		NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers()
		{ 
			Velocity = 1f
		});

		NPCProfile = new Profiles.StackedNPCProfile(
			new Profiles.DefaultNPCProfile(Texture, NPCHeadLoader.GetHeadSlot(HeadTexture)),
			new Profiles.DefaultNPCProfile(Texture + "_Shimmer", NPCHeadLoader.GetHeadSlot(Texture + "_Shimmer_Head"))
		);
	}

	public override void SetDefaults()
	{
		NPC.CloneDefaults(NPCID.Merchant);
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.DD2_WyvernDiveDown;
		NPC.Size = new Vector2(30, 40);

		AnimationType = NPCID.Guide;
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "Surface");

	public override string GetChat() => Language.GetTextValue("Mods.SpiritReforged.NPCs.Cartographer.Dialogue." + Main.rand.Next(5));

	public override ITownNPCProfile TownNPCProfile() => NPCProfile;

	public override bool CanTownNPCSpawn(int numTownNPCs)
	{
		if (ModContent.GetInstance<EnchanterSystem>().enchanterSpawned || NPC.downedSlimeKing || NPC.downedBoss1 || Main.hardMode) //Has downed a boss or has spawned previously
			return true;

		foreach (Player player in Main.ActivePlayers)
		{
			foreach (Item item in player.inventory)
			{
				if (!item.IsAir && (item.type == ModContent.ItemType<ChromaticWax>() || item.GetGlyph() != default)) //The item is Chromatic Wax or has an enchantment
					return true;
			}
		}

		return false;
	}

	public override void OnSpawn(IEntitySource source) => ModContent.GetInstance<EnchanterSystem>().enchanterSpawned = true;

	public override List<string> SetNPCNameList()
	{
		List<string> names = [];

		for (int i = 0; i < 6; ++i)
			names.Add(Language.GetTextValue("Mods.SpiritReforged.NPCs.Cartographer.Names." + i));

		return names;
	}

	public override void SetChatButtons(ref string button, ref string button2)
	{
		button = Language.GetTextValue("LegacyInterface.28");
		button2 = Language.GetTextValue("Mods.SpiritReforged.Misc.Enchantment.Enchant");
	}

	public override void OnChatButtonClicked(bool firstButton, ref string shopName)
	{
		if (firstButton)
		{
			shopName = "Shop";
		}
		else
		{
			Main.playerInventory = true;
			UISystem.SetActive<EnchanterUI>();
		}
	}

	public override void AddShops() => new NPCShop(Type)
		.Add<EnchantedStamp>()
		.Add<Flarepowder>(Condition.NotBloodMoon, Condition.PreHardmode)
		.Add<VexpowderBlue>(Condition.CorruptWorld, Condition.BloodMoonOrHardmode)
		.Add<VexpowderRed>(Condition.CrimsonWorld, Condition.BloodMoonOrHardmode)
		.Add(ItemID.PeaceCandle, Condition.NotBloodMoon, Condition.PreHardmode)
		.Add(ItemID.WaterCandle, Condition.NotBloodMoon, Condition.Hardmode)
		.Add(ItemID.ShadowCandle, Condition.BloodMoon)
		.Add<CharmcasterHat>()
		.Add<CharmcasterRobe>()
		.Add<CharmcasterLeggings>()
		.Register();

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (!Main.dedServ && NPC.life <= 0)
		{
			for (int i = 0; i < 10; i++)
				ParticleHandler.SpawnParticle(new CartoonSmoke(Main.rand.NextVector2FromRectangle(NPC.Hitbox), 30, 1, Main.rand.NextVector2Circular(2, 2)));
		}

		for (int d = 0; d < 8; d++)
			Dust.NewDustPerfect(Main.rand.NextVector2FromRectangle(NPC.getRect()), DustID.Blood, Main.rand.NextVector2Unit() * 1.5f, 0, default, Main.rand.NextFloat(1f, 1.5f));
	}

	public override void FindFrame(int frameHeight)
	{
		if (Main.dedServ)
			return;

		Texture2D texture = TextureAssets.Npc[Type].Value;
		Rectangle fallFrame = texture.Frame(1, Main.npcFrameCount[Type], 0, 2, 0, -2);
		bool falling = NPC.velocity.Y > 0;

		if (falling)
		{
			NPC.frame = fallFrame;
		}
		else if (NPC.frame == fallFrame)
		{
			NPC.frame.Y += frameHeight; //Forcefully skip `fallFrame` during the walk cycle
		}
	}

	/*public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		Texture2D npcTexture = TextureAssets.Npc[Type].Value;
		Rectangle npcSource = NPC.frame;
		SpriteEffects effects = (NPC.spriteDirection == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		Main.EntitySpriteDraw(npcTexture, NPC.Center - Main.screenPosition + new Vector2(0, NPC.gfxOffY), npcSource, NPC.DrawColor(drawColor), NPC.rotation, npcSource.Size() / 2, NPC.scale, effects);
		return false;
	}*/

	public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		Texture2D npcTexture = TextureAssets.Npc[Type].Value;
		Texture2D flameTexture = TextureAssets.Flames[0].Value;

		Rectangle npcSource = NPC.frame;
		Rectangle flameSource = new(22, 0, 22, 22);
		
		int frame = NPC.frame.Y / (npcTexture.Height / Main.npcFrameCount[Type]);
		Vector2 offset = (frame >= 0 && frame < TailOrigin.Length) ? TailOrigin[frame] : TailOrigin[0];

		for (int i = 0; i < 3; i++)
		{
			Vector2 position = NPC.Center - npcSource.Size() / 2 - screenPos + new Vector2((NPC.spriteDirection == 1) ? (npcSource.Width - offset.X) : offset.X, offset.Y + NPC.gfxOffY) + Main.rand.NextVector2Circular(2, 2);
			Main.EntitySpriteDraw(flameTexture, position, flameSource, NPC.DrawColor(Color.White.Additive()), NPC.rotation, flameSource.Size() / 2, NPC.scale, 0);
		}
	}

	#region attack
	public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown) => base.TownNPCAttackCooldown(ref cooldown, ref randExtraCooldown);

	public override void TownNPCAttackProj(ref int projType, ref int attackDelay) => projType = Main.hardMode ? ModContent.ProjectileType<VexpowderBlueDust>() : ModContent.ProjectileType<FlarepowderDust>();

	public override void TownNPCAttackMagic(ref float auraLightMultiplier) => base.TownNPCAttackMagic(ref auraLightMultiplier);
	#endregion
}