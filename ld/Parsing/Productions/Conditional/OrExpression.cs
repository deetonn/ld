﻿
using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions.Conditional;

public record class OrExpression(Expression Left, Expression Right, SourceLocation Location)
    : Expression(Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitOrExpression(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        AnsiConsole.WriteLine("Or");

        Left.Visualize(indent, false);
        Right.Visualize(indent, true);
    }
}
