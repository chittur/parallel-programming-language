/******************************************************************************
 * Filename    = Opcode.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = Intermediary
 *
 * Description = Defines the enum "Opcode", which enumerates all the opcodes 
 *               supported in the intermediate language.
 *****************************************************************************/

namespace Intermediary;

/// <summary>
/// Enumerates all the opcodes supported in the intermediate language.
/// </summary>
public enum Opcode
{
    /// <summary>
    /// Unknown opcode
    /// </summary>
    Unknown = 1,

    /// <summary>
    /// Beginning of a program block.
    /// </summary>
    Program,

    /// <summary>
    /// End of a program block.
    /// </summary>
    EndProgram,

    /// <summary>
    /// Beginning of a new block.
    /// </summary>
    Block,

    /// <summary>
    /// End of a block.
    /// </summary>
    EndBlock,

    /// <summary>
    /// Beginning of a procedure definition block.
    /// </summary>
    ProcedureBlock,

    /// <summary>
    /// End of a procedure definition block.
    /// </summary>
    EndProcedureBlock,

    /// <summary>
    /// A procedure invocation.
    /// </summary>
    ProcedureInvocation,

    /// <summary>
    /// Indexed selector, for acessing an array variable.
    /// </summary>
    Index,

    /// <summary>
    /// A variable access.
    /// </summary>
    Variable,

    /// <summary>
    /// A reference parameter access.
    /// </summary>
    ReferenceParameter,

    /// <summary>
    /// A constant variable.
    /// </summary>
    Constant,

    /// <summary>
    /// Value of a variable.
    /// </summary>
    Value,

    /// <summary>
    /// Not operator.
    /// </summary>
    Not,

    /// <summary>
    /// And operator.
    /// </summary>
    And,

    /// <summary>
    /// Or operator.
    /// </summary>
    Or,

    /// <summary>
    /// Multiplication operator.
    /// </summary>
    Multiply,

    /// <summary>
    /// Division operator.
    /// </summary>
    Divide,

    /// <summary>
    /// Modulo operator.
    /// </summary>
    Modulo,

    /// <summary>
    /// Power operator.
    /// </summary>
    Power,

    /// <summary>
    /// Less operator.
    /// </summary>
    Less,

    /// <summary>
    /// LessOrEqual operator.
    /// </summary>
    LessOrEqual,

    /// <summary>
    /// Equality operator.
    /// </summary>
    Equal,

    /// <summary>
    /// Inequality operator.
    /// </summary>
    NotEqual,

    /// <summary>
    /// Greater operator.
    /// </summary>
    Greater,

    /// <summary>
    /// GreaterOrEqual operator.
    /// </summary>
    GreaterOrEqual,

    /// <summary>
    /// Addition operator.
    /// </summary>
    Add,

    /// <summary>
    /// Subtraction operator.
    /// </summary>
    Subtract,

    /// <summary>
    ///  Read operator for a boolean.
    /// </summary>
    ReadBoolean,

    /// <summary>
    /// Read operator for an integer.
    /// </summary>
    ReadInteger,

    /// <summary>
    /// Write operator for a boolean.
    /// </summary>
    WriteBoolean,

    /// <summary>
    /// Write operator for an integer.
    /// </summary>
    WriteInteger,

    /// <summary>
    /// Assignment operator; assigns value to a variable.
    /// </summary>
    Assign,

    /// <summary>
    /// Minus operator; value of the variable = -1 * value of the variable.
    /// </summary>
    Minus,

    /// <summary>
    /// Do operator; conditionally executes a statement,
    /// branch to the specified address otherwise.
    /// </summary>
    Do,

    /// <summary>
    /// Goto operator; unconditionally branch to the specified address.
    /// </summary>
    Goto,

    /// <summary>
    /// Open operator; opens up a channel for use.
    /// </summary>
    Open,

    /// <summary>
    /// Randomize operator; assigns a random value to an integer variable.
    /// </summary>
    Randomize,

    /// <summary>
    /// Send operator; sends an integer value through the channel.
    /// </summary>
    Send,

    /// <summary>
    /// Receive operator; receives an integer value through the channel.
    /// </summary>
    Receive,

    /// <summary>
    /// Parallel operator; spawns a new node that runs in parallel.
    /// </summary>
    Parallel
}
