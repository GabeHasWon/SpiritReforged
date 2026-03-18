using SpiritReforged.Common.Visuals;

namespace SpiritReforged.Content.Desert.ScarabBoss.Items.Crook;

// These are purely visual
public class BabyLocust(int Lifetime, int ParentWhoAmI, bool ParentingNPC = true)
{
	public Entity Parent => parentingNPC ? Main.npc[parentWhoAmI] : Main.projectile[parentWhoAmI];

	public float AnimationSpeed => lifetime * 0.125f * (2 - scale);

	public static readonly Asset<Texture2D> Texture = DrawHelpers.RequestLocal<BabyLocust>("CrookBabyLocust", false);

	public bool drawBehind;
	public bool parentingNPC = ParentingNPC;

	public int lifetime = Lifetime;
	public int parentWhoAmI = ParentWhoAmI;

	internal int fadeInTimer;

	internal int frame;
	internal int frameCounter;
	internal float rotationOffset = Main.rand.NextFloat(MathHelper.TwoPi);
	internal float scale = Main.rand.NextFloat(0.8f, 1.2f);
	internal float direction;

	public Vector2 position;
	public float rotation;

	public void Update()
	{
		if (fadeInTimer < 20)
			fadeInTimer++;

		rotationOffset += Main.rand.NextFloat(0.05f);

		if (!parentingNPC)
			lifetime++;
		else
			lifetime--;

		float sin = (float)Math.Sin(AnimationSpeed);
		float cos = (float)Math.Cos(AnimationSpeed);

		if (position.X < Parent.Center.X)
			direction = -1;
		else
			direction = 1;

		Vector2 pos = Parent.Center;
		if (!parentingNPC)
			pos = Parent.Center + new Vector2(-20, 0).RotatedBy(Main.projectile[parentWhoAmI].rotation - MathHelper.PiOver4);

		position = pos + new Vector2(Parent.width * cos, 0f).RotatedBy(rotationOffset);
		rotation = MathHelper.Lerp(rotation, cos, 0.05f);

		if (sin is < 1f and > (-0.5f))
			drawBehind = true;
		else
			drawBehind = false;

		if (++frameCounter >= 5)
		{
			frameCounter = 0;
			if (++frame >= 4)
				frame = 0;
		}
	}

	public void DrawSelf(SpriteBatch sb, Vector2 screenPosition, Color drawColor)
	{
		Texture2D texture = Texture.Value;
		Rectangle sourceRectangle = texture.Frame(1, 4, frameY: frame);

		float fadeIn = fadeInTimer / 20f;
		float sin = (float)Math.Sin(AnimationSpeed);
		Color color = drawColor;

		if (sin > 0.8f)
			color = Color.Lerp(drawColor, new Color(90, 90, 90), (sin - 0.8f) / 0.2f);

		SpriteEffects effects = (direction == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		sb.Draw(texture, position + Main.rand.NextVector2Circular(1f, 1f) * (float)Math.Abs(sin) - screenPosition,
			sourceRectangle, color * fadeIn, rotation, sourceRectangle.Size() / 2f, scale, effects, 0f);
	}
}