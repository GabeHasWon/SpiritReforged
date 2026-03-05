using Terraria.DataStructures;

namespace SpiritReforged.Content.Savanna.Items.Vanity;

[AutoloadEquip(EquipType.Legs)]
public class OstrichPants : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 34;
		Item.height = 30;
		Item.value = Item.sellPrice(0, 1, 0, 0);
		Item.rare = ItemRarityID.White;
		Item.vanity = true;
	}
}

internal class OstrichPantsLayer : PlayerDrawLayer
{
	private static Asset<Texture2D> Texture;

	public override void Load() => Texture = ModContent.Request<Texture2D>(ModContent.GetInstance<OstrichPants>().Texture + "_Legs");
	public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.FaceAcc);

	protected override void Draw(ref PlayerDrawSet drawInfo)
	{
		var player = drawInfo.drawPlayer;
		if (player.dead || player.invis)
			return;

		if (Equipped(player))
		{
			var pos = new Vector2((int)(drawInfo.Position.X - Main.screenPosition.X - player.legFrame.Width / 2 + player.width / 2), 
				(int)(drawInfo.Position.Y - Main.screenPosition.Y + player.height - player.legFrame.Height + 4f)) + player.legPosition + drawInfo.legVect;

			var data = new DrawData(Texture.Value, pos, player.legFrame, drawInfo.colorArmorLegs, player.legRotation, drawInfo.legVect, 1f, drawInfo.playerEffect)
			{
				shader = player.cLegs
			};
			drawInfo.DrawDataCache.Add(data);
		}
	}

	private static bool Equipped(Player player)
	{
		var vLegs = player.armor[12];
		if (vLegs != null && !vLegs.IsAir)
			return vLegs.type == ModContent.ItemType<OstrichPants>();

		var legs = player.armor[2];
		return legs != null && legs.type == ModContent.ItemType<OstrichPants>();
	}
}