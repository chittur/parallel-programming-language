/******************************************************************************
 * Filename    = ErrorCategory.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = ErrorReporting
 *
 * Description = Defines several enumerations, which enumerate various 
 *               categories of compilation errors.
 *****************************************************************************/

namespace ErrorReporting;

/// <summary>
/// Enumerates errors caused by the compiler itself.
/// </summary>
public enum InternalErrorCategory
{
    /// <summary>
    /// An invalid symbol has been passed to generate operation code.
    /// </summary>
    InvalidOperationSymbol = 1,

    /// <summary>
    /// Assembly table is full; the compiler has run out of memory.
    /// </summary>
    AssemblyTableIsFull = 2,

    /// <summary>
    /// Internal processing error.
    /// </summary>
    InternalProcessingError = 3
}

/// <summary>
/// Enumerates generic syntax errors.
/// </summary>
public enum GenericErrorCategory
{
    /// <summary>
    /// Generic syntax error.
    /// </summary>
    GenericSyntaxError = 100,
}

/// <summary>
/// Enumerates scope errors.
/// </summary>
public enum ScopeErrorCategory
{
    /// <summary>
    /// The name is undefined in the current scope.
    /// </summary>
    UndefinedName = 201,

    /// <summary>
    /// The name has been declared already.
    /// </summary>
    AmbiguousName = 202
}

/// <summary>
/// Enumerates kind errors.
/// </summary>
public enum KindErrorCategory
{
    /// <summary>
    /// Right hand side entity in constant definition is not a constant.
    /// </summary>
    NonConstantInConstantDefinition = 301,

    /// <summary>
    /// Assignment statement is attempting to modify the value of a constant.
    /// </summary>
    AssignmentModifiesConstant = 302,

    /// <summary>
    /// Variable of kind array is missing indexed selector.
    /// </summary>
    ArrayVariableMissingIndexedSelector = 303,

    /// <summary>
    /// The index for array declaration does not evaluate 
    /// to a positive integer.
    /// </summary>
    NonPositiveIntegerIndexInArrayDeclaration = 304,

    /// <summary>
    /// A procedure name is used where an object name was expected.
    /// </summary>
    ProcedureAccessedAsObject = 305,

    /// <summary>
    /// The argument count in procedure invocation does not tally with 
    /// the parameter count specified in its signature.
    /// </summary>
    ArgumentCountMismatch = 306,

    /// <summary>
    /// Mismatch in reference-value parameter kind between an argument and 
    /// its corresponding parameter.
    /// </summary>
    ParameterKindMismatch = 307,

    /// <summary>
    /// A constant object is being passed as reference parameter.
    /// </summary>
    ConstantPassedAsReferenceParameter = 308,

    /// <summary>
    /// The expression list count to the right of the assignment operator
    /// does not tally with the variable access count to the left of it.
    /// </summary>
    AssignmentCountMismatch = 309,

    /// <summary>
    /// Read statement is attempting to modify the value of a constant.
    /// </summary>
    ReadModifiesConstant = 310,

    /// <summary>
    /// Randomize statement is attempting to modify the value of a constant.
    /// </summary>
    RandomizeModifiesConstant = 311,

    /// <summary>
    /// Receive statement is attempting to modify the value of a constant.
    /// </summary>
    ReceiveModifiesConstant = 312,

    /// <summary>
    /// Parallel statement is not used for procedure invocation.
    /// </summary>
    NonProcedureInParallelStatement = 313,

    /// <summary>
    /// Procedure invoked in parallel statement does not have a 
    /// void return type.
    /// </summary>
    ParallelProcedureHasNonVoidReturn = 314,

    /// <summary>
    /// Procedure invoked in parallel statement contains reference parameter.
    /// </summary>
    ParallelProcedureHasReferenceParameter = 315,

    /// <summary>
    /// Procedure invoked in parallel statement does not contain any channel
    /// parameter.
    /// </summary>
    ParallelProcedureHasNoChannelParameter = 316,

    /// <summary>
    /// Procedure invoked in parallel statement uses input or output 
    /// statement(s).
    /// </summary>
    ParallelProcedureUsesIO = 317,

    /// <summary>
    /// Procedure invoked in parallel statement uses non local variable(s).
    /// </summary>
    ParallelProcedureUsesNonLocals = 318,

    /// <summary>
    /// Procedure invoked in parallel statement invokes another procedure that
    /// is not parallel friendly; i.e. it either uses input / output 
    /// statement(s), or uses non local variable(s), or both.
    /// </summary>
    ParallelProcedureCallsUnfriendly = 319,

    /// <summary>
    /// The previous procedure block employs parallel recursion, but uses
    /// input or output statement(s).
    /// </summary>
    ParallelRecursionUsesIO = 320,

    /// <summary>
    /// The previous procedure block employs parallel recursion, but uses
    /// non local variable(s).
    /// </summary>
    ParallelRecursionUsesNonLocals = 321,

    /// <summary>
    /// The previous procedure block employs parallel recursion, but invokes
    /// another procedure that is not parallel friendly; i.e. it either uses
    /// input / output statement(s), or uses non local variable(s), or both.
    /// </summary>
    ParallelRecursionCallsUnfriendly = 322
}

/// <summary>
/// Enumerates type errors.
/// </summary>
public enum TypeErrorCategory
{
    /// <summary>
    /// The index for array declaration is not of type integer.
    /// </summary>
    NonIntegerIndexInArrayDeclaration = 401,

    /// <summary>
    /// Index for array variable does not evaluate to type integer.
    /// </summary>
    NonIntegerArrayIndex = 402,

    /// <summary>
    /// Types don't match on either side of assignment operator.
    /// </summary>
    TypeMismatchInAssignment = 403,

    /// <summary>
    /// Conditional expression for if statement does not evaluate 
    /// to type boolean.
    /// </summary>
    NonBooleanInIfCondition = 404,

    /// <summary>
    /// Conditional expression for while statement does not evaluate 
    /// to type boolean.
    /// </summary>
    NonBooleanInWhileCondition = 405,

    /// <summary>
    /// Right hand side of Not operator does not evaluate to type boolean.
    /// </summary>
    NonBooleanToTheRightOfNotOperator = 406,

    /// <summary>
    /// Minus symbol precedes a non-integer constant in constant definition.
    /// </summary>
    MinusPrecedingNonIntegerInConstantDefinition = 407,

    /// <summary>
    /// Type mismatch between an argument and its corresponding parameter.
    /// </summary>
    ParameterTypeMismatch = 408,

    /// <summary>
    /// Expression in write statement does not evaluate to type
    /// integer or boolean.
    /// </summary>
    InvalidTypeInWriteStatement = 409,

    /// <summary>
    /// Object in read statement is not of type integer or boolean.
    /// </summary>
    InvalidTypeInReadStatement = 410,

    /// <summary>
    /// Object in randomize statement is not of type integer.
    /// </summary>
    NonIntegerInRandomizeStatement = 411,

    /// <summary>
    /// Attempt to send a non-integer value.
    /// </summary>
    NonIntegerValueInSendStatement = 412,

    /// <summary>
    /// Attempt to send a value through a non-channel variable.
    /// </summary>
    NonChannelInSendStatement = 413,

    /// <summary>
    /// Attempt to receive a non-integer value.
    /// </summary>
    NonIntegerValueInReceiveStatement = 414,

    /// <summary>
    /// Attempt to receive a value through a non-channel variable.
    /// </summary>
    NonChannelInReceiveStatement = 415,

    /// <summary>
    /// Object in open statement is not of type channel.
    /// </summary>
    NonChannelInOpenStatement = 416
}

/// <summary>
/// Enumerates type errors for diadic operators.
/// </summary>
public enum DiadicTypeErrorCategory
{
    /// <summary>
    /// Types don't match on either side of equality operator.
    /// </summary>
    TypeMismatchAcrossEqualityOperator = 451,

    /// <summary>
    /// Expression to the left of logical operator does not evaluate
    /// to type boolean.
    /// </summary>
    NonBooleanLeftOfLogicalOperator = 452,

    /// <summary>
    /// Expression to the right of logical operator does not evaluate
    /// to type boolean.
    /// </summary>
    NonBooleanRightOfLogicalOperator = 453,

    /// <summary>
    /// Expression to the left of relational operator does not evaluate
    /// to type integer.
    /// </summary>
    NonIntegerLeftOfRelationalOperator = 454,

    /// <summary>
    /// Expression to the right of relational operator does not evaluate
    /// to type integer.
    /// </summary>
    NonIntegerRightOfRelationalOperator = 455,

    /// <summary>
    /// Expression to the left of addition operator does not evaluate 
    /// to type integer.
    /// </summary>
    NonIntegerLeftOfAdditionOperator = 456,

    /// <summary>
    /// Expression to the right of addition operator does not evaluate 
    /// to type integer.
    /// </summary>
    NonIntegerRightOfAdditionOperator = 457,

    /// <summary>
    /// Expression to the left of multiplication operator does not evaluate
    /// to type integer.
    /// </summary>
    NonIntegerLeftOfMultiplicationOperator = 458,

    /// <summary>
    /// Expression to the right of multiplication operator does not evaluate
    /// to type integer.
    /// </summary>
    NonIntegerRightOfMultiplicationOperator = 459,

    /// <summary>
    /// Invalid type used across equality operator.
    /// </summary>
    InvalidTypeAcrossEqualityOperator = 460
}
