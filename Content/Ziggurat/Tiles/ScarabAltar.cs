using SpiritReforged.Common;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.PrimitiveRendering;
using SpiritReforged.Common.PrimitiveRendering.PrimitiveShape;
using SpiritReforged.Common.PrimitiveRendering.Trail_Components;
using SpiritReforged.Common.PrimitiveRendering.Trails;
using SpiritReforged.Common.TileCommon;
using SpiritReforged.Common.TileCommon.PresetTiles;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Desert.ScarabBoss.Boss;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;
using Terraria.Graphics.CameraModifiers;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;

namespace SpiritReforged.Content.Ziggurat.Tiles;

public class ScarabAltar : EntityTile<ScarabAltarEntity>, IAutoloadTileItem
{
	public sealed class LightShaderData(Asset<Effect> shader, string passName) : ScreenShaderData(shader, passName)
	{
		public override void Update(GameTime gameTime)
		{
			float screenPositionInTiles = (Main.screenPosition.Y + Main.screenHeight / 2f) / 16f;

			float surfaceValue = 1f - Utils.SmoothStep((float)Main.worldSurface, (float)Main.worldSurface + 30f, screenPositionInTiles);
			Vector2 midnightDirection = Utils.GetDayTimeAsDirectionIn24HClock(0f);
			surfaceValue *= 1 - Utils.SmoothStep(0.2f, 0.4f, Vector2.Dot(midnightDirection, Utils.GetDayTimeAsDirectionIn24HClock()));

			UseProgress(1 - surfaceValue * 0.7f);
		}
	}

	#region projectiles
	public sealed class FloatingGem : ModProjectile
	{
		public override string Texture => AssetLoader.EmptyTexture;

		public int ItemType
		{
			get => (int)Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}

		public int TileEntityID
		{
			get => (int)Projectile.ai[1];
			set => Projectile.ai[1] = value;
		}

		public ref float Counter => ref Projectile.ai[2];

		private Vector2 _origin;
		private Color[] _sampleColors;

		public override void SetDefaults()
		{
			Projectile.Size = new(16);
			Projectile.tileCollide = false;
		}

		public override void AI()
		{
			if (_origin == default)
			{
				if (!Main.dedServ)
				{
					float scale = Projectile.scale;
					_sampleColors = TextureColorCache.GetColors(GetItemTexture());

					TrailSystem.ProjectileRenderer.CreateTrail(Projectile, new VertexTrail(new StandardColorTrail(_sampleColors[_sampleColors.Length / 3].Additive() * 2), 
						new RoundCap(), new EntityTrailPosition(Projectile), new DefaultShader(), 10 * scale, 50 * scale));

					TrailSystem.ProjectileRenderer.CreateTrail(Projectile, new VertexTrail(new LightColorTrail(Color.White.Additive(), Color.Transparent),
						new RoundCap(), new EntityTrailPosition(Projectile), new DefaultShader(), 5 * scale, 50 * scale));
				}

				_origin = Projectile.Center;

				Projectile.Center = Main.player[Projectile.owner].Center;
				Projectile.scale = 0;
			} //Spawn effects

			if (!Main.dedServ && Main.rand.NextBool(7))
				ParticleHandler.SpawnParticle(new EmberParticle(Main.rand.NextVector2FromRectangle(Projectile.Hitbox), Projectile.velocity * 0.1f, _sampleColors[_sampleColors.Length / 2], 0.5f, 30, 2));

			Projectile.scale = Math.Min(Projectile.scale + 0.05f, 1);

			float speed = EaseFunction.EaseCubicInOut.Ease(Counter / 30) * 8;
			var velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(_origin) * speed, 0.1f);

			if (!velocity.HasNaNs())
				Projectile.velocity = velocity;

			Projectile.rotation += Projectile.velocity.X * 0.08f;

			if (++Counter > 30 && Projectile.DistanceSQ(_origin) < 16 * 16)
			{
				Projectile.Kill();
				
				if (!Main.dedServ)
					TrailSystem.ProjectileRenderer.DissolveTrail(Projectile);

				if (TileEntity.ByID[TileEntityID] is ScarabAltarEntity entity)
					entity.Interact();
			}
		}

		public override bool? CanDamage() => false;

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = GetItemTexture();
			Vector2 center = Projectile.Center - Main.screenPosition;

			DrawHelpers.DrawOutline(Main.spriteBatch, texture, center, default, 
				(offset) => Main.EntitySpriteDraw(texture, center + offset, null, Color.White.Additive(), Projectile.rotation, texture.Size() / 2, Projectile.scale, default));

			Main.EntitySpriteDraw(texture, center, null, Lighting.GetColor(Projectile.Center.ToTileCoordinates()), Projectile.rotation, texture.Size() / 2, Projectile.scale, default);

			return false;
		}

		public Texture2D GetItemTexture()
		{
			Main.instance.LoadItem(ItemType);
			return TextureAssets.Item[ItemType].Value;
		}
	}

	public sealed class BeamOLight : ModProjectile
	{
		[WorldBound]
		public static bool Enabled;

		public static readonly SoundStyle Anticipation = new("SpiritReforged/Assets/SFX/Tile/DissonantChime")
		{ 
			Pitch = 0.1f, 
			Volume = 0.25f 
		};

		public override string Texture => AssetLoader.EmptyTexture;

		public int TimeLeftMax
		{
			get => (int)Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}

		public ref float Counter => ref Projectile.ai[1];

		private const int WaitTime = 120;
		private bool _justSpawned = true;
		private bool _spawnedBoss = false;

		public override void Load()
		{
			if (!Main.dedServ)
			{
				const string name = "SpiritReforged:LightShaderData";
				Filters.Scene[name] = new Filter(
					new LightShaderData(ModContent.Request<Effect>("SpiritReforged/Assets/Shaders/LightFilter"), "LightFilterPass")
					.UseColor(new Color(100, 100, 100))
					.UseImage(ModContent.Request<Texture2D>("SpiritReforged/Assets/Textures/noiseCrystal2"))
					, EffectPriority.High);

				Filters.Scene[name].Load();
			}
		}

		public override void SetDefaults()
		{
			Projectile.Size = new(16);
			Projectile.tileCollide = false;
		}

		public override void AI()
		{
			Enabled = true;

			if (_justSpawned)
			{
				if (!Main.dedServ)
				{
					/*if (!CrossMod.Fables.Enabled)
					{
						Vector2 targetPosition = Projectile.Center - Main.ScreenSize.ToVector2() / 2;
						var easeAnimation = new AnimationSequence()
							.Add(new AnimationSequence.EaseSegment(WaitTime, Main.screenPosition, targetPosition, EaseFunction.EaseCubicInOut))
							.Add(new AnimationSequence.WaitSegment(TimeLeftMax - WaitTime - 40))
							.Add(new SequenceCameraModifier.ReturnSegment(60, EaseFunction.EaseCubicInOut));

						Main.instance.CameraModifiers.Add(new SequenceCameraModifier(easeAnimation));
					}*/

					Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center, Vector2.UnitX, 5, 5, 30));
				}

				Projectile.timeLeft = TimeLeftMax;
				_justSpawned = false;

				SoundEngine.PlaySound(Anticipation with { Pitch = 0.1f, Volume = 0.25f }, Projectile.Center);
			}

			if (Projectile.timeLeft >= TimeLeftMax - WaitTime)
			{
				if (Main.rand.NextFloat() < Counter / WaitTime * 0.4f)
				{
					float strength = Main.rand.NextFloat();
					var color = Color.Lerp(Color.Red, Color.Goldenrod, strength);
					Vector2 position = Projectile.Center - (Vector2.UnitY * Main.rand.NextFloat(40, 60)).RotatedByRandom(1);

					ParticleHandler.SpawnParticle(new EmberParticle(position, position.DirectionTo(Projectile.Center) * strength * 2, color, MathHelper.Lerp(0.2f, 0.5f, strength), 30, 2));
				}
			}
			else
			{
				if (!Main.dedServ)
				{
					if (Filters.Scene["SpiritReforged:LightShaderData"].IsActive())
					{
						var shader = Filters.Scene["SpiritReforged:LightShaderData"].GetShader();
						float power = Math.Min(((Counter - WaitTime < 20) ? Counter - WaitTime : Projectile.timeLeft) / 20f, 1);

						shader.UseOpacity(power);
						shader.UseIntensity(power / 2);
					}
					else
					{
						Filters.Scene.Activate("SpiritReforged:LightShaderData");
					}
				}

				if (Main.rand.NextBool(3))
				{
					float progress = (float)(Projectile.timeLeft / (float)TimeLeftMax);

					Rectangle hitbox = new((int)Projectile.Center.X - 8, (int)(Projectile.Center.Y - Main.screenHeight / 2), 16, Main.screenHeight / 2);
					Vector2 position = Main.rand.NextVector2FromRectangle(hitbox);
					float scale = Main.rand.NextFloat(0.5f, 2f);

					ParticleHandler.SpawnParticle(new ImpactLinePrim(position, Vector2.UnitY * -5 * progress, Color.Goldenrod.Additive() * progress, new(0.3f, 1 * scale), 20, 1));

					if (Main.rand.NextFloat() > 0.3f)
						ParticleHandler.SpawnParticle(new ImpactLinePrim(position, Vector2.UnitY * -5 * progress, Color.White.Additive() * progress, new(0.2f, 0.5f * scale), 20, 1));
				}
			}

			if (++Counter >= WaitTime && !_spawnedBoss)
			{
				if (!Main.dedServ)
				{
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center, Vector2.UnitY, 6, 2, 50));
					ParticleHandler.SpawnParticle(new LightBurst(Projectile.Center, 0, Color.PaleVioletRed, 1, 20) { Velocity = -Vector2.UnitY });

					for (int i = 0; i < 5; i++)
						ParticleHandler.SpawnParticle(new EmberParticle(Projectile.Center, -Vector2.UnitY.RotatedByRandom(1) * Main.rand.NextFloat(0.2f, 1), Color.Goldenrod, Color.MediumPurple, Main.rand.NextFloat(0.2f, 0.5f), 150, 2));
				}

				if (Main.netMode != NetmodeID.MultiplayerClient) //Summon Scarabeus
					NPC.NewNPCDirect(Projectile.GetSource_Death(), Projectile.Center, ModContent.NPCType<Scarabeus>());

				_spawnedBoss = true;
			}
		}

		public override void OnKill(int timeLeft)
		{
			if (!Main.dedServ && Filters.Scene["SpiritReforged:LightShaderData"].IsActive())
				Filters.Scene.Deactivate("SpiritReforged:LightShaderData");

			Enabled = false;
		}

		public override bool? CanDamage() => false;

		public override bool PreDraw(ref Color lightColor)
		{
			if (Projectile.timeLeft > TimeLeftMax - WaitTime)
			{
				Texture2D rayTexture = TextureAssets.Projectile[ProjectileID.MedusaHeadRay].Value;
				float rayOpacity = 1f - (float)Projectile.timeLeft / TimeLeftMax;

				Main.EntitySpriteDraw(rayTexture, Projectile.Center - Main.screenPosition, null, Color.Goldenrod.Additive() * rayOpacity, 0, new(rayTexture.Width / 2, rayTexture.Height), new Vector2(2, 0.2f) * Projectile.scale, default);
				Main.EntitySpriteDraw(rayTexture, Projectile.Center - Main.screenPosition, null, Color.White.Additive() * rayOpacity, 0, new(rayTexture.Width / 2, rayTexture.Height), new Vector2(1, 0.1f) * Projectile.scale, default);

				return false;
			}

			float progress = EaseFunction.EaseCircularIn.Ease(Projectile.timeLeft / (float)(TimeLeftMax - WaitTime));

			Vector2 center = Projectile.Center;
			float opacity = Math.Clamp(((float)(TimeLeftMax - WaitTime) - Projectile.timeLeft) * 0.2f, 0, 1);

			Color rainbow = Main.hslToRgb((float)Main.timeForVisualEffects / 10f % 1, 1f, 0.5f);
			var subColor = Color.Lerp(Color.Lerp(rainbow, Color.Goldenrod, 1f - progress), Color.PaleVioletRed, progress);

			DrawLightBeam(center, subColor, opacity * progress);

			for (int i = 0; i < 2; i++)
			{
				SquarePrimitive blurLine = new()
				{
					Position = Projectile.Center - Main.screenPosition,
					Height = 72,
					Length = 36,
					Rotation = MathHelper.PiOver2,
					Color = ((i == 0) ? Color.Goldenrod : Color.White).Additive() * progress
				};

				PrimitiveRenderer.DrawPrimitiveShape(blurLine, AssetLoader.LoadedShaders["BlurLine"].Value);
			}

			Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.7f, 0.2f) * progress * 3);
			return false;
		}

		public static void DrawLightBeam(Vector2 position, Color color, float opacity)
		{
			Texture2D texture = AssetLoader.LoadedTextures["GlowTrail"].Value;
			Rectangle screen = new((int)Main.screenPosition.X, (int)Main.screenPosition.Y, Main.screenWidth, Main.screenHeight);
			Vector2 origin = new(0, texture.Height / 2);
			float rotation = -MathHelper.PiOver2;

			while (screen.Contains(position.ToPoint()))
			{
				Main.EntitySpriteDraw(texture, position - Main.screenPosition, null, color.Additive() * opacity, rotation, origin, new Vector2(1, 0.5f), default);
				Main.EntitySpriteDraw(texture, position - Main.screenPosition, null, Color.White.Additive() * opacity, rotation, origin, new Vector2(1, 0.25f), default);

				position.Y -= texture.Height;
			}
		}
	}
	#endregion

	private int[] _hoverTypes;

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileID.Sets.PreventsTileRemovalIfOnTopOfIt[Type] = true;
		TileID.Sets.PreventsTileHammeringIfOnTopOfIt[Type] = true;
		TileID.Sets.HasOutlines[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Width = 4;
		TileObjectData.newTile.Height = 3;
		TileObjectData.newTile.CoordinateHeights = [16, 16, 18];
		TileObjectData.newTile.Origin = new(2, 2);
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.HookPostPlaceMyPlayer = Hook;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(124, 24, 28), CreateMapEntryName());
		DustType = -1;
		MinPick = 55;
	}

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => settings.player.InInteractionRange(i, j, TileReachCheckSettings.Simple);

	public override void MouseOver(int i, int j)
	{
		if (_hoverTypes == null)
		{
			List<int> hoverTypes = [];

			for (int type = 0; type < SpiritSets.Gemstone.Length; type++)
				if (SpiritSets.Gemstone[type])
					hoverTypes.Add(type);

			_hoverTypes = hoverTypes.ToArray();
		}

		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = FindSacrifice(Main.LocalPlayer, out Item result) ? result.type : _hoverTypes[(int)Math.Abs(Main.timeForVisualEffects / 90) % _hoverTypes.Length];
	}

	public override bool RightClick(int i, int j)
	{
		int projectileType = ModContent.ProjectileType<FloatingGem>();

		if (Main.dayTime && !BeamOLight.Enabled && FindSacrifice(Main.LocalPlayer, out Item result) && Entity(i, j) is ScarabAltarEntity entity 
			&& entity.consumableCount + Main.LocalPlayer.ownedProjectileCounts[projectileType] < ScarabAltarEntity.ConsumableCountMax)
		{
			if (--result.stack <= 0)
				result.TurnToAir(); //Consume an item

			Vector2 origin = TileObjectData.TopLeft(i, j).ToWorldCoordinates(32, 8);

			Projectile.NewProjectile(new EntitySource_TileInteraction(Main.LocalPlayer, i, j), origin, (Vector2.UnitY * -Main.rand.NextFloat(9, 13)).RotateRandom(0.5), 
				projectileType, 0, 0, Main.myPlayer, result.type, entity.ID);

			return true;
		}

		return false;
	}

	private static bool FindSacrifice(Player player, out Item result)
	{
		if (!player.HeldItem.IsAir && SpiritSets.Gemstone[player.HeldItem.type])
		{
			result = player.HeldItem;
			return true;
		}

		foreach (Item item in player.inventory)
		{
			if (!item.IsAir && SpiritSets.Gemstone[item.type])
			{
				result = item;
				return true;
			}
		}

		result = new(ItemID.None);
		return false;
	}

	public override void PlaceInWorld(int i, int j, Item item)
	{
		if (Entity(i, j) is ScarabAltarEntity entity)
			entity.SetInteractTime();
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (Main.tile[i, j].TileFrameY <= 18)
		{
			Main.instance.TilesRenderer.AddSpecialPoint(i, j, Terraria.GameContent.Drawing.TileDrawing.TileCounterType.CustomNonSolid);
			return true;
		}

		return true;
	}

	public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
	{
		const int frame_duration = 5;

		Texture2D texture = TextureAssets.Tile[Type].Value;
		Texture2D highlight = TextureAssets.HighlightMask[Type].Value;

		Tile tile = Main.tile[i, j];
		bool drawingTop = tile.TileFrameX == 0 && tile.TileFrameY == 0; //Bottom right corner
		bool highlighted = Main.InSmartCursorHighlightArea(i, j, out bool actuallySelected);
		Color highlightColor = actuallySelected ? Color.Yellow : Color.Gray;

		if (drawingTop && Entity(i, j) is ScarabAltarEntity entity)
		{
			float interactProgress = entity.InteractTime / (float)ScarabAltarEntity.InteractTimeMax;
			int frame = (int)((Main.timeForVisualEffects % 300 > frame_duration * 5.9f) ? 0 : Main.timeForVisualEffects % 300 / frame_duration);

			Rectangle topSource = new(46 * frame, 56, 44, 14);
			Vector2 topPosition = new Vector2(i, j).ToWorldCoordinates(32, 7 - entity.InteractTime) - Main.screenPosition;
			float rotation = (float)Math.Sin(Main.timeForVisualEffects) * 0.2f * interactProgress;

			#region reactive glow
			float opacity = entity.consumableCount / (float)ScarabAltarEntity.ConsumableCountMax;
			if (opacity > 0)
			{
				Texture2D squareBeam = TextureAssets.Extra[ExtrasID.PortalGateHalo2].Value;
				Texture2D beam = TextureAssets.Projectile[ProjectileID.MedusaHeadRay].Value;
				Vector2 beamOrigin = new(beam.Width / 2, beam.Height);

				spriteBatch.Draw(squareBeam, new Vector2(i, j).ToWorldCoordinates(32, -entity.InteractTime) - Main.screenPosition, null, Color.PaleGoldenrod.Additive() * interactProgress * opacity, 0, squareBeam.Size() / 2, new Vector2(0.65f, 0.3f), default, 0);
				spriteBatch.Draw(squareBeam, new Vector2(i, j).ToWorldCoordinates(32, -entity.InteractTime) - Main.screenPosition, null, Color.PaleGoldenrod.Additive() * interactProgress * opacity, 0, squareBeam.Size() / 2, new Vector2(0.6f, 0.3f), default, 0);

				spriteBatch.Draw(beam, new Vector2(i, j).ToWorldCoordinates(32, -entity.InteractTime) - Main.screenPosition, null, Color.Goldenrod.Additive() * interactProgress * opacity, 0, beamOrigin, new Vector2(1, 0.5f * interactProgress), default, 0);
				spriteBatch.Draw(beam, new Vector2(i, j).ToWorldCoordinates(32, -entity.InteractTime) - Main.screenPosition, null, Color.White.Additive() * interactProgress * opacity * 0.5f, 0, beamOrigin, new Vector2(0.8f, 0.5f * interactProgress), default, 0);
			}
			#endregion

			spriteBatch.Draw(texture, topPosition, topSource, Lighting.GetColor(i, j), rotation, topSource.Size() / 2, 1, default, 0);

			if (highlighted)
				spriteBatch.Draw(highlight, topPosition, topSource, highlightColor, rotation, topSource.Size() / 2, 1, default, 0);
		}

		Rectangle source = new(tile.TileFrameX, tile.TileFrameY, 16, 16);
		Vector2 position = new Vector2(i, j) * 16 - Main.screenPosition;

		spriteBatch.Draw(texture, position, source, Lighting.GetColor(i, j), 0, Vector2.Zero, 1, default, 0);

		if (highlighted)
			spriteBatch.Draw(highlight, position, source, highlightColor, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
	}

	public override void EmitParticles(int i, int j, Tile tile, short tileFrameX, short tileFrameY, Color tileLight, bool visible)
	{
		if (TileObjectData.IsTopLeft(i, j) && Main.rand.NextBool(10) && Lighting.Brightness(i + 1, j) > 0.4f)
		{
			var dust = Dust.NewDustDirect(new Vector2(i, j) * 16, 64, 16, DustID.GoldCoin);
			dust.noGravity = true;
			dust.velocity = Vector2.Zero;
		}
	}
}

public class ScarabAltarEntity : ModTileEntity, IEntityUpdate
{
	/// <summary>
	/// Minimum time between two interactions.
	/// </summary>
	public const int InteractTimeMax = 10;

	/// <summary>
	/// Amount of gems needed to summon Scarabeus.
	/// </summary>
	public const int ConsumableCountMax = 5;

	public int consumableCount;

	public int InteractTime { get; private set; }

	public override bool IsTileValidForEntity(int x, int y)
	{
		Tile tile = Main.tile[x, y];
		return tile.HasTile && tile.TileType == ModContent.TileType<ScarabAltar>() && TileObjectData.IsTopLeft(x, y);
	}

	public override int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternate)
	{
		TileExtensions.GetTopLeft(ref i, ref j);
		var d = TileObjectData.GetTileData(Main.tile[i, j]);
		var size = (d is null) ? new Point(1, 1) : new Point(d.Width, d.Height);

		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			NetMessage.SendTileSquare(Main.myPlayer, i, j, size.X, size.Y);
			NetMessage.SendData(MessageID.TileEntityPlacement, number: i, number2: j, number3: Type);

			return -1;
		}

		return Place(i, j);
	}

	public void Interact()
	{
		SetInteractTime();

		Rectangle area = new(Position.X * 16 + 12, Position.Y * 16, 40, 2);
		for (int i = 0; i < 8; i++)
		{
			var dust = Dust.NewDustPerfect(Main.rand.NextVector2FromRectangle(area), DustID.GoldCoin);
			dust.noGravity = true;
			dust.velocity = Vector2.UnitY * -Main.rand.NextFloat(1, 2);
		}

		SoundEngine.PlaySound(SoundID.Coins with { Pitch = (float)consumableCount / ConsumableCountMax }, area.Center());

		float subVolume = (float)(consumableCount - ConsumableCountMax / 2) / (ConsumableCountMax / 2);
		if (subVolume > 0)
			SoundEngine.PlaySound(SoundID.CoinPickup with { Volume = subVolume }, area.Center());

		if (++consumableCount >= ConsumableCountMax)
		{
			consumableCount = 0;

			if (Main.netMode != NetmodeID.MultiplayerClient)
				Projectile.NewProjectile(new EntitySource_TileEntity(this), area.Center() - new Vector2(0, 1), Vector2.Zero, ModContent.ProjectileType<ScarabAltar.BeamOLight>(), 0, 0, -1, 300);
		}
	}

	public void SetInteractTime() => InteractTime = InteractTimeMax;

	public void GlobalUpdate()
	{
		if (InteractTime > 0)
			InteractTime--;
	}

	public override void OnNetPlace() => NetMessage.SendData(MessageID.TileEntitySharing, number: ID, number2: Position.X, number3: Position.Y);
	public override void NetSend(BinaryWriter writer) => writer.Write((byte)consumableCount);
	public override void NetReceive(BinaryReader reader) => consumableCount = reader.ReadByte();
}