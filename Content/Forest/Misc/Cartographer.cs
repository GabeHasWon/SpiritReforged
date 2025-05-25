using SpiritReforged.Common.ItemCommon.Pins;
using SpiritReforged.Common.MapCommon;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Content.Forest.Misc.Pins;
using SpiritReforged.Content.Forest.Misc.Maps;
using SpiritReforged.Content.Savanna.Biome;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using SpiritReforged.Common.WorldGeneration.PointOfInterest;
using SpiritReforged.Common.NPCCommon.Abstract;
using SpiritReforged.Common.PlayerCommon;
using System.IO;
using SpiritReforged.Common.ModCompat;

namespace SpiritReforged.Content.Forest.Misc;

[AutoloadHead]
public class Cartographer : WorldNPC, ITravelNPC
{
	protected override bool CloneNewInstances => true;

	private bool _hasPin = true;

	public override ModNPC Clone(NPC newEntity)
	{
		var cartographer = base.Clone(newEntity) as Cartographer;
		cartographer._hasPin = _hasPin;
		return cartographer;
	}

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		Main.npcFrameCount[Type] = 25;

		NPCID.Sets.ExtraFramesCount[Type] = 9;
		NPCID.Sets.AttackFrameCount[Type] = 4;
		NPCID.Sets.DangerDetectRange[Type] = 600;
		NPCID.Sets.AttackType[Type] = -1;
		NPCID.Sets.AttackTime[Type] = 20;
		NPCID.Sets.HatOffsetY[Type] = 2;

		NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers()
		{ Velocity = 1f });
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "Surface");
	public override string GetChat() => Language.GetTextValue("Mods.SpiritReforged.NPCs.Cartographer.Dialogue." + Main.rand.Next(5));

	public override List<string> SetNPCNameList()
	{
		List<string> names = [];

		for (int i = 0; i < 6; ++i)
			names.Add(Language.GetTextValue("Mods.SpiritReforged.NPCs.Cartographer.Names." + i));

		return names;
	}

	public override void SendExtraAI(BinaryWriter writer) => writer.Write(_hasPin);
	public override void ReceiveExtraAI(BinaryReader reader) => _hasPin = reader.ReadBoolean();

	public override void SetChatButtons(ref string button, ref string button2)
	{
		button = Language.GetTextValue("LegacyInterface.28");
		button2 = !PointOfInterestSystem.HasAnyInterests() || !_hasPin ? string.Empty : Language.GetTextValue("Mods.SpiritReforged.NPCs.Cartographer.Buttons.Map");
	}

	public override void OnChatButtonClicked(bool firstButton, ref string shopName)
	{
		if (firstButton)
			shopName = "Shop";
		else
			MapFunctionality();
	}

	public override void AddShops() => new NPCShop(Type).Add<PinRed>().Add<PinYellow>().Add<PinGreen>().Add<PinBlue>()
		.AddLimited<TornMapPiece>(4, 6).Add(ItemID.Binoculars).Add(ItemID.Compass, Condition.InBelowSurface).AddLimited(ItemID.TrifoldMap, 1, Condition.Hardmode).Register();

	private void MapFunctionality()
	{
		const int Radius = 60;

		InterestType type;

		do 
		{
			type = (InterestType)Main.rand.Next((int)InterestType.Count);
		} while (!PointOfInterestSystem.HasInterestType(type));

		var item = new Item(GetPinType(type));
		string pinName = item.ModItem.Name;
		bool firstPin = Main.LocalPlayer.GetModPlayer<PinPlayer>().unlockedPins.Count == 0;

		if (Main.LocalPlayer.PinUnlocked(pinName))
			Main.LocalPlayer.QuickSpawnItem(new EntitySource_Gift(NPC), item); //If the pin is already unlocked, give the player an item copy
		else
			Main.LocalPlayer.UnlockPin(pinName);

		Point16 point = PointOfInterestSystem.GetPoint(type);
		PinSystem.Place(pinName, point.ToVector2());
		PointOfInterestSystem.RemovePoint(point, type);

		string text = Language.GetTextValue("Mods.SpiritReforged.NPCs.Cartographer.Dialogue.Map." + type + "." + Main.rand.Next(3));
		if (firstPin)
			text += " " + Language.GetTextValue("Mods.SpiritReforged.NPCs.Cartographer.Dialogue.Map.FirstPin"); //Append "first pin" dialogue at the end

		Main.npcChatText = text;
		Main.npcChatCornerItem = item.type;

		RevealMap.Reveal(point.X, point.Y, Radius);

		_hasPin = false;

		static int GetPinType(InterestType interest)
		{
			if (interest is InterestType.BloodAltar && CrossMod.Thorium.Enabled)
				return ModContent.ItemType<PinBlood>();
			else if (interest is InterestType.WulfrumBunker && CrossMod.Fables.Enabled)
				return ModContent.ItemType<PinWulfrum>();

			int type = interest switch
			{
				InterestType.FloatingIsland => ModContent.ItemType<PinSky>(),
				InterestType.EnchantedSword => ModContent.ItemType<PinSword>(),
				InterestType.ButterflyShrine => ModContent.ItemType<PinButterfly>(),
				InterestType.Shimmer => ModContent.ItemType<PinFaeling>(),
				InterestType.Savanna => ModContent.ItemType<PinSavanna>(),
				InterestType.Hive => ModContent.ItemType<PinHive>(),
				InterestType.Curiosity => ModContent.ItemType<PinCuriosity>(),
				_ => Main.rand.Next([.. ModContent.GetContent<PinItem>()]).Type //Random
			};

			return type;
		}
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (Main.dedServ)
			return;

		if (NPC.life <= 0)
		{
			for (int i = 1; i < 7; i++)
			{
				int goreType = Mod.Find<ModGore>(nameof(Cartographer) + i).Type;
				Gore.NewGore(NPC.GetSource_Death(), Main.rand.NextVector2FromRectangle(NPC.getRect()), NPC.velocity, goreType);
			}
		}

		for (int d = 0; d < 8; d++)
			Dust.NewDustPerfect(Main.rand.NextVector2FromRectangle(NPC.getRect()), DustID.Blood,
				Main.rand.NextVector2Unit() * 1.5f, 0, default, Main.rand.NextFloat(1f, 1.5f));
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo)
	{
		if (SpawnedToday || spawnInfo.Invasion || spawnInfo.Water)
			return 0; //Never spawn during an invasion, in water or if already spawned that day

		float multiplier = MathHelper.Lerp(1.75f, .5f, spawnInfo.Player.GetModPlayer<PinPlayer>().PinProgress) * (Main.hardMode ? .6f : 1f);

		if (spawnInfo.SpawnTileY > Main.worldSurface && spawnInfo.SpawnTileY < Main.UnderworldLayer && !spawnInfo.Player.ZoneEvil())
			return .00023f * multiplier; //Rarely spawn in caves above underworld height

		if ((spawnInfo.Player.InModBiome<SavannaBiome>() || spawnInfo.Player.ZoneDesert || spawnInfo.Player.ZoneJungle || OuterThirds(spawnInfo.SpawnTileX) && spawnInfo.Player.InZonePurity() && !spawnInfo.Player.ZoneSkyHeight) && Main.dayTime)
			return .0024f * multiplier; //Spawn most commonly in the Savanna, Desert, Jungle, and outer thirds of the Forest during the day

		return 0;

		static bool OuterThirds(int x) => x < Main.maxTilesX / 3 || x > Main.maxTilesX - Main.maxTilesY / 3;
	}

	public bool CanSpawnTraveler()
	{
		foreach (var p in Main.ActivePlayers)
		{
			if (p.TryGetModPlayer(out PinPlayer pinPl) && pinPl.PinProgress != 0)
				return true;
		}

		return false;
	}
}