namespace SpiritReforged.Common.DebuffOverhaul;

/// <summary> A damage-over-time buff extension that can be registered through mod call. </summary>
public class CustomDoT(/*float scalability, int damageLimit, bool stackable, */DoTExtension.Category category, Action<SpriteBatch, NPC, Color, Vector2, float, float> onPostDraw = null) : DoTExtension
{
	public static bool AddCustomDoT(object[] args)
	{
		if (args.Length < 6)
			throw new ArgumentException("AddCustomDoT requires at least 6 parameters.");

		if (args[0] is not int)
			throw new ArgumentException("AddCustomDoT parameter 1 should be an int.");

		int buffType = (int)args[0];

		if (args[1] is not int)
			throw new ArgumentException("AddCustomDoT parameter 2 should be an int.");

		int category = (int)args[4];

		if (args[2] is not Action<SpriteBatch, NPC, Color, Vector2, float, float> or null)
			throw new ArgumentException("AddCustomDoT parameter 3 should be an Action<SpriteBatch, NPC, Color, Vector2, float, float> or null.");

		var onPostDraw = (Action<SpriteBatch, NPC, Color, Vector2, float, float>)args[5];

		BuffHandler.Register(new CustomDoT((Category)category, onPostDraw), buffType);
		return true;

		/*if (args.Length < 6)
			throw new ArgumentException("AddCustomDoT requires at least 6 parameters.");

		if (args[0] is not int)
			throw new ArgumentException("AddCustomDoT parameter 1 should be an int.");

		int buffType = (int)args[0];

		if (args[1] is not float)
			throw new ArgumentException("AddCustomDoT parameter 2 should be a float.");

		float scalability = (float)args[1];

		if (args[2] is not int)
			throw new ArgumentException("AddCustomDoT parameter 3 should be an int.");

		int damageLimit = (int)args[2];

		if (args[3] is not bool)
			throw new ArgumentException("AddCustomDoT parameter 4 should be a bool.");

		bool stackable = (bool)args[3];

		if (args[4] is not int)
			throw new ArgumentException("AddCustomDoT parameter 5 should be an int.");

		int category = (int)args[4];

		if (args[5] is not Action<SpriteBatch, NPC, Color, Vector2, float, float> or null)
			throw new ArgumentException("AddCustomDoT parameter 6 should be an Action<SpriteBatch, NPC, Color, Vector2, float, float> or null.");

		var onPostDraw = (Action<SpriteBatch, NPC, Color, Vector2, float, float>)args[5];

		BuffHandler.Register(new CustomDoT(scalability, damageLimit, stackable, (Category)category, onPostDraw), buffType);
		return true;*/
	}

	public override BuffSettings Settings => _settings;

    private readonly BuffSettings _settings = new(/*scalability, damageLimit, stackable,*/ category);
    private readonly Action<SpriteBatch, NPC, Color, Vector2, float, float> _onPostDraw = onPostDraw;

    public override void PostDrawHealthBar(SpriteBatch spriteBatch, NPC npc, HealthBarHook.Options options) => _onPostDraw?.Invoke(spriteBatch, npc, options.Color, options.Position, options.Lightness, options.Scale);
}