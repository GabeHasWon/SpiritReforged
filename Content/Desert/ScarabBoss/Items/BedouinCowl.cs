using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Content.Dusts;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items;

[AutoloadEquip(EquipType.Head)]
public class BedouinCowl : ModItem
{
	public sealed class BedouinDash : ModDash
	{
		public override void SetDefaults(out DashInfo info) => info = new(15, 25, 18f, new PolynomialEase((x) => (float)Math.Sin(x * (MathHelper.Pi * 0.95f))), 0.9f);

		public override void DashEffects(Player player)
		{
			if (!Main.dedServ)
			{
				for (int k = 0; k < 2; k++)
				{
					int dust;
					if (player.velocity.Y == 0f)
						dust = Dust.NewDust(new Vector2(player.position.X, player.position.Y + player.height - 4f), player.width, 8, ModContent.DustType<SandDust>(), 0f, 0f, 100, default, 1.4f);
					else
						dust = Dust.NewDust(new Vector2(player.position.X, player.position.Y + (player.height >> 1) - 8f), player.width, 16, ModContent.DustType<SandDust>(), 0f, 0f, 100, default, 1.4f);

					Main.dust[dust].velocity *= 0.1f;
					Main.dust[dust].scale *= 1f + Main.rand.Next(20) * 0.01f;
				}
			}

			DashPlayer dashPlayer = player.GetModPlayer<DashPlayer>();

			if (dashPlayer.DashDirection.Y < 0)
				dashPlayer.cooldown = dashPlayer.dashInfo.Cooldown * 3; //Increase the dash cooldown if ascending

			player.opacityForAnimation = (dashPlayer.DashProgress > 0.5f) ? 1f : 0.1f;
			player.noKnockback = true;

			player.immuneTime = 5;
			player.immune = true;
			player.immuneNoBlink = true;
		}
	}

	public sealed class TornadoLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.FrontAccFront);

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			Player player = drawInfo.drawPlayer;
			if (player.active && !player.outOfRange && player.GetModPlayer<DashPlayer>().ActiveDash is BedouinDash)
			{
				const float density = 20f;

				Main.instance.LoadProjectile(ProjectileID.SandnadoHostile);
				Texture2D texture = TextureAssets.Projectile[ProjectileID.SandnadoHostile].Value;
				Rectangle source = texture.Frame();

				for (float i = 0; i < (int)density; i++)
				{
					var offset = Vector2.SmoothStep(player.Center + Vector2.UnitY * (player.height / 2), player.Center - Vector2.UnitY * (player.height / 2), i / density);
					float scale = MathHelper.Lerp(0.6f, 1f, i / density);
					float lerp = (Math.Abs(density / 2 - i) > density / 2 * 0.6f) ? Math.Abs(density / 2 - i) / (density / 2) : 0f;
					float rotation = i / 6f - Main.GlobalTimeWrappedHourly * 7f;

					drawInfo.DrawDataCache.Add(new(texture, offset - Main.screenPosition, source, GetColor(player, lerp), rotation, source.Size() / 2, scale, drawInfo.playerEffect, 0));
				}
			}
		}

		private static Color GetColor(Player player, float progress)
		{
			var tint = Color.Lerp(new Color(212, 192, 100).Additive(127), Color.Transparent, progress);
			Color finalColor = Lighting.GetColor(player.Center.ToTileCoordinates()).MultiplyRGBA(tint) * 0.5f * EaseFunction.EaseSine.Ease(player.GetModPlayer<DashPlayer>().DashProgress);

			return finalColor;
		}
	}

	public override void Load() => DoubleTapPlayer.OnDoubleTap += ActivateDash;

	private static void ActivateDash(Player player, DoubleTapPlayer.Direction direction)
	{
		if (player.CheckFlag("Bedouin") == true)
			player.GetModPlayer<DashPlayer>().EnableDash<BedouinDash>(direction);
	}

	public override void SetDefaults()
	{
		Item.width = 22;
		Item.height = 22;
		Item.value = 7500;
		Item.rare = ItemRarityID.Green;
		Item.defense = 3;
	}

	public override bool IsArmorSet(Item head, Item body, Item legs) => head.type == Type && body.type == ModContent.ItemType<BedouinBreastplate>() && legs.type == ModContent.ItemType<BedouinLeggings>();
	public override void UpdateArmorSet(Player player)
	{
		player.setBonus = Language.GetTextValue("Mods.SpiritReforged.SetBonuses.Bedouin");
		player.SetFlag("Bedouin");
	}

	public override void UpdateEquip(Player player) => player.GetCritChance(DamageClass.Generic) += 5;
}