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
    /// Prints out internal error, error caused by the compiler itself.
    /// </summary>
    /// <param name="category">Category of internal error.</param>
    public void InternalError(InternalErrorCategory category)
    {
        ErrorFree = false;
        string message = string.Empty;

        switch (category)
        {
            case InternalErrorCategory.InvalidOperationSymbol:
                {
                    message += "An invalid symbol has been passed " +
                               "to generate operation code.";
                    break;
                }

            case InternalErrorCategory.AssemblyTableIsFull:
                {
                    message += "Assembly table is full; " +
                               "the compiler has run out of memory.";
                    break;
                }

            case InternalErrorCategory.InternalProcessingError:
                {
                    message += "Internal processing error.";
                    break;
                }
        }

        PrintError("Internal", (int)category, message);
    }

    /// <summary>
    /// Prints out syntax error.
    /// </summary>
    public void SyntaxError()
    {
        ErrorFree = false;
        PrintError("Syntax",
                        (int)GenericErrorCategory.GenericSyntaxError,
                        string.Empty);
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
                    message += "The name has been declared already.";
                    break;
                }

            case ScopeErrorCategory.UndefinedName:
                {
                    message += "The name is undefined in the current scope.";
                    break;
                }
        }

        PrintError("Scope", (int)category, message);
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
                        message += "Right hand side entity in constant definition " +
                                   "is not a constant.";
                        break;
                    }

                case KindErrorCategory.AssignmentModifiesConstant:
                    {
                        message += "Assignment statement is attempting to modify " +
                                   "the value of a constant.";
                        break;
                    }

                case KindErrorCategory.ArrayVariableMissingIndexedSelector:
                    {
                        message += "Variable of kind array is missing indexed selector.";
                        break;
                    }

                case KindErrorCategory.NonPositiveIntegerIndexInArrayDeclaration:
                    {
                        message += "The index for array declaration does not evaluate " +
                                   "to a positive integer.";
                        break;
                    }

                case KindErrorCategory.ProcedureAccessedAsObject:
                    {
                        message += "A procedure name is used where an object name was " +
                                   "expected.";
                        break;
                    }

                case KindErrorCategory.ArgumentCountMismatch:
                    {
                        message += "The argument count in procedure invocation does " +
                                   "not tally with the parameter count specified in " +
                                   "its signature.";
                        break;
                    }

                case KindErrorCategory.ParameterKindMismatch:
                    {
                        message += "Mismatch in reference-value parameter kind between " +
                                   "an argument and its corresponding parameter.";
                        break;
                    }

                case KindErrorCategory.ConstantPassedAsReferenceParameter:
                    {
                        message += "A constant object is being passed as reference " +
                                   "parameter.";
                        break;
                    }

                case KindErrorCategory.AssignmentCountMismatch:
                    {
                        message += "The expression list count to the right of the " +
                                   "assignment operator does not tally with the " +
                                   "variable access count to the left of it.";
                        break;
                    }

                case KindErrorCategory.ReadModifiesConstant:
                    {
                        message += "Read statement is attempting to modify the value " +
                                   "of a constant.";
                        break;
                    }

                case KindErrorCategory.RandomizeModifiesConstant:
                    {
                        message += "Randomize statement is attempting to modify the " +
                                   "value of a constant.";
                        break;
                    }

                case KindErrorCategory.ReceiveModifiesConstant:
                    {
                        message += "Receive statement is attempting to modify the " +
                                   "value of a constant.";
                        break;
                    }

                case KindErrorCategory.NonProcedureInParallelStatement:
                    {
                        message += "Parallel statement is not used for procedure " +
                                   "invocation.";
                        break;
                    }

                case KindErrorCategory.ParallelProcedureHasNonVoidReturn:
                    {
                        message += "Procedure invoked in parallel statement does " +
                                   "not have a void return type.";
                        break;
                    }

                case KindErrorCategory.ParallelProcedureHasReferenceParameter:
                    {
                        message += "Procedure invoked in parallel statement contains " +
                                   "reference parameter.";
                        break;
                    }

                case KindErrorCategory.ParallelProcedureHasNoChannelParameter:
                    {
                        message += "Procedure invoked in parallel statement does not " +
                                   "contain any channel parameter.";
                        break;
                    }

                case KindErrorCategory.ParallelProcedureUsesIO:
                    {
                        message += "Procedure invoked in parallel statement uses input " +
                                   "or output statement(s).";
                        break;
                    }

                case KindErrorCategory.ParallelProcedureUsesNonLocals:
                    {
                        message += "Procedure invoked in parallel statement " +
                                   "uses non local variable(s).";
                        break;
                    }

                case KindErrorCategory.ParallelProcedureCallsUnfriendly:
                    {
                        message += "Procedure invoked in parallel statement invokes " +
                                   "another procedure that is not parallel friendly; " +
                                   "i.e. it either uses input / output statement(s), " +
                                   "or uses non local variable(s), or both.";
                        break;
                    }
            }

            PrintError("Kind", (int)category, message);
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
                        message += "The index for array declaration is not of type " +
                                   "integer.";
                        break;
                    }

                case TypeErrorCategory.NonIntegerArrayIndex:
                    {
                        message += "Index for array variable does not evaluate " +
                                   "to type integer.";
                        break;
                    }

                case TypeErrorCategory.TypeMismatchInAssignment:
                    {
                        message += "Types don't match on either side of " +
                                   "assignment operator.";
                        break;
                    }

                case TypeErrorCategory.NonBooleanInIfCondition:
                    {
                        message += "Conditional expression for if statement does not " +
                                   "evaluate to type boolean.";
                        break;
                    }

                case TypeErrorCategory.NonBooleanInWhileCondition:
                    {
                        message += "Conditional expression for while statement does not " +
                                   "evaluate to type boolean.";
                        break;
                    }

                case TypeErrorCategory.NonBooleanToTheRightOfNotOperator:
                    {
                        message += "Right hand side of Not operator does not " +
                                   "evaluate to type boolean.";
                        break;
                    }

                case TypeErrorCategory.MinusPrecedingNonIntegerInConstantDefinition:
                    {
                        message += "Minus symbol precedes a non-integer constant " +
                                   "in constant definition.";
                        break;
                    }

                case TypeErrorCategory.ParameterTypeMismatch:
                    {
                        message += "Type mismatch between an argument and its " +
                                   "corresponding parameter.";
                        break;
                    }

                case TypeErrorCategory.InvalidTypeInWriteStatement:
                    {
                        message += "Expression in write statement does not evaluate to " +
                                   "type integer or boolean.";
                        break;
                    }

                case TypeErrorCategory.InvalidTypeInReadStatement:
                    {
                        message += "Object in read statement is not of type integer " +
                                   "or boolean.";
                        break;
                    }

                case TypeErrorCategory.NonIntegerInRandomizeStatement:
                    {
                        message += "Object in randomize statement is not of type integer.";
                        break;
                    }

                case TypeErrorCategory.NonIntegerValueInSendStatement:
                    {
                        message += "Attempt to send a non-integer value.";
                        break;
                    }

                case TypeErrorCategory.NonChannelInSendStatement:
                    {
                        message += "Attempt to send a value through a non-channel " +
                                   "variable.";
                        break;
                    }

                case TypeErrorCategory.NonIntegerValueInReceiveStatement:
                    {
                        message += "Attempt to receive a non-integer value.";
                        break;
                    }

                case TypeErrorCategory.NonChannelInReceiveStatement:
                    {
                        message += "Attempt to receive a value through a non-channel " +
                                   "variable.";
                        break;
                    }

                case TypeErrorCategory.NonChannelInOpenStatement:
                    {
                        message += "Object in open statement is not of type channel.";
                        break;
                    }
            }

            PrintError("Type", (int)category, message);
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
    public void TypeError(Type type,
                          DiadicTypeErrorCategory category,
                          Symbol diadicOperator)
    {
        ErrorFree = false;

        if (type != Type.Universal)
        {
            string message = string.Empty;

            switch (category)
            {
                case DiadicTypeErrorCategory.TypeMismatchAcrossEqualityOperator:
                    {
                        message += "Types don't match on either side of equality " +
                                   "operator, " + diadicOperator.ToString() + ".";
                        break;
                    }

                case DiadicTypeErrorCategory.NonBooleanLeftOfLogicalOperator:
                    {
                        message += "Expression to the left of logical operator, " +
                                   diadicOperator.ToString() +
                                   ", does not evaluate to type boolean.";
                        break;
                    }

                case DiadicTypeErrorCategory.NonBooleanRightOfLogicalOperator:
                    {
                        message += "Expression to the right of logical operator, " +
                                   diadicOperator.ToString() +
                                   ", does not evaluate to type boolean.";
                        break;
                    }

                case DiadicTypeErrorCategory.NonIntegerLeftOfRelationalOperator:
                    {
                        message += "Expression to the left of relational operator, " +
                                   diadicOperator.ToString() +
                                   ", does not evaluate to type integer.";
                        break;
                    }

                case DiadicTypeErrorCategory.NonIntegerRightOfRelationalOperator:
                    {
                        message += "Expression to the right of relational operator, " +
                                   diadicOperator.ToString() +
                                   ", does not evaluate to type integer.";
                        break;
                    }

                case DiadicTypeErrorCategory.NonIntegerLeftOfAdditionOperator:
                    {
                        message += "Expression to the left of addition operator, " +
                                   diadicOperator.ToString() +
                                   ", does not evaluate to type integer.";
                        break;
                    }

                case DiadicTypeErrorCategory.NonIntegerRightOfAdditionOperator:
                    {
                        message += "Expression to the right of addition operator, " +
                                   diadicOperator.ToString() +
                                   ", does not evaluate to type integer.";
                        break;
                    }

                case DiadicTypeErrorCategory.NonIntegerLeftOfMultiplicationOperator:
                    {
                        message += "Expression to the left of multiplication operator, " +
                                   diadicOperator.ToString() +
                                   ", does not evaluate to type integer.";
                        break;
                    }

                case DiadicTypeErrorCategory.NonIntegerRightOfMultiplicationOperator:
                    {
                        message += "Expression to the right of multiplication operator, " +
                                   diadicOperator.ToString() +
                                   ", does not evaluate to type integer.";
                        break;
                    }

                case DiadicTypeErrorCategory.InvalidTypeAcrossEqualityOperator:
                    {
                        message += "Invalid type used across equality operator, " +
                                   diadicOperator.ToString();
                        break;
                    }
            }

            PrintError("Type", (int)category, message);
        }
    }

    /// <summary>
    /// Delegates the compilation error details to registered clients.
    /// </summary>
    /// <param name="errorCategory">Category of the compilation error.</param>
    /// <param name="errorCode">Error code.</param>
    /// <param name="errorMessage">Error message.</param>
    void PrintError(string errorCategory, int errorCode, string errorMessage)
    {
        PrintErrorReport?.Invoke(errorCategory, errorCode, errorMessage);
    }
}
