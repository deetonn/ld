
using Language.Config;

namespace Language.ErrorHandling;

internal static class Logger
{
    private static readonly string _fileName;

    static Logger()
    {
        _fileName = "compiler.log";

        if (File.Exists(_fileName))
            File.Delete(_fileName);

        using var file = File.Create(_fileName);
    }

    private static void LogBase(object self, string message)
    {
        File.AppendAllText(_fileName, $"{self.GetType().Name}: {message}");
    }

    public static void LogDebug(object self, string message)
    {
        if (CompilerConfig.GetFlag("verbose"))
        {
            LogBase(self, $"debug: {message}");
        }
    }
}
