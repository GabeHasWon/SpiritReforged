using SpiritReforged.Common.PlayerCommon;
using System.Linq;

namespace SpiritReforged.Common.ItemCommon.Abstract;

/// <summary> Used for info accessories like the Radar. Autoloads an <see cref="InfoDisplay"/> instance in <see cref="Load"/> and provides handled equip flags in <see cref="InfoPlayer"/>. </summary>
public abstract class InfoItem : ModItem
{
	public readonly struct DisplayItem(InfoDisplay InfoDisplay)
	{
		private readonly InfoDisplay _diplay = InfoDisplay;

		public readonly bool Active => InfoDisplayLoader.Active(_diplay);
		public readonly bool Hidden => !Active || Main.LocalPlayer.hideInfo[_diplay.Type];
	}

	private static readonly Dictionary<string, DisplayItem> DisplayByName = [];
	public static DisplayItem GetDisplay<T>() where T : ModItem => DisplayByName[ModContent.GetInstance<T>().Name];

	/// <summary> Autoloads an info display based on this item. </summary>
	protected void AutoloadInfoDisplay()
	{
		var value = new AutoloadedInfoDisplay(GetType().Namespace + '/' + Name);
		Mod.AddContent(value);

		DisplayByName.Add(Name, new(value));
	}

	public override void Load() => AutoloadInfoDisplay();

	public override void SetDefaults() => Item.CloneDefaults(ItemID.Radar);
	public override void UpdateInfoAccessory(Player player) => player.GetModPlayer<InfoPlayer>().info[Name] = true;
}

internal class InfoPlayer : ModPlayer
{
	internal readonly Dictionary<string, bool> info = [];

	public override void Initialize()
	{
		info.Clear();

		foreach (var item in ModContent.GetContent<InfoItem>())
			info.Add(item.Name, false);
	}

	public override void ResetInfoAccessories()
	{
		foreach (string key in info.Keys)
			info[key] = false;
	}

	public override void RefreshInfoAccessoriesFromTeamPlayers(Player otherPlayer)
	{
		var otherInfo = otherPlayer.GetModPlayer<InfoPlayer>().info;

		foreach (string key in otherInfo.Keys)
		{
			if (otherInfo[key])
				info[key] = true;
		}
	}
}

internal sealed class AutoloadedInfoDisplay(string fullname) : InfoDisplay
{
	private LocalizedText _description;

	private readonly string _itemName = fullname.Split('/').Last();
	private readonly string _texture = fullname.Replace('.', '/') + "InfoDisplay";

	public override string Name => _itemName + "InfoDisplay";
	public override string Texture => _texture;
	public override string HoverTexture => _texture + "Hover";

	public override void SetStaticDefaults() => _description = this.GetLocalization("Description");

	public override bool Active() => Main.LocalPlayer.HasInfoItem(_itemName);
	public override string DisplayValue(ref Color displayColor, ref Color displayShadowColor) => _description.Value;
}