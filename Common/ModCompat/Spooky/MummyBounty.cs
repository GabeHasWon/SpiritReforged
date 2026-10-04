using SpiritReforged.Content.Crossmod.Spooky.Items;
using Terraria.DataStructures;

namespace SpiritReforged.Common.ModCompat.Spooky;

internal class MummyBounty : SpookyBounty
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
		Asset<Texture2D> tex = ModContent.Request<Texture2D>("SpiritReforged/Common/ModCompat/Spooky/MummyBounty");
		spooky.Call("EyeQuest", SpiritReforgedMod.Instance, "MummyBounty", tex, () => BountyActive, (Action<bool>)OnActivate, 
			() => Main.LocalPlayer.HasItem(ModContent.ItemType<BallOfBandages>()), MapDialogue(DialogueType.Encounter), MapDialogue(DialogueType.Recover), 
			MapDialogue(DialogueType.Complete), (Action)OnComplete);
	}

	private static void OnActivate(bool recover)
	{
		BountyActive = true;

		NPC entity = Main.npc[GetLittleEyeIndex()];
		Item.NewItem(new EntitySource_Gift(entity), entity.Hitbox, ModContent.ItemType<MummyQuestItem>());

		if (Main.netMode == NetmodeID.MultiplayerClient)
			new SpookyBountyData(0).Send();
	}

	private static void OnComplete()
	{
		NPC entity = Main.npc[GetLittleEyeIndex()];
		Main.LocalPlayer.ConsumeItem(ModContent.ItemType<BallOfBandages>());
		Item.NewItem(new EntitySource_Gift(entity), entity.Hitbox, ItemID.Zenith);
	}
}
