using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.Visuals;
using Terraria.Audio;

namespace SpiritReforged.Common.Subclasses.Wrenches;

public class ScrapPickup : ModItem, IDrawPixelated
{
	public static readonly Asset<Texture2D> WorldIcon = DrawHelpers.RequestLocal<ScrapPickup>("ScrapPickup_World", false);

	private VertexTrail _trail;
	private int _timeSinceGrabbed;

	public override void SetStaticDefaults() => ItemID.Sets.IsAPickup[Type] = true;

	public override void SetDefaults() => Item.CloneDefaults(ItemID.Silk);

	public override void PostUpdate()
	{
		if (!Main.dedServ)
			_trail?.Update();

		if (_timeSinceGrabbed > 0)
			_timeSinceGrabbed--;

		if (Main.rand.NextBool(20))
		{
			var dust = Dust.NewDustDirect(Item.position, Item.width, Item.height, DustID.GoldCoin);
			dust.noGravity = true;
			dust.velocity = -Vector2.UnitY;
		}
	}

	public override void GrabRange(Player player, ref int grabRange) => grabRange *= 3;

	public override bool GrabStyle(Player player)
	{
		if (!Main.dedServ)
		{
			if (_trail == null)
			{
				var trailColor = new StandardColorTrail(Color.Yellow.Additive(150));
				_trail = new VertexTrail(trailColor, new NoCap(), new EntityTrailPosition(Item), new DefaultShader(), 5, 30);
			}
		}

		if (_timeSinceGrabbed == 0)
			Item.velocity = Vector2.UnitY * -10f;

		Item.velocity = Vector2.Lerp(Item.velocity, Item.DirectionTo(player.Center) * 10f, 0.07f);

		if (_timeSinceGrabbed <= 30)
			_timeSinceGrabbed += 2;

		return true;
	}

	public override bool OnPickup(Player player)
	{
		if (player.TryGetModPlayer(out WrenchPlayer wrenchPlayer))
		{
			wrenchPlayer.StoredScrap += Item.stack;
			SoundEngine.PlaySound(SoundID.CoinPickup, player.Center);

			#region popup text
			const int max_popup_text = 20;

			bool createNew = true;
			string itemName = DisplayName.Value;
			Color textColor = Color.Yellow;

			for (int i = 0; i < max_popup_text; i++)
			{
				PopupText popup = Main.popupText[i];

				if (!popup.active || !popup.name.Contains(itemName) || popup.color != textColor)
					continue;

				popup.name = wrenchPlayer.StoredScrap + $" {itemName}";
				createNew = false;

				break;
			}

			if (createNew)
			{
				int index = PopupText.NewText(PopupTextContext.RegularItemPickup, Item, 1);
				if (index != -1)
				{
					PopupText popup = Main.popupText[index];

					popup.name = wrenchPlayer.StoredScrap + $" {itemName}";
					popup.color = textColor;
				}
			}
			#endregion
		}

		return false;
	}

	public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
	{
		Texture2D texture = DrawHelpers.RequestLocal<ScrapPickup>("ScrapPickup_World", false).Value; //WorldIcon.Value;
		Rectangle source = texture.Frame(3, 1, whoAmI % 3, 0, -2, 0);
		Vector2 position = Item.Center - Main.screenPosition + Vector2.UnitY * EaseFunction.EaseSine.Ease(Item.timeSinceItemSpawned / 50f);

		float itemRotation = rotation;
		float itemScale = scale;

		DrawHelpers.DrawOutline(default, default, default, default, (offset) =>
		{
			float opacity = Math.Max(_timeSinceGrabbed / 30f, 1f - Item.timeSinceItemSpawned / 30f);
			Texture2D solid = TextureColorCache.ColorSolid(texture, Color.White);

			spriteBatch.Draw(solid, position + offset, source, Item.GetAlpha(Color.PaleGoldenrod).Additive() * opacity, itemRotation, source.Size() / 2, itemScale, 0, 0);
			spriteBatch.Draw(solid, position + offset * 2, source, Item.GetAlpha(Color.Orange).Additive() * opacity * 0.3f, itemRotation, source.Size() / 2, itemScale, 0, 0);
		});

		spriteBatch.Draw(texture, position, source, Item.GetAlpha(lightColor), itemRotation, source.Size() / 2, itemScale, 0, 0);
		return false;
	}

	void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch) => _trail?.Draw(TrailSystem.TrailShaders, Main.graphics.GraphicsDevice, Matrix.Identity);
}