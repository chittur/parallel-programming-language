/******************************************************************************
 * Filename    = Assembler.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = GrammarAnalysis
 *
 * Description = Defines the class "Assembler", which provides methods to
 *               generate the intermediate code.
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using ErrorReporting;
using Intermediary;

namespace CodeGeneration;

/// <summary>
/// Provides methods to assemble the intermediate code.
/// </summary>
public class Assembler
{
    private const int Max = 10000;              // Maximum Size of the assembly table.
    private readonly List<int> _assemblyTable;  // The assembly table.
    private readonly Annotator _annotator;      // Annotator instance, for error reporting.

    /// <summary>
    /// Creates an instance of Assembler, which provides methods to
    /// generate the intermediate code.
    /// </summary>
    /// <param name="errorReporter">
    /// Annotator instance for error reporting.
    /// </param>
    public Assembler(Annotator errorReporter)
    {
        CurrentAddress = 0;
        _assemblyTable = [];
        _annotator = errorReporter;
    }

    /// <summary>
    /// Writes the intermediate code to the specified file.
    /// </summary>
    /// <param name="filename">Name of the file.</param>
    public void GenerateExecutable(string filename)
    {
        using FileStream stream = new FileStream(filename,
                                                  FileMode.Create,
                                                  FileAccess.Write);
        using (StreamWriter writer = new StreamWriter(stream))
        {
            foreach (int code in _assemblyTable)
            {
                writer.WriteLine(code);
            }

            writer.Close();
        }

        stream.Close();
    }

    /// <summary>
    /// Gets the current address.
    /// </summary>
    public int CurrentAddress { get; private set; }

    /// <summary>
    /// Populates the specified location with the current address.
    /// </summary>
    /// <param name="label">
    /// The label number (which in turn is the address of another location).
    /// </param>
    public void ResolveAddress(int label)
    {
        _assemblyTable[label] = CurrentAddress;
    }

    /// <summary>
    /// Populates the specified location with the given value.
    /// </summary>
    /// <param name="label">
    /// The label number (which in turn is the address of another location).
    /// </param>
    /// <param name="value">
    /// The value to populate the specified location with.
    /// </param>
    public void ResolveArgument(int label, int value)
    {
        _assemblyTable[label] = value;
    }

    /// <summary>
    /// Processes start of program block.
    /// </summary>
    /// <param name="objectsLength">
    /// Total length of the objects in the block.
    /// </param>
    public void Program(int objectsLength)
    {
        Emit(Opcode.Program, objectsLength);
    }

    /// <summary>
    /// Processes end of program block.
    /// </summary>
    public void EndProgram()
    {
        Emit(Opcode.EndProgram);
    }

    /// <summary>
    /// Processes start of a procedure block.
    /// </summary>
    /// <param name="objectsLength">
    /// Total length of the objects in the block.
    /// </param>
    public void ProcedureBlock(int objectsLength)
    {
        Emit(Opcode.ProcedureBlock, objectsLength);
    }

    /// <summary>
    /// Processes end of a procedure block.
    /// </summary>
    /// <param name="parametersLength">
    /// Total length of the parameters.
    /// </param>
    public void EndProcedureBlock(int parametersLength)
    {
        Emit(Opcode.EndProcedureBlock, parametersLength);
    }

    /// <summary>
    /// Processes procedure invocation.
    /// </summary>
    /// <param name="level">
    /// Relative level of the procedure invocation.
    /// </param>
    /// <param name="startAddress">
    /// Start address of the procedure block.
    /// </param>
    public void ProcedureInvocation(int level, int startAddress)
    {
        Emit(Opcode.ProcedureInvocation, level, startAddress);
    }

    /// <summary>
    /// Processes start of a block.
    /// </summary>
    /// <param name="objectsLength">
    /// Total length of the objects in the block.
    /// </param>
    public void Block(int objectsLength)
    {
        Emit(Opcode.Block, objectsLength);
    }

    /// <summary>
    /// Processes end of a block.
    /// </summary>
    public void EndBlock()
    {
        Emit(Opcode.EndBlock);
    }

    /// <summary>
    /// Process a variable access.
    /// </summary>
    /// <param name="level">scope level of the variable.</param>
    /// <param name="displacement">displacement of the variable.</param>
    public void Variable(int level, int displacement)
    {
        Emit(Opcode.Variable, level, displacement);
    }

    /// <summary>
    /// Process a reference parameter access.
    /// </summary>
    /// <param name="level">scope level of the parameter.</param>
    /// <param name="displacement">displacement of the parameter.</param>
    public void ReferenceParameter(int level, int displacement)
    {
        Emit(Opcode.ReferenceParameter, level, displacement);
    }

    /// <summary>
    /// Processes the indexed selector, for access of an array variable.
    /// </summary>
    /// <param name="upperBound">upper bound of the array.</param>
    public void Index(int upperBound)
    {
        Emit(Opcode.Index, upperBound);
    }

    /// <summary>
    /// Processes a constant.
    /// </summary>
    /// <param name="value">Value of the constant.</param>
    public void Constant(int value)
    {
        Emit(Opcode.Constant, value);
    }

    /// <summary>
    /// Assigns value on top of stack to the variable.
    /// </summary>
    public void Value()
    {
        Emit(Opcode.Value);
    }

    /// <summary>
    /// Conditionally executes a statement;
    /// branches to the specified address otherwise.
    /// </summary>
    /// <param name="label">
    /// Address to branch to in case the conditional expression returns false.
    /// </param>
    public void Do(int label)
    {
        Emit(Opcode.Do, label);
    }

    /// <summary>
    /// Unconditionally branches to the specified address.
    /// </summary>
    /// <param name="label">The address to branch to.</param>
    public void Goto(int label)
    {
        Emit(Opcode.Goto, label);
    }

    /// <summary>
    /// Assigns value to variable.
    /// </summary>
    /// <param name="total">Total number of assignments to make.</param>
    public void Assign(int total)
    {
        Emit(Opcode.Assign, total);
    }

    /// <summary>
    /// Processes reading of a boolean value.
    /// </summary>
    public void ReadBoolean()
    {
        Emit(Opcode.ReadBoolean);
    }

    /// <summary>
    /// Processes reading of an integer value.
    /// </summary>
    public void ReadInteger()
    {
        Emit(Opcode.ReadInteger);
    }

    /// <summary>
    /// Processes writing of a boolean value.
    /// </summary>
    public void WriteBoolean()
    {
        Emit(Opcode.WriteBoolean);
    }

    /// <summary>
    /// Processes writing of an integer value.
    /// </summary>
    public void WriteInteger()
    {
        Emit(Opcode.WriteInteger);
    }

    /// <summary>
    /// Processes Minus operator (-1 * value of the variable).
    /// </summary>
    public void Minus()
    {
        Emit(Opcode.Minus);
    }

    /// <summary>
    /// Processes Addition operator.
    /// </summary>
    public void Add()
    {
        Emit(Opcode.Add);
    }

    /// <summary>
    /// Processes Subtraction operator.
    /// </summary>
    public void Subtract()
    {
        Emit(Opcode.Subtract);
    }

    /// <summary>
    /// Processes Less operator.
    /// </summary>
    public void Less()
    {
        Emit(Opcode.Less);
    }

    /// <summary>
    /// Processes LessOrEqual operator.
    /// </summary>
    public void LessOrEqual()
    {
        Emit(Opcode.LessOrEqual);
    }

    /// <summary>
    /// Processes Equal operator.
    /// </summary>
    public void Equal()
    {
        Emit(Opcode.Equal);
    }

    /// <summary>
    /// Processes NotEqual operator.
    /// </summary>
    public void NotEqual()
    {
        Emit(Opcode.NotEqual);
    }

    /// <summary>
    /// Processes Greater operator.
    /// </summary>
    public void Greater()
    {
        Emit(Opcode.Greater);
    }

    /// <summary>
    /// Processes GreaterOrEqual operator.
    /// </summary>
    public void GreaterOrEqual()
    {
        Emit(Opcode.GreaterOrEqual);
    }

    /// <summary>
    /// Processes And operator.
    /// </summary>
    public void And()
    {
        Emit(Opcode.And);
    }

    /// <summary>
    /// Processes Or operator.
    /// </summary>
    public void Or()
    {
        Emit(Opcode.Or);
    }

    /// <summary>
    /// Processes Not operator.
    /// </summary>
    public void Not()
    {
        Emit(Opcode.Not);
    }

    /// <summary>
    /// Processes Multiply operator.
    /// </summary>
    public void Multiply()
    {
        Emit(Opcode.Multiply);
    }

    /// <summary>
    /// Processes Divide operator.
    /// </summary>
    public void Divide()
    {
        Emit(Opcode.Divide);
    }

    /// <summary>
    /// Processes Modulo operator.
    /// </summary>
    public void Modulo()
    {
        Emit(Opcode.Modulo);
    }

    /// <summary>
    /// Processes Power operator.
    /// </summary>
    public void Power()
    {
        Emit(Opcode.Power);
    }

    /// <summary>
    /// Opens up a channel for use.
    /// </summary>
    public void Open()
    {
        Emit(Opcode.Open);
    }

    /// <summary>
    /// Assigns a random value to an integer variable.
    /// </summary>
    public void Randomize()
    {
        Emit(Opcode.Randomize);
    }

    /// <summary>
    /// Processes sending an integer value through a channel.
    /// </summary>
    public void Send()
    {
        Emit(Opcode.Send);
    }

    /// <summary>
    /// Processes receiving an integer value through a channel.
    /// </summary>
    public void Receive()
    {
        Emit(Opcode.Receive);
    }

    /// <summary>
    /// Processes spawning a new node that runs in parallel.
    /// </summary>
    public void Parallel()
    {
        Emit(Opcode.Parallel);
    }

    /// <summary>
    /// Emits the specified code.
    /// </summary>
    /// <param name="opcode">The opcode to be emitted.</param>
    /// <param name="arguments">Values to be emitted.</param>
    private void Emit(Opcode opcode, params int[] arguments)
    {
        EmitCode(Convert.ToInt32(opcode));
        foreach (int argument in arguments)
        {
            EmitCode(argument);
        }
    }

    /// <summary>
    /// Emits the specified code.
    /// </summary>
    /// <param name="value">The code to be emitted.</param>
    private void EmitCode(int value)
    {
        if (CurrentAddress >= Assembler.Max)
        {
            _annotator.InternalError(InternalErrorCategory.AssemblyTableIsFull);
        }
        else
        {
            _assemblyTable.Add(value);
            ++CurrentAddress;
        }
    }
}
