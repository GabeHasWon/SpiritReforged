using SpiritReforged.Content.Crossmod.Spooky;
using Terraria.DataStructures;

namespace SpiritReforged.Common.ModCompat.Spooky;

internal class DhampirBounty : SpookyBounty
{
	private static bool BountyActive = false;

	protected override int DialogueLength => 3;
	protected override int RecoverDialogueLength => 3;

	protected override void InnerLoad() 
	{
		if (!ModLoader.HasMod("Spooky"))
			return;

		Mod spooky = ModLoader.GetMod("Spooky");
		Asset<Texture2D> tex = ModContent.Request<Texture2D>("SpiritReforged/Common/ModCompat/Spooky/DhampirBounty");
		spooky.Call("EyeQuest", SpiritReforgedMod.Instance, "TestBounty", tex, () => BountyActive, (Action<bool>)OnActivate, () => false, MapDialogue(false), MapDialogue(true));
	}

	private static void OnActivate(bool recover)
	{
		BountyActive = true;

		NPC entity = Main.npc[GetLittleEyeIndex()];
		Item.NewItem(new EntitySource_Gift(entity), entity.Hitbox, ModContent.ItemType<DhampirQuestItem>());
	}
}
