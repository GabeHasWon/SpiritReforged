using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.ItemCommon.MagazineSystem;
using SpiritReforged.Content.Dusts;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underground.Items;

public class Luger : ReloadableItem
{
	public static readonly SoundStyle EmptyClip = new("SpiritReforged/Assets/SFX/Item/Subclasses/EmptyClip", 2);

	public override IReloadable.MagazineInfo Magazine => new(4, 60);

	public override InterfaceProfile? Profile => new(MagazinePlayer.StyleID.Bullet);

	public override void SetDefaults()
	{
		Item.DefaultToBow(24, 10, true);
		Item.UseSound = new SoundStyle("SpiritReforged/Assets/SFX/Item/Subclasses/Shotguns/ShotgunShoot", 3) with { Volume = 0.5f };
		Item.useAmmo = AmmoID.Bullet;
		Item.damage = 24;
		Item.knockBack = 4;
	}

	public override bool? UseItem(Player player)
	{
		if (!Main.dedServ) //Spawn dusts on shoot
		{
			Vector2 endPosition = player.MountedCenter + new Vector2(36, 8 * -player.direction).RotatedBy(ReloadInfo.LastRotation);
			Vector2 unit = player.MountedCenter.DirectionTo(endPosition);

			for (int i = 0; i < 5; i++)
				Dust.NewDustPerfect(endPosition, Main.rand.NextFromList(DustID.Smoke, DustID.Torch), unit * Main.rand.NextFloat(), 150, Scale: Main.rand.NextFloat(1, 2)).noGravity = true;

			Dust.NewDustPerfect(player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, ReloadInfo.LastRotation), ModContent.DustType<ShellDust>(), new Vector2(-player.direction, -8));

			if (ReloadInfo.capacity == 0)
				SoundEngine.PlaySound(EmptyClip, player.Center);
		}

		return null;
	}

	public override void OnReload(Item item, Player player, IReloadable.ReloadContext context)
	{
		if (context == IReloadable.ReloadContext.Started)
			SoundEngine.PlaySound(EmptyMagazine with { Pitch = 0.5f }, player.Center);
		else if (context == IReloadable.ReloadContext.Ended && item.TryGetGlobalItem(out ReloadableGlobalItem global))
			global.capacity = Magazine.GetFullCapacity(player);
	}

	public override bool ModifyItemDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData, ref DrawData? coloredDrawData, ref DrawData? glowMaskDrawData)
	{
		Player player = drawInfo.drawPlayer;
		Vector2 origin = new(-2, 20);

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
			drawInfo.DrawDataCache.Add(GetDrawHeld(drawData, drawInfo.drawPlayer, origin, out float armRotation, new Recoil(EaseFunction.CompoundEase([EaseFunction.EaseCubicOut, EaseFunction.EaseSine]), 3, 1)));
			player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);

			Texture2D flash = Mac10.FlashTexture.Value; //Draw muzzle flash
			Vector2 size = drawData.sourceRect?.Size() ?? drawData.texture.Size();
			int direction = (drawData.effect == SpriteEffects.FlipHorizontally) ? -1 : 1;

			Vector2 end = drawData.position + new Vector2(size.X * direction, -8).RotatedBy(drawData.rotation);
			float scale = Math.Max((player.itemAnimation / (float)player.itemAnimationMax - 0.9f) * 10f, 0);

			drawInfo.DrawDataCache.Add(new(flash, end, null, Color.White, drawData.rotation, new Vector2((direction == -1) ? flash.Width : 0, flash.Height / 2), scale, drawData.effect, 0));
		}

		return false;
	}
}