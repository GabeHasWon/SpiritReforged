using SpiritReforged.Common.Misc;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Desert.ScarabBoss.Items;

namespace SpiritReforged.Content.Desert.NPCs.TownBeetle;

public class BeetleTownPet : ModNPC
{
	private class ScarabTownPetProfile : ITownNPCProfile
	{
		private readonly record struct PetProfileInfo(string Identifier, int HeadIndex, Asset<Texture2D> Texture);

		private readonly List<PetProfileInfo> _profileInfo = [];

		public ScarabTownPetProfile AddVariant(string identifier)
		{
			Asset<Texture2D> texture = DrawHelpers.RequestLocal<BeetleTownPet>(identifier, false);
			int headIndex = NPCHeadLoader.GetHeadSlot(DrawHelpers.RequestLocal<BeetleTownPet>(identifier + "_Head"));

			_profileInfo.Add(new(identifier, headIndex, texture));
			return this;
		}

		public int RollVariation() => Main.rand.Next(_profileInfo.Count);
		public string GetNameForVariant(NPC npc) => npc.getNewNPCName();
		public Asset<Texture2D> GetTextureNPCShouldUse(NPC npc) => _profileInfo[npc.townNpcVariationIndex].Texture;
		public int GetHeadTextureIndex(NPC npc) => _profileInfo[npc.townNpcVariationIndex].HeadIndex;
		public List<string> GetNames(ILocalizedModType localizedType, NPC npc)
		{
			List<string> result = [];

			for (int i = 0; i < 15; i++)
				result.Add(localizedType.GetLocalizedValue("Names." + _profileInfo[npc.townNpcVariationIndex].Identifier + i));

			return result;
		}
	}

	public override string Texture => AssetLoader.EmptyTexture;

	private static ScarabTownPetProfile NPCProfile;

	public override void Load()
	{
		Mod.AddNPCHeadTexture(Type, DrawHelpers.RequestLocal<BeetleTownPet>("RoyalScarab_Head"));
		Mod.AddNPCHeadTexture(Type, DrawHelpers.RequestLocal<BeetleTownPet>("DungBeetle_Head"));
		Mod.AddNPCHeadTexture(Type, DrawHelpers.RequestLocal<BeetleTownPet>("JewelBeetle_Head"));
		Mod.AddNPCHeadTexture(Type, DrawHelpers.RequestLocal<BeetleTownPet>("Ladybug_Head"));
		Mod.AddNPCHeadTexture(Type, DrawHelpers.RequestLocal<BeetleTownPet>("Weevil_Head"));
		Mod.AddNPCHeadTexture(Type, DrawHelpers.RequestLocal<BeetleTownPet>("Maybug_Head"));
	}

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 5;
		NPCID.Sets.DangerDetectRange[Type] = 250;
		NPCID.Sets.HatOffsetY[Type] = -2;
		NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Shimmer] = true;
		NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
		NPCID.Sets.NPCFramingGroup[Type] = 8;

		NPCID.Sets.IsTownPet[Type] = true;
		NPCID.Sets.CannotSitOnFurniture[Type] = true;
		NPCID.Sets.TownNPCBestiaryPriority.Add(Type);
		NPCID.Sets.PlayerDistanceWhilePetting[Type] = 32;
		NPCID.Sets.IsPetSmallForPetting[Type] = true;

		NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new()
		{
			Velocity = 0.25f
		});

		NPCProfile = new ScarabTownPetProfile()
			.AddVariant("RoyalScarab")
			.AddVariant("DungBeetle")
			.AddVariant("JewelBeetle")
			.AddVariant("Ladybug")
			.AddVariant("Weevil")
			.AddVariant("Maybug");
	}

	public override void SetDefaults()
	{
		NPC.townNPC = true; // Town Pets are still considered Town NPCs
		NPC.friendly = true;
		NPC.width = 20;
		NPC.height = 20;
		NPC.aiStyle = NPCAIStyleID.Passive;
		NPC.damage = 10;
		NPC.defense = 15;
		NPC.lifeMax = 250;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath6;
		NPC.knockBackResist = 0.5f;
		NPC.housingCategory = 1;
	}

	public override bool CanTownNPCSpawn(int numTownNPCs) => WorldSystem.CheckWorldFlag(BeetleLicense.UsedLicense);

	public override ITownNPCProfile TownNPCProfile() => NPCProfile;

	public override List<string> SetNPCNameList() => NPCProfile.GetNames(this, NPC);

	public override void SetChatButtons(ref string button, ref string button2) => button = Language.GetTextValue("UI.PetTheAnimal"); //Pet

	public override string GetChat() => this.GetLocalizedValue("Chitter" + Main.rand.Next(3));

	public override void FindFrame(int frameHeight)
	{
		if (Main.dedServ)
			return;

		bool moving = NPC.velocity.X != 0;
		Texture2D texture = NPCProfile.GetTextureNPCShouldUse(NPC).Value;
		NPC.frame.Width = texture.Width / 2;
		NPC.frame.Height = texture.Height / Main.npcFrameCount[Type];

		NPC.frameCounter = moving ? ((NPC.frameCounter + 0.15f) % Main.npcFrameCount[Type]) : 0;

		NPC.frame.X = moving ? NPC.frame.Width : 0;
		NPC.frame.Y = (int)NPC.frameCounter * NPC.frame.Height;

		NPC.frame.Width -= 2; //Remove padding
		NPC.frame.Height -= 2;
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		NPC.spriteDirection = NPC.direction;

		Texture2D texture = NPCProfile.GetTextureNPCShouldUse(NPC).Value;
		SpriteEffects effects = (NPC.spriteDirection == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		Vector2 origin = new(NPC.frame.Width / 2, NPC.frame.Height);

		Main.EntitySpriteDraw(texture, NPC.Bottom - screenPos + new Vector2(0, NPC.gfxOffY + 2), NPC.frame, NPC.DrawColor(drawColor), NPC.rotation, origin, NPC.scale, effects);
		return false;
	}
}