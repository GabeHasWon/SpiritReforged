using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.Crossmod.Spooky.Items;
using SpiritReforged.Content.Crossmod.Spooky.Items.Pets;
using SpiritReforged.Content.Crossmod.Spooky.Tiles;
using Terraria.DataStructures;

namespace SpiritReforged.Common.ModCompat.Spooky;

internal class DhampirBounty : SpookyBounty
{
	internal static bool BountyActive = false;

	protected override int DialogueLength => 3;
	protected override int RecoverDialogueLength => 3;
	protected override int CompleteLength => 2;

	protected override void InnerLoad() 
	{
		if (!ModLoader.HasMod("Spooky"))
			return;

		Mod spooky = ModLoader.GetMod("Spooky");
		Asset<Texture2D> tex = ModContent.Request<Texture2D>("SpiritReforged/Common/ModCompat/Spooky/DhampirBounty");
		spooky.Call("EyeQuest", SpiritReforgedMod.Instance, "DhampirBounty", tex, () => BountyActive, (Action<bool>)OnActivate, 
			() => Main.LocalPlayer.HasItem(ModContent.ItemType<DhampirCompletionItem>()), MapDialogue(DialogueType.Encounter), MapDialogue(DialogueType.Recover), 
			MapDialogue(DialogueType.Complete), (Action)OnComplete);
	}

	private static void OnActivate(bool recover)
	{
		BountyActive = true;

		NPC entity = Main.npc[GetLittleEyeIndex()];
		Item.NewItem(new EntitySource_Gift(entity), entity.Hitbox, ModContent.ItemType<DhampirQuestItem>());

		if (Main.netMode == NetmodeID.MultiplayerClient)
			new SpookyBountyData(0).Send();
	}

	public static void OnComplete()
	{
		NPC entity = Main.npc[GetLittleEyeIndex()];
		Main.LocalPlayer.ConsumeItem(ModContent.ItemType<DhampirCompletionItem>());
		Item.NewItem(new EntitySource_Gift(entity), entity.Hitbox, ItemID.Zenith);
		Item.NewItem(new EntitySource_Gift(entity), entity.Hitbox, AutoContent.ItemType<DhampirPainting>());
		Item.NewItem(new EntitySource_Gift(entity), entity.Hitbox, ModContent.ItemType<FruitBrownie>());
	}
}
