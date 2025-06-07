namespace SpiritReforged.Content.Underground.Pottery;

/// <summary> Records details for tile bestiary purposes. </summary>
public struct TileRecord
{
	/// <summary> The default path localization key for <see cref="description"/>. </summary>
	public const string DescKey = "Mods.SpiritReforged.Tiles.Records";

	/// <summary> The value used for internal reference. For the front-facing name, see <see cref="name"/>. </summary>
	public readonly string key;
	public readonly int type;
	public readonly int[] styles;

	public string name;
	public string description = Language.GetTextValue(DescKey + ".Common");

	public Func<bool> hidden;
	public byte rating = 1;

	public TileRecord(string nameKey, int tileType, params int[] tileStyles)
	{
		key = nameKey;
		type = tileType;
		styles = tileStyles;

		string modName = TileLoader.GetTile(tileType).Mod.Name;
		name = Language.GetTextValue($"Mods.{modName}.Items.{key}Item.DisplayName");
		description = Language.GetTextValue(DescKey + ".Common");
	}

	/// <summary> Hides this record until discovered. </summary>
	public TileRecord Hide()
	{
		hidden = () => true;
		return this;
	}

	/// <inheritdoc cref="Hide()"/>
	public TileRecord Hide(Func<bool> condition)
	{
		hidden = condition;
		return this;
	}

	public TileRecord AddRating(byte value)
	{
		rating = value;
		return this;
	}

	public TileRecord AddDisplayName(LocalizedText text)
	{
		name = text.Value;
		return this;
	}

	public TileRecord AddDescription(LocalizedText text)
	{
		description = text.Value;
		return this;
	}

	public readonly void DrawIcon(SpriteBatch spriteBatch, Vector2 position, Color color)
	{
		const int maxSize = 5;

		int tileStyle = styles[0];
		var data = TileObjectData.GetTileData(type, 0);

		if (data is null)
			return;

		var texture = TextureAssets.Tile[type].Value;

		int wrapLimit = data.StyleWrapLimit;
		int width = data.Width;
		int height = data.Height;

		if (wrapLimit == 0)
			wrapLimit = (data.RandomStyleRange == 0) ? 2000 : data.RandomStyleRange;

		for (int x = 0; x < Math.Min(width, maxSize); x++)
		{
			for (int y = 0; y < Math.Min(height, maxSize); y++)
			{
				var start = FrameStart(x, y);
				var source = new Rectangle(start.X, start.Y, data.CoordinateWidth, data.CoordinateHeights[y]);
				source.Y += Main.tileFrame[type] * data.CoordinateFullHeight; //Animation

				if (x == maxSize - 1)
					source.Width = 8; //Avoid clipping
				if (y == maxSize - 1)
					source.Height = 8;

				var center = new Vector2(width * 16 / 2, height * 16 / 2);
				var drawPos = position + new Vector2(x * 16, y * 16) - center;

				spriteBatch.Draw(texture, drawPos, source, color, 0, Vector2.Zero, 1, default, 0);
			}
		}

		Point FrameStart(int x, int y)
		{
			const int tileFrame = 18;

			if (data.StyleHorizontal)
			{
				int startX = (tileStyle % wrapLimit * width + x) * tileFrame;
				int startY = (tileStyle / wrapLimit * height + y) * tileFrame;

				return new Point(startX, startY);
			}
			else
			{
				int startX = (tileStyle / wrapLimit * width + x) * tileFrame;
				int startY = (tileStyle % wrapLimit * height + y) * tileFrame;

				return new Point(startX, startY);
			}
		}
	}
}