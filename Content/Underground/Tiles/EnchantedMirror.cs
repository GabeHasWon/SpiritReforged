using SpiritReforged.Common.Easing;
using SpiritReforged.Common.ItemCommon;
using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Particle;
using SpiritReforged.Common.ProjectileCommon;
using SpiritReforged.Common.Visuals;
using SpiritReforged.Common.Visuals.RenderTargets;
using SpiritReforged.Content.Particles;
using SpiritReforged.Content.SaltFlats.NPCs;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using TileHelper.Common;

namespace SpiritReforged.Content.Underground.Tiles;

public sealed class EnchantedMirror : ModTile, ILoadItem
{
	public sealed class MirrorReturnPortal : ModProjectile, IInteractable
	{
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

		public override void SetDefaults()
		{
			Projectile.tileCollide = false;
			Projectile.Size = new(38);
			Projectile.Opacity = 0;
		}

		public override void AI()
		{
			Vector2 compareSpot = Vector2.Zero;
			Player owner = Main.player[Projectile.owner];

			if (owner.IsProjectileInteractibleAndInInteractionRange(Projectile, ref compareSpot) && Projectile.Opacity == 1)
			{
				if (Main.mouseRight && Main.mouseRightRelease)
				{
					owner.Teleport(Origin); //Return
					Projectile.Kill();
				}
			}

			if (!Main.dedServ)
			{
				Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.BlueFlare, 0, -5).noGravity = true;
			}

			Projectile.Opacity = MathHelper.Min(Projectile.Opacity + 0.1f, 1); //Fade in
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float sine = EaseFunction.EaseSine.Ease((float)Main.timeForVisualEffects / 60f);
			float rotation = Projectile.rotation + (sine - 0.25f) * 0.05f;

			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Texture2D outline = TextureColorCache.ColorSolid(texture, Color.White);
			Vector2 position = Projectile.Center - Main.screenPosition + new Vector2(0, sine * 5 + Projectile.gfxOffY);

			DrawHelpers.DrawOutline(Main.spriteBatch, outline, position, Color.White, (offset) =>
			{
				Main.EntitySpriteDraw(texture, position + offset * 2, null, Color.Cyan.Additive() * 0.5f, rotation, texture.Size() / 2, Projectile.scale, 0);
				Main.EntitySpriteDraw(texture, position + offset, null, Color.Cyan.Additive(), rotation, texture.Size() / 2, Projectile.scale, 0);
			});

			Main.EntitySpriteDraw(texture, position, null, Projectile.GetAlpha(Color.LightCyan.Additive(130)), rotation, texture.Size() / 2, Projectile.scale, 0);

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
		Rectangle tileSource = tileTexture.Frame(2, 1, 1, 0);

		foreach (Point16 position in SpecialPositions)
			spriteBatch.Draw(tileTexture, position.ToVector2() * 16 - Main.screenPosition + new Vector2(10, 5), tileSource, Color.White * 0.7f, 0, Vector2.Zero, 1, 0, 0);

		spriteBatch.End();
		graphics.SetRenderTarget(null);
		#endregion

		static void DrawNoise(SpriteBatch spriteBatch)
		{
			const float scale = 1;

			Texture2D noise = AssetLoader.GetTexture("EnchantedMirror_Shine", DrawHelpers.RequestLocal<EnchantedMirror>("EnchantedMirror_Shine")).Value;
			float scroll = EaseFunction.EaseCubicInOut.Ease((float)Main.timeForVisualEffects / 100f % 1);

			for (int x = 0; x < Main.screenWidth / (noise.Width * scale) + 1; x++)
			{
				for (int y = 0; y < Main.screenHeight / (noise.Height * scale) + 1; y++)
				{
					Vector2 position = new(noise.Width * scale * x, noise.Height * scale * (y - scroll));

					spriteBatch.Draw(noise, position, null, Color.DarkCyan * 0.4f, 0, Vector2.Zero, scale, default, 0);
					spriteBatch.Draw(noise, position + new Vector2(0, 10), null, Color.DarkCyan * 0.2f, 0, Vector2.Zero, scale, default, 0);
					spriteBatch.Draw(noise, position - new Vector2(0, 8), null, Color.LightCyan * 0.1f, 0, Vector2.Zero, scale, default, 0);
				}
			}

			noise = AssetLoader.LoadedTextures["particlenoise"].Value;
			scroll = (float)Main.timeForVisualEffects / 2000f % 1;

			for (int x = 0; x < Main.screenWidth / (noise.Width * scale) + 1; x++)
			{
				for (int y = 0; y < Main.screenHeight / (noise.Height * scale) + 1; y++)
				{
					Vector2 position = new(noise.Width * scale * (x - scroll), noise.Height * scale * (y - scroll));
					spriteBatch.Draw(noise, position, null, Color.DarkCyan * 0.4f, 0, Vector2.Zero, scale, default, 0);
				}
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

	public override void SetStaticDefaults()
	{
		Main.tileTable[Type] = true;
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = true;
		Main.tileLighted[Type] = true;

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

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = this.AutoItemType();
	}

	public override bool RightClick(int i, int j)
	{
		Player player = Main.LocalPlayer;

		(int left, int top) = Helpers.GetTopLeft(i, j);
		Vector2 startPosition = new Vector2(left, top + 1).ToWorldCoordinates();

		player.RemoveAllGrapplingHooks();
		player.Spawn(PlayerSpawnContext.RecallFromItem);

		for (int x = 0; x < 10; x++)
			ParticleHandler.SpawnParticle(new CompositeSmoke(Main.rand.NextVector2FromRectangle(player.Hitbox), Vector2.UnitY * -Main.rand.NextFloat(2), new Color(12, 25, 50), 80, true, false));

		ParticleHandler.SpawnParticle(new SharpStarParticle(player.Center, Vector2.Zero, Color.Cyan.Additive(), 1, 20)
		{ Layer = ParticleLayer.AbovePlayer, Rotation = 0 });

		ParticleHandler.SpawnParticle(new SharpStarParticle(player.Center, Vector2.Zero, Color.White.Additive(), 0.7f, 20)
		{ Layer = ParticleLayer.AbovePlayer, Rotation = 0 });

		SoundEngine.PlaySound(Wisp.Death);
		Projectile.NewProjectile(null, player.Center - new Vector2(0, 16), Vector2.Zero, ModContent.ProjectileType<MirrorReturnPortal>(), 0, 0, Main.myPlayer, startPosition.X, startPosition.Y);

		return true;
	}

	public override void EmitParticles(int i, int j, Tile tile, short tileFrameX, short tileFrameY, Color tileLight, bool visible)
	{
		if (TileObjectData.IsTopLeft(i, j) && Main.LocalPlayer.DistanceSQ(new Vector2(i, j).ToWorldCoordinates(16, 8)) < 100 * 100)
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
		float distance = Math.Clamp(1f - Main.LocalPlayer.DistanceSQ(new Vector2(i, j).ToWorldCoordinates()) / (150f * 150f), 0.1f, 1);
		(r, g, b) = (0.3f * distance, 0.6f * distance, 0.9f * distance);
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		if (TileObjectData.IsTopLeft(i, j))
			SpecialPositions.Add(new(i, j));

		return true;
	}
}