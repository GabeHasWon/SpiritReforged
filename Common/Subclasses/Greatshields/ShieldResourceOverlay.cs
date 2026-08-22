using SpiritReforged.Common.Misc;
using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Common.Subclasses.Greatshields;

public class ShieldResourceOverlay : ModResourceOverlay
{
	public const string FancyFolder = "Images/UI/PlayerResourceSets/FancyClassic/";
	public const string BarsFolder = "Images/UI/PlayerResourceSets/HorizontalBars/";

	private static readonly Asset<Texture2D> _resource = DrawHelpers.RequestLocal<ShieldResourceOverlay>("ShieldResource", false);
	private static readonly Dictionary<string, Asset<Texture2D>> _assetCache = [];

	public override bool PreDrawResource(ResourceOverlayDrawContext context)
	{
		Asset<Texture2D> asset = context.texture;

		if (Main.LocalPlayer.HeldItem.ModItem is GreatshieldItem shieldItem && context.resourceNumber < shieldItem.Info.ShieldHealth / 20f)
		{
			if (CompareAssets(asset, FancyFolder + "Heart_Left") || CompareAssets(asset, FancyFolder + "Heart_Middle") || CompareAssets(asset, FancyFolder + "Heart_Right") || CompareAssets(asset, FancyFolder + "Heart_Right_Fancy") || CompareAssets(asset, FancyFolder + "Heart_Single_Fancy"))
			{
				DrawPanel(context);
				return false;
			}
		}

		return true;
	}

	public override void PostDrawResource(ResourceOverlayDrawContext context)
	{
		Asset<Texture2D> asset = context.texture;
		bool drawingBarsPanels = CompareAssets(asset, BarsFolder + "HP_Panel_Middle");

		if (!Main.LocalPlayer.TryGetModPlayer(out GreatshieldPlayer shieldPlayer) || context.resourceNumber >= shieldPlayer.shieldHealth / 20f)
			return;

		if (asset == TextureAssets.Heart || asset == TextureAssets.Heart2)
		{
			DrawFill(context, shieldPlayer.shieldHealth);
		}
		else if (CompareAssets(asset, FancyFolder + "Heart_Fill") || CompareAssets(asset, FancyFolder + "Heart_Fill_B"))
		{
			DrawFill(context, shieldPlayer.shieldHealth);
		}
		else if (CompareAssets(asset, BarsFolder + "HP_Fill") || CompareAssets(asset, BarsFolder + "HP_Fill_Honey"))
		{
			//DrawBarsOverlay(context);
		}
		else if (CompareAssets(asset, FancyFolder + "Heart_Left") || CompareAssets(asset, FancyFolder + "Heart_Middle") || CompareAssets(asset, FancyFolder + "Heart_Right") || CompareAssets(asset, FancyFolder + "Heart_Right_Fancy") || CompareAssets(asset, FancyFolder + "Heart_Single_Fancy"))
		{
			//DrawPanel(context);
		}
		else if (drawingBarsPanels)
		{
			//DrawBarsPanelOverlay(context);
		}
	}

	private static bool CompareAssets(Asset<Texture2D> existingAsset, string compareAssetPath)
	{
		// This is a helper method for checking if a certain vanilla asset was drawn
		if (!_assetCache.TryGetValue(compareAssetPath, out var asset))
			asset = _assetCache[compareAssetPath] = Main.Assets.Request<Texture2D>(compareAssetPath);

		return existingAsset == asset;
	}

	private static void DrawFill(ResourceOverlayDrawContext context, float shieldHealth)
	{
		context.texture = _resource;
		context.scale = Vector2.One;

		float progress = (int)Math.Round(shieldHealth / 2f) * 2 / 20f - context.resourceNumber;
		if (progress > 0)
		{
			Color defaultColor = context.color;
			Rectangle shieldBar = new(190, 0, 22, (int)(22 * progress));

			context.color = Color.Yellow.Additive() * 0.5f; //Draw additive progress cap
			context.source = shieldBar with { Height = shieldBar.Height + 4 };
			context.Draw();

			context.color = Color.White.Additive();
			context.source = shieldBar with { Height = shieldBar.Height + 2 };
			context.Draw();

			context.color = defaultColor; //Draw the shield overlay icon
			context.source = shieldBar;
			context.Draw();
		}
	}

	private static void DrawPanel(ResourceOverlayDrawContext context)
	{
		const string fancyFolder = "Images/UI/PlayerResourceSets/FancyClassic/";

		Vector2 offset = Vector2.Zero;
		int frame;

		if (context.resourceNumber == context.snapshot.AmountOfLifeHearts - 1)
		{
			if (CompareAssets(context.texture, fancyFolder + "Heart_Single_Fancy")) //Single decorated panel
			{
				frame = 4;
			}
			else //Final decorated panel
			{
				frame = 3;
			}

			offset = Vector2.Zero;
		}
		else if (CompareAssets(context.texture, fancyFolder + "Heart_Left")) //First panel
		{
			frame = 0;
		}
		else if (CompareAssets(context.texture, fancyFolder + "Heart_Middle")) //Middle panel
		{
			frame = 2;
		}
		else //Final undecorated panel
		{
			frame = 2;
		}

		context.texture = _resource;
		context.source = context.texture.Frame(6, 1, frame, 0, -2, 0);
		context.position += offset;

		context.Draw();
	}
}