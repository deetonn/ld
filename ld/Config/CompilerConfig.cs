
using System.Diagnostics.CodeAnalysis;

namespace Language.Config;

public static class CompilerConfig
{
    private static readonly Dictionary<string, bool> Flags;
    private static readonly Dictionary<string, string> Options;

    static CompilerConfig()
    {
        Flags = new Dictionary<string, bool>();
        Options = new Dictionary<string, string>();
    }

    public static void AddFlag(string key, bool value)
        => Flags[key] = value;
    public static void AddOption(string key, string value)
        => Options[key] = value;

    public static bool GetFlag(string key)
    {
        if (Flags.TryGetValue(key, out bool value))
            return value;
        return false; // if the flag doesn't exist, its not set.
    }

    public static string? GetOption(string key)
    {
        return Options.TryGetValue(key, out string? value) ? value : null;
    }

    public static bool TryGetOption(string key, [NotNullWhen(true)] out string? value)
    {
        value = GetOption(key);
        return value != null;
    }
}
