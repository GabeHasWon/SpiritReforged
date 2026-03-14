using SpiritReforged.Common.Easing;
using SpiritReforged.Common.EmoteCommon;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ItemCommon.Pins;
using SpiritReforged.Common.MapCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.NPCCommon.Abstract;
using SpiritReforged.Common.NPCCommon.Interfaces;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.WorldGeneration.PointOfInterest;
using SpiritReforged.Content.Forest.Cartography.Maps;
using SpiritReforged.Content.Forest.Cartography.Pins;
using SpiritReforged.Content.Savanna.Biome;
using System.IO;
using System.Linq;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Forest.Cartography;

[AutoloadHead]
public class Cartographer : WorldNPC, ITravelNPC
{
	/// <summary> Records the position of the last placed pin for the local client. </summary>
	private static Point16 LastPosition;
	/// <summary> Indicates that the fullscreen map can be opened in <see cref="PostDrawChat"/> to avoid throwing an error. </summary>
	private static bool ClickedOpenMap;

	protected override bool CloneNewInstances => true;

	private bool _hasPin = true;

	public override ModNPC Clone(NPC newEntity)
	{
		var cartographer = base.Clone(newEntity) as Cartographer;
		cartographer._hasPin = _hasPin;
		return cartographer;
	}

	public override void Load()
	{
		AutoEmote.LoadFaceEmote(this, static () => NPC.AnyNPCs(ModContent.NPCType<Cartographer>()));
		On_Main.GUIChatDrawInner += PostDrawChat;
	}

	private static void PostDrawChat(On_Main.orig_GUIChatDrawInner orig, Main self)
	{
		orig(self);

		if (ClickedOpenMap)
		{
			Main.LocalPlayer.TryOpeningFullscreenMap();
			Main.mapFullscreenPos = Main.LocalPlayer.Center / 16f;

			MapAnimator.Register(new MapAnimator.Animation()
				.Add(new MapAnimator.EaseSegment(120, LastPosition.ToVector2(), EaseFunction.EaseCubicInOut)),
				new MapAnimator.Animation()
				.Add(new MapAnimator.ZoomSegment(80, 1.5f, EaseFunction.EaseQuarticOut))
				.Add(new MapAnimator.ZoomSegment(100, 1.5f, 4, EaseFunction.EaseQuarticInOut)));

			ClickedOpenMap = false;
		}
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

	public override string GetChat()
	{
		LastPosition = Point16.Zero; //Reset the position on next chat
		return Language.GetTextValue("Mods.SpiritReforged.NPCs.Cartographer.Dialogue." + Main.rand.Next(5));
	}

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

		if (PointOfInterestSystem.AnyInterests && _hasPin)
			button2 = Language.GetTextValue("Mods.SpiritReforged.NPCs.Cartographer.Buttons.Map");
		else if (LastPosition != Point16.Zero)
			button2 = Language.GetTextValue("LegacyInterface.108"); //Open map
	}

	public override void OnChatButtonClicked(bool firstButton, ref string shopName)
	{
		if (firstButton)
		{
			shopName = "Shop";
		}
		else
		{
			if (PointOfInterestSystem.AnyInterests && _hasPin)
			{
				PointOfInterestSystem.Interest interest;
				Point16 position;

				do
				{
					var pair = PointOfInterestSystem.InterestByPosition.ElementAt(Main.rand.Next(PointOfInterestSystem.InterestByPosition.Count));

					position = pair.Key;
					interest = pair.Value;
				}
				while (interest.discovered); //Select a random undiscovered point

				MapPointOfInterest(position, 60, new EntitySource_Gift(NPC));

				_hasPin = false;
				LastPosition = position;
			} //Mapping functionality
			else
			{
				ClickedOpenMap = true;
			} //Open map
		}
	}

	public override void AddShops() => new NPCShop(Type).Add<PinRed>().Add<PinYellow>().Add<PinGreen>().Add<PinBlue>()
		.AddLimited<TornMapPiece>(4, 6).Add(ItemID.Binoculars).Add(ItemID.Compass, Condition.InBelowSurface)
		.Add(AutoContent.ItemType<CartographyTable>()).AddLimited(ItemID.TrifoldMap, 1, Condition.Hardmode).Register();

	public static void MapPointOfInterest(Point16 position, int radius, IEntitySource itemSource)
	{
		const string dialogue = "Mods.SpiritReforged.NPCs.Cartographer.Dialogue.Map";
		PointOfInterestSystem.Interest interest = PointOfInterestSystem.InterestByPosition[position];

		int itemType = (GetPinItemType(interest.type) is int value && value > -1) ? value : Main.rand.Next([.. ModContent.GetContent<PinItem>()]).Type;
		Item item = new(itemType);
		string name = item.ModItem.Name; //The name of the pin, used for identification

		string text = Language.GetTextValue(dialogue + "." + interest.type + "." + Main.rand.Next(3));
		if (Main.LocalPlayer.GetModPlayer<PinPlayer>().unlockedPins.Count == 0)
			text += " " + Language.GetTextValue(dialogue + ".FirstPin"); //If the player owns no pins, append "first pin" dialogue at the end

		Main.npcChatText = text;
		Main.npcChatCornerItem = item.type;

		if (Main.LocalPlayer.PinUnlocked(name))
			Main.LocalPlayer.QuickSpawnItem(itemSource, item); //If the pin is already unlocked, give the player an item copy
		else
			Main.LocalPlayer.UnlockPin(name); //Otherwise, unlock this pin in the map interface

		PinSystem.Place(name, position.ToVector2());
		PointOfInterestSystem.InterestByPosition[position].discovered = true;
		RevealMap.Reveal(position.X, position.Y, radius);

		if (Main.netMode != NetmodeID.SinglePlayer)
			new PointOfInterestSystem.SyncPoIData(position, interest.type, true).Send();
	}

	/// <summary> Gets the pin item associated with <paramref name="interest"/>, returns -1 if none exists. </summary>
	public static int GetPinItemType(InterestType interest)
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
			InterestType.SaltFlat => ModContent.ItemType<PinSaltFlat>(),
			InterestType.Ziggurat => ModContent.ItemType<PinZiggurat>(),
			_ => -1
		};

		return type;
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
			Dust.NewDustPerfect(Main.rand.NextVector2FromRectangle(NPC.getRect()), DustID.Blood, Main.rand.NextVector2Unit() * 1.5f, 0, default, Main.rand.NextFloat(1f, 1.5f));
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
			if (p.TryGetModPlayer(out PinPlayer pinPl) && pinPl.PinProgress != 0)
				return true;

		return false;
	}
}