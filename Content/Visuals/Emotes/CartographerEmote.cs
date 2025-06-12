using Terraria.GameContent.UI;
using SpiritReforged.Common.EmoteCommon;

namespace SpiritReforged.Content.Visuals.Emotes;

public class CartorgrapherEmote : BaseCustomEmote
{
	public override string Texture => "SpiritReforged/Content/Visuals/Emotes/CartographerEmote";

	protected override Asset<Texture2D> EmoteTextureAsset => ModContent.Request<Texture2D>(Texture);
	protected override int EmoteCategoryID => EmoteID.Category.Town;
}