/*using MonoMod.Utils;
using System.Reflection;
using SpiritReforged.DebuffOverhaul.Common;

namespace DebuffOverhaul;

public partial class DebuffOverhaul : Mod
{
	[AttributeUsage(AttributeTargets.Method)]
    private class ParametersAttribute(params Type[] Types) : Attribute
    {
        public Type[] Types = Types;
    }

    private delegate void CallDelegate(params object[] args);
    private readonly record struct CallInfo(Type[] Types, CallDelegate Action);

    private static readonly Dictionary<string, CallInfo> Calls = [];

    public override object Call(params object[] args)
    {
        LoadInfo();

        if (args is null)
            Logger.Error("Call Error: Arguments are null.");

        if (args.Length == 0)
            Logger.Error("Call Error: Arguments are empty.");

        if (args[0] is not string context)
        {
            Logger.Error($"Call Error: The first argument must provide a context.");
            return null;
        }

        if (Calls.TryGetValue(context, out var info))
        {
            args = args[1..]; //Remove context from the array as it's no longer needed

            if (info.Types.Length != args.Length)
                Logger.Error($"Call Error: Incorrect number of arguments for '{context}'.");

            for (int i = 0; i < args.Length; i++) //Ensure all arguments are of the correct type
            {
                object argument = args[i];
                Type type = info.Types[i];

                if (argument != null && argument.GetType() != type)
                    Logger.Error($"Call Error: Argument {i} must be of type '{type.FullName}'.");
            }

            info.Action.Invoke(args);
        }
        else
        {
            Logger.Error($"Call Error: Context '{context}' is invalid.");
        }

        return null;
    }

    public bool LoadInfo()
    {
        if (Calls.Count == 0)
        {
            foreach (var method in GetType().GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
            {
                if (method.GetCustomAttribute<ParametersAttribute>() is ParametersAttribute p && method.TryCreateDelegate<CallDelegate>() is CallDelegate dele)
                    Calls.Add(method.Name, new(p.Types, dele));
            }

            return true;
        }

        return false;
    }

    [Parameters(typeof(int), typeof(float), typeof(int), typeof(Action<SpriteBatch, NPC, Color, Vector2, float, float>))]
    private static void AddDoT(params object[] args)
    {
        int buffType = (int)args[0];
        float scalability = (float)args[1];
        int damageLimit = (int)args[2];
        var onPostDraw = (Action<SpriteBatch, NPC, Color, Vector2, float, float>)args[3];

        BuffExtension.Handler.Register(new CustomDoT(scalability, damageLimit, onPostDraw), buffType);
    }
}*/