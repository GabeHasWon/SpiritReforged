using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon.MagazineSystem;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;
using Terraria.Audio;
using Terraria.DataStructures;
using static SpiritReforged.Common.ItemCommon.MagazineSystem.MagazinePlayer;

namespace SpiritReforged.Common.ItemCommon.Abstract;

/// <summary> Implements <see cref="IReloadable"/> and provides additional utilities for ease of use. </summary>
public abstract class ReloadableItem : ModItem, IReloadable
{
	public readonly record struct InterfaceProfile(StyleID Style)
	{
		public static float GlowStrength { get; set; }
		private static float _glowLerp;

		public readonly void Draw(Item item, SpriteBatch spriteBatch)
		{
			const int offset_size = 8;
			const int max_visible = 10;

			if (!item.TryGetGlobalItem(out ReloadableGlobalItem global) || !ReloadableGlobalItem.HasMagazine(item.type, out IReloadable iReloadable))
				return;

			Texture2D texture = Particle.GetTexture<InterfaceAmmo>();
			Rectangle source = texture.Frame(1, Enum.GetValues<StyleID>().Length, 0, (int)Style, 0, -2);
			Texture2D starTexture = AssetLoader.LoadedTextures["Star"].Value;
			Texture2D bloomTexture = AssetLoader.LoadedTextures["Bloom"].Value;

			int fullCapacity = iReloadable.Magazine.FullCapacity;
			int usedCapacity = iReloadable.Magazine.FullCapacity - global.capacity;
			int moveTimeMax = Main.LocalPlayer.itemTimeMax;
			int moveTime = global.ConsumedAmmo ? Main.LocalPlayer.itemTime : 0;

			for (int i = 0; i < global.capacity; i++)
			{
				float fadeOut = (i + 1) / (float)global.capacity;
				if (global.capacity >= max_visible)
				{
					if (i < global.capacity - max_visible)
						fadeOut = 0f;
					else
						fadeOut = (i - (global.capacity - max_visible)) / (float)max_visible;
				}

				Vector2 position = Main.MouseScreen + new Vector2(32, fullCapacity * offset_size);
				float moveShellUp = MathHelper.Lerp((usedCapacity - 1) * offset_size, usedCapacity * offset_size, moveTime > 0 ? EaseFunction.EaseCircularOut.Ease(1 - moveTime / (float)moveTimeMax) : 1f);

				if (global.IsReloading(Main.LocalPlayer) && moveTime <= 0)
					moveShellUp = usedCapacity * offset_size;

				Vector2 offset = new(0, -offset_size * i - moveShellUp);
				if (i < _glowLerp) //Draw empowered glow
				{
					float lerp = _glowLerp % 1f;
					float sine = (float)Math.Sin(Main.timeForVisualEffects / 20f);
					float rotation = sine * 0.2f;
					float scale = 1f;
					Vector2 shake = Vector2.Zero;

					if (sine > 0)
					{
						scale += sine * 0.1f;
						shake += Main.rand.NextVector2CircularEdge(sine, sine);
					}

					if (lerp > 0)
					{
						Vector2 starScale = new(MathHelper.Lerp(0.1f, 0.3f, EaseFunction.EaseOutElastic().Ease(1f - lerp)), 0.1f);

						spriteBatch.Draw(starTexture, position + offset, null, Color.Yellow.Additive() * fadeOut * lerp, 0f, starTexture.Size() / 2f, starScale * 1.2f, 0f, 0f);
						spriteBatch.Draw(starTexture, position + offset, null, Color.White.Additive() * fadeOut * lerp, 0f, starTexture.Size() / 2f, starScale * 1f, 0f, 0f);

						spriteBatch.Draw(bloomTexture, position + offset, null, Color.White.Additive() * fadeOut * lerp * 0.5f, 0f, bloomTexture.Size() / 2f, 0.15f, 0f, 0f);
						spriteBatch.Draw(bloomTexture, position + offset, null, Color.LightYellow.Additive() * fadeOut * lerp * 0.35f, 0f, bloomTexture.Size() / 2f, 0.1f, 0f, 0f);
					}

					spriteBatch.Draw(bloomTexture, position + offset, null, Color.White.Additive() * fadeOut * 0.15f, 0f, bloomTexture.Size() / 2f, 0.15f, 0f, 0f);
					spriteBatch.Draw(bloomTexture, position + offset, null, Color.LightYellow.Additive() * fadeOut * 0.1f, 0f, bloomTexture.Size() / 2f, 0.1f, 0f, 0f);

					DrawHelpers.DrawOutline((extraOffset) =>
					spriteBatch.Draw(texture, position + offset + extraOffset + shake, source, Color.Yellow * fadeOut, rotation, source.Size() / 2f, 1f, 0f, 0f));

					spriteBatch.Draw(texture, position + offset + shake, source, Main.mouseColor * fadeOut, rotation, source.Size() / 2f, scale, 0f, 0f);
				}
				else
				{
					DrawHelpers.DrawOutline((extraOffset) =>
					spriteBatch.Draw(texture, position + offset + extraOffset, source, Main.MouseBorderColor * fadeOut, 0f, source.Size() / 2f, 1f, 0f, 0f));

					spriteBatch.Draw(texture, position + offset, source, Main.mouseColor * fadeOut, 0f, source.Size() / 2f, 1f, 0f, 0f);
				}
			}
		}

		public readonly void Update()
		{
			_glowLerp = Math.Min(_glowLerp + 0.1f, GlowStrength);
			GlowStrength = 0; //Reset
		}

		public readonly void OnConsumeAmmo()
		{
			float scale = 1f / Main.GameViewMatrix.Zoom.X;
			Vector2 position = Main.MouseWorld + new Vector2(32 * scale, 0);
			ParticleRenderers.OverItems.Add(new InterfaceAmmo(position, -Vector2.UnitY.RotatedByRandom(0.75f) * Main.rand.NextFloat(4f, 7f), Style, 45));
		}
	}

	public readonly record struct Recoil(EaseFunction Ease = null, float Speed = 2f, float Strength = 0.5f);

	public static readonly SoundStyle EmptyMagazine = new("SpiritReforged/Assets/SFX/Item/EmptyMagazine");

	/// <summary> Shortcut for <see cref="Item.GetGlobalItem{T}()"/>. </summary>
	public ReloadableGlobalItem ReloadInfo => Item.GetGlobalItem<ReloadableGlobalItem>();

	public abstract IReloadable.MagazineInfo Magazine { get; }

	public virtual InterfaceProfile? Profile => null;

	public virtual void DrawInterface(Item item, SpriteBatch spriteBatch) => Profile?.Draw(item, spriteBatch);

	public virtual void OnReload(Item item, Player player, IReloadable.ReloadContext context)
	{
		if (context == IReloadable.ReloadContext.Started)
			SoundEngine.PlaySound(EmptyMagazine, player.Center);
		else if (context == IReloadable.ReloadContext.Ended && item.TryGetGlobalItem(out ReloadableGlobalItem global))
			global.capacity = Magazine.GetFullCapacity(player);
	}

	public override void OnConsumeAmmo(Item ammo, Player player) => Profile?.OnConsumeAmmo();

	public sealed override void HoldStyle(Player player, Rectangle heldItemFrame)
	{
		Profile?.Update();
		ItemStyle(player, heldItemFrame, false);
	}

	public sealed override void UseStyle(Player player, Rectangle heldItemFrame)
	{
		Profile?.Update();
		ItemStyle(player, heldItemFrame, true);
	}

	public virtual void ItemStyle(Player player, Rectangle heldItemFrame, bool inUse) { }

	public sealed override void HoldItemFrame(Player player) => ItemFrame(player, false);
	public sealed override void UseItemFrame(Player player) => ItemFrame(player, true);

	public virtual void ItemFrame(Player player, bool inUse) { }

	public override bool ModifyItemDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData, ref DrawData? coloredDrawData, ref DrawData? glowMaskDrawData)
	{
		drawInfo.DrawDataCache.Add(GetDrawHeld(drawData, drawInfo.drawPlayer, new(4, (drawData.sourceRect?.Height ?? drawData.texture.Height) - 4), out float armRotation, new Recoil(EaseFunction.EaseCubicOut)));
		drawInfo.drawPlayer.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
		
		return true;
	}

	public static DrawData GetDrawHeld(DrawData input, Player player, Vector2 holdOrigin, out float armRotation, Recoil? recoil = null, Vector2 offset = default)
	{
		float rotation = input.rotation;
		if (recoil is Recoil recoilValue)
		{
			float animation = Math.Min((1f - player.itemAnimation / (float)player.itemAnimationMax) * recoilValue.Speed, 1);
			rotation = input.rotation - (recoilValue.Ease?.Ease(animation) ?? animation) * recoilValue.Strength * player.direction;
		}

		Vector2 position = player.GetFrontHandPosition(player.compositeFrontArm.stretch, rotation) - Main.screenPosition + new Vector2(0, player.gfxOffY) + offset;
		Vector2 size = input.sourceRect?.Size() ?? input.texture.Size();
		Vector2 origin = (input.effect == SpriteEffects.FlipHorizontally) ? new Vector2(size.X - holdOrigin.X, holdOrigin.Y) : holdOrigin;

		armRotation = rotation - MathHelper.PiOver2 * player.direction;
		return new(input.texture, position, input.sourceRect, input.color, rotation, origin, input.scale, input.effect, 0);
	}
}