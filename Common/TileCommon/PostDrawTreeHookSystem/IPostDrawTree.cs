namespace SpiritReforged.Common.TileCommon.PostDrawTreeHookSystem;

/// <summary>
/// Allows something to draw after a tree at the given position.
/// </summary>
internal interface IPostDrawTree
{
	/// <summary>
	/// Whether this draw effect will draw immediately after the given tree, or after all trees.<br/>
	/// This is primarily a legacy value to keep Stargrass trees working with parity, but was untenable with the Spooky Scaregrass trees.
	/// </summary>
	public bool DrawAfter => true;

	public void PostDrawTree(int i, int j);
}
