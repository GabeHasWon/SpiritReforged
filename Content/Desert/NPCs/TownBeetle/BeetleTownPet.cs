using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Visuals;
using System.Linq;

namespace SpiritReforged.Content.Desert.NPCs.TownBeetle;

public class BeetleTownPet : ModNPC
{
	private readonly record struct PetProfile
	{
		public readonly int HeadIndex;
		public readonly Asset<Texture2D> Texture;
		public readonly string[] Names;

		public PetProfile(int HeadIndex, Asset<Texture2D> Texture, string[] Names)
		{
			this.HeadIndex = HeadIndex;
			this.Texture = Texture;
			this.Names = Names;
		}

		public PetProfile(string Identifier, string[] Names)
		{
			Mod mod = SpiritReforgedMod.Instance;

			HeadIndex = mod.AddNPCHeadTexture(ModContent.NPCType<BeetleTownPet>(), DrawHelpers.RequestLocal<BeetleTownPet>(Identifier + "_Head"));
			Texture = DrawHelpers.RequestLocal<BeetleTownPet>(Identifier, false);
			this.Names = Names;
		}
	}

	public class ScarabTownPetProfile : ITownNPCProfile
	{
		public int RollVariation() => Main.rand.Next(6);
		public string GetNameForVariant(NPC npc) => npc.getNewNPCName();
		public Asset<Texture2D> GetTextureNPCShouldUse(NPC npc) => GetPetProfile(npc.townNpcVariationIndex).Texture;
		public int GetHeadTextureIndex(NPC npc) => GetPetProfile(npc.townNpcVariationIndex).HeadIndex;
	}

	public override string Texture => AssetLoader.EmptyTexture;

	private static readonly List<PetProfile> PetProfiles = [];
	private static ITownNPCProfile NPCProfile;

	private static PetProfile GetPetProfile(int value)
	{
		if (value >= PetProfiles.Count || value < 0)
			value = 0;

		return PetProfiles[value];
	}

	public override void Load()
	{
		PetProfiles.Add(new("RoyalScarab", []));
		PetProfiles.Add(new("DungBeetle", []));
		PetProfiles.Add(new("JewelBeetle", []));
		PetProfiles.Add(new("Ladybug", []));
		PetProfiles.Add(new("Weevil", []));
		PetProfiles.Add(new("Maybug", []));
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
		NPCID.Sets.CannotSitOnFurniture[Type] = false;
		NPCID.Sets.TownNPCBestiaryPriority.Add(Type);
		NPCID.Sets.PlayerDistanceWhilePetting[Type] = 32;
		NPCID.Sets.IsPetSmallForPetting[Type] = true;

		NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new()
		{
			Velocity = 0.25f
		});

		NPCProfile = new ScarabTownPetProfile();
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

	public override bool CanTownNPCSpawn(int numTownNPCs) => false; //Change on license

	public override ITownNPCProfile TownNPCProfile() => NPCProfile;

	public override List<string> SetNPCNameList() => GetPetProfile(NPC.townNpcVariationIndex).Names.ToList();

	public override void SetChatButtons(ref string button, ref string button2) => button = Language.GetTextValue("UI.PetTheAnimal"); //Pet

	public override void FindFrame(int frameHeight)
	{
		if (Main.dedServ)
			return;

		bool moving = NPC.velocity.X != 0;
		Texture2D texture = GetPetProfile(NPC.townNpcVariationIndex).Texture.Value;
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

		Texture2D texture = GetPetProfile(NPC.townNpcVariationIndex).Texture.Value;
		SpriteEffects effects = (NPC.spriteDirection == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		Vector2 origin = new(NPC.frame.Width / 2, NPC.frame.Height);

		Main.EntitySpriteDraw(texture, NPC.Bottom - screenPos + new Vector2(0, NPC.gfxOffY + 2), NPC.frame, NPC.DrawColor(drawColor), NPC.rotation, origin, NPC.scale, effects);
		return false;
	}
}