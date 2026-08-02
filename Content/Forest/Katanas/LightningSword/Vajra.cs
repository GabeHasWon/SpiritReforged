using SpiritReforged.Common;
using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using SpiritReforged.Common.Visuals;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Katanas.LightningSword;

[AutoloadBuff]
public class Vajra : ModItem, IDrawHeld
{
	public sealed class LightningVisualNPC : GlobalNPC
	{
		public static readonly Asset<Texture2D> Electricity = DrawHelpers.RequestLocal<Vajra>("VajraMark", false);

		public override void HitEffect(NPC npc, NPC.HitInfo hit)
		{
			const int zap_distance = 200;

			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			int buffType = BuffAutoloader.GetAutoloadedBuffType<Vajra>();
			if (npc.HasBuff(buffType))
			{
				npc.RemoveBuff(buffType);

				foreach (NPC otherNPC in Main.ActiveNPCs)
				{
					if (otherNPC != npc && (otherNPC.CanBeChasedBy() || otherNPC.active && otherNPC.type == NPCID.TargetDummy) && otherNPC.DistanceSQ(npc.Center) < zap_distance * zap_distance)
					{
						Projectile.NewProjectile(npc.GetSource_OnHurt(null), npc.Center, Vector2.Zero, ModContent.ProjectileType<VajraLightning>(), 10, 2, -1, otherNPC.whoAmI);
						break;
					}
				}
			}
		}

		public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			if (!npc.HasBuff(BuffAutoloader.GetAutoloadedBuffType<Vajra>()))
				return;

			Texture2D texture = Electricity.Value;
			Rectangle source = texture.Frame(1, 4, 0, (int)Main.timeForVisualEffects / 4 % 4);

			DrawHelpers.DrawOutline(spriteBatch, default, default, default, (offset) =>
				spriteBatch.Draw(texture, npc.Center - screenPos + offset, source, npc.GetAlpha(Color.White).Additive() * 0.3f, 0, source.Size() / 2, 1, 0, 0));

			spriteBatch.Draw(texture, npc.Center - screenPos, source, npc.GetAlpha(Color.White), 0, source.Size() / 2, 1, 0, 0);
		}
	}

	private float _swingArc;

	public override void SetStaticDefaults() => SpiritSets.IsSword[Type] = true;

	public override void SetDefaults()
	{
		Item.DefaultToSpear(ModContent.ProjectileType<VajraSwing>(), 1, 40);
		Item.SetShopValues(ItemRarityColor.Orange3, Item.sellPrice(gold: 1));
		Item.damage = 22;
		Item.crit = 4;
		Item.knockBack = 5.5f;
		Item.autoReuse = false;
		Item.channel = true;
		MoRHelper.SetSlashBonus(Item);
	}

	public override bool AltFunctionUse(Player player) => player.GetModPlayer<DashSwordPlayer>().HasDashCharge;

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		_swingArc = (_swingArc == 0) ? Main.rand.NextFromList(-5, 5) : 0;
		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, _swingArc, source, player.altFunctionUse - 1);

		return false;
	}

	public void DrawHeld(ref PlayerDrawSet info) { }

	public override void AddRecipes() { }
}