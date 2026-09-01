using SpiritReforged.Common.Easing;
using SpiritReforged.Content.Aether.Items;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using static SpiritReforged.Common.Easing.EaseFunction;

namespace SpiritReforged.Common.ItemCommon.MagazineSystem;

/// <summary>
/// The type of reload the weapon uses.
/// OneAtATime: The weapon reloads one ammo at a time, spread out during the reload animation. The player can fire to cancel the reload animation, assuming they have any ammo.
/// EntireMagazine: The weapon reloads the entire magazine at once. The player cannot cancel the reload animation.
/// </summary>
public enum MagazineReloadType
{
	OneAtATime = 0,
	EntireMagazine = 1,
}

public enum MagazineUIType
{
	Shell = 0, // Shotguns
	Bullet = 1, // Guns that are not shotguns
	Flamethrower = 2, // Flamethrowers
	Bow = 3, // Greatbows, Bows,
	Bolt = 4, // Crossbows
	Dart = 5, // Dart Weapons
	Rocket = 6, // Launchers
}

/// <summary>
/// Holds data for a weapons magazine. Sound playing is handled by <see cref="MagazineGlobalItem.ShootSoundInvokation"/>
/// </summary>
/// <param name="minPitch">The pitch of the sound with a full magazine</param>
/// <param name="maxPitch">The pitch of the sound with an empty magazine</param>
/// <param name="magazineSize">Amount of shots before reloading</param>
/// <param name="reloadTime">time in takes to reload, in ticks</param>
public class MagazineData(float minPitch, float maxPitch, int magazineSize, int reloadTime)
{
	public float minPitch = minPitch;
	public float maxPitch = maxPitch;

	public int _magazineSize = magazineSize;
	public int _reloadTime = reloadTime;
}

public record struct CurrentMagazine(int AmmoUsed, int ReloadTimer);

public class MagazineGlobalItem : GlobalItem
{
	// testing UI
	public override void SetDefaults(Item entity)
	{
		switch (entity.type)
		{
			case ItemID.TheUndertaker:
				ActivateMagazine(entity, null, new MagazineData(0, 1, 20, 120), Vector2.Zero, Vector2.Zero, MagazineReloadType.EntireMagazine, MagazineUIType.Bullet, false);
				break;

			case ItemID.Musket:
				ActivateMagazine(entity, null, new MagazineData(0, 1, 20, 120), Vector2.Zero, Vector2.Zero, MagazineReloadType.OneAtATime, MagazineUIType.Bullet, false);
				break;

			case ItemID.Minishark:
				ActivateMagazine(entity, null, new MagazineData(0, 1, 20, 240), Vector2.Zero, Vector2.Zero, MagazineReloadType.EntireMagazine, MagazineUIType.Bullet, false);
				break;

			case ItemID.SnowmanCannon:
				ActivateMagazine(entity, null, new MagazineData(0, 1, 5, 240), Vector2.Zero, Vector2.Zero, MagazineReloadType.OneAtATime, MagazineUIType.Rocket, false);
				break;

			case ItemID.DartPistol:
				ActivateMagazine(entity, null, new MagazineData(0, 1, 15, 120), Vector2.Zero, Vector2.Zero, MagazineReloadType.EntireMagazine, MagazineUIType.Dart, false);
				break;

			case ItemID.DemonBow:
				ActivateMagazine(entity, null, new MagazineData(0, 1, 15, 120), Vector2.Zero, Vector2.Zero, MagazineReloadType.OneAtATime, MagazineUIType.Bow, false);
				break;
		}
	}

	/// Animation methods for the custom use style. If null, default will be used, unless <see cref="_useCustomUseStyle"/> is false
	public delegate void ShotUseStyle(Item item, Player player, Rectangle heldItemFrame, int shootDirection, float shootRotation, Vector2 itemSize, Vector2 itemOrigin);
	public delegate void ShotUseFrame(Item item, Player player, int shootDirection, float shootRotation, Vector2 itemSize, Vector2 itemOrigin);
	public delegate void ReloadUseStyle(Item item, Player player, Rectangle heldItemFrame, int shootDirection, float shootRotation, Vector2 itemSize, Vector2 itemOrigin, float animProgress);
	public delegate void ReloadUseFrame(Item item, Player player, int shootDirection, float shootRotation, Vector2 itemSize, Vector2 itemOrigin, float animProgress);

	/// <summary>
	/// Method for playing sound that is called upon firing the weapon. Should always use the pitch parameter to ensure the sound is pitched depending on ammo remaining
	/// </summary>
	public delegate void ShootSoundInvokation(float pitch, Vector2 position);

	public override bool InstancePerEntity => true;
	private MagazineData _magazineData = null;
	private CurrentMagazine _currentMagazine;
	private bool _useCustomUseStyle;

	private ShotUseStyle _shotUseStyle = null;
	private ShotUseFrame _shotUseFrame = null;
	private ReloadUseStyle _reloadUseStyle = null;
	private ReloadUseFrame _reloadUseFrame = null;

	private ShootSoundInvokation _soundInvokation = null;

	// used for custom UseStyle drawing
	private float _shotRecoil;
	private float _rotationRecoil;

	private float _shootRotation;
	private int _shootDirection;

	private int _maxReloadTimer;
	private int _oldAmmoUsed;

	private int oldHoldStyle;

	// used to automatically reload after a period of not doing anything.
	private int _reloadIdleTimer;
	private int _maxReloadIdleTimer = 60;

	// because we have to manually interrupt item time to cancel a reload, we store a seperate timer to ensure proper behavior.
	private int reloadCancelCooldown;

	private Vector2 _itemSize;
	private Vector2 _itemOrigin;

	private Vector2? _animationRatio = null;

	public MagazineReloadType ReloadType;
	public MagazineUIType UIType;

	public bool Active => _magazineData is not null;
	public bool Reloading => Active && _currentMagazine.ReloadTimer > 0;
	public int AmmoRemaining(Player player) => player.GetModPlayer<MagazinePlayer>().GetMagazineSize() - _currentMagazine.AmmoUsed;
	public float MagazineProgress(Player player) => 1f - AmmoRemaining(player) / (float)player.GetModPlayer<MagazinePlayer>().GetMagazineSize();
	public void ActivateMagazine(Item item, ShootSoundInvokation soundMethod, MagazineData data, Vector2 itemSize, Vector2 itemOrigin, MagazineReloadType reloadType, MagazineUIType uiType, bool useCustomUseStyle = true, float shotRecoil = 5f, float rotationRecoil = -0.5f)
	{
		oldHoldStyle = item.holdStyle;

		ReloadType = reloadType;
		UIType = uiType;

		_soundInvokation = soundMethod;

		_shotRecoil = shotRecoil;
		_rotationRecoil = rotationRecoil;

		_magazineData = data;
		_currentMagazine = new(0, 0);
		_useCustomUseStyle = useCustomUseStyle;

		_itemSize = itemSize;
		_itemOrigin = itemOrigin;
	}
	public MagazineData GetMagazineData() => _magazineData;
	public CurrentMagazine GetCurrentMagazine() => _currentMagazine;

	/// <summary>
	/// Sets the animations for the weapon. <see cref="ItemVisualHelpers"/> for examples of how the methods can be used.
	/// </summary>
	/// <param name="animationRatio">The ratio of the shooting animation></param>
	/// <param name="shotStyle">The UseStyle of the shooting animation></param>
	/// <param name="shotFrame">The UseItemFrame of the shooting animation</param>
	/// <param name="reloadStyle">The UseStyle of the reload animation</param>
	/// <param name="reloadFrame">The UseItemFrame of the reload animation</param>
	public void SetAnimations(Vector2? animationRatio = null, ShotUseStyle shotStyle = null, ShotUseFrame shotFrame = null, ReloadUseStyle reloadStyle = null, ReloadUseFrame reloadFrame = null)
	{
		_animationRatio = animationRatio;

		_shotUseStyle = shotStyle;
		_shotUseFrame = shotFrame;
		_reloadUseStyle = reloadStyle;
		_reloadUseFrame = reloadFrame;
	}

	public override bool CanUseItem(Item item, Player player)
	{
		if (Active)
		{
			if (ReloadType == MagazineReloadType.OneAtATime && reloadCancelCooldown <= 0)
			{
				if (AmmoRemaining(player) > 0)
				{
					reloadCancelCooldown = _magazineData._reloadTime;
					_maxReloadTimer = 0;
					_currentMagazine.ReloadTimer = 0;
					return true;
				}
				else
					return false;
			}
			else
				return _currentMagazine.ReloadTimer <= 0;
		}

		return true;
	}

	public override bool? UseItem(Item item, Player player)
	{
		if (Active)
		{
			_reloadIdleTimer = 0;

			var mp = player.GetModPlayer<MagazinePlayer>();

			MagazinePlayer.Fire(item);

			_currentMagazine.AmmoUsed++;

			int magazineSize = mp.GetMagazineSize(_magazineData._magazineSize);

			if (_currentMagazine.AmmoUsed == magazineSize)
				ActivateReload(player, item, magazineSize);

			return null;
		}

		return null;
	}

	void ActivateReload(Player player, Item item, int ammoUsed)
	{
		//reloadCancelCooldown = _magazineData._reloadTime;

		_shootRotation = (player.Center - Main.MouseWorld).ToRotation();
		_shootDirection = Main.MouseWorld.X < player.Center.X ? -1 : 1;

		if (Main.myPlayer == player.whoAmI)
			player.direction = _shootDirection;

		var mp = player.GetModPlayer<MagazinePlayer>();

		int reloadTime = mp.GetReloadTime(_magazineData._reloadTime) + item.useTime;

		if (ReloadType == MagazineReloadType.OneAtATime)
		{
			int maxReloadTime = (int)MathHelper.Lerp(reloadTime, reloadTime / 2, 1f - MagazineProgress(player));
			//int singleLoadTime = maxReloadTime / ammoUsed;

			_oldAmmoUsed = ammoUsed;	

			_maxReloadTimer = maxReloadTime;
			_currentMagazine.ReloadTimer = maxReloadTime;
			reloadCancelCooldown = (int)(reloadTime * 0.5f);
		}
		else
		{
			_maxReloadTimer = reloadTime;
			_currentMagazine.ReloadTimer = reloadTime;
		}
	}

	public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (Active)
		{
			var data = _magazineData;

			if (AmmoRemaining(player) <= 0)
				SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/EmptyMagazine"), position);

			_soundInvokation?.Invoke(MathHelper.Lerp(data.minPitch, data.maxPitch, MagazineProgress(player)), position);

			_shootRotation = (player.Center - Main.MouseWorld).ToRotation();
			_shootDirection = Main.MouseWorld.X < player.Center.X ? -1 : 1;
		}

		return true;
	}

	public override void UseStyle(Item item, Player player, Rectangle heldItemFrame)
	{
		if (Active && _useCustomUseStyle)
		{
			if (Reloading)
			{
				float animProgress = 1f - _currentMagazine.ReloadTimer / (float)_maxReloadTimer;

				if (_reloadUseStyle is not null)
					_reloadUseStyle.Invoke(item, player, heldItemFrame, _shootDirection, _shootRotation, _itemSize, _itemOrigin, animProgress);
				else
					ReloadStyle(player, animProgress);
			}
			else
			{

				if (_shotUseStyle is not null)
					_shotUseStyle.Invoke(item, player, heldItemFrame, _shootDirection, _shootRotation, _itemSize, _itemOrigin);
				else
					ItemVisualHelpers.SetGunUseStyle(player, item, _shootDirection, _shotRecoil, EaseCircularOut, EaseOutBack(), _itemSize, _itemOrigin, _animationRatio);

				_reloadIdleTimer = 0;
			}		
		}
	}

	public override void HoldStyle(Item item, Player player, Rectangle heldItemFrame)
	{
		if (Active && Reloading && _useCustomUseStyle)
		{
			if (Main.myPlayer == player.whoAmI)
				player.direction = _shootDirection;

			float animProgress = 1f - _currentMagazine.ReloadTimer / (float)_maxReloadTimer;

			if (_reloadUseStyle is not null)
				_reloadUseStyle.Invoke(item, player, heldItemFrame, _shootDirection, _shootRotation, _itemSize, _itemOrigin, animProgress);
			else
				ReloadStyle(player, animProgress);
		}
	}

	// default reload animation style. Should usually be overridden with the delegate.
	void ReloadStyle(Player player, float animProgress)
	{
		float itemRotation = player.compositeBackArm.rotation + 1.5707964f * player.gravDir;
		Vector2 itemPosition = player.MountedCenter;

		if (animProgress < 0.55f)
		{
			float lerper = animProgress / 0.55f;
			itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(-10f, -11f, EaseFunction.EaseCircularInOut.Ease(lerper));
		}
		else
		{
			if (animProgress < 0.75f)
			{
				float lerper = (animProgress - 0.55f) / 0.2f;
				itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(-11f, -18f, EaseFunction.EaseQuinticInOut.Ease(lerper));
			}
			else
			{
				float lerper = (animProgress - 0.75f) / 0.25f;
				itemPosition += itemRotation.ToRotationVector2() * MathHelper.Lerp(-18f, 0f, EaseFunction.EaseQuinticInOut.Ease(lerper));
			}
		}

		ItemVisualHelpers.CleanHoldStyle(player, itemRotation, itemPosition, _itemSize, _itemOrigin, true, false, true);
	}

	public override void UseItemFrame(Item item, Player player)
	{
		if (Active && _useCustomUseStyle)
		{
			if (_shotUseFrame is not null)
				_shotUseFrame.Invoke(item, player, _shootDirection, _shootRotation, _itemSize, _itemOrigin);
			else
				ItemVisualHelpers.SetGunUseItemFrame(player, _shootDirection, _shootRotation, _rotationRecoil, EaseCircularOut, EaseOutBack(), false, _animationRatio);
		}
	}

	public override void HoldItem(Item item, Player player)
	{
		if (Active && Reloading && _useCustomUseStyle)
		{
			if (Main.myPlayer == player.whoAmI)
				player.direction = _shootDirection;

			float animProgress = 1f - _currentMagazine.ReloadTimer / (float)_maxReloadTimer;

			if (_reloadUseFrame is not null && _maxReloadTimer > 0)
				_reloadUseFrame.Invoke(item, player, _shootDirection, _shootRotation, _itemSize, _itemOrigin, animProgress);
			else
				ReloadFrame(player, animProgress);
		}
	}

	// default reload animation frame. Should usually be overridden with the delegate.
	void ReloadFrame(Player player, float animProgress)
	{
		float rotation = _shootRotation * player.gravDir + 1.5707964f;
		float frontArmRotation = _shootRotation * player.gravDir + 1.5707964f;

		Player.CompositeArmStretchAmount frontStretch = Player.CompositeArmStretchAmount.Full;

		if (animProgress < 0.55f)
		{
			if (animProgress < 0.1f)
			{
				float lerper = animProgress / 0.1f;
				rotation += MathHelper.Lerp(0f, -0.45f, EaseFunction.EaseCircularOut.Ease(lerper)) * player.direction;
				frontArmRotation += MathHelper.Lerp(0f, -0.45f, EaseFunction.EaseCircularOut.Ease(lerper)) * player.direction;
			}
			else
			{
				float lerper = (animProgress - 0.1f) / 0.45f;
				rotation += MathHelper.Lerp(-0.45f, 0.55f, EaseFunction.EaseCircularInOut.Ease(lerper)) * player.direction;
				frontArmRotation += MathHelper.Lerp(-0.45f, 0.15f, EaseFunction.EaseCircularInOut.Ease(lerper)) * player.direction;
			}
		}
		else
		{
			frontArmRotation += 0.15f * player.direction;

			if (animProgress > 0.75f)
			{
				frontStretch = Player.CompositeArmStretchAmount.None;
				if (animProgress > 0.8f)
					frontStretch = Player.CompositeArmStretchAmount.Quarter;
				if (animProgress > 0.85f)
					frontStretch = Player.CompositeArmStretchAmount.ThreeQuarters;
				if (animProgress > 0.9f)
					frontStretch = Player.CompositeArmStretchAmount.Full;

				float lerper = (animProgress - 0.75f) / 0.25f;
				rotation += MathHelper.Lerp(0.55f, 0f, EaseFunction.EaseCircularInOut.Ease(lerper)) * player.direction;
			}
			else
			{
				rotation += 0.55f * player.direction;

				if (animProgress > 0.6f)
					frontStretch = Player.CompositeArmStretchAmount.ThreeQuarters;
				if (animProgress > 0.65f)
					frontStretch = Player.CompositeArmStretchAmount.Quarter;
				if (animProgress >= 0.7f)
					frontStretch = Player.CompositeArmStretchAmount.None;
			}
		}

		player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, rotation);
		player.SetCompositeArmFront(true, frontStretch, frontArmRotation);
	}

	public override void UpdateInventory(Item item, Player player)
	{
		if (Active && player.HeldItem == item)
		{
			if (reloadCancelCooldown > 0)
				reloadCancelCooldown--;

			if (_currentMagazine.ReloadTimer > 0)
			{
				item.holdStyle = ItemHoldStyleID.HoldFront;

				/*if (ReloadType == MagazineReloadType.OneAtATime && player.controlUseItem && AmmoRemaining(player) > 0 && reloadCancelCooldown <= 0)
				{
					reloadCancelCooldown = _magazineData._reloadTime; 
					_maxReloadTimer = 0;
					_currentMagazine.ReloadTimer = 0;
					return;
				}*/

				_currentMagazine.ReloadTimer--;

				if (ReloadType == MagazineReloadType.OneAtATime && _oldAmmoUsed > 0)
				{
					float interpolant = 1 - MagazineProgress(player);
					float reloadProgress = _currentMagazine.ReloadTimer / (float)_maxReloadTimer;

					const float padding = 0.25f;

					if (reloadProgress is > padding and < (1f - padding))
					{
						float lerp = 1f - (reloadProgress - padding) / (1f - padding * 2);

						int old = _currentMagazine.AmmoUsed;
						_currentMagazine.AmmoUsed = (int)MathHelper.Lerp(_oldAmmoUsed, 0, lerp);

						if (old != _currentMagazine.AmmoUsed)
						{
							MagazinePlayer.UnempowerShot();

							SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/UI/Magazine/ShellLoad") with { Volume = 2f, Pitch = MathHelper.Lerp(-0.25f, 0.25f, interpolant) });
						}
					}
				}
				else if (ReloadType == MagazineReloadType.EntireMagazine)
				{
					if (_currentMagazine.ReloadTimer == 0)
						_currentMagazine.AmmoUsed = 0;
				}

				if (_currentMagazine.ReloadTimer == 0)
					item.holdStyle = oldHoldStyle;
			}
			else
			{
				item.holdStyle = oldHoldStyle;

				if (_oldAmmoUsed > 0)
					_oldAmmoUsed = 0;

				if (_currentMagazine.AmmoUsed > 0)
				{
					if (++_reloadIdleTimer >= _maxReloadIdleTimer) // activate reload after a period of idling (no shooting)
					{
						ActivateReload(player, item, _currentMagazine.AmmoUsed);

						_reloadIdleTimer = 0;
					}
				}			
			}
		}
	}

	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		if (Active)
		{
			int magazineSize = Main.LocalPlayer.GetModPlayer<MagazinePlayer>().GetMagazineSize(item.GetGlobalItem<MagazineGlobalItem>()._magazineData._magazineSize);

			int index = tooltips.FindIndex(tt => tt.Mod.Equals("Terraria") && tt.Name.Equals("ItemName"));
			if (index != -1)
			{
				tooltips.Insert(index + 1, new(Mod, "SpiritReforged: Magazine Keyword", "Magazine Weapon")
				{
					OverrideColor = Color.Gray,				
				});
			}

			index = tooltips.FindIndex(tt => tt.Mod.Equals("Terraria") && tt.Name.Equals("Damage"));
			if (index != -1)
			{
				tooltips.Insert(index + 1, new(Mod, "SpiritReforged: Magazine Size", $"Can fire {magazineSize} shots before needing to reload"));
			}
		}
	}

	public override GlobalItem Clone(Item from, Item to)
	{
		var clone = (MagazineGlobalItem)base.Clone(from, to);

		clone._magazineData = _magazineData;
		clone._currentMagazine = _currentMagazine;
		clone._useCustomUseStyle = _useCustomUseStyle;

		clone._shotUseStyle = _shotUseStyle;
		clone._shotUseFrame = _shotUseFrame;
		clone._reloadUseStyle = _reloadUseStyle;
		clone._reloadUseFrame = _reloadUseFrame;
		clone._soundInvokation = _soundInvokation;

		clone._maxReloadTimer = _maxReloadTimer;
		clone._oldAmmoUsed = _oldAmmoUsed;
		clone._reloadIdleTimer = _reloadIdleTimer;
		clone._itemSize = _itemSize;
		clone._itemOrigin = _itemOrigin;
		clone._animationRatio = _animationRatio;

		clone.ReloadType = ReloadType;
		clone.UIType = UIType;
		clone.oldHoldStyle = oldHoldStyle;

		return clone;
	}
}
