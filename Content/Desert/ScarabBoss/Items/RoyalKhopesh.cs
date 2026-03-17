using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Content.Desert.ScarabBoss.Items.Projectiles;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.Underground.Items.BigBombs;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items;

public class RoyalKhopesh : ModItem
{
	internal int swingDirection = 1;

	public override void SetDefaults()
	{
		Item.damage = 28;
		Item.Size = new Vector2(48, 52);
		Item.useTime = Item.useAnimation = 48;
		Item.knockBack = 1f;
		Item.shootSpeed = 1;
		Item.noMelee = true;
		Item.channel = true;
		Item.noUseGraphic = true;
		Item.DamageType = DamageClass.Melee;
		Item.useTurn = false;
		Item.autoReuse = true;
		Item.rare = ItemRarityID.Green;
		Item.value = Item.sellPrice(gold: 2);
		Item.useStyle = ItemUseStyleID.Swing;
		Item.shoot = ModContent.ProjectileType<RoyalKhopeshHeld>();
		//Item.UseSound = SoundID.DD2_MonkStaffSwing;

		MoRHelper.SetSlashBonus(Item);
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		player.TryGetModPlayer<RoyalKhopeshPlayer>(out var modPlayer);

		if (player.altFunctionUse == 2)
		{
			//modPlayer.ThrowCooldown = RoyalKhopeshPlayer.COOLDOWN_TIME;

			type = ModContent.ProjectileType<RoyalKhopeshThrown>();
			velocity *= 20f;

			SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing, position);
		}		
		else
			swingDirection *= -1;

		Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, swingDirection);

		if (modPlayer.FastStrikeAmount <= 0)
			modPlayer.Combo++;
		
		return false;
	}

	public override bool AltFunctionUse(Player player) => player.ownedProjectileCounts[ModContent.ProjectileType<RoyalKhopeshThrown>()] <= 0 && player.GetModPlayer<RoyalKhopeshPlayer>().ThrowCooldown <= 0;

	public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
	public override bool MeleePrefix() => true;

	public override void NetReceive(BinaryReader reader) => swingDirection = reader.ReadInt32();

	public override void NetSend(BinaryWriter writer) => writer.Write(swingDirection);
}

public class RoyalKhopeshPlayer : ModPlayer
{
	public const int COOLDOWN_TIME = 300;
	public const int COOLDOWN_FLASH_TIMER = 20;

	private static readonly SoundStyle CooldownSound = SoundID.Item29 with { Volume = 0.2f, PitchVariance = 0.1f };

	public int Combo;
	public int FastStrikeAmount;
	public int EmpoweredStrikeTimer;
	public int ThrowCooldown;

	internal int CooldownFlashTimer;

	// not entirely sure if I need to unload
	public override void Load() => On_Main.DrawInfernoRings += DrawCooldownFlash;
	private void DrawCooldownFlash(On_Main.orig_DrawInfernoRings orig, Main self)
	{
		orig(self);

		foreach (Player p in Main.player)
		{
			p.TryGetModPlayer<RoyalKhopeshPlayer>(out var modPlayer);

			if (modPlayer?.CooldownFlashTimer > 0)
			{
				Texture2D star = AssetLoader.LoadedTextures["Star"].Value;
				Texture2D bloom = AssetLoader.LoadedTextures["Bloom"].Value;

				float flashTime = modPlayer.CooldownFlashTimer / (float)COOLDOWN_FLASH_TIMER;
				
				Main.spriteBatch.Draw(bloom, (p.HandPosition ?? p.Center) - Main.screenPosition, null, Color.MediumVioletRed.Additive() * 0.5f * flashTime, 0f, bloom.Size() / 2f, 0.3f, 0f, 0f);

				Main.spriteBatch.Draw(star, (p.HandPosition ?? p.Center) - Main.screenPosition, null, Color.MediumVioletRed.Additive(), 0f, star.Size() / 2f, 0.3f * flashTime, 0f, 0f);

				Main.spriteBatch.Draw(star, (p.HandPosition ?? p.Center) - Main.screenPosition, null, Color.White.Additive(), 0f, star.Size() / 2f, 0.15f * flashTime, 0f, 0f);
			}
		}
	}

	public override void ResetEffects()
	{
		if (EmpoweredStrikeTimer > 0)
			EmpoweredStrikeTimer--;

		if (ThrowCooldown > 0)
		{
			ThrowCooldown--;
			if (ThrowCooldown == 0)
			{
				SoundEngine.PlaySound(CooldownSound, Player.Center);
				CooldownFlashTimer = COOLDOWN_FLASH_TIMER;
			}
		}

		if (CooldownFlashTimer > 0)
			CooldownFlashTimer--;

		if (Combo > 2)
		{
			Combo = 0;
			FastStrikeAmount = 2;
		}			
	}
}

public class RoyalKhopeshGlobalNPC : GlobalNPC
{
	public Vector2? targetPosition;
	public int slowTimer;
	public override bool InstancePerEntity => true;

	public override void AI(NPC npc)
	{
		if (targetPosition != null && slowTimer > 0)
		{
			slowTimer--;

			float dist = npc.Distance(targetPosition.Value);

			npc.velocity *= MathHelper.Lerp(0.85f, 0.99f, dist / 400f);

			if (dist is < 10f or > 400f)
				targetPosition = null;
		}
	}

	public void SetTug(Vector2 targetPosition)
	{
		this.targetPosition = targetPosition;
		slowTimer = 30;
	}

	public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
	{
		bool hasPos = targetPosition is not null;
		bitWriter.WriteBit(hasPos);

		if (hasPos)
		{
			binaryWriter.WriteVector2(targetPosition.Value);
			binaryWriter.Write((byte)(slowTimer > 0 ? slowTimer : 0)); // Don't underflow the byte lol
		}
	}

	public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
	{
		if (bitReader.ReadBit())
		{
			targetPosition = binaryReader.ReadVector2();
			slowTimer = binaryReader.ReadByte();
		}
	}
}