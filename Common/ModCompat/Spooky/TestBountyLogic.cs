namespace SpiritReforged.Common.ModCompat.Spooky;

internal class TestBountyLogic : ILoadable
{
	private static bool BountyActive = false;

	void ILoadable.Load(Mod mod)
	{
		if (!ModLoader.HasMod("Spooky"))
			return;

		Mod spooky = ModLoader.GetMod("Spooky");
		Asset<Texture2D> tex = ModContent.Request<Texture2D>("SpiritReforged/Common/ModCompat/Spooky/TestBounty");
		(string npc, string player)[] dialogue = [("Hey man.", "What's up?"), ("Not much.", "Oh, alright."), ("Yeah.", "Yeah.")];
		(string npc, string player)[] recoverDialogue = [("Hey again.", "Yeah?"), ("Well...", "Well...?"), ("I dunno. Nothing.", "Oh, okay. Bye?")];
		spooky.Call("EyeQuest", mod, "TestBounty", tex, () => BountyActive, (Action<bool>)OnActivate, () => false, dialogue, recoverDialogue);
	}

	private static void OnActivate(bool recover)
	{
		BountyActive = true;
		Main.NewText("hi!" + (recover ? " im back" : ""));
	}

	void ILoadable.Unload() { }
}
