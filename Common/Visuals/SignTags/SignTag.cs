namespace SpiritReforged.Common.Visuals.CustomText;

/// <summary> Used to override sign text drawing using the associated <see cref="Key"/> value. <br/>
/// Loaded by <see cref="SignTagHandler"/>. </summary>
internal abstract class SignTag
{
	/// <summary> The beginning text key required for <see cref="Draw"/> to be called. </summary>
	public abstract string Key { get; }

	/// <summary>
	/// <inheritdoc cref="ParseParams"/>
	/// </summary>
	/// <param name="parameters"> An implicit value used to modify drawing behaviour. <br/>
	/// Parameters follow the <see cref="Key"/> beginning with ':' and each separated by ','. </param>
	/// <returns> Whether the given string was successfully parsed. </returns>
	public bool AddParameters(string parameters)
	{
		Reset();

		if (parameters is null)
			return false;

		return ParseParams(parameters.Split(','));
	}

	/// <summary> Used to reset variables to their default values if modified by <see cref="ParseParams"/>. </summary>
	protected virtual void Reset() { }

	/// <summary> Enables optional customization based on the given string. </summary>
	/// <param name="parameters"> The text provided after <see cref="Key"/> beginning with ':'. Can be parsed in any way to change how the text behaves. </param>
	/// <returns> Whether the parameters were successfully parsed. </returns>
	protected virtual bool ParseParams(string[] parameters) => false;

	/// <summary> Modifies how sign text draws when hovered over. The chat panel must be drawn manually. </summary>
	/// <param name="panel"> The area of the chat panel. </param>
	/// <param name="text"> Each line of text. </param>
	/// <param name="numLines"> The number of lines of text. </param>
	/// <returns> Whether to replace default drawing. </returns>
	public virtual bool Draw(Rectangle panel, string[] text, int numLines, ref Color color) => false;
}
