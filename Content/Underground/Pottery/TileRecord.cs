namespace SpiritReforged.Content.Underground.Pottery;

/// <summary> Records details for tile bestiary purposes. </summary>
public struct TileRecord(string key, int tileType, params int[] tileStyles)
{
	/// <summary> The default path localization key for <see cref="description"/>. </summary>
	public const string DescKey = "Mods.SpiritReforged.Tiles.Records";

	/// <summary> The value used for internal reference. For the front-facing name, see <see cref="name"/>. </summary>
	public string key = key;

	public int type = tileType;
	public int[] styles = tileStyles;

	public byte rating = 1;
	public string name = Language.GetTextValue($"Mods.SpiritReforged.Items.{key}Item.DisplayName");
	public string description = Language.GetTextValue(DescKey + ".Common");

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
		const int tileFrame = 18;

		int tileStyle = styles[0];
		var data = TileObjectData.GetTileData(type, 0);

		if (data is null)
			return;

		var texture = TextureAssets.Tile[type].Value;

		int wrapLimit = data.StyleWrapLimit;
		int width = data.Width;
		int height = data.Height;

		if (wrapLimit == 0)
			wrapLimit = data.RandomStyleRange;

		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
				//Expects a horizontal style
				int startX = (tileStyle % wrapLimit * width + x) * tileFrame;
				int startY = (tileStyle / wrapLimit * height + y) * tileFrame;

				var source = new Rectangle(startX, startY, 16, 16);

				if (y == 4)
					source.Height = 8; //Avoid clipping

				var center = new Vector2(width * 16 / 2, height * 16 / 2);
				var drawPos = position + new Vector2(x * 16, y * 16) - center;

				spriteBatch.Draw(texture, drawPos, source, color, 0, Vector2.Zero, 1, default, 0);
			}
		}
	}
}