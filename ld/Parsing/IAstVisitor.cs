
using Language.Parsing.Productions;
using Language.Parsing.Productions.Conditional;
using Language.Parsing.Productions.Literals;
using Language.Parsing.Productions.Math;

namespace Language.Parsing;

public interface IAstVisitor
{
    object AndExpression(AndExpression andExpression);
    object CopyVariable(CopiedVariable copiedVariable);
    object ReferenceVariable(ReferencedVariable referencedVariable);
    object Visit32BitInteger(Number32Bit number);
    object Visit64BitInteger(Number64Bit number);
    object VisitAddition(AdditionExpression additionExpression);
    public object VisitAssignment(Assignment assignment);
    object VisitBitwiseAndExpression(BitwiseAndExpression bitwiseAndExpression);
    object VisitBitwiseOrExpression(BitwiseOrExpression bitwiseOrExpression);
    object VisitBlock(Block block);
    object VisitDivisionExpression(DivisionExpression divisionExpression);
    object VisitDotNotation(VariableDotNotation variableDotNotation);
    object VisitFloatingPointNumber(FloatingPointNumber floatingPointNumber);
    object VisitFunctionCall(FunctionCall functionCall);
    object VisitFunctionDeclaration(FunctionDeclaration functionDeclaration);
    object VisitGrouping(GroupingExpression groupingExpression);
    object VisitIsEqualToExpression(IsEqualToExpression isEqualToExpression);
    object VisitIsGreaterEqualToExpression(IsGreaterEqualToExpression isGreaterEqualToExpression);
    object VisitIsGreaterThanExpression(IsGreaterThanExpression isGreaterThanExpression);
    object VisitIsLesserEqualToExpression(IsLesserEqualToExpression isLesserEqualToExpression);
    object VisitIsLesserThanExpression(IsLesserThanExpression isLesserThanExpression);
    object VisitIsNotEqualToExpression(IsNotEqualToExpression isNotEqualToExpression);
    object VisitModuloExpression(ModuloExpression moduloExpression);
    object VisitMultiplicationExpression(MultiplicationExpression multiplicationExpression);
    object VisitNotExpression(NotExpression notExpression);
    object VisitOrExpression(OrExpression orExpression);
    object VisitReturnStatement(ReturnStatement returnStatement);
    object VisitStringLiteral(StringLiteral stringLiteral);
    object VisitSubtractionExpression(SubtractionExpression subtractionExpression);
}
