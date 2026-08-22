using SpiritReforged.Common.Multiplayer;
using Terraria.DataStructures;

namespace SpiritReforged.Common.Subclasses.Greatshields;

public class GreatshieldPlayer : ModPlayer
{
	public sealed class GreatshieldItemLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new Multiple()
		{
			{ new Between(PlayerDrawLayers.FrontAccBack, PlayerDrawLayers.Shield), drawinfo => LayerEqual(drawinfo, GreatshieldItem.DrawLayer.FrontArm) },
			{ new Between(PlayerDrawLayers.Backpacks, PlayerDrawLayers.Tails), drawinfo => LayerEqual(drawinfo, GreatshieldItem.DrawLayer.BackArm) }
		};

		private static bool LayerEqual(PlayerDrawSet set, GreatshieldItem.DrawLayer layer) => set.heldItem.ModItem is GreatshieldItem shieldItem && shieldItem.Layer == layer;

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			Player player = drawInfo.drawPlayer;

			if (!player.dead && player.HeldItem.ModItem is GreatshieldItem shield)
				shield.DrawShield(ref drawInfo, player.ItemAnimationActive);
		}
	}

	/// <summary> The rate in which shields regenerate or drain shield health. </summary>
	public const float SHIELD_RATE = 0.2f;

	public int LastDefense { get; private set; } // Last frame's defense for use in GreatshieldClass's damage boost

	public bool Blocking
	{
		get => _blocking;
		set
		{
			bool oldValue = _blocking;
			if (oldValue != value)
			{
				if (Main.netMode != NetmodeID.SinglePlayer)
					MultiplayerLoader.Send(nameof(SetBlocking), -1, Player.whoAmI, Player, value); //Automatically sync

				SetBlocking(Player, value);
			}
		}
	}

	/// <summary> Used for multiplayer syncing. Assign <see cref="Blocking"/> instead of calling this method. </summary>
	[NetSynced(true)]
	public static void SetBlocking(Player player, bool value)
	{
		if (player.TryGetModPlayer(out GreatshieldPlayer shieldPlayer))
		{
			shieldPlayer._delayCounter = 0; //Reset delay time
			shieldPlayer._blocking = value;
		}
	}

	public float shieldHealth;

	private bool _blocking;
	private int _delayCounter;

	public override void PostUpdateEquips() 
	{
		LastDefense = Player.statDefense;

		if (Player.HeldItem.ModItem is GreatshieldItem shieldItem)
		{
			var info = shieldItem.Info;
			if (++_delayCounter >= shieldItem.Info.DelayTime)
			{
				if (Blocking)
				{
					shieldHealth = Math.Max(shieldHealth - SHIELD_RATE, 0); //Remove shield health
				}
				else
				{
					shieldHealth = Math.Min(shieldHealth + SHIELD_RATE, info.ShieldHealth); //Regenerate shield health
				}
			}

			Player.statDefense += shieldItem.Item.defense;
		}
		else
		{
			shieldHealth = 0;
		}

		if (Blocking)
			Player.itemTime = Player.itemAnimation = 2; //Lock player item time

		if (Player.whoAmI == Main.myPlayer && !Main.mouseRight)
			Blocking = false; //Stop blocking if right click is released
	}

	public override void ModifyHurt(ref Player.HurtModifiers modifiers)
	{
		if (Blocking && shieldHealth > 0)
			modifiers.ModifyHurtInfo += ModifyShieldHurt;
	}

	public void ModifyShieldHurt(ref Player.HurtInfo info)
	{
		if (Player.HeldItem.ModItem is GreatshieldItem shieldItem)
			shieldItem.OnBlockDamage(Player, info);

		shieldHealth = Math.Max((int)(shieldHealth - info.Damage), 0);
		info.Knockback /= 2;

		if (info.Damage - (int)shieldHealth <= 0) //Damage was blocked completely
		{
			info.Cancelled = true;

			Player.velocity += new Vector2(info.HitDirection * info.Knockback, -1);
			Player.SetImmuneTimeForAllTypes(30); //TODO: add more feedback
		}
		else //Damage was blocked partially
		{
			info.Damage -= (int)shieldHealth;
		}
	}

	public override void HideDrawLayers(PlayerDrawSet drawInfo)
	{
		if (drawInfo.drawPlayer.HeldItem.ModItem is GreatshieldItem)
			PlayerDrawLayers.Shield.Hide();
	}
}