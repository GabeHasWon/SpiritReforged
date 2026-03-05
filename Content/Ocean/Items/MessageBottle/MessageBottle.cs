using SpiritReforged.Common.ItemCommon.FloatingItem;

namespace SpiritReforged.Content.Ocean.Items.MessageBottle;

public class MessageBottle : FloatingItem
{
	private static Asset<Texture2D> WorldTexture;

	public override float SpawnWeight => 0.1f;
	public override float Weight => base.Weight * 0.9f;
	public override float Bouyancy => base.Bouyancy * 1.08f;

	public override void SetStaticDefaults()
	{
		if (!Main.dedServ)
			WorldTexture = ModContent.Request<Texture2D>(Texture + "_World");
	}

	public override void SetDefaults()
	{
		Item.width = 22;
		Item.height = 10;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.value = Item.sellPrice(0, 1, 0, 0);
		Item.rare = ItemRarityID.Yellow;
		Item.maxStack = 1;
		Item.UseSound = SoundID.Item79;
		Item.mountType = ModContent.MountType<MessageBottleMount>();
		Item.useTime = Item.useAnimation = 20;
	}

	public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
	{
		Texture2D tex = WorldTexture.Value;
		spriteBatch.Draw(tex, Item.Center - Main.screenPosition, null, Item.GetAlpha(lightColor), rotation, tex.Size() / 2, scale, SpriteEffects.None, 0f);
		return false;
	}
}