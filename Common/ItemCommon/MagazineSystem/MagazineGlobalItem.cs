using SpiritReforged.Common.Easing;
using SpiritReforged.Content.Aether.Items;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using static SpiritReforged.Common.Easing.EaseFunction;

namespace SpiritReforged.Common.ItemCommon.MagazineSystem;

/// <summary>
/// Holds data for a weapons magazine
/// </summary>
/// <param name="magazineSize">Amount of shots before reloading</param>
/// <param name="reloadTime">time in takes to reload, in ticks</param>
public class MagazineData(int magazineSize, int reloadTime)
{
	public int _magazineSize = magazineSize;
	public int _reloadTime = reloadTime;
}

public record struct CurrentMagazine(int AmmoUsed, int ReloadTimer);

public class MagazineGlobalItem : GlobalItem
{
	/// Animation methods for the custom use style. If null, default will be used, unless <see cref="_useCustomUseStyle"/> is false
	public delegate void ShotUseStyle(Item item, Player player, Rectangle heldItemFrame);
	public delegate void ShotUseFrame(Item item, Player player);
	public delegate void ReloadUseStyle(Item item, Player player, Rectangle heldItemFrame);
	public delegate void ReloadUseFrame(Item item, Player player);

	public override bool InstancePerEntity => true;
	private MagazineData _magazineData = null;
	private CurrentMagazine _currentMagazine;
	private bool _useCustomUseStyle;

	private ShotUseStyle _shotUseStyle = null;
	private ShotUseFrame _shotUseFrame = null;
	private ReloadUseStyle _reloadUseStyle = null;
	private ReloadUseFrame _reloadUseFrame = null;

	// used for custom UseStyle drawing
	private float _shotRecoil;
	private float _rotationRecoil;

	private float _shootRotation;
	private int _shootDirection;

	private int _reloadAnimationTimer;

	private Vector2 _itemSize;
	private Vector2 _itemOrigin;

	private Vector2? _animationRatio = null;

	public bool Active => _magazineData is not null;
	public bool Reloading => Active && _currentMagazine.ReloadTimer > 0;

	public void ActivateMagazine(MagazineData data, Vector2 itemSize, Vector2 itemOrigin, bool useCustomUseStyle = true, float shotRecoil = 5f, float rotationRecoil = -0.5f)
	{
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
	public void SetAnimations(Vector2? animationRatio = null,ShotUseStyle shotStyle = null, ShotUseFrame shotFrame = null, ReloadUseStyle reloadStyle = null, ReloadUseFrame reloadFrame = null)
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
			return _currentMagazine.ReloadTimer <= 0;

		return true;
	}

	public override bool? UseItem(Item item, Player player)
	{
		if (Active)
		{
			_currentMagazine.AmmoUsed++;

			if (_currentMagazine.AmmoUsed == _magazineData._magazineSize)
			{
				int reloadTime = _magazineData._reloadTime;

				_currentMagazine.ReloadTimer = reloadTime;
				_reloadAnimationTimer = reloadTime;
			}

			return null;
		}

		return null;
	}

	public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (Active)
		{
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
				if (_reloadUseStyle is not null)
					_reloadUseStyle.Invoke(item, player, heldItemFrame);
				else
					ReloadStyle(player);
			}
			else
			{
				if (_shotUseStyle is not null)
					_shotUseStyle.Invoke(item, player, heldItemFrame);
				else
					ItemVisualHelpers.SetGunUseStyle(player, item, _shootDirection, _shotRecoil, EaseCircularOut, EaseOutBack(), _itemSize, _itemOrigin, _animationRatio);
			}
		}
	}

	// default reload animation style. Should usually be overridden with the delegate.
	void ReloadStyle(Player player)
	{
		if (_reloadAnimationTimer > 0)
			_reloadAnimationTimer--;

		float animProgress = 1f - _reloadAnimationTimer / (float)_magazineData._reloadTime;

		if (Main.myPlayer == player.whoAmI)
			player.direction = _shootDirection;

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
			if (Reloading)
			{
				if (_reloadUseFrame is not null)
					_reloadUseFrame.Invoke(item, player);
				else
					ReloadFrame(player);
			}
			else
			{
				if (_shotUseFrame is not null)
					_shotUseFrame.Invoke(item, player);
				else
					ItemVisualHelpers.SetGunUseItemFrame(player, _shootDirection, _shootRotation, _rotationRecoil, EaseCircularOut, EaseOutBack(), true, _animationRatio);
			}
		}
	}

	// default reload animation frame. Should usually be overridden with the delegate.
	void ReloadFrame(Player player)
	{
		if (Main.myPlayer == player.whoAmI)
			player.direction = _shootDirection;

		float animProgress = 1f - _currentMagazine.ReloadTimer / (float)_magazineData._reloadTime;
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
		if (Active)
		{
			if (_currentMagazine.ReloadTimer > 0)
			{
				_currentMagazine.ReloadTimer--;
				if (_currentMagazine.ReloadTimer == 0)
					_currentMagazine.AmmoUsed = 0;

				player.itemTime = 2;
				player.itemAnimation = 2;
			}			
		}
	}
}
