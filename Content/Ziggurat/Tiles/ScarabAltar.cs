using SpiritReforged.Common;
using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.ModCompat;
using SpiritReforged.Common.Multiplayer;
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
using SpiritReforged.Content.Desert.Tiles;
using SpiritReforged.Content.Particles;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;
using Terraria.Graphics.CameraModifiers;
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
			}
		}

		public override void OnKill(int timeLeft)
		{
			if (TileEntity.ByID[TileEntityID] is ScarabAltarEntity entity)
			{
				bool isFablesItem = CrossMod.Fables.Enabled && _fablesStormlionItems[ItemType];
				entity.Interact(isFablesItem);
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

		public static readonly SoundStyle AnticipationWeird = new("SpiritReforged/Assets/SFX/Tile/WackyDissonantChime")
		{
			Volume = 0.65f
		};

		public override string Texture => AssetLoader.EmptyTexture;

		public int MaxTime
		{
			get => (int)Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}
		public ref float Time => ref Projectile.ai[1];

		public ref float GemCount => ref Projectile.ai[2];

		public bool StartDuoFight => CrossMod.Fables.Enabled && Projectile.ai[2] == 666;

		public float FlashTime;
		private bool _justSpawned = true;

		public override void SetDefaults()
		{
			Projectile.Size = new(16);
			Projectile.tileCollide = false;
		}

		public override void AI()
		{
			if (FlashTime > 0)
				FlashTime--;

			if (GemCount >= ScarabAltarEntity.ConsumableCountMax)
			{
				Enabled = true;

				if (_justSpawned)
				{
					if (!Main.dedServ)
						Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center, Vector2.UnitX, 5, 5, 30));

					if (Main.netMode != NetmodeID.MultiplayerClient) //Summon Scarabeus
					{
						if (StartDuoFight && CrossMod.Fables.TryFind("ScourgeVsScarab", out ModNPC duoFightManager))
							NPC.NewNPCDirect(Projectile.GetSource_Death(), Projectile.Center, duoFightManager.Type);
						else
						{
							NPC.NewNPCDirect(Projectile.GetSource_Death(), Projectile.Center, ModContent.NPCType<Scarabeus>());
							if (Main.getGoodWorld)
								NPC.NewNPCDirect(Projectile.GetSource_Death(), Projectile.Center, ModContent.NPCType<Scarabeus>(), ai2 : 1);
						}
					}

					Projectile.timeLeft = MaxTime / 2;
					_justSpawned = false;

					SoundEngine.PlaySound(StartDuoFight ? AnticipationWeird : Anticipation, Projectile.Center);

					if (!Main.dedServ)
						Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center, Vector2.UnitX, 10, 10, 30));

					FlashTime = 60;

					static void DecelerateAction(Particle p) => p.Velocity *= 0.94f;

					for (int i = 0; i < 30; i++)
					{
						Vector2 velocity = -Vector2.UnitY.RotatedByRandom(0.75f) * Main.rand.NextFloat(9f);

						ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center, velocity, Color.White, Color.Orange, 1f, 30, 3, DecelerateAction));

						ParticleHandler.SpawnParticle(new GlowParticle(Projectile.Center, velocity, Color.White, 0.8f, 20, 3, DecelerateAction));
					}
				}
			}
			else
			{
				if (Time < GemCount * (MaxTime / (ScarabAltarEntity.ConsumableCountMax * 2)))
					Time++;

				Projectile.timeLeft = (int)(MaxTime - Time);
			}
		}

		public override void OnKill(int timeLeft)
		{
			Enabled = false;
		}

		public override bool? CanDamage() => false;

		public override bool PreDraw(ref Color lightColor)
		{
			var godray = AssetLoader.LoadedTextures["GodrayCircle"].Value;

			float progress = 1f - Projectile.timeLeft / (float)MaxTime;

			float scale;
			float opacity;

			if (progress < 0.5f)
			{
				float lerp = progress / 0.5f;

				scale = MathHelper.Lerp(0.5f, 1f, lerp);

				opacity = lerp;
			}
			else
			{
				float lerp = (progress - 0.5f) / 0.5f;

				scale = 1f;

				opacity = 1f - lerp;
			}

			float glowSin = 0;

			if (GemCount < 5)
				glowSin = 0.1f * (float)Math.Sin(Main.timeForVisualEffects * 0.04f);

			Main.spriteBatch.Draw(godray, Projectile.Center - Main.screenPosition, null, Color.Lerp(Color.LightGoldenrodYellow, Color.Orange, opacity).Additive() * (opacity + glowSin), MathHelper.Pi, godray.Size() / 2f, (scale + glowSin) * 0.2f , 0f, 0f);

			Effect effect = AssetLoader.LoadedShaders["LightRay"].Value;

			effect.Parameters["uTexture"].SetValue(AssetLoader.LoadedTextures["FlameTrail"].Value);
			float scrollAmount = EaseBuilder.EaseCircularIn.Ease(opacity) * 0.6f;

			if (GemCount < 5)
				scrollAmount = (float)Math.Sin(Main.timeForVisualEffects * 0.01f) * 0.5f;

			effect.Parameters["scroll"].SetValue(new Vector2(0, scrollAmount));
			effect.Parameters["textureStretch"].SetValue(new Vector2(4, 1) * 0.2f);
			effect.Parameters["texExponentRange"].SetValue(new Vector2(1, 0.25f));
			effect.Parameters["flipCoords"].SetValue(true);

			float easedScale = EaseBuilder.EaseCircularIn.Ease(scale);
			float easedFlashProgress = EaseBuilder.EaseCubicOut.Ease(opacity * 1.5f);
			effect.Parameters["finalIntensityMod"].SetValue(1 * (1 + easedFlashProgress / 2) * easedScale * Projectile.scale);
			effect.Parameters["textureStrength"].SetValue(easedFlashProgress); //Don't display texture while not flashing
			effect.Parameters["finalExponent"].SetValue(2f);

			Color colorOne = Color.LightGoldenrodYellow;
			Color colorTwo = Color.Orange;

			if (FlashTime > 0)
			{
				float lerp = EaseBuilder.EaseCircularOut.Ease((int)FlashTime / 60f);

				scale += 0.3f * lerp;
			}

			effect.Parameters["uColor"].SetValue(colorOne.ToVector4() * opacity);

			effect.Parameters["uColor2"].SetValue(colorTwo.ToVector4() * opacity);

			var rayFinalDimensions = new Vector3(350f * scale, 100f * scale, 1f);

			float sunWidth = 70;
			effect.Parameters["taperRatio"].SetValue(sunWidth / rayFinalDimensions.X);

			var square = new SquarePrimitive
			{
				Color = Color.White,
				Height = rayFinalDimensions.Y,
				Length = rayFinalDimensions.X,
			};

			square.SetTopPosition(Projectile.Center - Main.screenPosition + new Vector2(0f, -rayFinalDimensions.Y));

			PrimitiveRenderer.DrawPrimitiveShape(square, effect);

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
	private static bool[] _fablesStormlionItems;
	private static bool[] _fablesSpawnBlockingNPCs;
	private static int _fablesDeadStormlionLarvaType = -1;
	private static int _fablesStormlionBucketType = -1;

	void IAutoloadTileItem.AddItemRecipes(ModItem item)
	{
		item.CreateRecipe().AddIngredient(ModContent.GetInstance<RedSandstoneBrick>().AutoItemType(), 15).AddIngredient(ModContent.GetInstance<CarvedLapis>().AutoItemType(), 5)
			.AddRecipeGroup("GoldBars", 4).AddTile(TileID.Anvils).Register();

		item.CreateRecipe().AddIngredient(ModContent.GetInstance<RedSandstoneBrick>().AutoItemType(), 15).AddIngredient(ItemID.Sapphire, 5)
			.AddRecipeGroup("GoldBars", 4).AddTile(TileID.Anvils).Register();
	}

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;

		TileID.Sets.PreventsTileRemovalIfOnTopOfIt[Type] = true;
		TileID.Sets.PreventsTileHammeringIfOnTopOfIt[Type] = true;
		TileID.Sets.PreventsTileReplaceIfOnTopOfIt[Type] = true;
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

		AddMapEntry(new Color(124, 24, 28), Language.GetText("Mods.SpiritReforged.Items.ScarabAltarItem.DisplayName"));
		DustType = -1;
		MinPick = 55;

		//Load the stormlion items from fables
		if (CrossMod.Fables.Enabled &&
			CrossMod.Fables.TryFind("DeadStormlionLarvaItem", out ModItem deadLarva) &&
			CrossMod.Fables.TryFind("StormlionLarvaItem", out ModItem aliveLarva) &&
			CrossMod.Fables.TryFind("BucketOfLarvae", out ModItem larvaBucket))
		{
			_fablesStormlionItems = ItemID.Sets.Factory.CreateBoolSet(deadLarva.Type, aliveLarva.Type, larvaBucket.Type);
			_fablesStormlionBucketType = larvaBucket.Type;
			_fablesDeadStormlionLarvaType = deadLarva.Type;
		}
		else
			_fablesStormlionItems = ItemID.Sets.Factory.CreateBoolSet(false);

		if (CrossMod.Fables.Enabled && CrossMod.Fables.TryFind("DesertScourge", out ModNPC dscourge) &&
			CrossMod.Fables.TryFind("ScourgeVsScarab", out ModNPC duoFight))
		{
			_fablesSpawnBlockingNPCs = NPCID.Sets.Factory.CreateBoolSet(dscourge.Type, duoFight.Type);
		}
		else
			_fablesSpawnBlockingNPCs = NPCID.Sets.Factory.CreateBoolSet(false);
	}

	public override bool CanExplode(int i, int j) => false;
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

	public bool CanRecieveOfferings
	{
		get
		{
			if (!Main.dayTime || BeamOLight.Enabled)
				return false;
			if (!Main.LocalPlayer.ZoneDesert)
				return false;

			//Can't use the altar while scourge is already spawned. To summon Scarab & Scourge at once, you must use the stormlion offering feature
			if (CrossMod.Fables.Enabled)
			{
				for (int i = 0; i < Main.maxNPCs; i++)
				{
					if (Main.npc[i].active && _fablesSpawnBlockingNPCs[Main.npc[i].type])
						return false;
				}
			}

			return true;
		}
	}

	public override bool RightClick(int i, int j)
	{
		int projectileType = ModContent.ProjectileType<FloatingGem>();

		if (CanRecieveOfferings && FindSacrifice(Main.LocalPlayer, out Item result) && Entity(i, j) is ScarabAltarEntity entity)
		{
			bool bowlFull = entity.consumableCount + Main.LocalPlayer.ownedProjectileCounts[projectileType] >= ScarabAltarEntity.ConsumableCountMax;
			//Since stormlion items from fables instantly set the bowl's fill level to 666, prevent more than 1 from being dispensed at once
			bowlFull |= Main.LocalPlayer.ownedProjectileCounts[projectileType] > 0 && _fablesStormlionItems[result.type];

			if (bowlFull)
				return false;

			Vector2 origin = TileObjectData.TopLeft(i, j).ToWorldCoordinates(32, 8);

			int itemType = result.type;
			//When sacrificing the stormlion bucket, it should spawn a dead stormlion instead of consuming the bucket
			if (itemType == _fablesStormlionBucketType)
				itemType = _fablesDeadStormlionLarvaType;

			Projectile.NewProjectile(new EntitySource_TileInteraction(Main.LocalPlayer, i, j), origin, (Vector2.UnitY * -Main.rand.NextFloat(9, 13)).RotateRandom(0.5), 
				projectileType, 0, 0, Main.myPlayer, itemType, entity.ID);

			if (result.type != _fablesStormlionBucketType && --result.stack <= 0)
				result.TurnToAir(); //Consume an item
			return true;
		}

		return false;
	}

	private static bool FindSacrifice(Player player, out Item result)
	{
		if (!player.HeldItem.IsAir && ValidItem(player.HeldItem))
		{
			result = player.HeldItem;
			return true;
		}

		//Holding a fables item, check for stormlion grub items
		//Importantly, we DO NOT! Check for the grubs in the player's inventory, only their held item (To make it extra secret)
		if (CrossMod.Fables.Enabled && !player.HeldItem.IsAir)
		{
			//Accepts stormlion larvae as offerings
			if (_fablesStormlionItems[player.HeldItem.type])
			{
				result = player.HeldItem;
				return true;
			}
		}

		foreach (Item item in player.inventory)
		{
			if (!item.IsAir && ValidItem(item))
			{
				result = item;
				return true;
			}
		}

		result = new(ItemID.None);
		return false;
	}

	private static bool ValidItem(Item item) => SpiritSets.Gemstone[item.type] || item.type == ModContent.GetInstance<PolishedAmber>().AutoItemType();

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
	public int BeamWhoAmI = -1;

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

	public void Interact(bool fablesGrub = false)
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

		float subVolume = (float)(consumableCount - ConsumableCountMax / 2f) / (ConsumableCountMax / 2f);
		if (subVolume > 0)
			SoundEngine.PlaySound(SoundID.CoinPickup with { Volume = subVolume }, area.Center());

		consumableCount++;
		//When sacrificing a fables stormlion grub for the Scarab VS Scourge duo fight, the altar gets filled instantly with the value 666 so we know its from fables
		if (fablesGrub)
			consumableCount = 666;

		int beamType = ModContent.ProjectileType<ScarabAltar.BeamOLight>();
		bool validBeam = BeamWhoAmI >= 0 && BeamWhoAmI < Main.maxProjectiles && Main.projectile[BeamWhoAmI].active && Main.projectile[BeamWhoAmI].type == beamType;
		if (!validBeam)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				BeamWhoAmI = Projectile.NewProjectile(new EntitySource_TileEntity(this), area.Center() - new Vector2(0, 1), Vector2.Zero, beamType, 0, 0, -1, 240, consumableCount);
				validBeam = true;
			}
		}

		//Update the gem count of the altar
		if (validBeam)
		{
			Main.projectile[BeamWhoAmI].ai[2] = consumableCount;
			Main.projectile[BeamWhoAmI].netUpdate = true;
		}

		if (consumableCount >= ConsumableCountMax)
		{
			consumableCount = 0;
			BeamWhoAmI = -1;
		}

		if (Main.netMode == NetmodeID.Server)
			NetMessage.SendData(MessageID.TileEntitySharing, number: ID);
	}

	public void SetInteractTime() => InteractTime = InteractTimeMax;

	public void GlobalUpdate()
	{
		if (InteractTime > 0)
			InteractTime--;
	}

	public override void OnNetPlace() => NetMessage.SendData(MessageID.TileEntitySharing, number: ID, number2: Position.X, number3: Position.Y);
	public override void NetSend(BinaryWriter writer)
	{
		writer.Write((byte)consumableCount);
		writer.Write(BeamWhoAmI);
	}

	public override void NetReceive(BinaryReader reader)
	{
		consumableCount = reader.ReadByte();
		BeamWhoAmI = reader.ReadByte();
	}
}