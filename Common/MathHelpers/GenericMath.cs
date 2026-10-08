using System.Numerics;

namespace SpiritReforged.Common.MathHelpers;

internal class GenericMath
{
    public static T Lerp<T>(T from, T to, float factor) where T : INumber<T> => from + T.CreateSaturating(float.CreateSaturating(to - from) * factor);
    public static float InverseLerp<T>(T from, T to, T factor) where T : INumber<T> 
        => (float.CreateSaturating(factor) - float.CreateSaturating(from)) / (float.CreateSaturating(to) - float.CreateSaturating(from));
}
