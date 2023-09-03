
using Language.Api;
using Language.Parsing.Productions;
using Language.Parsing.Productions.Conditional;
using Language.Parsing.Productions.Literals;
using Language.Parsing.Productions.Math;

namespace Language.Parsing;

public interface IAstVisitor
{
    public object AndExpression(AndExpression andExpression);
    public object CopyVariable(CopiedVariable copiedVariable);
    public object ReferenceVariable(ReferencedVariable referencedVariable);
    public object Visit32BitInteger(Number32Bit number);
    public object Visit64BitInteger(Number64Bit number);
    public object VisitAddition(AdditionExpression additionExpression);
    public object VisitArrayLiteralExpression(ArrayLiteralExpression arrayLiteralExpression);
    public object VisitAssignment(Assignment assignment);
    public object VisitBitwiseAndExpression(BitwiseAndExpression bitwiseAndExpression);
    public object VisitBitwiseOrExpression(BitwiseOrExpression bitwiseOrExpression);
    public object VisitBlock(Block block);
    public object VisitBooleanExpression(BooleanExpression booleanExpression);
    public object VisitDivisionExpression(DivisionExpression divisionExpression);
    public object VisitDotNotation(VariableDotNotation variableDotNotation);
    public object VisitFloatingPointNumber(FloatingPointNumber floatingPointNumber);
    public object VisitFunctionCall(FunctionCall functionCall);
    public object VisitFunctionDeclaration(FunctionDeclaration functionDeclaration);
    public object VisitGrouping(GroupingExpression groupingExpression);
    public object VisitIfStatement(IfStatement ifStatement);
    public object VisitIsEqualToExpression(IsEqualToExpression isEqualToExpression);
    public object VisitIsGreaterEqualToExpression(IsGreaterEqualToExpression isGreaterEqualToExpression);
    public object VisitIsGreaterThanExpression(IsGreaterThanExpression isGreaterThanExpression);
    public object VisitIsLesserEqualToExpression(IsLesserEqualToExpression isLesserEqualToExpression);
    public object VisitIsLesserThanExpression(IsLesserThanExpression isLesserThanExpression);
    public object VisitIsNotEqualToExpression(IsNotEqualToExpression isNotEqualToExpression);
    public object VisitModuloExpression(ModuloExpression moduloExpression);
    public object VisitMultiplicationExpression(MultiplicationExpression multiplicationExpression);
    public object VisitNotExpression(NotExpression notExpression);
    public object VisitOrExpression(OrExpression orExpression);
    public object VisitReturnStatement(ReturnStatement returnStatement);
    public object VisitStringLiteral(StringLiteral stringLiteral);
    public object VisitSubtractionExpression(SubtractionExpression subtractionExpression);

    /// <summary>
    /// Get the active scope.
    /// </summary>
    /// <returns>The current scope being used.</returns>
    public LdScope ThisScope();

    /// <summary>
    /// Switch the context to a new scope. This will push the scope
    /// into the scope stack. This requires you to call ExitContext.
    /// </summary>
    /// <param name="newContext">The new scope to switch into</param>
    /// <returns>The previous scope.</returns>
    public void SwitchContext(LdScope newContext);

    /// <summary>
    /// Remove the previous scope from the scope stack.
    /// </summary>
    /// <returns>The previous scope, once popped off the stack.</returns>
    public LdScope ExitContext();
    object VisitStructDeclaration(StructDeclaration structDeclaration);
    object VisitStructFieldDeclaration(StructFieldDeclaration structFieldDeclaration);
    object VisitImplStatement(ImplStatement implStatement);
    object VisitStructInitialization(StructInitializationExpression structInitializationExpression);
    object VisitEnumVariantDeclaration(EnumVariantDeclaration enumVariant);
    object VisitEnumDeclaration(EnumDeclaration enumDeclaration);
    object VisitColonNotation(ColonNotation colonNotation);
    object VisitEnumVariantExpression(EnumVariantExpression enumVariantExpression);
    object VisitStaticStructAccessExpression(StaticStructAccessExpression staticStructAccessExpression);
}
