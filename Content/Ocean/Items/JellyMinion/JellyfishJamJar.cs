using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.Visuals.Glowmasks;
using SpiritReforged.Content.Ocean.Items.Reefhunter.Particles;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Ocean.Items.JellyMinion;

[LegacyName("JellyfishStaff")]
[AutoloadGlowmask("255,255,255")]
public class JellyfishJamJar : ModItem, IDrawHeld
{
	public override void SetStaticDefaults()
	{
		NPCLootDatabase.AddLoot(new(NPCLootDatabase.MatchId(NPCID.PinkJellyfish), ItemDropRule.Common(Type, 50)));
		NPCLootDatabase.AddLoot(new(NPCLootDatabase.MatchId(NPCID.BlueJellyfish), ItemDropRule.Common(Type, 250)));

		ItemLootDatabase.AddItemRule(ItemID.OceanCrate, ItemDropRule.Common(Type, 15));
		ItemLootDatabase.AddItemRule(ItemID.OceanCrateHard, ItemDropRule.Common(Type, 15));

		MoRHelper.AddElement(Item, MoRHelper.Thunder);
		MoRHelper.AddElement(Item, MoRHelper.Water, true);

		Main.RegisterItemAnimation(Type, new DrawAnimationVertical(2, 2) { NotActuallyAnimating = true });
	}

	public override void SetDefaults()
	{
		Item.width = 52;
		Item.height = 46;
		Item.value = Item.sellPrice(0, 2, 0, 0);
		Item.rare = ItemRarityID.Blue;
		Item.mana = 10;
		Item.damage = 14;
		Item.knockBack = 2.5f;
		Item.useStyle = ItemUseStyleID.Rapier;
		Item.useTime = 30;
		Item.useAnimation = 30;
		Item.DamageType = DamageClass.Summon;
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.shoot = ModContent.ProjectileType<JellyfishMinion>();
		Item.shootSpeed = 4;
		Item.UseSound = SoundID.Item44;
		Item.autoReuse = true;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		SoundEngine.PlaySound(SoundID.SplashWeak with { PitchVariance = 0.3f }, position);

		for (int i = 0; i < 3; i++)
		{
			ParticleHandler.SpawnParticle(new BubbleParticle(position + Main.rand.NextVector2Circular(15f, 15f), Main.rand.NextVector2Circular(2.5f, 2.5f), Main.rand.NextFloat(0.12f, 0.26f), Main.rand.Next(20, 40)));
			Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(15f, 15f), DustID.Water, Main.rand.NextVector2Circular(5f, 5f), 55, default, 0.7f).noGravity = true;
		}

		return true;
	}

	public override void Update(ref float gravity, ref float maxFallSpeed) => Lighting.AddLight(Item.position, 0.224f * 2, 0.133f * 2, 0.255f * 2);

	void IDrawHeld.DrawHeld(ref PlayerDrawSet drawinfo)
	{
		Player player = drawinfo.drawPlayer;
		if (!player.ItemAnimationActive)
			return;

		Texture2D texture = TextureAssets.Item[Type].Value;
		Rectangle source = texture.Frame(1, 2, 0, 1, 0, -2);

		float progress = (float)player.itemAnimation / player.itemAnimationMax;
		Vector2 offhand = GetOffhand(player, out _) + new Vector2((10 - (int)(progress * 4 / 2) * 2) * player.direction, 4);
		Vector2 halfSize = player.Size / 2;

		float armRotation = drawinfo.compositeFrontArmRotation + MathHelper.PiOver2;

		if (player.direction == -1)
			armRotation += MathHelper.Pi;

		Vector2 position = drawinfo.Position + halfSize + (offhand - halfSize).RotatedBy(armRotation) - Main.screenPosition;
		float rotation = MathHelper.PiOver4 * player.direction;
		Color color = Lighting.GetColor((int)player.Center.X / 16, (int)player.Center.Y / 16);

		drawinfo.DrawDataCache.Add(new DrawData(texture, position, source, color, rotation, source.Size() / 2, 1, drawinfo.playerEffect, 0));

		static Vector2 GetOffhand(Player player, out int frame)
		{
			Vector2 offhand = Main.OffsetsPlayerOffhand[frame = player.bodyFrame.Y / player.bodyFrame.Height];

			if (player.direction != 1)
				offhand.X = player.width - offhand.X;

			if (player.gravDir != 1f)
				offhand.Y -= player.height;

			return offhand;
		}
	}
}