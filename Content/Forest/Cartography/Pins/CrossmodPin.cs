using SpiritReforged.Common.ItemCommon.Pins;

namespace SpiritReforged.Content.Forest.Cartography.Pins;

#nullable enable

/// <summary>
/// Defines a crossmod pin.
/// </summary>
/// <param name="internalName"></param>
/// <param name="texturePath"></param>
public class CrossmodPin(string internalName, string texturePath) : PinItem
{
	public override string Name => InternalName;
	public override string Texture => TexturePath;

	private readonly string InternalName = internalName;
	private readonly string TexturePath = texturePath;

	public static CrossmodPin? RegisterPin(Mod mod, string name, string texture)
	{
		CrossmodPin pin = new(name, texture);

		if (mod.AddContent(pin))
			return pin;

		return null;
	}
}
