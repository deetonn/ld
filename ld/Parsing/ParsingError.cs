using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Language.Parsing;

public enum ParsingErrorCode
{
    Base = 4000,
    UnexpectedEof,
    UnknownToken,
}

public struct ParsingErrorMessage
{
    public string Message;
    public List<string> Notes;

    public ParsingErrorMessage(string message, params string[] notes)
    {
        Message = message;
        Notes = notes.ToList();
    }
}
