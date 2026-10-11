using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.WorldGeneration.Ecotones;
using SpiritReforged.Content.Crossmod.Spooky.Items;
using SpiritReforged.Content.Crossmod.Spooky.Items.Pets;
using SpiritReforged.Content.Crossmod.Spooky.Tiles;
using SpiritReforged.Content.SaltFlats;
using Terraria.DataStructures;

namespace SpiritReforged.Common.ModCompat.Spooky;

internal class DhampirBounty : SpookyBounty
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
		Asset<Texture2D> tex = ModContent.Request<Texture2D>("SpiritReforged/Common/ModCompat/Spooky/DhampirBounty");
		spooky.Call("EyeQuest", SpiritReforgedMod.Instance, "DhampirBounty", tex, () => Active, (Action<bool>)OnActivate, 
			() => Main.LocalPlayer.HasItem(ModContent.ItemType<DhampirCompletionItem>()), () => Complete, MapDialogue(DialogueType.Encounter), 
			MapDialogue(DialogueType.Recover), MapDialogue(DialogueType.Complete), (Action)OnComplete, null, 
			() => !EcotoneSurfaceMapping.ContainsEcotone<SaltFlatsEcotone>());
	}

	private static void OnActivate(bool recover)
	{
		Active = true;

		NPC entity = Main.npc[GetLittleEyeIndex()];
		Item.NewItem(new EntitySource_Gift(entity), entity.Hitbox, ModContent.ItemType<DhampirQuestItem>());

		if (Main.netMode == NetmodeID.MultiplayerClient)
			new SpookyBountyData().Send();
	}

	public static void OnComplete()
	{
		NPC entity = Main.npc[GetLittleEyeIndex()];
		Main.LocalPlayer.ConsumeItem(ModContent.ItemType<DhampirCompletionItem>());
		QuickItem(entity, ModContent.ItemType<DhampirCape>());
		QuickItem(entity, AutoContent.ItemType<DhampirPainting>());
		QuickItem(entity, ModContent.ItemType<FruitBrownie>());

		Complete = true;

		if (Main.netMode == NetmodeID.MultiplayerClient)
			new SpookyBountyData().Send();
	}
}
