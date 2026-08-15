using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Subclasses;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Candles;

public class Flamebelcher : ModItem, IDrawHeld, IManaBoon
{
	public int ManaLimit => 100;

	public override void SetStaticDefaults()
	{
		Main.RegisterItemAnimation(Type, new NightlightLead.DrawGrid(1, 2));
		MoRHelper.AddElement(Item, MoRHelper.Arcane, true);
	}

	public override void SetDefaults()
	{
		Item.DefaultToMagicWeapon(ProjectileID.WoodenArrowFriendly, 20, 10, true);
		Item.damage = 11;
		Item.mana = 8;
		Item.useStyle = ItemUseStyleID.HoldUp;
		Item.noUseGraphic = true;
		Item.UseSound = SoundID.Item1;
		Item.maxStack = 1;
		Item.channel = true;
		Item.value = Item.sellPrice(silver: 40);
	}

	public override void HoldItem(Player player)
	{
		player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, -1f * player.direction);
		float strength = IManaBoon.GetManaStrength(this, player);

		if (!Main.dedServ)
		{
			Lighting.AddLight(player.Center, Color.Orange.ToVector3() * strength * 0.6f);

			if (strength > 0 && Main.rand.NextFloat() < strength / 2f)
			{
				Vector2 top = player.RotatedRelativePoint(player.Center + new Vector2(18 * player.direction, -12));
				var dust = Dust.NewDustPerfect(top + Main.rand.NextVector2Circular(4, 4), Main.rand.NextFromList(DustID.Torch, DustID.Smoke), Vector2.UnitY * -Main.rand.NextFloat(3 * strength), Scale: 1.3f);
				dust.noGravity = true;
				dust.alpha = 150;
			}
		}
	}

	public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
	{
		position += new Vector2(10 * player.direction, -12 * player.gravDir);
		velocity = new Vector2(velocity.Length(), 0).RotatedBy(position.AngleTo(Main.MouseWorld));
	}

	void IDrawHeld.DrawHeld(ref PlayerDrawSet drawinfo)
	{
		Player player = drawinfo.drawPlayer;
		Texture2D texture = TextureAssets.Item[Type].Value;
		Rectangle source = Main.itemAnimations[Type].GetFrame(texture, 1);

		Vector2 bobOffset = Main.OffsetsPlayerHeadgear[player.bodyFrame.Y / player.bodyFrame.Height] * player.gravDir;
		Vector2 center = player.MountedCenter + bobOffset + new Vector2(13 * player.direction, 0 * player.gravDir);
		Vector2 drawPosition = new((int)(center.X - Main.screenPosition.X), (int)(center.Y - Main.screenPosition.Y + player.gfxOffY));

		float rotation = 0.5f * player.direction;
		Color color = Lighting.GetColor((int)center.X / 16, (int)center.Y / 16);

		drawinfo.DrawDataCache.Add(new DrawData(texture, drawPosition, source, color, rotation, source.Size() / 2, 1, drawinfo.itemEffect, 0));
	}
}