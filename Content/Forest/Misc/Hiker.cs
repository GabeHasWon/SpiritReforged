using SpiritReforged.Common.NPCCommon;
using Terraria.DataStructures;
using SpiritReforged.Common.ItemCommon.Backpacks;
using SpiritReforged.Content.Forest.Backpacks;
using SpiritReforged.Content.Savanna.Items.Gar;
using Terraria.Utilities;
using Terraria.GameContent.Bestiary;
using System.Linq;
using SpiritReforged.Common.NPCCommon.Abstract;

namespace SpiritReforged.Content.Forest.Misc;

[AutoloadHead]
public class Hiker : WorldNPC
{
	/// <summary> Stores all information for the hiker to pass properly between clones. </summary>
	private class HikerInfo
	{
		/// <summary> If the hiker has a bundle to sell. </summary>
		public bool hasBundle = true;

		/// <summary> If the hiker has been fed, and now gives away the bundle for free. </summary>
		public bool priceOff = false;
	}

	public static WeightedRandom<(int, Range)> ItemPool
	{
		get
		{
			WeightedRandom<(int, Range)> pool = new(Main.rand);
			pool.Add((ItemID.Glowstick, 6..12), 1);
			pool.Add((ItemID.Rope, 25..35), 1);
			pool.Add((ItemID.SwiftnessPotion, 1..3), 0.8f);
			pool.Add((ItemID.Bomb, 5..10), 0.5f);
			pool.Add((ModContent.ItemType<QuenchPotion>(), 1..2), 0.3f);
			pool.Add((ItemID.Dynamite, 1..2), 0.1f);
			return pool;
		}
	}

	protected override bool CloneNewInstances => true;

	private static Asset<Texture2D> stickTexture;
	//private static Profiles.StackedNPCProfile npcProfile;

	private HikerInfo _info = new();

	public override ModNPC Clone(NPC newEntity)
	{
		var newNPC = base.Clone(newEntity);
		var hiker = newNPC as Hiker;
		hiker._info = _info;
		return newNPC;
	}

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();

		Main.npcFrameCount[Type] = 25;

		NPCID.Sets.ExtraFramesCount[Type] = 9;
		NPCID.Sets.AttackFrameCount[Type] = 4;
		NPCID.Sets.DangerDetectRange[Type] = 500;
		NPCID.Sets.PrettySafe[Type] = 50;
		NPCID.Sets.AttackType[Type] = 3;
		NPCID.Sets.AttackTime[Type] = 20;
		NPCID.Sets.HatOffsetY[Type] = 2;
		NPCID.Sets.AttackAverageChance[Type] = 30;

		stickTexture = ModContent.Request<Texture2D>(Texture + "Stick");
		//npcProfile = new Profiles.StackedNPCProfile(new Profiles.DefaultNPCProfile(Texture, NPCHeadLoader.GetHeadSlot(HeadTexture), Texture + "_Party"));

		NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers()
		{ Velocity = 1f });
	}

	public override void SetDefaults()
	{
		base.SetDefaults();
		_info = new();
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "Surface");

	public override List<string> SetNPCNameList()
	{
		List<string> nameList = [];

		for (int i = 0; i < 6; i++)
			nameList.Add(Language.GetTextValue("Mods.SpiritReforged.NPCs.Hiker.Name." + i));

		return nameList;
	}

	public override void SetChatButtons(ref string button, ref string button2)
	{
		if (_info.hasBundle)
		{
			string silver = Language.GetTextValue("Mods.SpiritReforged.NPCs.Hiker.Buttons.Silver");
			button = Language.GetTextValue("Mods.SpiritReforged.NPCs.Hiker.Buttons.Supplies") + (_info.priceOff ? "" : $"([c/AAAAAA:{silver}])");
		}
		else
			button = "";

		if (_info.hasBundle && !_info.priceOff && PlayerHasFood(out int _))
			button2 = Language.GetTextValue("Mods.SpiritReforged.NPCs.Hiker.Buttons.Feed");
	}

	public override void OnChatButtonClicked(bool firstButton, ref string shopName)
	{
		if (firstButton) // Buy bundle
		{
			int cost = Item.buyPrice(0, 0, 20, 0);

			if (Main.LocalPlayer.CanAfford(cost) && Main.LocalPlayer.PayCurrency(cost) && _info.hasBundle)
			{
				SpawnBundle();

				Main.npcChatText = Language.GetTextValue("Mods.SpiritReforged.NPCs.Hiker.Dialogue.Purchase." + Main.rand.Next(5));
			}
			else
				Main.npcChatText = Language.GetTextValue("Mods.SpiritReforged.NPCs.Hiker.Dialogue.FailPurchase." + Main.rand.Next(3));
		}
		else if (PlayerHasFood(out int id))
		{
			Main.LocalPlayer.ConsumeItem(id);
			Main.npcChatText = Language.GetTextValue("Mods.SpiritReforged.NPCs.Hiker.Dialogue.Hungry.Thanks." + Main.rand.Next(4));

			AdvancedPopupRequest request = new()
			{
				Color = Color.Red,
				DurationInFrames = 120,
				Text = $"-1 {Lang.GetItemNameValue(id)}",
				Velocity = new Vector2(0, -16)
			};

			PopupText.NewText(request, Main.LocalPlayer.Center);

			_info.priceOff = true;
		}
	}

	private void SpawnBundle()
	{
		var newItem = new Item(Main.rand.Next(5) switch
		{
			0 => ModContent.ItemType<FashionableBackpack>(),
			1 => ModContent.ItemType<HikingBackpack>(),
			2 => ModContent.ItemType<Minipack>(),
			3 => ModContent.ItemType<PinkPack>(),
			_ => ModContent.ItemType<PouchPack>()
		});

		var backpack = newItem.ModItem as BackpackItem;
		bool checkForDuplicates = ItemPool.elements.Count >= backpack.items.Length; //Only check for duplicates if enough items exist in the pool

		for (int i = 0; i < backpack.items.Length; ++i)
		{
			var slot = backpack.items[i];
			(int type, Range stackRange) = ItemPool.Get();

			if (checkForDuplicates && backpack.items.Where(x => x.type == type).Any()) //This is a duplicate item type; try again
			{
				i--;
				continue;
			}

			slot.SetDefaults(type);
			slot.stack = Main.rand.Next(stackRange.Start.Value, stackRange.End.Value + 1);
		}

		Main.LocalPlayer.QuickSpawnItem(new EntitySource_Gift(NPC), newItem);
		_info.hasBundle = false;
	}

	public override string GetChat()
	{
		if (_info.hasBundle && !_info.priceOff && PlayerHasFood(out int type))
			return Language.GetText("Mods.SpiritReforged.NPCs.Hiker.Dialogue.Hungry.Asking." + Main.rand.Next(4)).WithFormatArgs($"[i:{type}]").Value;

		return Language.GetTextValue("Mods.SpiritReforged.NPCs.Hiker.Dialogue.Idle." + Main.rand.Next(5));
	}

	private static bool PlayerHasFood(out int itemId)
	{
		itemId = -1;

		for (int i = 0; i < Main.LocalPlayer.inventory.Length; ++i)
		{
			Item item = Main.LocalPlayer.inventory[i];

			if (item.potion || ItemID.Sets.IsFood[item.type])
			{
				itemId = item.type;
				return true;
			}
		}

		return false;
	}

	public ButtonText[] AddButtons()
	{
		if (_info.hasBundle && !_info.priceOff && PlayerHasFood(out _))
			return [new ButtonText("Feed", Language.GetTextValue("Mods.SpiritReforged.NPCs.Hiker.Buttons.Feed"))];

		return [];
	}

	public void OnClickButton(ButtonText button)
	{
		if (button.Name == "Feed" && PlayerHasFood(out int id))
		{
			Main.LocalPlayer.ConsumeItem(id);
			Main.npcChatText = Language.GetTextValue("Mods.SpiritReforged.NPCs.Hiker.Dialogue.Hungry.Thanks." + Main.rand.Next(4));

			AdvancedPopupRequest request = new()
			{
				Color = Color.Red,
				DurationInFrames = 120,
				Text = $"-1 {Lang.GetItemNameValue(id)}",
				Velocity = new Vector2(0, -16)
			};

			PopupText.NewText(request, Main.LocalPlayer.Center);

			_info.priceOff = true;
		}
	}

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (Main.dedServ)
			return;

		if (NPC.life <= 0)
		{
			for (int i = 1; i < 6; i++)
			{
				int goreType = Mod.Find<ModGore>(nameof(Hiker) + i).Type;
				Gore.NewGore(NPC.GetSource_Death(), Main.rand.NextVector2FromRectangle(NPC.getRect()), NPC.velocity, goreType);
			}
		}

		for (int d = 0; d < 8; d++)
			Dust.NewDustPerfect(Main.rand.NextVector2FromRectangle(NPC.getRect()), DustID.Blood,
				Main.rand.NextVector2Unit() * 1.5f, 0, default, Main.rand.NextFloat(1f, 1.5f));
	}

	//public override ITownNPCProfile TownNPCProfile() => npcProfile;

	public override void TownNPCAttackStrength(ref int damage, ref float knockback)
	{
		damage = 15;
		knockback = 3f;
	}
	public override void TownNPCAttackSwing(ref int itemWidth, ref int itemHeight) => itemWidth = itemHeight = 30;
	public override void DrawTownAttackSwing(ref Texture2D item, ref Rectangle itemFrame, ref int itemSize, ref float scale, ref Vector2 offset)
	{
		item = stickTexture.Value;
		itemFrame = stickTexture.Frame();
		itemSize = 30;
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo)
	{
		if (SpawnedToday || spawnInfo.Invasion || spawnInfo.Water)
			return 0; //Never spawn during an invasion, in water or if already spawned that day

		float multiplier = Main.hardMode ? .5f : ((NPC.downedBoss1 || NPC.downedSlimeKing) ? 1f : 2f); //Double the rate pre-boss, and halve it in hardmode

		if ((spawnInfo.Player.ZoneSnow || InnerThird(spawnInfo.SpawnTileX) && spawnInfo.Player.InZonePurity()) && spawnInfo.Player.ZoneOverworldHeight && Main.dayTime)
			return .0024f * multiplier; //Spawn most commonly in the Snow and inner thirds of the Forest during the day

		return 0;

		static bool InnerThird(int x)
		{
			int third = Main.maxTilesX / 3;
			return x > third && x < Main.maxTilesX - third;
		}
	}
}