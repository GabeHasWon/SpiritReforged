using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Particles;

namespace SpiritReforged.Content.Forest.Glyphs;

public class BlazeGlyph : GlyphItem
{
	public sealed class BlazePlayer : ModPlayer
	{
		public override void MeleeEffects(Item item, Rectangle hitbox)
		{
			if (Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<BlazeGlyph>() && Main.rand.NextBool(5))
			{
				var dust = Dust.NewDustDirect(hitbox.TopLeft(), hitbox.Width, hitbox.Height, DustID.Torch);
				dust.noGravity = true;
				dust.fadeIn = 1.1f;
				dust.noLightEmittence = true;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<BlazeGlyph>())
			{
				Player.AddBuff(BuffID.OnFire, 120);
				SpawnHitEffects(target.Hitbox.ClosestPointInRect(Player.Center));
			}
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Player.HeldItem.GetGlyph().ItemType == ModContent.ItemType<BlazeGlyph>())
			{
				Player.AddBuff(BuffID.OnFire, 120);
				SpawnHitEffects(proj.Center);
			}
		}

		public void SpawnHitEffects(Vector2 position)
		{
			Color[] colors = [new(255, 200, 0, 100), new(255, 115, 0, 100), new(200, 3, 33, 100)];
			ParticleHandler.SpawnParticle(new FireParticle(position, Player.DirectionTo(position), colors, 1, 0.075f, EaseFunction.EaseQuadOut, 40));

			for (int i = 0; i < 4; i++)
			{
				var dust = Dust.NewDustPerfect(position, DustID.Torch, Scale: 1.5f);
				dust.noGravity = Main.rand.NextBool();
				dust.noLightEmittence = true;
			}
		}
	}

	public override void ApplyGlyph(Item item, ApplicationContext context)
	{
		base.ApplyGlyph(item, context);

		item.damage += (int)Math.Round(item.damage * 0.25f);
		item.crit += 10;
	}

	public override void SetDefaults()
	{
		Item.height = Item.width = 28;
		Item.rare = ItemRarityID.Pink;
		Item.maxStack = Item.CommonMaxStack;
		settings = new(new(233, 143, 26));
	}
}