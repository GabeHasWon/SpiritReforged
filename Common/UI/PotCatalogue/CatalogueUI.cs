using SpiritReforged.Common.UI.System;
using SpiritReforged.Content.Underground.Pottery;
using Terraria.GameContent.UI.Elements;

namespace SpiritReforged.Common.UI.PotCatalogue;

public partial class CatalogueUI : AutoUIState
{
	public CatalogueEntry Selected { get; private set; }
	public bool selectedNew;

	private CatalogueList _info;
	private CatalogueList _entries;

	public void Select(CatalogueEntry entry)
	{
		selectedNew = false;

		if (Selected == entry)
			return;

		Selected = entry;
		selectedNew = true;
		RecalculateInfo();
	}

	public override void OnInitialize()
	{
		LoadAssets();

		Width.Set(600, 0);
		Height.Set(400, 0);
		Left.Set(-Width.Pixels / 2, .5f);
		Top.Set(-Height.Pixels / 2 - 50, .5f);

		_entries = new();
		_entries.Width.Set(406, 0);
		_entries.Height.Set(Height.Pixels, 0);
		_entries.AddScrollbar(new UIScrollbar());

		_info = new();
		_info.Width.Set(194, 0);
		_info.Height.Set(Height.Pixels, 0);
		_info.Left.Set(_entries.Width.Pixels, 0);
		_info.AddScrollbar(new UIScrollbar());

		Append(_entries);
		Append(_info);
	}

	public override void OnActivate()
	{
		_entries.ClearEntries();

		foreach (var value in RecordHandler.Records)
		{
			bool locked = !Main.LocalPlayer.GetModPlayer<RecordPlayer>().IsValidated(value.key);
			bool newAndShiny = Main.LocalPlayer.GetModPlayer<RecordPlayer>().IsNew(value.key);

			if (value.hidden?.Invoke() != true || !locked)
				_entries.AddEntry(new CatalogueEntry(value, locked, newAndShiny));
		}
	}

	public override void Draw(SpriteBatch spriteBatch)
	{
		const int fluff = 6;

		//Draw the background panel
		var area = GetDimensions().ToRectangle();
		area.Inflate(fluff, fluff);
		DrawPanel(spriteBatch, area, new Color(24, 35, 80) * .8f, Color.Black * .8f);

		//Draw a divider
		var source = new Rectangle(0, 0, (int)Height.Pixels - 8, 4);
		var left = _info.GetDimensions().ToRectangle().Left();

		spriteBatch.Draw(Divider.Value, left, source, Color.White, MathHelper.PiOver2, source.Size() / 2, 1, default, 0);

		base.Draw(spriteBatch);
	}

	/// <summary> Draws a background panel based on vanilla code. </summary>
	internal static void DrawPanel(SpriteBatch spriteBatch, Rectangle area, Color color, Color borderColor = default, int cornerSize = 12)
	{
		const int bar = 4;
		int corner = cornerSize;

		var point = new Point(area.X, area.Y);
		var point2 = new Point(point.X + area.Width - corner, point.Y + area.Height - corner);
		int width = point2.X - point.X - corner;
		int height = point2.Y - point.Y - corner;

		for (int i = 0; i < 2; i++)
		{
			var texture = ((i == 0) ? Background : Border).Value;
			var c = (i == 0) ? color : borderColor;

			spriteBatch.Draw(texture, new Rectangle(point.X, point.Y, corner, corner), new Rectangle(0, 0, corner, corner), c);
			spriteBatch.Draw(texture, new Rectangle(point2.X, point.Y, corner, corner), new Rectangle(corner + bar, 0, corner, corner), c);
			spriteBatch.Draw(texture, new Rectangle(point.X, point2.Y, corner, corner), new Rectangle(0, corner + bar, corner, corner), c);
			spriteBatch.Draw(texture, new Rectangle(point2.X, point2.Y, corner, corner), new Rectangle(corner + bar, corner + bar, corner, corner), c);
			spriteBatch.Draw(texture, new Rectangle(point.X + corner, point.Y, width, corner), new Rectangle(corner, 0, bar, corner), c);
			spriteBatch.Draw(texture, new Rectangle(point.X + corner, point2.Y, width, corner), new Rectangle(corner, corner + bar, bar, corner), c);
			spriteBatch.Draw(texture, new Rectangle(point.X, point.Y + corner, corner, height), new Rectangle(0, corner, corner, bar), c);
			spriteBatch.Draw(texture, new Rectangle(point2.X, point.Y + corner, corner, height), new Rectangle(corner + bar, corner, corner, bar), c);
			spriteBatch.Draw(texture, new Rectangle(point.X + corner, point.Y + corner, width, height), new Rectangle(corner, corner, bar, bar), c);
		}
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		if (ContainsPoint(Main.MouseScreen))
			Main.LocalPlayer.mouseInterface = true;

		base.DrawSelf(spriteBatch);
	}

	public override void Update(GameTime gameTime)
	{
		if (Main.LocalPlayer.controlInv)
			UISystem.SetInactive<CatalogueUI>();

		base.Update(gameTime);
	}
}