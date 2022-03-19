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

using ErrorReporting;
using Intermediary;
using System;
using System.Collections.Generic;
using System.IO;

namespace CodeGeneration
{
    /// <summary>
    /// Provides methods to assemble the intermediate code.
    /// </summary>
    public class Assembler
    {
        const int Max = 10000;             // Maximum Size of the assembly table.
        readonly List<int> assemblyTable;  // The assembly table.
        readonly Annotator annotator;      // Annotator instance, for error reporting.

        /// <summary>
        /// Creates an instance of Assembler, which provides methods to
        /// generate the intermediate code.
        /// </summary>
        /// <param name="errorReporter">
        /// Annotator instance for error reporting.
        /// </param>
        public Assembler(Annotator errorReporter)
        {
            this.CurrentAddress = 0;
            this.assemblyTable = new List<int>();
            this.annotator = errorReporter;
        }

        /// <summary>
        /// Writes the intermediate code to the specified file.
        /// </summary>
        /// <param name="filename">Name of the file.</param>
        public void GenerateExecutable(string filename)
        {
            using (FileStream stream = new FileStream(filename,
                                                      FileMode.Create,
                                                      FileAccess.Write))
            {
                using (TextWriter writer = new StreamWriter(stream))
                {
                    foreach (int code in this.assemblyTable)
                    {
                        writer.WriteLine(code);
                    }

                    writer.Close();
                }

                stream.Close();
            }
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
            this.assemblyTable[label] = this.CurrentAddress;
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
            this.assemblyTable[label] = value;
        }

        /// <summary>
        /// Processes start of program block.
        /// </summary>
        /// <param name="objectsLength">
        /// Total length of the objects in the block.
        /// </param>
        public void Program(int objectsLength)
        {
            this.Emit(Opcode.Program, objectsLength);
        }

        /// <summary>
        /// Processes end of program block.
        /// </summary>
        public void EndProgram()
        {
            this.Emit(Opcode.EndProgram);
        }

        /// <summary>
        /// Processes start of a procedure block.
        /// </summary>
        /// <param name="objectsLength">
        /// Total length of the objects in the block.
        /// </param>
        public void ProcedureBlock(int objectsLength)
        {
            this.Emit(Opcode.ProcedureBlock, objectsLength);
        }

        /// <summary>
        /// Processes end of a procedure block.
        /// </summary>
        /// <param name="parametersLength">
        /// Total length of the parameters.
        /// </param>
        public void EndProcedureBlock(int parametersLength)
        {
            this.Emit(Opcode.EndProcedureBlock, parametersLength);
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
            this.Emit(Opcode.ProcedureInvocation, level, startAddress);
        }

        /// <summary>
        /// Processes start of a block.
        /// </summary>
        /// <param name="objectsLength">
        /// Total length of the objects in the block.
        /// </param>
        public void Block(int objectsLength)
        {
            this.Emit(Opcode.Block, objectsLength);
        }

        /// <summary>
        /// Processes end of a block.
        /// </summary>
        public void EndBlock()
        {
            this.Emit(Opcode.EndBlock);
        }

        /// <summary>
        /// Process a variable access.
        /// </summary>
        /// <param name="level">scope level of the variable.</param>
        /// <param name="displacement">displacement of the variable.</param>
        public void Variable(int level, int displacement)
        {
            this.Emit(Opcode.Variable, level, displacement);
        }

        /// <summary>
        /// Process a reference parameter access.
        /// </summary>
        /// <param name="level">scope level of the parameter.</param>
        /// <param name="displacement">displacement of the parameter.</param>
        public void ReferenceParameter(int level, int displacement)
        {
            this.Emit(Opcode.ReferenceParameter, level, displacement);
        }

        /// <summary>
        /// Processes the indexed selector, for access of an array variable.
        /// </summary>
        /// <param name="upperBound">upper bound of the array.</param>
        public void Index(int upperBound)
        {
            this.Emit(Opcode.Index, upperBound);
        }

        /// <summary>
        /// Processes a constant.
        /// </summary>
        /// <param name="value">Value of the constant.</param>
        public void Constant(int value)
        {
            this.Emit(Opcode.Constant, value);
        }

        /// <summary>
        /// Assigns value on top of stack to the variable.
        /// </summary>
        public void Value()
        {
            this.Emit(Opcode.Value);
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
            this.Emit(Opcode.Do, label);
        }

        /// <summary>
        /// Unconditionally branches to the specified address.
        /// </summary>
        /// <param name="label">The address to branch to.</param>
        public void Goto(int label)
        {
            this.Emit(Opcode.Goto, label);
        }

        /// <summary>
        /// Assigns value to variable.
        /// </summary>
        /// <param name="total">Total number of assignments to make.</param>
        public void Assign(int total)
        {
            this.Emit(Opcode.Assign, total);
        }

        /// <summary>
        /// Processes reading of a boolean value.
        /// </summary>
        public void ReadBoolean()
        {
            this.Emit(Opcode.ReadBoolean);
        }

        /// <summary>
        /// Processes reading of an integer value.
        /// </summary>
        public void ReadInteger()
        {
            this.Emit(Opcode.ReadInteger);
        }

        /// <summary>
        /// Processes writing of a boolean value.
        /// </summary>
        public void WriteBoolean()
        {
            this.Emit(Opcode.WriteBoolean);
        }

        /// <summary>
        /// Processes writing of an integer value.
        /// </summary>
        public void WriteInteger()
        {
            this.Emit(Opcode.WriteInteger);
        }

        /// <summary>
        /// Processes Minus operator (-1 * value of the variable).
        /// </summary>
        public void Minus()
        {
            this.Emit(Opcode.Minus);
        }

        /// <summary>
        /// Processes Addition operator.
        /// </summary>
        public void Add()
        {
            this.Emit(Opcode.Add);
        }

        /// <summary>
        /// Processes Subtraction operator.
        /// </summary>
        public void Subtract()
        {
            this.Emit(Opcode.Subtract);
        }

        /// <summary>
        /// Processes Less operator.
        /// </summary>
        public void Less()
        {
            this.Emit(Opcode.Less);
        }

        /// <summary>
        /// Processes LessOrEqual operator.
        /// </summary>
        public void LessOrEqual()
        {
            this.Emit(Opcode.LessOrEqual);
        }

        /// <summary>
        /// Processes Equal operator.
        /// </summary>
        public void Equal()
        {
            this.Emit(Opcode.Equal);
        }

        /// <summary>
        /// Processes NotEqual operator.
        /// </summary>
        public void NotEqual()
        {
            this.Emit(Opcode.NotEqual);
        }

        /// <summary>
        /// Processes Greater operator.
        /// </summary>
        public void Greater()
        {
            this.Emit(Opcode.Greater);
        }

        /// <summary>
        /// Processes GreaterOrEqual operator.
        /// </summary>
        public void GreaterOrEqual()
        {
            this.Emit(Opcode.GreaterOrEqual);
        }

        /// <summary>
        /// Processes And operator.
        /// </summary>
        public void And()
        {
            this.Emit(Opcode.And);
        }

        /// <summary>
        /// Processes Or operator.
        /// </summary>
        public void Or()
        {
            this.Emit(Opcode.Or);
        }

        /// <summary>
        /// Processes Not operator.
        /// </summary>
        public void Not()
        {
            this.Emit(Opcode.Not);
        }

        /// <summary>
        /// Processes Multiply operator.
        /// </summary>
        public void Multiply()
        {
            this.Emit(Opcode.Multiply);
        }

        /// <summary>
        /// Processes Divide operator.
        /// </summary>
        public void Divide()
        {
            this.Emit(Opcode.Divide);
        }

        /// <summary>
        /// Processes Modulo operator.
        /// </summary>
        public void Modulo()
        {
            this.Emit(Opcode.Modulo);
        }

        /// <summary>
        /// Processes Power operator.
        /// </summary>
        public void Power()
        {
            this.Emit(Opcode.Power);
        }

        /// <summary>
        /// Opens up a channel for use.
        /// </summary>
        public void Open()
        {
            this.Emit(Opcode.Open);
        }

        /// <summary>
        /// Assigns a random value to an integer variable.
        /// </summary>
        public void Randomize()
        {
            this.Emit(Opcode.Randomize);
        }

        /// <summary>
        /// Processes sending an integer value through a channel.
        /// </summary>
        public void Send()
        {
            this.Emit(Opcode.Send);
        }

        /// <summary>
        /// Processes receiving an integer value through a channel.
        /// </summary>
        public void Receive()
        {
            this.Emit(Opcode.Receive);
        }

        /// <summary>
        /// Processes spawning a new node that runs in parallel.
        /// </summary>
        public void Parallel()
        {
            this.Emit(Opcode.Parallel);
        }

        /// <summary>
        /// Emits the specified code.
        /// </summary>
        /// <param name="opcode">The opcode to be emitted.</param>
        /// <param name="arguments">Values to be emitted.</param>
        void Emit(Opcode opcode, params int[] arguments)
        {
            this.EmitCode(Convert.ToInt32(opcode));
            foreach (int argument in arguments)
            {
                this.EmitCode(argument);
            }
        }

        /// <summary>
        /// Emits the specified code.
        /// </summary>
        /// <param name="value">The code to be emitted.</param>
        void EmitCode(int value)
        {
            if (this.CurrentAddress >= Assembler.Max)
            {
                this.annotator.InternalError(InternalErrorCategory.AssemblyTableIsFull);
            }
            else
            {
                this.assemblyTable.Add(value);
                this.CurrentAddress = this.CurrentAddress + 1;
            }
        }
    }
}