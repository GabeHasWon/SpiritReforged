using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon.MagazineSystem;
using SpiritReforged.Common.Visuals;
using Terraria.Audio;
using Terraria.DataStructures;
using static SpiritReforged.Common.ItemCommon.MagazineSystem.MagazineInterfacePlayer;

namespace SpiritReforged.Common.ItemCommon.Abstract;

/// <summary> Implements <see cref="IReloadable"/> and provides additional utilities for ease of use. </summary>
public abstract class ReloadableItem : ModItem, IReloadable
{
	public readonly record struct InterfaceProfile(StyleID Style)
	{
		public readonly void Draw(Item item, SpriteBatch spriteBatch)
		{
			if (!item.TryGetGlobalItem(out ReloadableGlobalItem global) || !ReloadableGlobalItem.HasMagazine(item.type, out IReloadable iReloadable))
				return;

			Texture2D texture = Particle.GetTexture<InterfaceAmmo>();
			Rectangle source = texture.Frame(1, Enum.GetValues<StyleID>().Length, 0, (int)Style, 0, -2);
			//Texture2D starTexture = AssetLoader.LoadedTextures["Star"].Value;
			//Texture2D bloomTexture = AssetLoader.LoadedTextures["Bloom"].Value;

			int fullCapacity = iReloadable.Magazine.FullCapacity;
			int usedCapacity = iReloadable.Magazine.FullCapacity - global.capacity;

			int moveTimeMax = Main.LocalPlayer.itemTimeMax;
			int moveTime = Main.LocalPlayer.itemTime;

			const int offsetSize = 8;
			const int maxVisible = 10;

			for (int i = 0; i < global.capacity; i++)
			{
				float fadeOut = (i + 1) / (float)global.capacity;

				if (global.capacity >= maxVisible)
				{
					if (i < global.capacity - maxVisible) // anything more than maxVisible just gets turned invisible
						fadeOut = 0f;
					else
						fadeOut = (i - (global.capacity - maxVisible)) / (float)maxVisible;
				}

				Vector2 position = Main.MouseScreen + new Vector2(32, fullCapacity * offsetSize);
				float moveShellUp = MathHelper.Lerp((usedCapacity - 1) * offsetSize, usedCapacity * offsetSize, moveTime > 0 ? EaseFunction.EaseCircularOut.Ease(1 - moveTime / (float)moveTimeMax) : 1f);

				if (global.IsReloading(Main.LocalPlayer) && moveTime <= 0)
					moveShellUp = usedCapacity * offsetSize;

				Vector2 offset = new Vector2(0, -offsetSize * i - moveShellUp);

				/*if (i < empoweredCount)
				{
					float sine = (float)Math.Sin(Main.timeForVisualEffects / 20f);

					Vector2 shake = Vector2.Zero;
					float rotation = sine * 0.2f;
					float scale = 1f;
					if (sine > 0)
					{
						scale += sine * 0.1f;
						shake += Main.rand.NextVector2CircularEdge(sine, sine);
					}

					if (empoweredFlashTimer > 0)
					{
						float lerp = empoweredFlashTimer / (float)maxEmpoweredFlashTimer;

						Vector2 starScale = new Vector2(MathHelper.Lerp(0.1f, 0.3f, 1f - lerp), 0.1f);

						spriteBatch.Draw(starTexture, position + offset, null, Color.Yellow.Additive() * fadeOut * lerp, 0f, starTexture.Size() / 2f, starScale * 1.2f, 0f, 0f);
						spriteBatch.Draw(starTexture, position + offset, null, Color.LightYellow.Additive() * fadeOut * lerp, 0f, starTexture.Size() / 2f, starScale * 1f, 0f, 0f);
					}

					spriteBatch.Draw(outlineTexture, position + offset + shake, null, Color.Yellow * fadeOut, rotation, outlineTexture.Size() / 2f, scale, 0f, 0f);
					spriteBatch.Draw(texture, position + offset + shake, null, Main.mouseColor * fadeOut, rotation, texture.Size() / 2f, scale, 0f, 0f);

					spriteBatch.Draw(bloomTexture, position + offset, null, Color.Yellow.Additive() * fadeOut * 0.15f, 0f, bloomTexture.Size() / 2f, 0.15f, 0f, 0f);
					spriteBatch.Draw(bloomTexture, position + offset, null, Color.LightYellow.Additive() * fadeOut * 0.1f, 0f, bloomTexture.Size() / 2f, 0.1f, 0f, 0f);

					if (empoweredFlashTimer > 0)
					{
						float lerp = empoweredFlashTimer / (float)maxEmpoweredFlashTimer;

						spriteBatch.Draw(bloomTexture, position + offset, null, Color.Yellow.Additive() * fadeOut * lerp * 0.5f, 0f, bloomTexture.Size() / 2f, 0.15f, 0f, 0f);
						spriteBatch.Draw(bloomTexture, position + offset, null, Color.LightYellow.Additive() * fadeOut * lerp * 0.35f, 0f, bloomTexture.Size() / 2f, 0.1f, 0f, 0f);
					}

					continue;
				}*/

				DrawHelpers.DrawOutline((extraOffset) =>
				spriteBatch.Draw(texture, position + offset + extraOffset, source, Main.MouseBorderColor * fadeOut, 0f, source.Size() / 2f, 1f, 0f, 0f));

				spriteBatch.Draw(texture, position + offset, source, Main.mouseColor * fadeOut, 0f, source.Size() / 2f, 1f, 0f, 0f);
			}
		}

		public readonly void OnConsumeAmmo()
		{
			Vector2 position = Main.MouseWorld + new Vector2(16, 0);
			ParticleRenderers.OverItems.Add(new InterfaceAmmo(position, -Vector2.UnitY.RotatedByRandom(0.75f) * Main.rand.NextFloat(4f, 7f), Style, 45));
		}
	}

	public readonly record struct Recoil(EaseFunction Ease = null, float Speed = 2f, float Strength = 0.5f);

	/// <summary> Shortcut for <see cref="Item.GetGlobalItem{T}()"/>. </summary>
	public ReloadableGlobalItem ReloadInfo => Item.GetGlobalItem<ReloadableGlobalItem>();

	public abstract IReloadable.MagazineInfo Magazine { get; }

	public virtual InterfaceProfile? Profile => null;

	public virtual void DrawInterface(Item item, SpriteBatch spriteBatch) => Profile?.Draw(item, spriteBatch);

	public virtual void OnReload(Item item, Player player, IReloadable.ReloadContext context)
	{
		if (context == IReloadable.ReloadContext.Started)
			SoundEngine.PlaySound(new SoundStyle("SpiritReforged/Assets/SFX/Item/EmptyMagazine"), player.Center);
	}

	public override void OnConsumeAmmo(Item ammo, Player player) => Profile?.OnConsumeAmmo();

	public sealed override void HoldStyle(Player player, Rectangle heldItemFrame) => ItemStyle(player, heldItemFrame, false);
	public sealed override void UseStyle(Player player, Rectangle heldItemFrame) => ItemStyle(player, heldItemFrame, true);

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

		Vector2 position = player.GetFrontHandPosition(player.compositeFrontArm.stretch, rotation) - Main.screenPosition + offset;
		Vector2 size = input.sourceRect?.Size() ?? input.texture.Size();
		Vector2 origin = (input.effect == SpriteEffects.FlipHorizontally) ? new Vector2(size.X - holdOrigin.X, holdOrigin.Y) : holdOrigin;

		armRotation = rotation - MathHelper.PiOver2 * player.direction;
		return new(input.texture, position, input.sourceRect, input.color, rotation, origin, input.scale, input.effect, 0);
	}
}