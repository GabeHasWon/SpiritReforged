namespace SpiritReforged.Common.WallCommon;

/// <summary> Implements <see cref="PostWallFrame"/>, Called after <see cref="ModWall.WallFrame"/>.</summary>
public interface IPostWallFrame
{
	public void PostWallFrame(int i, int j, bool resetFrame);
}

public sealed class PostWallFrameLoader : ILoadable
{
	public void Load(Mod mod) => On_Framing.WallFrame += PostFrameWalls;

	private static void PostFrameWalls(On_Framing.orig_WallFrame orig, int i, int j, bool resetFrame)
	{
		orig(i, j, resetFrame);

		if (WallLoader.GetWall(Main.tile[i, j].WallType) is IPostWallFrame iPost)
			iPost.PostWallFrame(i, j, resetFrame);
	}

	public void Unload() { }
}