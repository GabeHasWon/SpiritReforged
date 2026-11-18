using SpiritReforged.Common.Misc;
using SpiritReforged.Common.PlayerCommon;
using SpiritReforged.Common.Visuals;
using Terraria.Audio;
using Terraria.Graphics.Renderers;

namespace SpiritReforged.Common.ItemCommon.MagazineSystem;

/// <summary> Denotes an item that has a magazine and can be reloaded. </summary>
public interface IReloadable
{
	public enum ReloadContext { Started, Ended }

	public readonly record struct MagazineInfo(int FullCapacity, int ReloadDuration, bool ManualReload = true);

	public MagazineInfo Magazine { get; }

	public void DrawInterface(Item item, SpriteBatch spriteBatch) { }

	public void OnReload(Item item, Player player, ReloadContext context) { }
}

public class ReloadableGlobalItem : GlobalItem
{
	#region static
	private static readonly Dictionary<int, IReloadable> MagazineByType = [];

	public static bool HasMagazine(int itemType, out IReloadable iReloadable)
	{
		if (MagazineByType.TryGetValue(itemType, out IReloadable _iReloadable))
		{
			iReloadable = _iReloadable;
			return true;
		}
		else
		{
			iReloadable = null;
			return false;
		}
	}
	#endregion

	public override bool InstancePerEntity => true;

	/*public float GetAmmoProgress(int itemType)
	{
		if (HasMagazine(itemType, out IReloadable iReloadable))
			return capacity / (float)iReloadable.Magazine.FullCapacity;
		else
			return 0;
	}*/

	public int LastDirection => Math.Sign(LastRotation.ToRotationVector2().X);

	public float LastRotation { get; private set; }

	public bool IsReloading(Player player) => !player.ItemAnimationActive && reloadTime > 0;

	public int capacity;
	public int reloadTime;

	public override bool AppliesToEntity(Item entity, bool lateInstantiation) => MagazineByType.ContainsKey(entity.type);

	public override void SetStaticDefaults() //Register all interface types during content loading
	{
		foreach (ModItem modItem in ModContent.GetContent<ModItem>())
		{
			if (modItem is IReloadable iMagazine)
				MagazineByType.Add(modItem.Type, iMagazine);
		}
	}

	public override void SetDefaults(Item entity)
	{
		if (HasMagazine(entity.type, out IReloadable iReloadable))
			capacity = iReloadable.Magazine.FullCapacity;
	}

	public override bool AltFunctionUse(Item item, Player player) => capacity < (item.ModItem as IReloadable).Magazine.FullCapacity;

	public override bool CanUseItem(Item item, Player player)
	{
		if (player.altFunctionUse == 2 && HasMagazine(item.type, out IReloadable iReloadable) && iReloadable.Magazine.ManualReload) //Manually prompted a reload
		{
			StartReload(player, item);
			return false;
		}

		return reloadTime == 0;
	}

	public override bool? UseItem(Item item, Player player)
	{
		SoundEngine.PlaySound(item.UseSound.Value with { Pitch = 1f - capacity / (float)(item.ModItem as IReloadable).Magazine.FullCapacity }, player.Center);
		Vector2 mouse = PlayerMouseHandler.GetMouse(player.whoAmI);

		LastRotation = player.AngleTo(mouse);
		return null;
	}

	public override void OnConsumeAmmo(Item weapon, Item ammo, Player player)
	{
		if (--capacity <= 0) //Consume ammo from the magazine
			StartReload(player, weapon);
	}

	public override void HoldStyle(Item item, Player player, Rectangle heldItemFrame)
	{
		if (IsReloading(player))
		{
			float rotation = LastRotation;
			int direction = LastDirection;

			if (direction == -1)
				rotation -= MathHelper.Pi;

			player.itemRotation = rotation;
			player.direction = direction;
		}
	}

	public override void HoldItem(Item item, Player player) //Update reload
	{
		if (IsReloading(player))
		{
			item.holdStyle = ItemHoldStyleID.HoldFront;

			if (--reloadTime <= 0 && item.ModItem is IReloadable iReloadable)
			{
				capacity = iReloadable.Magazine.FullCapacity;
				iReloadable.OnReload(item, player, IReloadable.ReloadContext.Ended);
			}
		}
		else
		{
			item.holdStyle = ItemHoldStyleID.None;
		}
	}

	public void StartReload(Player player, Item item)
	{
		if (item.ModItem is IReloadable iReloadable)
		{
			reloadTime = iReloadable.Magazine.ReloadDuration;
			iReloadable.OnReload(item, player, IReloadable.ReloadContext.Started);
		}
	}
}

public class MagazineInterfacePlayer : ModPlayer
{
	public enum StyleID { Arrow, Bullet, Dart, Rocket, Shell }

	public class InterfaceAmmo : Particle
	{
		public readonly StyleID Style;

		public InterfaceAmmo(Vector2 position, Vector2 velocity, StyleID style, int timeActive)
		{
			Position = position;
			Velocity = velocity;
			MaxTime = timeActive;
			Style = style;
		}

		public override void Update(ref ParticleRendererSettings settings)
		{
			Velocity.Y += 0.2f;
			Velocity *= 0.98f;

			base.Update(ref settings);

			Rotation += Velocity.Length() * 0.05f;
		}

		public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
		{
			Scale = 1f / Main.GameViewMatrix.Zoom.X;

			Texture2D texture = Texture;
			Vector2 anchor = settings.AnchorPosition;
			Rectangle source = texture.Frame(1, Enum.GetValues<StyleID>().Length, 0, (int)Style, 0, -2);

			if (Progress <= 0.5f)
			{
				float lerp = (1f - Progress - 0.5f) / 0.5f;

				DrawHelpers.DrawOutline((offset) =>
				spritebatch.Draw(texture, Position + anchor + offset * Scale, source, Color.Lerp(Color.Orange, Main.MouseBorderColor, 1 - lerp) * lerp, Rotation, source.Size() / 2f, Scale, 0f, 0f));
			}

			spritebatch.Draw(texture, Position + anchor, source, Main.mouseColor * (1f - Progress), Rotation, source.Size() / 2f, Scale, 0f, 0f);
		}
	}

	public static bool UIActive => !Main.gameMenu && !Main.LocalPlayer.mouseInterface; //TODO: add config

	public override void Load() => CustomCursor.DrawCustomCursor += DrawAmmo;

	private static void DrawAmmo(bool thick)
	{
		if (!thick && UIActive)
		{
			Item item = Main.LocalPlayer.HeldItem;

			if (ReloadableGlobalItem.HasMagazine(item.type, out IReloadable iReloadable))
				iReloadable.DrawInterface(item, Main.spriteBatch);
		}
	}
}