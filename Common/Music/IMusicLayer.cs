namespace SpiritReforged.Common.Music;

internal interface IMusicLayer : ILoadable
{
	public int MusicSlot { get; }
	public int MatchSlot { get; }

	public bool CanPlay(ref float fadeIn);
}
