using Terraria.DataStructures;

namespace SpiritReforged.Common.ItemCommon;

public interface IDrawHeld
{
	public sealed class DashSwordLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.HeldItem);

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			if (drawInfo.drawPlayer.HeldItem.ModItem is IDrawHeld iHeld)
				iHeld.DrawHeld(ref drawInfo);
		}
	}

	public void DrawHeld(ref PlayerDrawSet drawinfo);
}