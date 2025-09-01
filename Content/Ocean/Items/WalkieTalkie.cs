using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.WorldGeneration;
using System.Linq;
using Terraria.Audio;

namespace SpiritReforged.Content.Ocean.Items;

public class WalkieTalkie : ModItem
{
	public static readonly SoundStyle Static = new("SpiritReforged/Assets/SFX/Item/RadioStatic")
	{
		Volume = 0.7f,
		PitchVariance = 0.15f
	};

	[WorldBound]
	internal static bool Paging;
	internal static bool DidCompleteQuest;

	#region detours
	public override void Load()
	{
		On_Player.Update += ForceChatText;
		On_Main.GUIChatDrawInner += PreventQuestCompletion;
		On_Lang.AnglerQuestChat += ModifyQuestDialogue;
		On_ShopHelper.GetShoppingSettings += RemoveHappinessReport;
	}

	/// <summary> Forces NPC dialogue to persist even when the player is out of range of the NPC. </summary>
	private static void ForceChatText(On_Player.orig_Update orig, Player self, int i) //IL 12445 //CS 2965
	{
		if (Paging)
		{
			if (self.talkNPC != -1)
			{
				var npc = Main.npc[self.talkNPC];

				Vector2 oldPosition = npc.position;
				string happinessReport = Main.LocalPlayer.currentShoppingSettings.HappinessReport;

				npc.position = self.Center;
				Main.LocalPlayer.currentShoppingSettings.HappinessReport = string.Empty;

				orig(self, i);

				npc.position = oldPosition;
				Main.LocalPlayer.currentShoppingSettings.HappinessReport = happinessReport;
				return;
			}
			else
			{
				Paging = false;
			}
		}

		orig(self, i);
	}

	/// <summary> Prevents angler quest completion when paging by pretending the player has already completed it, in addition to drawing a custom text icon using <see cref="DrawIcon(Vector2)"/>. </summary>
	private static void PreventQuestCompletion(On_Main.orig_GUIChatDrawInner orig, Main self)
	{
		if (Paging)
		{
			DidCompleteQuest = Main.anglerQuestFinished;
			Main.anglerQuestFinished = true;

			orig(self);

			Main.anglerQuestFinished = DidCompleteQuest;
			DrawIcon(new Vector2(Main.screenWidth / 2 - 250, 110));

			return;
		}

		orig(self);
	}

	private static void DrawIcon(Vector2 position)
	{
		var sb = Main.spriteBatch;
		var icon = TextureAssets.Item[ModContent.ItemType<WalkieTalkie>()].Value;
		var outline = TextureColorCache.ColorSolid(icon, Color.White);

		DrawHelpers.DrawOutline(sb, outline, position, Color.Black * 0.25f);
		sb.Draw(icon, position, null, Color.White, 0, icon.Size() / 2, 1, default, 0);
	}

	/// <summary> Prevents quest completion dialogue from <see cref="PreventQuestCompletion"/>. </summary>
	private string ModifyQuestDialogue(On_Lang.orig_AnglerQuestChat orig, bool turnIn)
	{
		string value = orig(turnIn);

		if (Paging && !DidCompleteQuest)
		{
			int id = Main.npcChatCornerItem = Main.anglerQuestItemNetIDs[Main.anglerQuest];
			value = Language.GetTextValueWith("AnglerQuestText.Quest_" + ItemID.Search.GetName(id), Lang.CreateDialogSubstitutionObject());

			if (Main.LocalPlayer.HasItem(id))
			{
				value = value.Insert(value.LastIndexOf('\n'), $"\n{Language.GetTextValue("Mods.SpiritReforged.NPCs.VanillaDialogue.Angler.Pager0")}\n");
			}
			else if (Main.rand.NextBool())
			{
				value = value.Insert(value.LastIndexOf('\n'), $"\n{Language.GetTextValue("Mods.SpiritReforged.NPCs.VanillaDialogue.Angler.Pager1")}\n");
			}
		}

		return value;
	}

	private static ShoppingSettings RemoveHappinessReport(On_ShopHelper.orig_GetShoppingSettings orig, ShopHelper self, Player player, NPC npc)
	{
		var value = orig(self, player, npc);

		if (Paging)
			value.HappinessReport = string.Empty;

		return value;
	}
	#endregion

	public override void SetStaticDefaults() => PlayerEvents.OnAnglerQuestReward += (player, rareMultiplier, rewardItems) =>
	{
		if (player.anglerQuestsFinished == 3) //Guaranteed on the 3rd completed quest
			rewardItems.Add(new Item(Type));
	};

	public override void SetDefaults()
	{
		Item.width = Item.height = 32;
		Item.useAnimation = Item.useTime = 10;
		Item.maxStack = 1;
		Item.autoReuse = false;
		Item.useTurn = true;
		Item.rare = ItemRarityID.Green;
		Item.useStyle = ItemUseStyleID.HiddenAnimation;
		Item.noUseGraphic = true;
		Item.value = Item.sellPrice(silver: 10);
	}

	public override bool? UseItem(Player player)
	{
		if (!Main.dedServ && player.whoAmI == Main.myPlayer && player.ItemAnimationJustStarted)
		{
			SoundEngine.PlaySound(SoundID.Mech, player.Center);

			if (NPC.FindFirstNPC(NPCID.Angler) is int whoAmI && whoAmI != -1)
			{
				Paging = true;
				player.SetTalkNPC(whoAmI);
				Main.npcChatText = Main.npc[whoAmI].GetChat();

				SoundEngine.PlaySound(Static, player.Center);
				
				return true;
			}
			else
			{
				return false;
			}
		}

		return null;
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		if (!NPC.AnyNPCs(NPCID.Angler)) //Display whether the angler exists
		{
			int index = tooltips.IndexOf(tooltips.FirstOrDefault(x => x.Mod == "Terraria" && x.Name == "Tooltip0"));
			TooltipLine tooltip = new(Mod, "Deactivated", Language.GetTextValue("Mods.SpiritReforged.Items.WalkieTalkie.Disabled"));

			if (index == -1)
				tooltips.Add(tooltip);
			else
				tooltips.Insert(index + 1, tooltip);
		}
	}
}