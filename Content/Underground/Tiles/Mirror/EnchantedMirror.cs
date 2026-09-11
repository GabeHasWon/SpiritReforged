using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.RenderTargets;
using SpiritReforged.Common.WorldGeneration;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.SaltFlats.NPCs;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.ObjectInteractions;
using Terraria.GameInput;
using TileHelper.Common;

namespace SpiritReforged.Content.Underground.Tiles.Mirror;

public sealed class EnchantedMirror : ModTile, ILoadItem
{
	public sealed class MirrorReturnPortal : ModProjectile, IInteractable
	{
		public static readonly Asset<Texture2D> Highlight = DrawHelpers.RequestLocal<EnchantedMirror>("EnchantedMirrorItem_Highlight", false);

		public override string Texture => ModContent.GetInstance<EnchantedMirror>().AutoModItem().Texture;

		public Vector2 Origin
		{
			get => new(Projectile.ai[0], Projectile.ai[1]);
			set
			{
				Projectile.ai[0] = value.X;
				Projectile.ai[1] = value.Y;
			}
		}

		private bool _inInteractionRange;

		public override void SetDefaults()
		{
			Projectile.tileCollide = false;
			Projectile.Size = new(38);
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			Vector2 compareSpot = owner.Center;

			if (owner.whoAmI == Main.myPlayer && owner.IsProjectileInteractibleAndInInteractionRange(Projectile, ref compareSpot))
			{
				bool usingSmartCursor = Main.SmartCursorIsUsed || PlayerInput.UsingGamepad;
				bool mouseOver = Projectile.Hitbox.Contains(Main.MouseWorld.ToPoint());

				_inInteractionRange = usingSmartCursor;

				if (mouseOver)
				{
					owner.noThrow = 2;
					owner.cursorItemIconEnabled = true;
					owner.cursorItemIconID = ModContent.GetInstance<EnchantedMirror>().AutoItemType();
				}

				if (mouseOver || usingSmartCursor)
				{
					Main.HasInteractibleObjectThatIsNotATile = true;

					if (Main.mouseRight && Main.mouseRightRelease)
					{
						Main.mouseRightRelease = false;

						owner.tileInteractAttempted = true;
						owner.tileInteractionHappened = true;
						owner.releaseUseTile = false;

						owner.Teleport(Origin, TeleportationStyleID.RecallPotion); //Return
						Projectile.Kill();
					}
				}
			}
			else
				_inInteractionRange = false;

			if (!Main.dedServ)
			{
				Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.BlueFlare, 0, -5).noGravity = true;
				
				if (Main.rand.NextBool())
					ParticleHandler.SpawnParticle(new CompositeSmoke(Main.rand.NextVector2FromRectangle(Projectile.Hitbox), Vector2.UnitY * -Main.rand.NextFloat(2), new Color(12, 25, 50), 80, true, false));
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float sine = EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 60f);
			float rotation = Projectile.rotation + (sine - 0.25f) * 0.05f;
			float mult = Projectile.owner == Main.myPlayer ? 1 : 0.3f;

			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Texture2D outline = TextureColorCache.ColorSolid(texture, Color.White);
			Vector2 position = Projectile.Center - Main.screenPosition + new Vector2(0, sine * 5 + Projectile.gfxOffY);

			DrawHelpers.DrawOutline(Main.spriteBatch, outline, position, Color.White, (offset) =>
			{
				Main.EntitySpriteDraw(texture, position + offset * 2, null, Projectile.GetAlpha(Color.Cyan.Additive()) * 0.5f * mult, rotation, texture.Size() / 2, Projectile.scale, 0);
				Main.EntitySpriteDraw(texture, position + offset, null, Projectile.GetAlpha(Color.Cyan.Additive()) * mult, rotation, texture.Size() / 2, Projectile.scale, 0);
			});

			Main.EntitySpriteDraw(texture, position, null, Projectile.GetAlpha(Color.LightCyan.Additive(130)) * mult, rotation, texture.Size() / 2, Projectile.scale, 0);

			if (_inInteractionRange)
				Main.EntitySpriteDraw(Highlight.Value, position, null, Projectile.GetAlpha(Color.Yellow) * mult, rotation, texture.Size() / 2, Projectile.scale, 0);

			return false;
		}

		public override bool? CanDamage() => false;
	}

	#region special drawing
	private static readonly EasyTarget MirrorTarget = new(), FilterTarget = new();
	private static readonly HashSet<Point16> SpecialPositions = [];

	public override void Load()
	{
		TargetSetup.DrawIntoRendertargets += DrawContent;
		On_TileDrawing.PostDrawTiles += DrawReflection;
		On_TileDrawing.ClearCachedTileDraws += ClearSpecialDrawPoints;
	}

	private static void DrawContent()
	{
		GraphicsDevice graphics = Main.graphics.GraphicsDevice;
		SpriteBatch spriteBatch = Main.spriteBatch;

		#region mirror drawing
		Vector2 storedZoom = Main.GameViewMatrix.Zoom;
		Main.GameViewMatrix.Zoom = Vector2.One;

		graphics.SetRenderTarget(MirrorTarget.Value);
		graphics.Clear(Color.Transparent);

		Reflections.DrawPlayers_BehindNPCs(Main.instance);
		Reflections.DrawPlayers_AfterProjectiles(Main.instance);
		Main.GameViewMatrix.Zoom = storedZoom;

		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, null);
		DrawNoise(spriteBatch);
		spriteBatch.End();
		graphics.SetRenderTarget(null);
		#endregion

		#region filter drawing
		graphics.SetRenderTarget(FilterTarget.Value);
		graphics.Clear(Color.Transparent);

		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, null);
		Texture2D tileTexture = TextureAssets.Tile[ModContent.TileType<EnchantedMirror>()].Value;

		foreach (Point16 position in SpecialPositions)
		{
			Rectangle tileSource = tileTexture.Frame(2, 3, 1, Framing.GetTileSafely(position).TileFrameY / 74);  //74 is the tile frame height
			spriteBatch.Draw(tileTexture, position.ToVector2() * 16 - Main.screenPosition + new Vector2(10, 5), tileSource, Color.White * 0.7f, 0, Vector2.Zero, 1, 0, 0);
		}

		spriteBatch.End();
		graphics.SetRenderTarget(null);
		#endregion

		static void DrawNoise(SpriteBatch spriteBatch)
		{
			const float scale = 1;

			Texture2D noise = AssetLoader.LoadedTextures["MirrorShine"].Value;
			float scroll = EaseFunction.EaseCubicInOut.Ease((float)Main.timeForVisualEffects / 100f % 1);

			for (int x = 0; x < Main.screenWidth / (noise.Width * scale) + 1; x++)
				for (int y = 0; y < Main.screenHeight / (noise.Height * scale) + 1; y++)
				{
					Vector2 position = new(noise.Width * scale * x, noise.Height * scale * (y - scroll));

					spriteBatch.Draw(noise, position, null, Color.DarkCyan * 0.4f, 0, Vector2.Zero, scale, default, 0);
					spriteBatch.Draw(noise, position + new Vector2(0, 10), null, Color.DarkCyan * 0.2f, 0, Vector2.Zero, scale, default, 0);
					spriteBatch.Draw(noise, position - new Vector2(0, 8), null, Color.LightCyan * 0.1f, 0, Vector2.Zero, scale, default, 0);
				}

			noise = AssetLoader.LoadedTextures["particlenoise"].Value;
			scroll = (float)Main.timeForVisualEffects / 2000f % 1;

			for (int x = 0; x < Main.screenWidth / (noise.Width * scale) + 1; x++)
				for (int y = 0; y < Main.screenHeight / (noise.Height * scale) + 1; y++)
				{
					Vector2 position = new(noise.Width * scale * (x - scroll), noise.Height * scale * (y - scroll));
					spriteBatch.Draw(noise, position, null, Color.DarkCyan * 0.4f, 0, Vector2.Zero, scale, default, 0);
				}
		}
	}

	private static void DrawReflection(On_TileDrawing.orig_PostDrawTiles orig, TileDrawing self, bool solidLayer, bool forRenderTargets, bool intoRenderTargets)
	{
		orig(self, solidLayer, forRenderTargets, intoRenderTargets);

		if (!solidLayer && !intoRenderTargets && MirrorTarget?.Value != null)
		{
			SpriteBatch spriteBatch = Main.spriteBatch;
			Effect effect = AssetLoader.LoadedShaders["SimpleMultiply"].Value;

			effect.Parameters["tileTexture"].SetValue(FilterTarget.Value);
			effect.Parameters["lightness"].SetValue(10);

			spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.Transform);
			spriteBatch.Draw(MirrorTarget.Value, -new Vector2(10, 5), null, Color.White, 0, Vector2.Zero, 1, 0, 0);
			spriteBatch.End();
		}
	}

	private static void ClearSpecialDrawPoints(On_TileDrawing.orig_ClearCachedTileDraws orig, TileDrawing self, bool solidLayer)
	{
		orig(self, solidLayer);

		if (!solidLayer)
			SpecialPositions.Clear();
	}
	#endregion

	public const int Frame_Height = 74;

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLighted[Type] = true;

		TileID.Sets.HasOutlines[Type] = true;
		TileID.Sets.DisableSmartCursor[Type] = true;
		TileHelperSets.TileGlowmask[Type] = Helpers.RequestGlowmask(this);

		TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
		TileObjectData.newTile.Height = 4;
		TileObjectData.newTile.CoordinateHeights = [16, 16, 16, 18];
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
		TileObjectData.newTile.Origin = new(0, 3);
		TileObjectData.addTile(Type);

		AddMapEntry(FurnitureTile.MapColor, this.AutoModItem().DisplayName);
		DustType = -1;
	}

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

	public override void MouseOver(int i, int j)
	{
		if (!Active(i, j))
			return;

		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = this.AutoItemType();
	}

	public override bool RightClick(int i, int j)
	{
		if (!Active(i, j))
			return false;

		Player player = Main.LocalPlayer;

		(int left, int top) = Helpers.GetTopLeft(i, j);
		Vector2 startPosition = new Vector2(left, top + 1).ToWorldCoordinates();

		player.RemoveAllGrapplingHooks();
		player.Spawn(PlayerSpawnContext.RecallFromItem);
		Main.mouseRightRelease = false;

		for (int x = 0; x < 10; x++)
			ParticleHandler.SpawnParticle(new CompositeSmoke(Main.rand.NextVector2FromRectangle(player.Hitbox), Vector2.UnitY * -Main.rand.NextFloat(2), new Color(12, 25, 50), 80, true, false));

		ParticleHandler.SpawnParticle(new SharpStarParticle(player.Center, Vector2.Zero, Color.Cyan.Additive(), 1, 20)
		{ Layer = ParticleLayer.AbovePlayer, Rotation = 0 });

		ParticleHandler.SpawnParticle(new SharpStarParticle(player.Center, Vector2.Zero, Color.White.Additive(), 0.7f, 20)
		{ Layer = ParticleLayer.AbovePlayer, Rotation = 0 });

		KillOldPortals(player, out int type);

		SoundEngine.PlaySound(Wisp.Death);
		Projectile.NewProjectile(null, player.Center - new Vector2(0, 16), Vector2.Zero, type, 0, 0, Main.myPlayer, startPosition.X, startPosition.Y);

		return true;
	}

	public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (effectOnly || !Active(i, j))
		{
			if (!fail)
				for (int d = 0; d < 3; d++)
					Dust.NewDust(new Vector2(i, j) * 16, 16, 16, DustID.Silver);

			return;
		}

		(i, j) = Helpers.GetTopLeft(i, j);
		fail = noItem = true;
		bool firstHit = false;

		if (!WorldMethods.Generating && Main.tile[i, j].TileFrameY == 0)
		{
			SoundEngine.PlaySound(SoundID.DD2_GhastlyGlaiveImpactGhost with { Pitch = 0.5f }, new Vector2(i, j).ToWorldCoordinates());
			SoundEngine.PlaySound(SoundID.NPCDeath27 with { Pitch = -1 }, new Vector2(i, j).ToWorldCoordinates());

			if (Main.LocalPlayer.TryGetModPlayer(out LuckPlayer luckPlayer))
			{
				luckPlayer.luckModifier -= 0.5f;
				luckPlayer.luckResetTime = 60 * 60 * 10;

				Main.LocalPlayer.luckNeedsSync = true;
			}

			firstHit = true;
		}

		for (int x = i; x < i + 2; x++)
		{
			for (int y = j; y < j + 4; y++)
			{
				Tile tile = Main.tile[x, y];
				tile.TileFrameY += Frame_Height;
			}
		}

		NetMessage.SendTileSquare(-1, i, j, 2, 4, TileChangeType.None);
		if (!Main.dedServ)
		{
			for (int p = 0; p < (firstHit ? 4 : 10); p++)
			{
				Vector2 position = Main.rand.NextVector2FromRectangle(new(i * 16 + 8, j * 16 + 8, 16, Frame_Height - 32));
				ParticleHandler.SpawnParticle(new EnchantedMirrorShard(position, (Vector2.UnitY * -Main.rand.NextFloat(1, 5)).RotatedByRandom(1), Main.rand.NextFloat() - 0.5f, Main.rand.NextFloat(0.5f, 1), 60));
			}
		}
	}

	public override bool KillSound(int i, int j, bool fail)
	{
		Vector2 worldPosition = new Vector2(i, j).ToWorldCoordinates();
		if (fail)
		{
			SoundEngine.PlaySound(SoundID.Shatter with { Pitch = 1, PitchVariance = 0.2f }, worldPosition);
			SoundEngine.PlaySound(SoundID.DeerclopsIceAttack with { Pitch = 1, PitchVariance = 0.2f }, worldPosition);
		}

		return true;
	}

	private static bool Active(int i, int j) => Main.tile[i, j].TileFrameY < Frame_Height * 2;

	private static void KillOldPortals(Player owner, out int type)
	{
		type = ModContent.ProjectileType<MirrorReturnPortal>();

		if (owner.ownedProjectileCounts[type] < 1)
			return;

		foreach (Projectile projectile in Main.ActiveProjectiles)
			if (projectile.owner == owner.whoAmI && projectile.type == type)
				projectile.Kill();
	}

	public override bool CanDrop(int i, int j) => false; //Don't drop the dedicated item

	public override void EmitParticles(int i, int j, Tile tile, short tileFrameX, short tileFrameY, Color tileLight, bool visible)
	{
		if (TileObjectData.IsTopLeft(i, j) && Active(i, j) && Main.LocalPlayer.DistanceSQ(new Vector2(i, j).ToWorldCoordinates(16, 8)) < 100 * 100)
		{
			const float backtile_spread = 44;
			const float tile_spread = 32;

			float range = Main.rand.NextFloat();
			Vector2 backPosition = new(i * 16 + backtile_spread * range - (backtile_spread - tile_spread) / 2, (j + 4) * 16);
			Vector2 position = new(i * 16 + tile_spread * range, (j + 4) * 16);

			ParticleHandler.SpawnParticle(new CompositeSmoke(backPosition, Vector2.UnitY * -Main.rand.NextFloat(2), new Color(12, 25, 50), 160, false, false)
			{ Layer = ParticleLayer.BelowWall });

			float taperOff = 1f - EaseFunction.EaseSine.Ease(range);
			ParticleHandler.SpawnParticle(new SmallCompositeSmoke(position, Vector2.UnitY * -Main.rand.NextFloat(taperOff), new Color(44, 90, 120) * taperOff, 120, false, false)
			{ Layer = ParticleLayer.BelowNPC });

			if (Main.rand.NextBool())
				ParticleHandler.SpawnParticle(new SmallCompositeSmoke(position, Vector2.UnitY * (taperOff * -0.5f), Color.DarkCyan, 80, false, true)
				{ Layer = ParticleLayer.AbovePlayer });
		}
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		if (!Active(i, j))
			return;

		float distance = Math.Clamp(1f - Main.LocalPlayer.DistanceSQ(new Vector2(i, j).ToWorldCoordinates()) / (150f * 150f), 0.1f, 1);
		(r, g, b) = (0.3f * distance, 0.6f * distance, 0.9f * distance);
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (TileObjectData.IsTopLeft(i, j) && Active(i, j))
			SpecialPositions.Add(new(i, j));

		return true;
	}
}