using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.PlayerCommon;
using Terraria.DataStructures;

namespace SpiritReforged.Content.Forest.Rapiers;

[AutoloadEquip(EquipType.Face)]
public class DuelistRose : EquippableItem
{
	public class RosePetal : ModGore
	{
		public override void OnSpawn(Gore gore, IEntitySource source)
		{
			Texture2D texture = TextureAssets.Gore[Type].Value;

			gore.Frame = new(1, gore.numFrames = 4);
			gore.position -= texture.Frame(1, gore.numFrames, 0, 0, 0, -2).Size() / 2; //Center
		}

		public override bool Update(Gore gore)
		{
			if (--gore.timeLeft < 100)
			{
				gore.alpha = (int)((1f - gore.timeLeft / 100f) * 255f);

				if (gore.timeLeft <= 0)
					gore.active = false;
			}

			if (Collision.SolidCollision(gore.position - new Vector2(4), 8, 8))
			{
				gore.timeLeft = Math.Min(gore.timeLeft, 100);
				return false;
			}

			if (++gore.frameCounter >= 4)
			{
				gore.frameCounter = 0;
				gore.frame = (byte)(++gore.frame % gore.numFrames);
			}

			gore.velocity.Y = Math.Min(gore.velocity.Y + 0.1f, 0.5f);
			gore.velocity.X = MathHelper.Lerp(gore.velocity.X, Main.WindForVisuals, 0.05f);

			gore.position += gore.velocity;
			gore.rotation = gore.velocity.ToRotation() + MathHelper.PiOver2;

			return false;
		}
	}

	public sealed class OffBalance : ModBuff
	{
		public override string Texture => "Terraria/Images/Buff";

		public override void Update(Player player, ref int buffIndex) => player.statDefense -= 10;
		public override void Update(NPC npc, ref int buffIndex) => npc.GetStats().statDefense -= 10;
	}

	public override void SetDefaults()
	{
		Item.width = 32;
		Item.height = 22;
		Item.value = Item.sellPrice(gold: 1);
		Item.rare = ItemRarityID.Blue;
		Item.accessory = true;
	}

	public override void UpdateAccessory(Player player, bool hideVisual) => player.GetModPlayer<FreeDodgePlayer>().freeDodgeTime *= 1.5f;

	/// <summary> Activates the effects of the accessory. Should be placed in a relevant OnHitNPC method. </summary>
	public static void ApplyEffect(Player player, NPC target, NPC.HitInfo hit)
	{
		if (player.HasEquip<DuelistRose>())
		{
			target.AddBuff(ModContent.BuffType<OffBalance>(), 300);

			for (int i = 0; i < 3; i++)
			{
				Vector2 velocity = Main.rand.NextVector2Circular(hit.HitDirection * 8, Main.rand.NextFloat(1, 2)) - Vector2.UnitY;
				Gore.NewGoreDirect(player.GetSource_OnHit(target), target.Center + velocity, velocity, ModContent.GoreType<RosePetal>(), Main.rand.NextFloat(0.5f, 1));

				Dust.NewDustDirect(target.Center - new Vector2(4), 8, 8, DustID.Smoke, Alpha: 150, Scale: Main.rand.NextFloat() + 1).noGravity = true;
			}
		}
	}
}