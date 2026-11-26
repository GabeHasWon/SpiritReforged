using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.Misc;
using SpiritReforged.Content.Dusts;
using Terraria.Audio;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Underground.Items;

public class MammothGun : ModItem
{
	public override void SetDefaults()
	{
		Item.DefaultToBow(50, 10, true);
		Item.UseSound = new SoundStyle("SpiritReforged/Assets/SFX/Item/Subclasses/Shotguns/ShotgunShoot", 3);
		Item.useAmmo = AmmoID.Bullet;
		Item.damage = 35;
	}

	public override bool? UseItem(Player player)
	{
		if (!Main.dedServ) //Spawn dusts on shoot
		{
			Vector2 endPosition = player.MountedCenter + new Vector2(36, 8 * -player.direction).RotatedBy(player.itemRotation);
			Vector2 unit = player.MountedCenter.DirectionTo(endPosition);

			for (int i = 0; i < 5; i++)
				Dust.NewDustPerfect(endPosition, Main.rand.NextFromList(DustID.Smoke, DustID.Torch), unit * Main.rand.NextFloat(), 150, Scale: Main.rand.NextFloat(1, 2)).noGravity = true;

			Dust.NewDustPerfect(player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, player.itemRotation), ModContent.DustType<ShellDust>(), new Vector2(-player.direction, -8));
		}

		return null;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) => base.Shoot(player, source, position, velocity, type, damage, knockback);

	public override bool ModifyItemDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData, ref DrawData? coloredDrawData, ref DrawData? glowMaskDrawData)
	{
		Player player = drawInfo.drawPlayer;
		Vector2 origin = new(10, 18);

		drawInfo.DrawDataCache.Add(ReloadableItem.GetDrawHeld(drawData, drawInfo.drawPlayer, origin, out float armRotation, new ReloadableItem.Recoil(EaseFunction.CompoundEase([EaseFunction.EaseCubicOut, EaseFunction.EaseSine]), 3, 1.2f)));
		player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);

		Texture2D flash = Mac10.FlashTexture.Value; //Draw muzzle flash
		Vector2 size = drawData.sourceRect?.Size() ?? drawData.texture.Size();
		int direction = (drawData.effect == SpriteEffects.FlipHorizontally) ? -1 : 1;

		Vector2 end = drawData.position + new Vector2((size.X - origin.X - 4) * direction, -6).RotatedBy(drawData.rotation);
		float scale = Math.Max((player.itemAnimation / (float)player.itemAnimationMax - 0.9f) * 10f, 0);
		float randomRotation = (float)EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 30f) * 0.1f;

		drawInfo.DrawDataCache.Add(new(flash, end, null, Color.White.Additive(100), drawData.rotation + randomRotation, new Vector2((direction == -1) ? flash.Width : 0, flash.Height / 2), new Vector2(3 - scale, 1 * scale), drawData.effect, 0));

		for (int i = 0; i < 2; i++)
			drawInfo.DrawDataCache.Add(new(flash, end, null, Color.White.Additive(100), drawData.rotation - randomRotation - 0.5f + 1f * i, new Vector2((direction == -1) ? flash.Width : 0, flash.Height / 2), scale, drawData.effect, 0));

		return false;
	}

	public override void AddRecipes() => CreateRecipe()
		.AddIngredient(ItemID.BorealWood, 25)
		.AddIngredient(ItemID.Bone, 20)
		.AddTile(TileID.Anvils)
		.Register();
}