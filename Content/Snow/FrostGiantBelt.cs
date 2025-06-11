using SpiritReforged.Common.ItemCommon.Abstract;
using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.ProjectileCommon.Abstract;
using Terraria.GameContent.ItemDropRules;

namespace SpiritReforged.Content.Snow;

[AutoloadEquip(EquipType.Waist)]
public class FrostGiantBelt : EquippableItem
{
	/// <summary> Checks if <paramref name="player"/> is charging a club projectile. </summary>
	public static bool ClubCharging(Player player)
	{
		foreach(Projectile proj in Main.ActiveProjectiles)
			if(proj.owner == player.whoAmI && proj.ModProjectile is BaseClubProj clubProj && clubProj.CheckAIState(BaseClubProj.AIStates.CHARGING))
				return true;

		return false;
	}

	public override void SetStaticDefaults() => NPCLootDatabase.AddLoot(new(NPCLootDatabase.MatchId(NPCID.UndeadViking), ItemDropRule.Common(Type, 15)));

	public override void SetDefaults()
	{
		Item.Size = new Vector2(30);
		Item.value = Item.sellPrice(0, 0, 90, 0);
		Item.rare = ItemRarityID.Orange;
		Item.accessory = true;
	}
}

internal class FrostGiantPlayer : ModPlayer
{
	public float extraDefense;

	public override void UpdateEquips()
	{
		if(Player.HasEquip<FrostGiantBelt>())
		{
			if(FrostGiantBelt.ClubCharging(Player))
			{
				extraDefense = Math.Min(extraDefense + (float)(1 / 6f), 15);
				Player.statDefense += (int)extraDefense;
			}
			else
			{
				extraDefense = Math.Max(extraDefense - 1, 0);
				Player.statDefense += (int)extraDefense;
			}
		}	
		else
		{
			extraDefense = 0;
		}
	}

	public override void ModifyHurt(ref Player.HurtModifiers modifiers)
	{
		if (Player.HasEquip<FrostGiantBelt>() && FrostGiantBelt.ClubCharging(Player))
			modifiers.Knockback *= 0.5f;
	}
}