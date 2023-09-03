
namespace Language.ErrorHandling;

public enum LdErrorCode
{
    UnknownEscapeSequence = 1000,
    ExpectedIdentifier,
    ExpectedComma,
    ExpectedExpressionReasonMathematical,
    UnexpectedEOF,
    UnrecognizedOperator,
    ArgumentsCanOnlyBeExpressions,
    ExpectedExpressionOrRightParen,
    InvalidFloatLiteral,
    UnknownNumberLiteral,
    ExpectedKeyword,
    ExpectedLeftBrace,
    ExpectedRightBrace,
    ExpectedColonFnArgs,
    ExpectedColon,
    ExpectedClosingGenericCroc,
    ExpectedLeftParen,
    ExpectedRightParen,
    ExpectedArrow,
    UnexpectedToken,
    ExpectedExpression,
    UnknownOperand,
    ExpectedBlockAfterIf,
    ExpectedBlockAfterElse,
    ExpectedLeftBracket,
    ExpectedCommaOrCloseBracket,
    ExpectedRightBracket,
    ExpectedColonStructField,
    ExpectedCommaOrCloseBrace,
    OnlyFunctionsInImplStmts,
    EnumVariantExpectedType
}
