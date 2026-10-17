using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Content.Crossmod.Spooky.Items;
using SpiritReforged.Content.Crossmod.Spooky.Items.Pets;
using SpiritReforged.Content.Crossmod.Spooky.Tiles;
using Terraria.DataStructures;

namespace SpiritReforged.Common.ModCompat.Spooky;

internal class MummyBounty : SpookyBounty
{
	internal static bool Active = false;
	internal static bool Complete = false;

	protected override int DialogueLength => 3;
	protected override int RecoverDialogueLength => 3;
	protected override int CompleteLength => 2;

	protected override void InnerLoad() 
	{
		if (!ModLoader.HasMod("Spooky"))
			return;

		Mod spooky = ModLoader.GetMod("Spooky");
		Asset<Texture2D> tex = ModContent.Request<Texture2D>("SpiritReforged/Common/ModCompat/Spooky/MummyBounty");
		spooky.Call("EyeQuest", SpiritReforgedMod.Instance, "MummyBounty", tex, () => Active, (Action<bool>)OnActivate, 
			() => Main.LocalPlayer.HasItem(ModContent.ItemType<BallOfBandages>()), () => Complete, MapDialogue(DialogueType.Encounter), MapDialogue(DialogueType.Recover), 
			MapDialogue(DialogueType.Complete), (Action)OnComplete);
	}

	private static void OnActivate(bool recover)
	{
		Active = true;

		QuickItem(Main.npc[GetLittleEyeIndex()], ModContent.ItemType<MummyQuestItem>());

		if (Main.netMode == NetmodeID.MultiplayerClient)
			new SpookyBountyData().Send();
	}

	private static void OnComplete()
	{
		NPC entity = Main.npc[GetLittleEyeIndex()];
		Main.LocalPlayer.ConsumeItem(ModContent.ItemType<BallOfBandages>());
		QuickItem(entity, ItemID.Zenith);
		QuickItem(entity, AutoContent.ItemType<MummyPainting>());

		if (Main.rand.NextBool(20))
			QuickItem(entity, ModContent.ItemType<SandBrownie>());

		CommonDrops(entity);

		Complete = true;

		if (Main.netMode == NetmodeID.MultiplayerClient)
			new SpookyBountyData().Send();
	}
}
