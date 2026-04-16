using SpiritReforged.Common;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using System.IO;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Forest.Katanas;

public class Vajra : ModItem, IDrawHeld
{
	public sealed class VajraMarkNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public bool hasMark;

		public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			if (hasMark)
			{

			}
		}

		public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter) => bitWriter.WriteBit(hasMark);
		public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader) => hasMark = bitReader.ReadBit();
	}

	public sealed class VajraSwing : SwungProjectile
	{
		public bool Secondary { get => Projectile.ai[0] == 1; set => Projectile.ai[0] = value ? 1 : 0; }

		public override LocalizedText DisplayName => ModContent.GetInstance<Vajra>().DisplayName;

		public override void SetStaticDefaults() => Main.projFrames[Type] = 4;

		public override IConfiguration SetConfiguration() => new BasicConfiguration(Common.Easing.EaseFunction.EaseCubicOut, 84, 25);

		public override void AI()
		{
			base.AI();

			if (SwingArc == 0)
			{
				float offset = Math.Max(40 * (0.5f - Progress * 2), -10);
				HoldDistance = offset;
			}
		}

		public override float GetRotation(out float armRotation, out Player.CompositeArmStretchAmount stretch) => base.GetRotation(out armRotation, out stretch);

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (!Secondary && target.TryGetGlobalNPC(out VajraMarkNPC vajraMarkNPC))
				vajraMarkNPC.hasMark = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			SpriteEffects effects = (SwingDirection == -1) ? SpriteEffects.FlipVertically : default;
			Vector2 origin = new(4, 24); //The handle
			Rectangle source = (SwingArc == 0) ? TextureAssets.Projectile[Type].Frame(1, Main.projFrames[Type], 0, Main.projFrames[Type] - 1, 0, -2) : default;

			DrawHeld(lightColor, origin, Projectile.rotation, effects, source);
			return false;
		}
	}

	private float _swingArc;

	public override void SetStaticDefaults() => SpiritSets.IsSword[Type] = true;

	public override void SetDefaults()
	{
		Item.DefaultToSpear(ModContent.ProjectileType<VajraSwing>(), 1, 30);
		Item.SetShopValues(ItemRarityColor.Orange3, Item.sellPrice(gold: 1));
		Item.damage = 22;
		Item.crit = 4;
		Item.knockBack = 5.5f;
		Item.autoReuse = true;
		MoRHelper.SetSlashBonus(Item);
	}

	public override bool AltFunctionUse(Player player) => player.GetModPlayer<DashSwordPlayer>().HasDashCharge;

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		_swingArc = _swingArc switch
		{
			5f => 0f,
			0f => -4f,
			_ => 5f
		};

		SwungProjectile.Spawn(position, velocity, type, damage, knockback, player, _swingArc, source, player.altFunctionUse - 1);
		return false;
	}

	public void DrawHeld(ref PlayerDrawSet info) { }

	public override void AddRecipes() { }
}