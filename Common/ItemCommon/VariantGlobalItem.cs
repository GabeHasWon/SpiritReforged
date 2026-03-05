using Terraria.DataStructures;
using Terraria.UI;

namespace SpiritReforged.Common.ItemCommon;

/// <summary> Used to create items with variants by calling <see cref="AddVariants"/> in SetStaticDefaults. This does not exist on the server. </summary>
[Autoload(Side = ModSide.Client)]
internal class VariantGlobalItem : GlobalItem
{
	private const byte defaultID = 255;
	public override bool InstancePerEntity => true;

	private struct VarData(Point[] sizes, bool auto)
	{
		public Point[] sizes = sizes;
		public bool auto = auto;

		public Asset<Texture2D> worldTexture = null;
	}

	private static readonly Dictionary<int, VarData> variantData = []; //Type, frame sizes
	public byte subID = defaultID;

	/// <summary> Registers the given item <paramref name="type"/> as having variants of <paramref name="amount"/>.<para/>
	/// The item texture must be a vertical sheet with 2px padding to work correctly.<br/>
	/// Additionally, each frame should be at the top-left of the "frame" box.<para/>
	/// Adding "_World" to the name of an item texture will cause it to be prioritized over the default item texture when drawn in the world. </summary>
	/// <param name="type"> The item type. </param>
	/// <param name="amount"> The number of variants. </param>
	/// <param name="auto"> Whether this item automatically selects a variant. </param>
	public static void AddVariants(int type, int amount, bool auto = true)
	{
		if (Main.dedServ)
			return;

		Main.RegisterItemAnimation(type, new DrawAnimationVertical(2, amount) { NotActuallyAnimating = true });
		TryGetWorldTexture(type, out var world);

		variantData.Add(type, new VarData(null, auto) { worldTexture = world });
	}

	/// <summary><inheritdoc cref="AddVariants(int, int, bool)"/></summary>
	/// <param name="type"> The item type. </param>
	/// <param name="sizes"> The dimensions of each individual frame. Also determines the frame count based on length. </param>
	/// <param name="auto"> Whether this item automatically selects a variant. </param>
	public static void AddVariants(int type, Point[] sizes, bool auto = true)
	{
		if (Main.dedServ)
			return;

		Main.RegisterItemAnimation(type, new DrawAnimationVertical(2, sizes.Length) { NotActuallyAnimating = true });
		TryGetWorldTexture(type, out var world);

		variantData.Add(type, new VarData(sizes, auto) { worldTexture = world });
	}

	private static void TryGetWorldTexture(int type, out Asset<Texture2D> texture)
	{
		Asset<Texture2D> world = null;
		var modItem = ItemLoader.GetItem(type);

		if (modItem != null)
		{
			string fullName = modItem.GetType().FullName.Replace(".", "/");
			if (ModContent.RequestIfExists(fullName + "_World", out Asset<Texture2D> toWorld))
				world = toWorld;
		}

		texture = world;
	}

	/// <returns> Which variant this item is using (<see cref="subID"/>). </returns>
	public static int GetVariant(Item item) => item.TryGetGlobalItem(out VariantGlobalItem vItem) ? vItem.subID : 0;

	/// <summary> Gets the draw frame of this varianted <paramref name="item"/>. </summary>
	/// <param name="item"> The item. </param>
	/// <returns> <see cref="Rectangle.Empty"/> if the item is invalid. </returns>
	public static Rectangle GetSource(Item item)
	{
		if (item.TryGetGlobalItem(out VariantGlobalItem vItem))
			return vItem.GetSource(item.type);

		return Rectangle.Empty;
	}

	private Rectangle GetSource(int type)
	{
		bool inInventory = subID == defaultID;

		int frameCount = Main.itemAnimations[type].FrameCount;
		int frame = (subID == defaultID) ? 0 : subID;

		var texture = ((variantData[type].worldTexture is null || inInventory) ? TextureAssets.Item[type] : variantData[type].worldTexture).Value;
		var rectangle = texture.Frame(1, frameCount, 0, frame, 0, -2);
		var sizes = variantData[type].sizes;

		if (sizes != null && frame < sizes.Length)
		{
			if (variantData[type].worldTexture is null || !inInventory)
			{
				rectangle.Width = sizes[frame].X;
				rectangle.Height = sizes[frame].Y;
			}
		}

		return rectangle;
	}

	public override bool AppliesToEntity(Item entity, bool lateInstantiation) => variantData.ContainsKey(entity.type);

	public override bool PreDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
	{
		int frameCount = Main.itemAnimations[item.type].FrameCount;

		if (variantData[item.type].auto && subID == defaultID)
			subID = (byte)Main.rand.Next(frameCount);

		var texture = (variantData[item.type].worldTexture ?? TextureAssets.Item[item.type]).Value;
		var source = GetSource(item.type);

		spriteBatch.Draw(texture, item.position + item.Size - source.Size() - Main.screenPosition, source, GetAlpha(item, lightColor) ?? lightColor, rotation, Vector2.Zero, scale, SpriteEffects.None, 0);
		return false;
	}

	public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
	{
		if (variantData[item.type].auto)
			subID = defaultID;

		var texture = TextureAssets.Item[item.type].Value;
		var source = GetSource(item.type);

		//Scale the item according to 'source' instead of 'frame'
		ItemSlot.DrawItem_GetColorAndScale(item, Main.inventoryScale, ref drawColor, 32, ref source, out _, out scale);

		spriteBatch.Draw(texture, position, source, drawColor, 0, source.Size() / 2, scale, SpriteEffects.None, 0);
		return false;
	}
}
