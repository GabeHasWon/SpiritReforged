using SpiritReforged.Common.WorldGeneration;
using System.IO;
using Terraria.GameContent.Bestiary;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Underground.NPCs;

[AutoloadHead]
public class PotterySlime : ModNPC
{
	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 14;

		NPCID.Sets.DangerDetectRange[Type] = 250;
		NPCID.Sets.HatOffsetY[Type] = 0;
		NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Shimmer] = true;
		NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
		
		NPCID.Sets.CannotSitOnFurniture[Type] = false;
		NPCID.Sets.TownNPCBestiaryPriority.Add(Type);
		NPCID.Sets.PlayerDistanceWhilePetting[Type] = 26;
		NPCID.Sets.IsPetSmallForPetting[Type] = true;

		NPCID.Sets.IsTownSlime[Type] = true;
		NPCID.Sets.IsTownPet[Type] = true;

		NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new()
		{ Velocity = 0.25f };

		NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);
	}

	public override void SetDefaults()
	{
		const int template = NPCID.TownSlimeBlue;

		NPC.CloneDefaults(template);
		AnimationType = template;
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "Underground");
	public override string GetChat() => Language.GetTextValue("SlimeBlueChatter.Chatter_" + Main.rand.Next(1, 4));
	public override void SetChatButtons(ref string button, ref string button2) => button = Language.GetTextValue("UI.PetTheAnimal"); //Pet
	public override bool CanTownNPCSpawn(int numTownNPCs) => PotteryTracker.Remaining == 0;

	public override List<string> SetNPCNameList()
	{
		List<string> names = [];

		for (int i = 0; i < 27; i++)
			names.Add(Language.GetTextValue("Mods.SpiritReforged.NPCs.PotterySlime.Names." + i));

		return names;
	}
}

/// <summary> Keeps track of the remaining uncommon pots needed to spawn <see cref="PotterySlime"/>. </summary>
internal class PotteryTracker : ModSystem
{
	[WorldBound]
	public static ushort Remaining;

	/// <summary> Safely increments <see cref="Remaining"/>. </summary>
	public static bool TrackOne()
	{
		Remaining = (ushort)Math.Max(Remaining - 1, 0);
		return Remaining == 0;
	}

	public override void NetSend(BinaryWriter writer) => writer.Write(Remaining);
	public override void NetReceive(BinaryReader reader) => Remaining = reader.ReadUInt16();

	public override void SaveWorldData(TagCompound tag) => tag[nameof(Remaining)] = Remaining;
	public override void LoadWorldData(TagCompound tag) => Remaining = tag.Get<ushort>(nameof(Remaining));
}