﻿/******************************************************************************
 * Filename    = Translator.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = Runtime
 *
 * Description = Defines the class "Translator", which provides methods to
 *               interpret and execute the intermediate code, in parallel
 *               if required.
 *****************************************************************************/

using System;
using System.Diagnostics;
using Intermediary;
using Parallelism;

namespace Runtime;

/// <summary>
/// Provides methods to interpret and execute the intermediate code,
/// in parallel if required. Derives from the abstract class for
/// parallel nodes - "Node".
/// </summary>
internal class Translator : Node
{
    private int _programRegister;      // Points to the current instruction.  
    private int _baseRegister;         // Points to the base of the current context.
    private int _stackRegister;        // Points to the top of the stack.
    private readonly int[] _stack;     // Stack.
    private bool _running;             // Running mode?
    private bool _mainProcedure;       // Main procedure for the node?
    private readonly Interpreter _interpreter;  // The interpreter instance.

    // Exception messages.
    internal const string IncorrectOpcodeMessage = "Incorrect opcode.";
    internal const string ArrayIndexOutOfBoundsMessage = "Array index is out of bounds.";
    internal const string BooleanInputIncorrectFormatMessage = "Boolean input was not in the correct format.";
    internal const string SendThroughUnopenedChannelMessage = "Attempt to send through an un-opened channel.";
    internal const string ReceiveThroughUnopenedChannelMessage = "Attempt to receive through an un-opened channel.";

    /// <summary>
    /// Creates an instance of Translator, which provides methods to interpret
    /// and execute the intermediate code, in parallel if required.
    /// </summary>
    /// <param name="root">The interpreter instance</param>
    /// <param name="currentStack">Stack</param>
    /// <param name="startAddress">start address</param>
    /// <param name="baseAddress">base address</param>
    /// <param name="stackAddress">stack address</param>
    public Translator(Interpreter root,
                      int[] currentStack,
                      int startAddress,
                      int baseAddress,
                      int stackAddress)
    {
        _interpreter = root;
        _programRegister = startAddress;
        _baseRegister = baseAddress;
        _stackRegister = stackAddress;
        _mainProcedure = false;
        _stack = new int[Interpreter.Max];
        if (currentStack != null)
        {
            Array.Copy(currentStack, _stack, currentStack.Length);
        }
    }

    /// <summary>
    /// Invokes the node's "Run" function in a new thread.
    /// </summary>
    protected override void Start()
    {
        _mainProcedure = true;
        base.Start();
    }

    /// <summary>
    /// Executes the node function - interpreting the intermediate code.
    /// </summary>
    public override void Run()
    {
        try
        {
            Translate();
        }
        catch (Exception exception)
        {
            _running = false;
            _interpreter.HandleException(exception.Message);
        }
    }

    /// <summary>
    /// Interprets the intermediate code.
    /// </summary>
    private void Translate()
    {
        _running = true;

        // Run the program.
        while (_running && _interpreter.NoGlobalErrors)
        {
            // Check for programming getting too big.
            Debug.Assert((_programRegister < Interpreter.Max), "Stack overflow.");

            Opcode opcode = (Opcode)_interpreter.Store[_programRegister];
            switch (opcode)
            {
                case Opcode.Program:
                    {
                        Program(_interpreter.Store[_programRegister + 1]);
                        break;
                    }

                case Opcode.EndProgram:
                    {
                        EndProgram();
                        break;
                    }

                case Opcode.Block:
                    {
                        Block(_interpreter.Store[_programRegister + 1]);
                        break;
                    }

                case Opcode.EndBlock:
                    {
                        EndBlock();
                        break;
                    }

                case Opcode.ProcedureBlock:
                    {
                        ProcedureBlock(_interpreter.Store[_programRegister + 1]);
                        break;
                    }

                case Opcode.ProcedureInvocation:
                    {
                        ProcedureInvocation(_interpreter.Store[_programRegister + 1],
                                            _interpreter.Store[_programRegister + 2]);
                        break;
                    }

                case Opcode.EndProcedureBlock:
                    {
                        EndProcedureBlock(_interpreter.Store[_programRegister + 1]);
                        break;
                    }

                case Opcode.Index:
                    {
                        Index(_interpreter.Store[_programRegister + 1]);
                        break;
                    }

                case Opcode.Variable:
                    {
                        Variable(_interpreter.Store[_programRegister + 1],
                                 _interpreter.Store[_programRegister + 2]);
                        break;
                    }

                case Opcode.ReferenceParameter:
                    {
                        ReferenceParameter(_interpreter.Store[_programRegister + 1],
                                           _interpreter.Store[_programRegister + 2]);
                        break;
                    }

                case Opcode.Constant:
                    {
                        Constant(_interpreter.Store[_programRegister + 1]);
                        break;
                    }

                case Opcode.Value:
                    {
                        Value();
                        break;
                    }

                case Opcode.Not:
                    {
                        Not();
                        break;
                    }

                case Opcode.And:
                    {
                        And();
                        break;
                    }

                case Opcode.Or:
                    {
                        Or();
                        break;
                    }

                case Opcode.Multiply:
                    {
                        Multiply();
                        break;
                    }

                case Opcode.Divide:
                    {
                        Divide();
                        break;
                    }

                case Opcode.Modulo:
                    {
                        Modulo();
                        break;
                    }

                case Opcode.Power:
                    {
                        Power();
                        break;
                    }

                case Opcode.Less:
                    {
                        Less();
                        break;
                    }

                case Opcode.LessOrEqual:
                    {
                        LessOrEqual();
                        break;
                    }

                case Opcode.Equal:
                    {
                        Equal();
                        break;
                    }

                case Opcode.NotEqual:
                    {
                        NotEqual();
                        break;
                    }

                case Opcode.Greater:
                    {
                        Greater();
                        break;
                    }

                case Opcode.GreaterOrEqual:
                    {
                        GreaterOrEqual();
                        break;
                    }

                case Opcode.Add:
                    {
                        Add();
                        break;
                    }

                case Opcode.Subtract:
                    {
                        Subtract();
                        break;
                    }

                case Opcode.ReadBoolean:
                    {
                        ReadBoolean();
                        break;
                    }

                case Opcode.ReadInteger:
                    {
                        ReadInteger();
                        break;
                    }

                case Opcode.WriteBoolean:
                    {
                        WriteBoolean();
                        break;
                    }

                case Opcode.WriteInteger:
                    {
                        WriteInteger();
                        break;
                    }

                case Opcode.Assign:
                    {
                        Assign(_interpreter.Store[_programRegister + 1]);
                        break;
                    }

                case Opcode.Minus:
                    {
                        Minus();
                        break;
                    }

                case Opcode.Do:
                    {
                        Do(_interpreter.Store[_programRegister + 1]);
                        break;
                    }

                case Opcode.Goto:
                    {
                        Goto(_interpreter.Store[_programRegister + 1]);
                        break;
                    }

                case Opcode.Open:
                    {
                        Open();
                        break;
                    }

                case Opcode.Randomize:
                    {
                        Randomize();
                        break;
                    }

                case Opcode.Send:
                    {
                        Send();
                        break;
                    }

                case Opcode.Receive:
                    {
                        Receive();
                        break;
                    }

                case Opcode.Parallel:
                    {
                        Parallel();
                        break;
                    }

                default:
                    {
                        throw new Exception(IncorrectOpcodeMessage);
                    }
            }
        }
    }

    /// <summary>
    /// Allocates the specified number of locations for the stack. 
    /// </summary>
    /// <param name="words">Number of locations to allocate.</param>
    private void Allocate(int words)
    {
        _stackRegister += words;
        Debug.Assert(_stackRegister < Interpreter.Max, "Stack overflow.");
    }

    /// <summary>
    /// Processes a variable.
    /// </summary>
    /// <param name="level">Relative level of the variable.</param>
    /// <param name="displacement">Displacement of the variable.</param>
    private void Variable(int level, int displacement)
    {
        Allocate(1);
        int x = _baseRegister;
        while (level > 0)
        {
            x = _stack[x];
            --level;
        }

        _stack[_stackRegister] = x + displacement;
        _programRegister += 3;
    }

    /// <summary>
    /// Processes a reference parameter.
    /// </summary>
    /// <param name="level">Relative level of the parameter.</param>
    /// <param name="displacement">Displacement of the parameter.</param>
    private void ReferenceParameter(int level, int displacement)
    {
        Allocate(1);
        int x = _baseRegister;
        while (level > 0)
        {
            x = _stack[x];
            --level;
        }

        _stack[_stackRegister] = _stack[x + displacement];
        _programRegister += 3;
    }

    /// <summary>
    /// Processes index.
    /// </summary>
    /// <param name="bound">Upper bound of the array.</param>
    private void Index(int bound)
    {
        int i = _stack[_stackRegister];
        --_stackRegister;
        if ((i < 1) || (i > bound))
        {
            throw new Exception(ArrayIndexOutOfBoundsMessage);
        }
        else
        {
            _stack[_stackRegister] = _stack[_stackRegister] + i - 1;
        }

        _programRegister += 2;
    }

    /// <summary>
    /// Processes a constant.
    /// </summary>
    /// <param name="value">Value of the constant.</param>
    private void Constant(int value)
    {
        Allocate(1);
        _stack[_stackRegister] = value;
        _programRegister += 2;
    }

    /// <summary>
    /// Processes the value of an object.
    /// </summary>
    private void Value()
    {
        _stack[_stackRegister] = _stack[_stack[_stackRegister]];
        ++_programRegister;
    }

    /// <summary>
    /// Processes Not (!) operation.
    /// </summary>
    private void Not()
    {
        _stack[_stackRegister] = 1 - _stack[_stackRegister];
        ++_programRegister;
    }

    /// <summary>
    /// Processes Multiplication (*) operation.
    /// </summary>
    private void Multiply()
    {
        --_stackRegister;
        checked
        {
            _stack[_stackRegister] = _stack[_stackRegister] * _stack[_stackRegister + 1];
        }

        ++_programRegister;
    }

    /// <summary>
    /// Processes Division (/) operation.
    /// </summary>
    private void Divide()
    {
        --_stackRegister;
        checked
        {
            _stack[_stackRegister] = _stack[_stackRegister] / _stack[_stackRegister + 1];
        }

        ++_programRegister;
    }

    /// <summary>
    /// Processes Modulo (%) operation.
    /// </summary>
    private void Modulo()
    {
        --_stackRegister;
        checked
        {
            _stack[_stackRegister] = _stack[_stackRegister] % _stack[_stackRegister + 1];
        }

        ++_programRegister;
    }

    /// <summary>
    /// Processes Power operation.
    /// </summary>
    private void Power()
    {
        --_stackRegister;
        checked
        {
            _stack[_stackRegister] = Convert.ToInt32(Math.Pow(_stack[_stackRegister],
                                                              _stack[_stackRegister + 1]));
        }

        ++_programRegister;
    }

    /// <summary>
    /// Processes Minus (-) operation.
    /// </summary>
    private void Minus()
    {
        checked
        {
            _stack[_stackRegister] = -1 * _stack[_stackRegister];
        }

        ++_programRegister;
    }

    /// <summary>
    /// Processes Addition (+) operation.
    /// </summary>
    private void Add()
    {
        --_stackRegister;
        checked
        {
            _stack[_stackRegister] = _stack[_stackRegister] + _stack[_stackRegister + 1];
        }

        ++_programRegister;
    }

    /// <summary>
    /// Processes Subtraction (-) operation.
    /// </summary>
    private void Subtract()
    {
        --_stackRegister;
        checked
        {
            _stack[_stackRegister] = _stack[_stackRegister] - _stack[_stackRegister + 1];
        }

        ++_programRegister;
    }

    /// <summary>
    /// Processes Less (&lt;) operation.
    /// </summary>
    private void Less()
    {
        --_stackRegister;
        _stack[_stackRegister] = (_stack[_stackRegister] < _stack[_stackRegister + 1]) ? 1 : 0;
        ++_programRegister;
    }

    /// <summary>
    /// Processes Less or Equal (&lt;=) operation.
    /// </summary>
    private void LessOrEqual()
    {
        --_stackRegister;
        _stack[_stackRegister] = (_stack[_stackRegister] <= _stack[_stackRegister + 1]) ? 1 : 0;
        ++_programRegister;
    }

    /// <summary>
    /// Processes Equal (==) operation.
    /// </summary>
    private void Equal()
    {
        --_stackRegister;
        _stack[_stackRegister] = (_stack[_stackRegister] == _stack[_stackRegister + 1]) ? 1 : 0;
        ++_programRegister;
    }

    /// <summary>
    /// Processes Not-equal (!=) operation.
    /// </summary>
    private void NotEqual()
    {
        --_stackRegister;
        _stack[_stackRegister] = (_stack[_stackRegister] != _stack[_stackRegister + 1]) ? 1 : 0;
        ++_programRegister;
    }

    /// <summary>
    /// Processes Greater (>) operation.
    /// </summary>
    private void Greater()
    {
        --_stackRegister;
        _stack[_stackRegister] = (_stack[_stackRegister] > _stack[_stackRegister + 1]) ? 1 : 0;
        ++_programRegister;
    }

    /// <summary>
    /// Processes Greater or Equal (>=) operation.
    /// </summary>
    private void GreaterOrEqual()
    {
        --_stackRegister;
        _stack[_stackRegister] = (_stack[_stackRegister] >= _stack[_stackRegister + 1]) ? 1 : 0;
        ++_programRegister;
    }

    /// <summary>
    /// Processes And (&amp;) operation.
    /// </summary>
    private void And()
    {
        --_stackRegister;
        if (_stack[_stackRegister] == 1)
        {
            _stack[_stackRegister] = _stack[_stackRegister + 1];
        }

        ++_programRegister;
    }

    /// <summary>
    /// Processes Or (|) operation.
    /// </summary>
    private void Or()
    {
        --_stackRegister;
        if (_stack[_stackRegister] == 0)
        {
            _stack[_stackRegister] = _stack[_stackRegister + 1];
        }

        ++_programRegister;
    }

    /// <summary>
    /// Reads a boolean value.
    /// </summary>
    private void ReadBoolean()
    {
        --_stackRegister;
        string input = _interpreter.Input.ReadLine();
        if (input.ToLower().Equals("true"))
        {
            _stack[_stack[_stackRegister + 1]] = 1;
        }
        else if (input.ToLower().Equals("false"))
        {
            _stack[_stack[_stackRegister + 1]] = 0;
        }
        else
        {
            throw new Exception(BooleanInputIncorrectFormatMessage);
        }

        ++_programRegister;
    }

    /// <summary>
    /// Reads an integer value.
    /// </summary>
    private void ReadInteger()
    {
        --_stackRegister;
        checked
        {
            _stack[_stack[_stackRegister + 1]] = Convert.ToInt32(_interpreter.Input.ReadLine());
        }

        ++_programRegister;
    }

    /// <summary>
    /// Writes a boolean value.
    /// </summary>
    private void WriteBoolean()
    {
        --_stackRegister;
        string output = (_stack[_stackRegister + 1] == 0) ? "false" : "true";
        _interpreter.Output.WriteLine(output);
        ++_programRegister;
    }

    /// <summary>
    /// Writes an integer value.
    /// </summary>
    private void WriteInteger()
    {
        --_stackRegister;
        _interpreter.Output.WriteLine(_stack[_stackRegister + 1]);
        ++_programRegister;
    }

    /// <summary>
    /// Assigns a value to an object. 
    /// </summary>
    /// <param name="total">Total number of assignments to make.</param>
    private void Assign(int total)
    {
        _stackRegister -= 2 * total;
        int x = _stackRegister;
        while (x < (_stackRegister + total))
        {
            ++x;
            _stack[_stack[x]] = _stack[x + total];
        }

        _programRegister += 2;
    }

    /// <summary>
    /// Conditionally executes a statement; 
    /// branches to the specified address otherwise.
    /// </summary>
    /// <param name="address">
    /// The address to branch to in case the conditional expression 
    /// evaluates to false.
    /// </param>
    private void Do(int address)
    {
        if (_stack[_stackRegister] != 0)
        {
            _programRegister += 2;
        }
        else
        {
            _programRegister = address;
        }

        --_stackRegister;
    }

    /// <summary>
    /// Unconditionally branches to the specified address.
    /// </summary>
    /// <param name="address">The address to branch to.</param>
    private void Goto(int address)
    {
        _programRegister = address;
    }

    /// <summary>
    /// Opens up a channel for use.
    /// </summary>
    private void Open()
    {
        --_stackRegister;
        lock (_interpreter.Channels)
        {
            _stack[_stack[_stackRegister + 1]] = _interpreter.Channels.Count;
            _interpreter.Channels.Add(new Channel<int>());
        }

        ++_programRegister;
    }

    /// <summary>
    /// Assigns a random value to an integer variable.
    /// </summary>
    private void Randomize()
    {
        --_stackRegister;
        Random random = new Random();
        _stack[_stack[_stackRegister + 1]] = random.Next();
        ++_programRegister;
    }

    /// <summary>
    /// Sends an integer value through a channel.
    /// </summary>
    private void Send()
    {
        --_stackRegister;
        int key = _stack[_stackRegister + 1];
        bool error = false;
        lock (_interpreter.Channels)
        {
            if ((key < 1) || (key >= _interpreter.Channels.Count))
            {
                error = true;
            }
        }

        if (error)
        {
            throw new Exception(SendThroughUnopenedChannelMessage);
        }
        else
        {
            _interpreter.Channels[key].Send(_stack[_stackRegister]);
            ++_programRegister;
        }
    }

    /// <summary>
    /// Receives an integer value through a channel.
    /// </summary>
    private void Receive()
    {
        --_stackRegister;
        int key = _stack[_stackRegister + 1];
        bool error = false;
        lock (_interpreter.Channels)
        {
            if ((key < 1) || (key >= _interpreter.Channels.Count))
            {
                error = true;
            }
        }

        if (error)
        {
            throw new Exception(ReceiveThroughUnopenedChannelMessage);
        }
        else
        {
            _stack[_stack[_stackRegister]] = _interpreter.Channels[key].Receive();
            ++_programRegister;
        }
    }

    /// <summary>
    /// Spawns a new node that runs in parallel.
    /// </summary>
    private void Parallel()
    {
        int[] currentStack = new int[_stackRegister + 1];
        Array.Copy(_stack, currentStack, _stackRegister + 1);
        Translator worker = new Translator(_interpreter,
                                           currentStack,
                                           _programRegister + 1,
                                           _baseRegister,
                                           _stackRegister);
        worker.Start();

        // Skip past the parallel statement, and the following 
        // procedure invocation as well.
        _programRegister += 4;
    }

    /// <summary>
    /// Processes start of a block.
    /// </summary>
    /// <param name="objectsLength">
    /// Total length of the objects in this block.
    /// </param>
    private void Block(int objectsLength)
    {
        Allocate(1);
        _stack[_stackRegister] = _baseRegister;
        _baseRegister = _stackRegister;
        _programRegister += 2;
        Allocate(objectsLength + 2);
    }

    /// <summary>
    /// Processes end of a block.
    /// </summary>
    private void EndBlock()
    {
        _stackRegister = _baseRegister - 1;
        _baseRegister = _stack[_baseRegister];
        ++_programRegister;
    }

    /// <summary>
    /// Processes start of a procedure block.
    /// </summary>
    /// <param name="objectsLength">
    /// Total length of the objects in this block.
    /// </param>
    private void ProcedureBlock(int objectsLength)
    {
        Allocate(objectsLength);
        _programRegister += 2;
    }

    /// <summary>
    /// Processes end of a procedure block.
    /// </summary>
    /// <param name="parametersLength">Total length of parameters.</param>
    private void EndProcedureBlock(int parametersLength)
    {
        _stackRegister = _baseRegister - parametersLength;
        if (_baseRegister < (Interpreter.Max - 3))
        {
            _stack[_stackRegister] = _stack[_baseRegister + 3];
        }

        _programRegister = _stack[_baseRegister + 2];
        _baseRegister = _stack[_baseRegister + 1];
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
    private void ProcedureInvocation(int level, int startAddress)
    {
        Allocate(3);
        int x = _baseRegister;
        while (level > 0)
        {
            x = _stack[x];
            level--;
        }

        _stack[_stackRegister - 2] = x;
        _stack[_stackRegister - 1] = _baseRegister;
        if (_mainProcedure)
        {
            _stack[_stackRegister] = _interpreter.EndOfProgram;
            _mainProcedure = false;
        }
        else
        {
            _stack[_stackRegister] = _programRegister + 3;
        }

        _baseRegister = _stackRegister - 2;
        _programRegister = startAddress;
    }

    /// <summary>
    /// Processes start of program block.
    /// </summary>
    /// <param name="objectsLength">
    /// Total length of the objects in the block.
    /// </param>
    private void Program(int objectsLength)
    {
        _baseRegister = 0;
        _stackRegister = _baseRegister;
        Allocate(objectsLength + 2);
        _programRegister = 2;
    }

    /// <summary>
    /// Processes end of program block.
    /// </summary>
    private void EndProgram()
    {
        _running = false;
    }
}
