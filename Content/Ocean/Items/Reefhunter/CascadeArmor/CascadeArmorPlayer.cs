using SpiritReforged.Common.Multiplayer;
using SpiritReforged.Common.Particle;
using SpiritReforged.Content.Ocean.Items.Reefhunter.Particles;
using System.IO;
using Terraria.Audio;

namespace SpiritReforged.Content.Ocean.Items.Reefhunter.CascadeArmor;

public class CascadeArmorPlayer : ModPlayer
{
	private float GetBaseBubbleScale => Common.Easing.EaseFunction.EaseCubicOut.Ease(_bubbleVisual) * (1 + (float)Math.Sin(Main.time * MathHelper.TwoPi / 120) / 30);

	private static Asset<Texture2D> ShieldTexture, OutlineTexture;

	internal float bubbleStrength = 0;
	internal int bubbleCooldown = 120;
	internal bool setActive = false;

	//Visual data
	private Vector2 _realOldVelocity = Vector2.Zero; //Mandatory, player.oldVelocity just doesn't work????????? ???? ???
	private Vector2 _bubbleSquish = Vector2.One;
	private Vector2 _squishVelocity = Vector2.Zero;
	private float _bubbleVisual = 0;

	/// <summary> The last value of <see cref="GetResist"/> before being struck. </summary>
	private float _lastResisted;

	/// <summary> Gets a flat damage resistance value based on difficulty and current <see cref="bubbleStrength"/>, with slight random variance. </summary>
	private float GetResist()
	{
		const float variance = 1.8f;
		int maxResist = 20;

		if (Main.masterMode)
			maxResist = 50;
		else if (Main.expertMode)
			maxResist = 35;

		return (maxResist + Main.rand.NextFloat(-variance, variance)) * bubbleStrength;
	}

	public override void SetStaticDefaults()
	{
		if (!Main.dedServ)
		{
			string texturePath = typeof(CascadeArmorPlayer).Namespace.Replace(".", "/") + "/BubbleShield";

			ShieldTexture = ModContent.Request<Texture2D>(texturePath);
			OutlineTexture = ModContent.Request<Texture2D>(texturePath + "Outline");
		}
	}

	public override void ResetEffects() => setActive = false;

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (setActive && bubbleCooldown == 0)
		{
			float added = bubbleStrength + Math.Min(damageDone / 180f, .05f);
			bubbleStrength = MathHelper.Clamp(added, 0, 1);

			if (Main.netMode != NetmodeID.SinglePlayer)
				new CascadeBubbleData(bubbleStrength, (byte)Player.whoAmI).Send();
		}
	}

	public override void PreUpdate() => _realOldVelocity = Player.velocity;
	public override void ModifyHurt(ref Player.HurtModifiers modifiers) => TryPopBubble(ref modifiers.FinalDamage);
	public override bool FreeDodge(Player.HurtInfo info)
	{
		if (setActive && info.Damage == 1) //Instead of taking one damage upon resisting an attack, dodge it
		{
			int index = CombatText.NewText(Player.getRect(), CombatText.DamagedFriendly, 0);
			ResistanceTextHandler.ApplyText(0, _lastResisted, index);

			Player.SetImmuneTimeForAllTypes(40);
			SoundEngine.PlaySound(SoundID.Item155 with { Pitch = .5f }, Player.Center);
			SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = 1f }, Player.Center);

			return true;
		}

		return false;
	}

	public override void PostHurt(Player.HurtInfo info)
	{
		if (setActive)
			ResistanceTextHandler.ApplyText(info.Damage, _lastResisted);
	}

	public override void PostUpdate()
	{
		if (setActive && !Player.dead)
		{
			var drawOn = Player.GetModPlayer<Common.PlayerCommon.ExtraDrawOnPlayer>();

			drawOn.DrawDict.Add(delegate (SpriteBatch sB) { DrawBubble(sB); }, Common.PlayerCommon.ExtraDrawOnPlayer.DrawType.NonPremultiplied);
			//drawOn.DrawDict.Add(delegate (SpriteBatch sB) { DrawBubble(sB, true); }, Common.PlayerCommon.ExtraDrawOnPlayer.DrawType.AlphaBlend);

			HandleBubbleJiggle();

			if (bubbleCooldown > 0)
				bubbleCooldown--;
		}
		else
		{
			if (bubbleStrength > 0) //Kill bubble if armor piece unequipped
				PopBubble();

			bubbleCooldown = 60;
			_bubbleSquish = Vector2.One;
			_squishVelocity = Vector2.Zero;
		}

		if (_bubbleVisual < bubbleStrength) //smooth transition for visual
			_bubbleVisual = MathHelper.Lerp(_bubbleVisual, bubbleStrength, .1f);

		_bubbleVisual = Math.Min(_bubbleVisual, bubbleStrength); //Cap visual strength at real strength value
	}

	private void TryPopBubble(ref StatModifier damage)
	{
		_lastResisted = GetResist();

		if (bubbleStrength > 0f)
		{
			damage.Flat -= _lastResisted;
			PopBubble();
		}
	}

	private void PopBubble()
	{
		int radius = (int)(120 * bubbleStrength);

		foreach (var npc in Main.ActiveNPCs)
		{
			if (npc.CanBeChasedBy() && npc.DistanceSQ(Player.Center) < radius * radius)
				npc.SimpleStrikeNPC(1, Player.Center.X < npc.Center.X ? 1 : -1, false, 3f * bubbleStrength);
		}

		if (!Main.dedServ)
		{
			ParticleHandler.SpawnParticle(new BubblePop(Player.Center, GetBaseBubbleScale, 0.8f * _bubbleVisual, 30));

			SoundEngine.PlaySound(SoundID.Item54 with { PitchVariance = 0.2f }, Player.Center);
			SoundEngine.PlaySound(SoundID.NPCHit3 with { PitchVariance = 0.2f, Pitch = -.5f }, Player.Center);
			SoundEngine.PlaySound(SoundID.Item86, Player.Center);
		}

		bubbleStrength = 0f;
		_bubbleVisual = 0f;
		bubbleCooldown = 120;
	}

	/// <summary>
	/// Makes the bubble squash and stretch based on the player's momentum, by loosely adjusting the bubble's size over time based on how fast the player is moving
	/// </summary>
	private void HandleBubbleJiggle()
	{
		float squishspeed = 0.08f; //How fast the bubble squishes and stretches
		float interpolationspeed = 0.05f; //How fast the velocity catches up to what it's meant to be
		var desiredSquish = new Vector2(1, 1);
		float moveStrength = 0.15f; //how much the player's speed affects the bubble
		float squishBounds = 0.075f; //the bounds to which the bubble can squash and stretch
		float SUPERjiggleThreshold = 5f;

		//Make it squish in the direction the player is moving
		var absVel = new Vector2(Math.Abs(Player.velocity.X), Math.Abs(Player.velocity.Y));

		float playerMovementFactor = MathHelper.Clamp((1 + absVel.X * moveStrength) / (1 + absVel.Y * moveStrength), 1 - squishBounds, 1 + squishBounds);
		desiredSquish.X *= playerMovementFactor;
		desiredSquish.Y /= playerMovementFactor;

		var squishDirectionUnit = (desiredSquish - _bubbleSquish).SafeNormalize(Vector2.One);
		if (Vector2.Distance(desiredSquish, _bubbleSquish) > 0.05f) //don't change velocity if it's super minor, to avoid rapid tiny changes in size
			_squishVelocity = Vector2.Lerp(_squishVelocity, squishDirectionUnit, interpolationspeed);

		//jiggle more if player makes a sudden big jump in velocity (ie dashing, landing, jumping)
		if (Math.Abs(Player.velocity.Length() - _realOldVelocity.Length()) > SUPERjiggleThreshold)
			_squishVelocity += squishDirectionUnit / 2;

		_bubbleSquish += _squishVelocity * squishspeed;

		//Loosely clamp the squish so it doesnt stretch too far in either direction, without making an awkward looking hard restriction
		var clampedBubbleSquish = new Vector2(MathHelper.Clamp(_bubbleSquish.X, 1 - squishBounds, 1 + squishBounds), MathHelper.Clamp(_bubbleSquish.Y, 1 - squishBounds, 1 + squishBounds));
		_bubbleSquish = Vector2.Lerp(_bubbleSquish, clampedBubbleSquish, interpolationspeed);
	}

	private void DrawBubble(SpriteBatch sB, bool outline = false)
	{
		if (_bubbleVisual > 0f)
		{
			Texture2D texture = (outline ? OutlineTexture : ShieldTexture).Value;

			Vector2 drawPos = Player.Center - Main.screenPosition + new Vector2(0, Player.gfxOffY);

			float opacity = outline ? .25f : 0.8f;
			Color lightColor = Lighting.GetColor((int)(Player.Center.X / 16), (int)(Player.Center.Y / 16));

			sB.Draw(texture, drawPos, null, lightColor * _bubbleVisual * opacity, 0f, texture.Size() / 2f, _bubbleSquish * GetBaseBubbleScale, SpriteEffects.None, 0);
		}
	}
}

/// <summary> Syncs bubble strength corresponding to <paramref name="value"/> for player <paramref name="index"/>. </summary>
internal class CascadeBubbleData : PacketData
{
	private readonly float _value;
	private readonly byte _playerIndex;

	public CascadeBubbleData() { }
	public CascadeBubbleData(float value, byte playerIndex)
	{
		_value = value;
		_playerIndex = playerIndex;
	}

	public override void OnReceive(BinaryReader reader, int whoAmI)
	{
		float value = reader.ReadSingle();
		byte player = reader.ReadByte();

		if (Main.netMode == NetmodeID.Server)
			new CascadeBubbleData(value, player).Send(ignoreClient: whoAmI);

		Main.player[player].GetModPlayer<CascadeArmorPlayer>().bubbleStrength = value;
	}

	public override void OnSend(ModPacket modPacket)
	{
		modPacket.Write(_value);
		modPacket.Write(_playerIndex);
	}
}