namespace SpiritReforged.Common.NPCCommon.Abstract;

internal abstract class PlayerContainerNPC : ModNPC
{
	// This has to be set to something near the size of the NPC because otherwise it'll give you a tiny clickbox. Who knows why!
	public override string Texture => "Terraria/Images/NPC_" + NPCID.Guide;

	protected Player _drawDummy = null;

	public override void SetDefaults()
	{
		Defaults();

		_drawDummy = new Player();
		InitializePlayer();
	}

	protected virtual void InitializePlayer() { }
	protected virtual void PreDrawPlayer() { }

	public virtual void Defaults() { }

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		_drawDummy.position = NPC.position;
		_drawDummy.velocity = NPC.velocity;
		_drawDummy.PlayerFrame();
		_drawDummy.direction = NPC.direction;
		PreDrawPlayer();

		Main.spriteBatch.End();
		Main.PlayerRenderer.DrawPlayers(Main.Camera, [_drawDummy]);
		Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
			DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

		return false;
	}
}
