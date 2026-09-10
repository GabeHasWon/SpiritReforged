using SpiritReforged.Common.BuffCommon;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Content.Particles;

namespace SpiritReforged.Content.Forest.Shields;

[AutoloadBuff]
public class Vendetta : ModItem, IFlagged
{
	public sealed class VendettaPlayer : ModPlayer
	{
		public override void OnHitByNPC(NPC npc, Player.HurtInfo hurtInfo)
		{
			if (Player.HasFlag<Vendetta>())
				npc.AddBuff(BuffType, 60 * 5);
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (target.HasBuff(BuffType))
				modifiers.SourceDamage *= 1 + DAMAGE_BONUS;
		}
	}

	public sealed class VendettaVisualNPC : GlobalNPC
	{
		public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			if (!npc.HasBuff(BuffType))
				return;

			Texture2D texture = MarkTexture.Value;
			Vector2 worldPosition = npc.Top - new Vector2(0, 20 + EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 50f));

			int time = npc.buffTime[npc.FindBuffIndex(BuffType)];
			float opacity = Math.Min(time / 10f, 1);
			Color color = Lighting.GetColor(worldPosition.ToTileCoordinates()) * 2 * opacity;

			spriteBatch.Draw(texture, worldPosition - screenPos, null, color, 0, texture.Size() / 2, 1, 0, 0);
			spriteBatch.Draw(texture, worldPosition - screenPos, null, color.Additive() * EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 30f) * 0.3f, 0, texture.Size() / 2, 1, 0, 0);

			if (time > 40 && Main.rand.NextBool())
				ParticleHandler.SpawnParticle(new AttachedCompositeSmoke(npc, worldPosition - npc.Center, (Vector2.UnitY * -Main.rand.NextFloat()).RotatedByRandom(0.5f), Color.Black, 50, false, false)
				{ Layer = ParticleLayer.BelowNPC, Scale = 0.5f });
		}
	}

	public const float DAMAGE_BONUS = 0.2f;
	public static readonly Asset<Texture2D> MarkTexture = DrawHelpers.RequestLocal<Vendetta>("Vendetta_Mark", false);

	public static int BuffType { get; private set; }

	public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs((int)(DAMAGE_BONUS * 100));

	public override void SetStaticDefaults() => BuffType = BuffAutoloader.GetAutoloadedBuffType<Vendetta>();

	public override void SetDefaults()
	{
		Item.width = 30;
		Item.height = 34;
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.Blue;
		Item.accessory = true;
	}
}