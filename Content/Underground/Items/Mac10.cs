using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.ItemCommon.MagazineSystem;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underground.Items;

public class Mac10 : ReloadableItem
{
	public static readonly Asset<Texture2D> FlashTexture = DrawHelpers.RequestLocal<Mac10>("Mac10_Flash", true);

	public override IReloadable.MagazineInfo Magazine => new(15, Item.useTime * 13);

	public override InterfaceProfile? Profile => new(MagazineInterfacePlayer.StyleID.Bullet);

	public override void SetDefaults()
	{
		Item.DefaultToBow(6, 10, true);
		Item.UseSound = new SoundStyle("SpiritReforged/Assets/SFX/Item/Subclasses/Shotguns/ShotgunShoot", 3);
		Item.useAmmo = AmmoID.Bullet;
		Item.damage = 12;
	}

	public override bool ModifyItemDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData, ref DrawData? coloredDrawData, ref DrawData? glowMaskDrawData)
	{
		Player player = drawInfo.drawPlayer;
		Vector2 origin = new(4, 22);

		if (ReloadInfo.IsReloading(player) && Item.TryGetGlobalItem(out ReloadableGlobalItem global)) //Draw reload visuals
		{
			float progress = 1f - global.reloadTime / (float)Magazine.GetReloadDuration(player);
			float rotation = drawData.rotation + EaseFunction.EaseCubicInOut.Ease(progress * 3f) * player.direction;

			if (progress > 0.8f)
			{
				rotation = drawData.rotation + (1 - EaseFunction.EaseCubicIn.Ease((progress - 0.8f) / 0.2f)) * player.direction;
			}
			else if (progress > 0.33f)
			{
				rotation = drawData.rotation + 1 * player.direction;
			}

			drawInfo.DrawDataCache.Add(GetDrawHeld(drawData with { rotation = rotation }, drawInfo.drawPlayer, origin, out float armRotation));

			player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
			player.SetCompositeArmBack(true, (progress > 0.5f) ? Player.CompositeArmStretchAmount.Quarter : Player.CompositeArmStretchAmount.Full, armRotation);
		}
		else if (player.ItemAnimationActive) //Draw shoot visuals
		{
			drawInfo.DrawDataCache.Add(GetDrawHeld(drawData, drawInfo.drawPlayer, origin, out float armRotation, new Recoil(EaseFunction.EaseCubicOut, 1, 0.4f)));
			player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);

			Texture2D flash = FlashTexture.Value; //Draw muzzle flash
			Vector2 size = drawData.sourceRect?.Size() ?? drawData.texture.Size();
			int direction = (drawData.effect == SpriteEffects.FlipHorizontally) ? -1 : 1;

			Vector2 end = player.GetFrontHandPosition(player.compositeFrontArm.stretch, drawData.rotation) + new Vector2((size.X - origin.X) * direction, -12).RotatedBy(drawData.rotation) - Main.screenPosition;
			float scale = Math.Max((player.itemAnimation / (float)player.itemAnimationMax - 0.5f) * 2f, 0);
			float rotation = drawData.rotation;

			drawInfo.DrawDataCache.Add(new(flash, end, null, Color.White.Additive() * 0.5f, rotation, new Vector2((direction == -1) ? flash.Width : 0, flash.Height / 2), scale, drawData.effect, 0));

			rotation = drawData.rotation + EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects * 0.2f) / 2;
			drawInfo.DrawDataCache.Add(new(flash, end, null, Color.White.Additive(), rotation, new Vector2((direction == -1) ? flash.Width : 0, flash.Height / 2), new Vector2(1.5f, 1) * scale, drawData.effect, 0));

			rotation = drawData.rotation + EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects * 0.1f) / 2;
			drawInfo.DrawDataCache.Add(new(flash, end, null, Color.White.Additive(), rotation, new Vector2((direction == -1) ? flash.Width : 0, flash.Height / 2), scale, drawData.effect, 0));
		}

		return false;
	}
}