namespace SpiritReforged.Common.ModCompat.Spooky;

internal class TestBountyLogic : ILoadable
{
	void ILoadable.Load(Mod mod)
	{
		if (!ModLoader.HasMod("Spooky"))
			return;

		Mod spooky = ModLoader.GetMod("Spooky");
		Asset<Texture2D> tex = ModContent.Request<Texture2D>("SpiritReforged/Common/ModCompat/Spooky/TestBounty");
		(string npc, string player)[] dialogue = [("Hey man.", "What's up?"), ("Not much.", "Oh, alright."), ("Yeah.", "Yeah.")];
		(string npc, string player)[] recoverDialogue = [("Hey again.", "Yeah?"), ("Well...", "Well...?"), ("I dunno. Nothing.", "Oh, okay. Bye?")];
		spooky.Call("EyeQuest", mod, "TestBounty", tex, (bool recover) => Main.NewText("hi!" + (recover ? " im back" : " ok")), () => false, dialogue, recoverDialogue);
	}

	void ILoadable.Unload() { }
}
