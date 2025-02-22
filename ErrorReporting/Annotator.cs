/******************************************************************************
 * Filename    = Annotator.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = ErrorReporting
 *
 * Description = Defines the class "Annotator", which provides methods 
 *               to print out various types of compilation errors.
 *****************************************************************************/

using LanguageConstructs;
using Type = LanguageConstructs.Type;

namespace ErrorReporting;

/// <summary>
/// Error message constants.
/// </summary>
static class ErrorMessages
{
    // Generic syntax error messages.
    internal const string Syntax = "Syntax";

    // Scope error messages.
    internal const string AmbiguousName = "The name has been declared already.";
    internal const string UndefinedName = "The name is undefined in the current scope.";
    internal const string Scope = "Scope";

    // Kind error messages.
    internal const string NonConstantInConstantDefinition = "Right hand side entity in constant definition is not a constant.";
    internal const string AssignmentModifiesConstant = "Assignment statement is attempting to modify the value of a constant.";
    internal const string ArrayVariableMissingIndexedSelector = "Variable of kind array is missing indexed selector.";
    internal const string NonPositiveIntegerIndexInArrayDeclaration = "The index for array declaration does not evaluate to a positive integer.";
    internal const string ProcedureAccessedAsObject = "A procedure name is used where an object name was expected.";
    internal const string ArgumentCountMismatch = "The argument count in procedure invocation does not tally with the parameter count specified in its signature.";
    internal const string ParameterKindMismatch = "Mismatch in reference-value parameter kind between an argument and its corresponding parameter.";
    internal const string ConstantPassedAsReferenceParameter = "A constant object is being passed as reference parameter.";
    internal const string AssignmentCountMismatch = "The expression list count to the right of the assignment operator does not tally with the variable access count to the left of it.";
    internal const string ReadModifiesConstant = "Read statement is attempting to modify the value of a constant.";
    internal const string RandomizeModifiesConstant = "Randomize statement is attempting to modify the value of a constant.";
    internal const string ReceiveModifiesConstant = "Receive statement is attempting to modify the value of a constant.";
    internal const string NonProcedureInParallelStatement = "Parallel statement is not used for procedure invocation.";
    internal const string ParallelProcedureHasNonVoidReturn = "Procedure invoked in parallel statement does not have a void return type.";
    internal const string ParallelProcedureHasReferenceParameter = "Procedure invoked in parallel statement contains reference parameter.";
    internal const string ParallelProcedureHasNoChannelParameter = "Procedure invoked in parallel statement does not contain any channel parameter.";
    internal const string ParallelProcedureUsesIO = "Procedure invoked in parallel statement uses input or output statement(s).";
    internal const string ParallelProcedureUsesNonLocals = "Procedure invoked in parallel statement uses non local variable(s).";
    internal const string ParallelProcedureCallsUnfriendly = "Procedure invoked in parallel statement invokes another procedure that is not parallel friendly; i.e. it either uses input / output statement(s), or uses non local variable(s), or both.";
    internal const string Kind = "Kind";

    // Type error messages.
    internal const string NonIntegerIndexInArrayDeclaration = "The index for array declaration is not of type integer.";
    internal const string NonIntegerArrayIndex = "Index for array variable does not evaluate to type integer.";
    internal const string TypeMismatchInAssignment = "Types don't match on either side of assignment operator.";
    internal const string NonBooleanInIfCondition = "Conditional expression for if statement does not evaluate to type boolean.";
    internal const string NonBooleanInWhileCondition = "Conditional expression for while statement does not evaluate to type boolean.";
    internal const string NonBooleanToTheRightOfNotOperator = "Right hand side of Not operator does not evaluate to type boolean.";
    internal const string MinusPrecedingNonIntegerInConstantDefinition = "Minus symbol precedes a non-integer constant in constant definition.";
    internal const string ParameterTypeMismatch = "Type mismatch between an argument and its corresponding parameter.";
    internal const string InvalidTypeInWriteStatement = "Expression in write statement does not evaluate to type integer or boolean.";
    internal const string InvalidTypeInReadStatement = "Object in read statement is not of type integer or boolean.";
    internal const string NonIntegerInRandomizeStatement = "Object in randomize statement is not of type integer.";
    internal const string NonIntegerValueInSendStatement = "Attempt to send a non-integer value.";
    internal const string NonChannelInSendStatement = "Attempt to send a value through a non-channel variable.";
    internal const string NonIntegerValueInReceiveStatement = "Attempt to receive a non-integer value.";
    internal const string NonChannelInReceiveStatement = "Attempt to receive a value through a non-channel variable.";
    internal const string NonChannelInOpenStatement = "Object in open statement is not of type channel.";
    internal const string TypeMismatchAcrossEqualityOperator = "Types don't match on either side of equality operator, ";
    internal const string NonBooleanLeftOfLogicalOperator = "Expression to the left of logical operator, ";
    internal const string NonBooleanRightOfLogicalOperator = "Expression to the right of logical operator, ";
    internal const string NonIntegerLeftOfRelationalOperator = "Expression to the left of relational operator, ";
    internal const string NonIntegerRightOfRelationalOperator = "Expression to the right of relational operator, ";
    internal const string NonIntegerLeftOfAdditionOperator = "Expression to the left of addition operator, ";
    internal const string NonIntegerRightOfAdditionOperator = "Expression to the right of addition operator, ";
    internal const string NonIntegerLeftOfMultiplicationOperator = "Expression to the left of multiplication operator, ";
    internal const string NonIntegerRightOfMultiplicationOperator = "Expression to the right of multiplication operator, ";
    internal const string InvalidTypeAcrossEqualityOperator = "Invalid type used across equality operator, ";
    internal const string Type = "Type";
}

/// <summary>
/// Provides methods to print out various types of compilation errors.
/// </summary>
public class Annotator
{
    /// <summary>
    /// Delegates error reporting to registered clients.
    /// </summary>
    /// <param name="errorCategory">Category of the compilation error.</param>
    /// <param name="errorCode">Error code.</param>
    /// <param name="errorMessage">Error message.</param>
    public delegate void OnPrintErrorReport(string errorCategory,
                                            int errorCode,
                                            string errorMessage);

    /// <summary>
    /// Creates an instance of Annotator, which provides methods 
    /// to print out various types of compilation errors.
    /// </summary>
    public Annotator(OnPrintErrorReport printErrorReport)
    {
        ErrorFree = true;
        PrintErrorReport = printErrorReport;
    }

    /// <summary>
    /// Sets the delegate instance for printing error reports.
    /// </summary>
    public OnPrintErrorReport PrintErrorReport { get; set; }

    /// <summary>
    /// Gets a value indicating if there has been any errors reported.
    /// True indicates that no errors have been reported. False otherwise.
    /// </summary>
    public bool ErrorFree { get; private set; }

    /// <summary>
    /// Prints out syntax error.
    /// </summary>
    public void SyntaxError()
    {
        ErrorFree = false;
        PrintError(ErrorMessages.Syntax, (int)GenericErrorCategory.GenericSyntaxError, string.Empty);
    }

    /// <summary>
    /// Prints out scope error.
    /// </summary>
    /// <param name="category">Category of scope error.</param>
    public void ScopeError(ScopeErrorCategory category)
    {
        ErrorFree = false;
        string message = string.Empty;

        switch (category)
        {
            case ScopeErrorCategory.AmbiguousName:
                {
                    message = ErrorMessages.AmbiguousName;
                    break;
                }

            case ScopeErrorCategory.UndefinedName:
                {
                    message = ErrorMessages.UndefinedName;
                    break;
                }
        }

        PrintError(ErrorMessages.Scope, (int)category, message);
    }

    /// <summary>
    /// Prints out kind error.
    /// </summary>
    /// <param name="kind">Kind of the object.</param>
    /// <param name="category">Category of kind error.</param>
    public void KindError(Kind kind, KindErrorCategory category)
    {
        ErrorFree = false;

        if (kind != Kind.Undefined)
        {
            string message = string.Empty;

            switch (category)
            {
                case KindErrorCategory.NonConstantInConstantDefinition:
                    {
                        message = ErrorMessages.NonConstantInConstantDefinition;
                        break;
                    }

                case KindErrorCategory.AssignmentModifiesConstant:
                    {
                        message = ErrorMessages.AssignmentModifiesConstant;
                        break;
                    }

                case KindErrorCategory.ArrayVariableMissingIndexedSelector:
                    {
                        message = ErrorMessages.ArrayVariableMissingIndexedSelector;
                        break;
                    }

                case KindErrorCategory.NonPositiveIntegerIndexInArrayDeclaration:
                    {
                        message = ErrorMessages.NonPositiveIntegerIndexInArrayDeclaration;
                        break;
                    }

                case KindErrorCategory.ProcedureAccessedAsObject:
                    {
                        message = ErrorMessages.ProcedureAccessedAsObject;
                        break;
                    }

                case KindErrorCategory.ArgumentCountMismatch:
                    {
                        message = ErrorMessages.ArgumentCountMismatch;
                        break;
                    }

                case KindErrorCategory.ParameterKindMismatch:
                    {
                        message = ErrorMessages.ParameterKindMismatch;
                        break;
                    }

                case KindErrorCategory.ConstantPassedAsReferenceParameter:
                    {
                        message = ErrorMessages.ConstantPassedAsReferenceParameter;
                        break;
                    }

                case KindErrorCategory.AssignmentCountMismatch:
                    {
                        message = ErrorMessages.AssignmentCountMismatch;
                        break;
                    }

                case KindErrorCategory.ReadModifiesConstant:
                    {
                        message = ErrorMessages.ReadModifiesConstant;
                        break;
                    }

                case KindErrorCategory.RandomizeModifiesConstant:
                    {
                        message = ErrorMessages.RandomizeModifiesConstant;
                        break;
                    }

                case KindErrorCategory.ReceiveModifiesConstant:
                    {
                        message = ErrorMessages.ReceiveModifiesConstant;
                        break;
                    }

                case KindErrorCategory.NonProcedureInParallelStatement:
                    {
                        message = ErrorMessages.NonProcedureInParallelStatement;
                        break;
                    }

                case KindErrorCategory.ParallelProcedureHasNonVoidReturn:
                    {
                        message = ErrorMessages.ParallelProcedureHasNonVoidReturn;
                        break;
                    }

                case KindErrorCategory.ParallelProcedureHasReferenceParameter:
                    {
                        message = ErrorMessages.ParallelProcedureHasReferenceParameter;
                        break;
                    }

                case KindErrorCategory.ParallelProcedureHasNoChannelParameter:
                    {
                        message = ErrorMessages.ParallelProcedureHasNoChannelParameter;
                        break;
                    }

                case KindErrorCategory.ParallelProcedureUsesIO:
                    {
                        message = ErrorMessages.ParallelProcedureUsesIO;
                        break;
                    }

                case KindErrorCategory.ParallelProcedureUsesNonLocals:
                    {
                        message = ErrorMessages.ParallelProcedureUsesNonLocals;
                        break;
                    }

                case KindErrorCategory.ParallelProcedureCallsUnfriendly:
                    {
                        message = ErrorMessages.ParallelProcedureCallsUnfriendly;
                        break;
                    }
            }

            PrintError(ErrorMessages.Kind, (int)category, message);
        }
    }

    /// <summary>
    /// Prints out type error.
    /// </summary>
    /// <param name="type">Type of the object.</param>
    /// <param name="category">Category of type error.</param>
    public void TypeError(Type type, TypeErrorCategory category)
    {
        ErrorFree = false;

        if (type != Type.Universal)
        {
            string message = string.Empty;

            switch (category)
            {
                case TypeErrorCategory.NonIntegerIndexInArrayDeclaration:
                    {
                        message = ErrorMessages.NonIntegerIndexInArrayDeclaration;
                        break;
                    }

                case TypeErrorCategory.NonIntegerArrayIndex:
                    {
                        message = ErrorMessages.NonIntegerArrayIndex;
                        break;
                    }

                case TypeErrorCategory.TypeMismatchInAssignment:
                    {
                        message = ErrorMessages.TypeMismatchInAssignment;
                        break;
                    }

                case TypeErrorCategory.NonBooleanInIfCondition:
                    {
                        message = ErrorMessages.NonBooleanInIfCondition;
                        break;
                    }

                case TypeErrorCategory.NonBooleanInWhileCondition:
                    {
                        message = ErrorMessages.NonBooleanInWhileCondition;
                        break;
                    }

                case TypeErrorCategory.NonBooleanToTheRightOfNotOperator:
                    {
                        message = ErrorMessages.NonBooleanToTheRightOfNotOperator;
                        break;
                    }

                case TypeErrorCategory.MinusPrecedingNonIntegerInConstantDefinition:
                    {
                        message = ErrorMessages.MinusPrecedingNonIntegerInConstantDefinition;
                        break;
                    }

                case TypeErrorCategory.ParameterTypeMismatch:
                    {
                        message = ErrorMessages.ParameterTypeMismatch;
                        break;
                    }

                case TypeErrorCategory.InvalidTypeInWriteStatement:
                    {
                        message = ErrorMessages.InvalidTypeInWriteStatement;
                        break;
                    }

                case TypeErrorCategory.InvalidTypeInReadStatement:
                    {
                        message = ErrorMessages.InvalidTypeInReadStatement;
                        break;
                    }

                case TypeErrorCategory.NonIntegerInRandomizeStatement:
                    {
                        message = ErrorMessages.NonIntegerInRandomizeStatement;
                        break;
                    }

                case TypeErrorCategory.NonIntegerValueInSendStatement:
                    {
                        message = ErrorMessages.NonIntegerValueInSendStatement;
                        break;
                    }

                case TypeErrorCategory.NonChannelInSendStatement:
                    {
                        message = ErrorMessages.NonChannelInSendStatement;
                        break;
                    }

                case TypeErrorCategory.NonIntegerValueInReceiveStatement:
                    {
                        message = ErrorMessages.NonIntegerValueInReceiveStatement;
                        break;
                    }

                case TypeErrorCategory.NonChannelInReceiveStatement:
                    {
                        message = ErrorMessages.NonChannelInReceiveStatement;
                        break;
                    }

                case TypeErrorCategory.NonChannelInOpenStatement:
                    {
                        message = ErrorMessages.NonChannelInOpenStatement;
                        break;
                    }
            }

            PrintError(ErrorMessages.Type, (int)category, message);
        }
    }

    /// <summary>
    /// Prints out type error for diadic operators. A diadic operator is an
    /// operator that acts upon two arguments 
    /// (example: "=", "&amp;", "|", "+", "*") 
    /// as opposed to monadic operators (like "!") that act upon only 
    /// one argument.
    /// </summary>
    /// <param name="type">Type of the object.</param>
    /// <param name="category">Category of type error.</param>
    /// <param name="diadicOperator">The diadicOperator.</param>
    public void TypeError(Type type, DiadicTypeErrorCategory category, Symbol diadicOperator)
    {
        ErrorFree = false;

        if (type != Type.Universal)
        {
            string message = string.Empty;

            switch (category)
            {
                case DiadicTypeErrorCategory.TypeMismatchAcrossEqualityOperator:
                    {
                        message = $"{ErrorMessages.TypeMismatchAcrossEqualityOperator}{diadicOperator}.";
                        break;
                    }

                case DiadicTypeErrorCategory.NonBooleanLeftOfLogicalOperator:
                    {
                        message = $"{ErrorMessages.NonBooleanLeftOfLogicalOperator}{diadicOperator}, does not evaluate to type boolean.";
                        break;
                    }

                case DiadicTypeErrorCategory.NonBooleanRightOfLogicalOperator:
                    {
                        message = $"{ErrorMessages.NonBooleanRightOfLogicalOperator}{diadicOperator}, does not evaluate to type boolean.";
                        break;
                    }

                case DiadicTypeErrorCategory.NonIntegerLeftOfRelationalOperator:
                    {
                        message = $"{ErrorMessages.NonIntegerLeftOfRelationalOperator}{diadicOperator}, does not evaluate to type integer.";
                        break;
                    }

                case DiadicTypeErrorCategory.NonIntegerRightOfRelationalOperator:
                    {
                        message = $"{ErrorMessages.NonIntegerRightOfRelationalOperator}{diadicOperator}, does not evaluate to type integer.";
                        break;
                    }

                case DiadicTypeErrorCategory.NonIntegerLeftOfAdditionOperator:
                    {
                        message = $"{ErrorMessages.NonIntegerLeftOfAdditionOperator}{diadicOperator}, does not evaluate to type integer.";
                        break;
                    }

                case DiadicTypeErrorCategory.NonIntegerRightOfAdditionOperator:
                    {
                        message = $"{ErrorMessages.NonIntegerRightOfAdditionOperator}{diadicOperator}, does not evaluate to type integer.";
                        break;
                    }

                case DiadicTypeErrorCategory.NonIntegerLeftOfMultiplicationOperator:
                    {
                        message = $"{ErrorMessages.NonIntegerLeftOfMultiplicationOperator}{diadicOperator}, does not evaluate to type integer.";
                        break;
                    }

                case DiadicTypeErrorCategory.NonIntegerRightOfMultiplicationOperator:
                    {
                        message = $"{ErrorMessages.NonIntegerRightOfMultiplicationOperator}{diadicOperator}, does not evaluate to type integer.";
                        break;
                    }

                case DiadicTypeErrorCategory.InvalidTypeAcrossEqualityOperator:
                    {
                        message = $"{ErrorMessages.InvalidTypeAcrossEqualityOperator}{diadicOperator}.";
                        break;
                    }
            }

            PrintError(ErrorMessages.Type, (int)category, message);
        }
    }

    /// <summary>
    /// Delegates the compilation error details to registered clients.
    /// </summary>
    /// <param name="errorCategory">Category of the compilation error.</param>
    /// <param name="errorCode">Error code.</param>
    /// <param name="errorMessage">Error message.</param>
    private void PrintError(string errorCategory, int errorCode, string errorMessage)
    {
        PrintErrorReport.Invoke(errorCategory, errorCode, errorMessage);
    }
}
