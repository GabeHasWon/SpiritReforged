using SpiritReforged.Content.Ocean.Items.Reefhunter.Buffs;
using SpiritReforged.Common.ItemCommon;
using Terraria.DataStructures;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Ocean.Items.Reefhunter.Particles;
using SpiritReforged.Common.ModCompat.Classic;
using SpiritReforged.Common.ItemCommon.Abstract;

namespace SpiritReforged.Content.Ocean.Items.Reefhunter.OceanPendant;

[AutoloadEquip(EquipType.Neck)]
[FromClassic("PendantOfTheOcean")]
public class OceanPendant : EquippableItem
{
	public override void SetStaticDefaults() => DiscoveryHelper.RegisterPickup(Type, SoundID.CoinPickup with { Pitch = .25f });

	public override void SetDefaults()
	{
		Item.width = 32;
		Item.height = 36;
		Item.rare = ItemRarityID.Green;
		Item.value = Item.sellPrice(0, 4, 80, 0);
		Item.accessory = true;
	}

	public override void UpdateEquippable(Player player)
	{
		if (Collision.WetCollision(player.position, player.width, player.height))
		{
			if (player.velocity.Length() > 2f && Main.rand.NextBool(12))
				ParticleHandler.SpawnParticle(new BubbleParticle(player.Center + player.velocity / 2, Vector2.Normalize(player.velocity).RotatedByRandom(MathHelper.Pi / 6) * Main.rand.NextFloat(2f, 4), Main.rand.NextFloat(0.2f, 0.4f), 40));

			player.AddBuff(ModContent.BuffType<EmpoweredSwim>(), 10);
		}
	}
}

public class OceanPendantLayer : PlayerDrawLayer
{
	private static Asset<Texture2D> glowTexture;

	public override void Load() => glowTexture = ModContent.Request<Texture2D>(GetType().Namespace.Replace(".", "/") + "/OceanPendant_Neck_Glow");
	public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.NeckAcc);

	public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
	{
		var player = drawInfo.drawPlayer;
		return player.HasBuff<EmpoweredSwim>() && player.active && !player.dead;
	}

	protected override void Draw(ref PlayerDrawSet drawInfo)
	{
		if (drawInfo.shadow != 0f)
			return;

		var texture = glowTexture.Value;
		Color color = Color.White;

		drawInfo.DrawDataCache.Add(new DrawData(
			texture,
			drawInfo.drawPlayer.position - Main.screenPosition,
			drawInfo.drawPlayer.bodyFrame,
			color,
			0f,
			new Vector2(10),
			1f,
			drawInfo.playerEffect,
			0
		));
	}
}
