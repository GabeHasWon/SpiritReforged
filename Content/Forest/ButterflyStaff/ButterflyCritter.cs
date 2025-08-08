using SpiritReforged.Common.NPCCommon;
using SpiritReforged.Common.WorldGeneration;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;

namespace SpiritReforged.Content.Forest.ButterflyStaff;

public class ButterflyCritter : ModNPC
{
	public ref float SettleCounter => ref NPC.ai[3];

	private bool _frameUpdating;
	/// <summary> Only used if this NPC is a bestiary dummy. </summary>
	private Vector2 _bestiaryOffset;

	private float _deathCounter = 1f;
	private int _settleCounter;
	private bool _settled;

	private float CountDeath()
	{
		NPC.dontTakeDamage = true;
		return _deathCounter -= 1f / 20;
	}

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 3; //Per column
		PersistentNPCSystem.PersistentTypes.Add(Type);
	}

	public override void SetDefaults()
	{
		NPC.Size = new(10);
		NPC.damage = 0;
		NPC.defense = 0;
		NPC.lifeMax = 1;
		NPC.dontCountMe = true;
		NPC.DeathSound = SoundID.NPCDeath6 with { Pitch = .2f };
		NPC.knockBackResist = .45f;
		NPC.aiStyle = 64;
		NPC.npcSlots = 0;
		NPC.noGravity = true;
		NPC.catchItem = ItemID.SilverCoin; //Allows bug nets to interact with this NPC, even though the player is never allowed to catch it

		if (!NPC.IsABestiaryIconDummy)
			NPC.Opacity = 0;

		AIType = NPCID.Firefly;
	}

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.AddInfo(this, "Surface NightTime");

	public override bool PreAI()
	{
		NPC.Opacity = MathHelper.Min(NPC.Opacity + .01f, 1); //Fade in

		if (_settled)
		{
			NPC.TargetClosest(false);

			if (NPC.HasValidTarget)
			{
				var target = Main.player[NPC.target];

				if (target.Distance(NPC.Center) < 90)
				{
					_settleCounter = 60 * 5;
					_settled = false;
				}
			}

			if (_settleCounter == 0)
			{
				_settleCounter = 60 * 5;
				_settled = false;
			}
		}
		else
		{
			NPC.rotation = 0;
		}

		if (_settleCounter == 0)
		{
			var tile = Framing.GetTileSafely(NPC.Center);
			if (tile.WallType != WallID.None || tile.HasTile && Main.tileAxe[tile.TileType]) //Just settled
			{
				_settled = true;
				_settleCounter = 60 * 30;

				NPC.rotation = Main.rand.NextFloat(MathHelper.Pi);
				NPC.velocity = Vector2.Zero;
				NPC.netUpdate = true;
			}
		}

		if (_deathCounter < 1) //Expire
		{
			if (CountDeath() <= 0)
				NPC.active = false;
		}

		_settleCounter = Math.Max(_settleCounter - 1, 0);
		return !_settled;
	}

	public override void AI()
	{
		NPC.spriteDirection = NPC.direction;

		if (NPC.velocity != Vector2.Zero && Main.rand.NextBool(10))
		{
			var dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.PinkTorch, Scale: 2f);
			dust.noGravity = true;
			dust.velocity = Vector2.Zero;
			dust.noLightEmittence = true;
		}
	}

	public override bool? CanBeCaughtBy(Item item, Player player)
	{
		CountDeath();
		SoundEngine.PlaySound(NPC.DeathSound, NPC.Center);

		return false;
	}

	public override bool CheckDead()
	{
		CountDeath();
		SoundEngine.PlaySound(NPC.DeathSound, NPC.Center);
		NPC.life = 1; //Don't die
		NPC.velocity = Vector2.Zero;

		return false;
	}

	public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers) => modifiers.HideCombatText();

	public override void FindFrame(int frameHeight)
	{
		NPC.frame.Width = 22;
		NPC.frame.X = NPC.frame.Width * (_settled ? 1 : 0);

		NPC.frameCounter += .15f;
		NPC.frameCounter %= Main.npcFrameCount[Type];

		NPC.frame.Y = (int)NPC.frameCounter * frameHeight;

		_frameUpdating = true;
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		const float blurStrength = 2f;

		var origin = NPC.Center - screenPos;

		if (NPC.IsABestiaryIconDummy) //Bestiary fun
		{
			if (Main.MouseScreen.Distance(origin + _bestiaryOffset) < 30)
			{
				_deathCounter = Math.Max(_deathCounter - .05f, 0);

				if (_deathCounter == 0)
					_bestiaryOffset = Main.rand.NextVector2Unit() * Main.rand.NextFloat(50f);
			}
			else
				_deathCounter = Math.Min(_deathCounter + .05f, 1);

			if (_frameUpdating)
				origin += (float)Math.Sin(Main.timeForVisualEffects / 40f) * 3f * Vector2.UnitY;

			origin += _bestiaryOffset;
		}

		_frameUpdating = false;
		var texture = TextureAssets.Npc[Type].Value;
		var frame = NPC.frame with { Width = NPC.frame.Width - 2, Height = NPC.frame.Height - 2 };
		var effects = (NPC.spriteDirection == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		const int images = 4;
		for (int i = 0; i < images; i++)
		{
			float radians = MathHelper.TwoPi / images * Lerp(50f) * i;
			var color = Color.Lerp(new Color(90, 70, 255, 50), Color.HotPink with { A = 50 }, Lerp(25f)) * _deathCounter;
			var position = origin + new Vector2(0f, blurStrength * (8f - _deathCounter * 7f)).RotatedBy(radians) * Lerp(60f);

			spriteBatch.Draw(texture, position, frame, color * NPC.Opacity, NPC.rotation, frame.Size() / 2, NPC.scale, effects, 0);
		}

		float Lerp(float rate) => (float)Math.Sin((Main.timeForVisualEffects + NPC.whoAmI * 3) / rate);

		return false;
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo)
	{
		if (!spawnInfo.Invasion && !spawnInfo.Water)
		{
			var coord = new Point(spawnInfo.SpawnTileX, spawnInfo.SpawnTileY);
			foreach (var biome in MicrobiomeSystem.Microbiomes)
			{
				if (biome is not ButterflyShrineBiome e)
					continue;

				if (e.Rectangle.Contains(coord))
					return 0.8f; //Commonly spawn butterflies in the zone

				if (coord.Y < e.Position.Y && Math.Abs(coord.X - e.Position.X) < e.Rectangle.Width)
					return Main.dayTime ? 0.047f : 0.08f; //Rarely spawn butterflies anywhere above the zone
			}
		}

		return 0;
	}
}
