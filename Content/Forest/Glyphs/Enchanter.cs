using SpiritReforged.Common.NPCCommon.Abstract;
using SpiritReforged.Common.NPCCommon.Interfaces;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.UI.Enchantment;
using SpiritReforged.Common.UI.System;
using SpiritReforged.Content.Forest.Glyphs.CharmcasterSet;
using SpiritReforged.Content.Forest.MagicPowder;
using SpiritReforged.Content.Particles;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Forest.Glyphs;

[AutoloadHead]
public class Enchanter : WorldNPC, ITravelNPC
{
	/// <summary> Stores a shop value by item type. </summary>
	public static readonly Dictionary<int, int> SpecialShop = [];

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
		NPC.CloneDefaults(NPCID.SkeletonMerchant);
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.DD2_WyvernDiveDown;
		NPC.Size = new Vector2(30, 40);

		AnimationType = NPCID.Guide;
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "Surface");

	public override string GetChat() => Language.GetTextValue("Mods.SpiritReforged.NPCs.Cartographer.Dialogue." + Main.rand.Next(5));

	public override ITownNPCProfile TownNPCProfile() => NPCProfile;

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
		.Add<Flarepowder>()
		.Add(ItemID.PeaceCandle)
		.Add(ItemID.WaterCandle, Condition.Hardmode)
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

	public bool CanSpawnTraveler() => true;

	#region attack
	public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown) => base.TownNPCAttackCooldown(ref cooldown, ref randExtraCooldown);

	public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
	{
		projType = Main.hardMode ? ModContent.ProjectileType<VexpowderBlueDust>() : ModContent.ProjectileType<FlarepowderDust>();
	}

	public override void TownNPCAttackMagic(ref float auraLightMultiplier) => base.TownNPCAttackMagic(ref auraLightMultiplier);
	#endregion
}