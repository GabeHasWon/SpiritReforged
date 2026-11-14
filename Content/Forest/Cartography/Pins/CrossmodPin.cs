using SpiritReforged.Common.ItemCommon.Pins;
using Steamworks;

namespace SpiritReforged.Content.Forest.Cartography.Pins;

#nullable enable

/// <summary>
/// Defines a crossmod, Mod.Call pin. If you have a direct reference, just use <see cref="PinItem"/>.
/// </summary>
[Autoload(false)]
public class CrossmodPin(string internalName, string texturePath) : PinItem
{
	public override string Name => InternalName;
	public override string Texture => TexturePath;
	public override string MapTexture => Texture + "Map";

	private string InternalName = internalName;
	private string TexturePath = texturePath;
	protected override bool CloneNewInstances => true;

	public override ModItem Clone(Item newEntity)
	{
		var pin = (CrossmodPin)base.Clone(newEntity);
		pin.InternalName = InternalName;
		pin.TexturePath = TexturePath;
		return pin;
	}

	public CrossmodPin() : this("", "") { }

	public static CrossmodPin? RegisterPin(Mod mod, string name, string texture)
	{
		CrossmodPin pin = new(name, texture);

		if (mod.AddContent(pin))
			return pin;

		return null;
	}
}
