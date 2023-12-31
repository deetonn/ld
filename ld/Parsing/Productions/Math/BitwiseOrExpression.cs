﻿
using Language.Lexing;
using Spectre.Console;

namespace Language.Parsing.Productions.Math;

public record class BitwiseOrExpression(Expression Left, Expression Right, SourceLocation Location)
    : MathematicalExpression(Left, Right, Location)
{
    public override object Visit(IAstVisitor visitor)
    {
        return visitor.VisitBitwiseOrExpression(this);
    }

    public override void Visualize(string indent, bool last)
    {
        indent = ShowIndent(indent, last);
        AnsiConsole.WriteLine("BitwiseOr");

        Left.Visualize(indent, false);
        Right.Visualize(indent, true);
    }
}
