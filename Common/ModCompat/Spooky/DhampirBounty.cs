namespace SpiritReforged.Common.ModCompat.Spooky;

internal class DhampirBounty : ILoadable
{
	private static bool BountyActive = false;

	void ILoadable.Load(Mod mod)
	{
		if (!ModLoader.HasMod("Spooky"))
			return;

		Mod spooky = ModLoader.GetMod("Spooky");
		Asset<Texture2D> tex = ModContent.Request<Texture2D>("SpiritReforged/Common/ModCompat/Spooky/DhampirBounty");
		(string npc, string player)[] dialogue = [("You know, I once knew someone like you! Someone spry, strong, and easy to use! But oh me oh my, they could only get so far, before the light of the sun left them scarred...So with a little bit of magic, I inverted their condition! A perfect specimen for daytime missions!",
												  "And then they left right?"), 
												  ("Uh...yeah, I mean, I had a more elaborate rhyme but, yeah that’s about what I want. Do keep in mind that they are invisible, but just looove seeing their own reflection. Here, I have a malnourished bat I poke with a stick every night in a dinky cage that you can use to lure them out of hiding.",
												  "...yeah, I wonder why they would ever leave!"), 
												  ("They probably just think they are too cool to hang around with little old me now. How ungrateful!",
												  "Yeah, no other reason, got it.")];
		(string npc, string player)[] recoverDialogue = [("You are back! Have you assassinated the deserter yet?", "No Little Eye, no. I’m here because I lost the bat."), 
														("Oh don’t worry, I have a big pile of malnourished bats I poke with a stick every night in dinky cages. I keep them around to remind myself of how much better I am than that stupid Dhampir… grr… stupid dumb Dhampir…",
														"What's wrong with you?!"), 
														("…Oh me oh my, many things, but yet you help me so, we aren’t so different are we?", 
														"Ahuh. Just give me the bat already.")];
		spooky.Call("EyeQuest", mod, "TestBounty", tex, () => BountyActive, (Action<bool>)OnActivate, () => false, dialogue, recoverDialogue);
	}

	private static void OnActivate(bool recover)
	{
		BountyActive = true;
		Main.NewText("hi!" + (recover ? " im back" : ""));
	}

	void ILoadable.Unload() { }
}
