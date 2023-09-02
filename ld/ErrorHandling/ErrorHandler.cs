using Language.Lexing;
using System.Text;

using Spectre.Console;

namespace Language.ErrorHandling;

public class ErrorMessage
{
    private StringBuilder _message;

    public ErrorMessage(LdErrorCode code, string fileContents, SourceLocation location, string message, List<string>? notes = null)
    {
        var allLines = fileContents.Split(Environment.NewLine);
        bool isOnFirstLine = location.Line == 0;
        bool canDisplayLineAbove = allLines.Length >= 2 && location.Line > 2;
        bool canUseNextLine = !((location.Line + 1) >= allLines.Length); 

        _message = new StringBuilder();
        _message.AppendLine($"[red]error[[[italic]E{(int)code}[/]]][/]: {message}");
        _message.AppendLine($" --> {location.File}:{location.Line}:{location.Column}");

        if (!isOnFirstLine && canDisplayLineAbove)
        {
            _message.AppendLine($" {location.Line - 1} | {allLines[location.Line - 2]}");
        }

        // This is the actual diagnostic location.

        // The amount of spacing before the "^^^^"
        var padding = new string(' ', (int)location.Column);
        var arrowsPointingToContent = new string('^', (int)(location.Span.End - location.Span.Begin));
        _message.AppendLine($" {location.Line} | {allLines[location.Line - 1]}");
        _message.AppendLine($"   | {padding}{arrowsPointingToContent}");

        if (canUseNextLine)
        {
            _message.AppendLine($" {location.Line + 1} | {allLines[location.Line]}");
        }

        if (notes is null) return;
        foreach (var note in notes)
        {
            _message.AppendLine($"   = note: {note}");
        }
    }

    public string GetMessage()
    {
        return _message.ToString();
    }
}
public class WarningMessage
{
    private StringBuilder _message;

    public WarningMessage(string fileContents, SourceLocation location, string message, List<string>? notes = null)
    {
        var allLines = fileContents.Split(Environment.NewLine);
        bool isOnFirstLine = location.Line == 0;
        bool canDisplayLineAbove = allLines.Length >= 2 && location.Line > 2;
        bool canUseNextLine = !((location.Line + 1) >= allLines.Length);

        _message = new StringBuilder();
        _message.AppendLine($"[orange]warning[/]: {message}");
        _message.AppendLine($" --> {location.File}:{location.Line}:{location.Column}");

        if (!isOnFirstLine && canDisplayLineAbove)
        {
            _message.AppendLine($" {location.Line - 1} | {allLines[location.Line - 2]}");
        }

        // This is the actual diagnostic location.

        // The amount of spacing before the "^^^^"
        var padding = new string(' ', (int)location.Column);
        var arrowsPointingToContent = new string('^', (int)(location.Span.End - location.Span.Begin));
        _message.AppendLine($" {location.Line} | {allLines[location.Line - 1]}");
        _message.AppendLine($"   | {padding}{arrowsPointingToContent}");

        if (canUseNextLine)
        {
            _message.AppendLine($" {location.Line + 1} | {allLines[location.Line]}");
        }

        if (notes is null) return;
        foreach (var note in notes)
        {
            _message.AppendLine($"   = note: {note}");
        }
    }

    public string GetMessage()
    {
        return _message.ToString();
    }
}

public class ErrorBuilder
{
    private string _message;
    private LdErrorCode _code;
    private List<string> _notes;
    private string _source;
    private SourceLocation? _location;

    public ErrorBuilder(string source)
    {
        _notes = new List<string>();
        _message = string.Empty;
        _source = source;
        _location = null;
    }

    public ErrorBuilder WithMessage(string msg)
    {
        _message = msg;
        return this;
    }
    public ErrorBuilder WithCode(LdErrorCode code)
    {
        _code = code;
        return this;
    }
    public ErrorBuilder WithNote(string note)
    {
        _notes.Add(note);
        return this;
    }
    public ErrorBuilder WithSourceLocation(SourceLocation location)
    {
        _location = location;
        return this;
    }

    public ErrorMessage Build()
    {
        var location = _location ?? throw new ArgumentException("errors require a source location.");
        return new ErrorMessage(_code, _source, location, _message, _notes.Any() ? _notes : null);
    }
}
public class WarningBuilder
{
    private string _message;
    private readonly List<string> _notes;
    private readonly string _source;
    private SourceLocation? _location;

    public WarningBuilder(string source)
    {
        _notes = new List<string>();
        _message = string.Empty;
        _source = source;
        _location = null;
    }

    public WarningBuilder WithMessage(string msg)
    {
        _message = msg;
        return this;
    }
    public WarningBuilder WithNote(string note)
    {
        _notes.Add(note);
        return this;
    }
    public WarningBuilder WithSourceLocation(SourceLocation location)
    {
        _location = location;
        return this;
    }

    public WarningMessage Build()
    {
        var location = _location ?? throw new ArgumentException("errors require a source location.");
        return new WarningMessage(_source, location, _message, _notes.Any() ? _notes : null);
    }
}

public static class ErrorHandler
{
    public readonly static List<ErrorMessage> QueuedErrors;

    static ErrorHandler()
    {
        QueuedErrors = new List<ErrorMessage>();
    }

    public static void QueueNow(ErrorMessage err)
    {
        QueuedErrors.Add(err);
    }

    public static void DisplayWarning(WarningMessage msg)
    {
        AnsiConsole.MarkupLine(msg.GetMessage());
        AnsiConsole.WriteLine();
    }

    public static void DisplayThenExitIfAny()
    {
        if (QueuedErrors.Count == 0)
            return;

        foreach (var err in QueuedErrors)
        {
            AnsiConsole.MarkupLine(err.GetMessage());
        }

        AnsiConsole.WriteLine($"aborting due to previous errors.");
        Environment.Exit(-1);
    }
}
