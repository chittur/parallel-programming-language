/******************************************************************************
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

using Intermediary;
using Parallelism;
using System;
using System.Threading;

namespace Runtime
{
    /// <summary>
    /// Provides methods to interpret and execute the intermediate code,
    /// in parallel if required. Derives from the abstract class for
    /// parallel nodes - "Node".
    /// </summary>
    class Translator : Node
    {
        int programRegister;      // Points to the current instruction.  
        int baseRegister;         // Points to the base of the current context.
        int stackRegister;        // Points to the top of the stack.
        int[] stack;              // Stack.
        bool running;             // Running mode?
        bool mainProcedure;       // Main procedure for the node?
        Interpreter interpreter;  // The interpreter instance.

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
            this.interpreter = root;
            this.programRegister = startAddress;
            this.baseRegister = baseAddress;
            this.stackRegister = stackAddress;
            this.mainProcedure = false;
            this.stack = new int[Interpreter.Max];
            if (currentStack != null)
            {
                Array.Copy(currentStack, this.stack, currentStack.Length);
            }
        }

        /// <summary>
        /// Invokes the node's "Run" function in a new thread.
        /// </summary>
        protected override void Start()
        {
            this.mainProcedure = true;
            base.Start();
        }

        /// <summary>
        /// Executes the node function - interpreting the intermediate code.
        /// </summary>
        public override void Run()
        {
            try
            {
                this.Translate();
            }
            catch (Exception exception)
            {
                this.running = false;
                this.interpreter.HandleException(exception.Message);
            }
        }

        /// <summary>
        /// Interprets the intermediate code.
        /// </summary>
        void Translate()
        {
            this.running = true;

            // Run the program.
            while ((this.running) && (this.interpreter.noGlobalErrors))
            {
                if (this.programRegister >= (Interpreter.Max - 2))
                {
                    throw new Exception("Stack overflow.");
                }

                Opcode opcode = (Opcode)(this.interpreter.store[this.programRegister]);
                switch (opcode)
                {
                    case Opcode.Program:
                        {
                            this.Program(this.interpreter.store[this.programRegister + 1]);
                            break;
                        }

                    case Opcode.EndProgram:
                        {
                            this.EndProgram();
                            break;
                        }

                    case Opcode.Block:
                        {
                            this.Block(this.interpreter.store[this.programRegister + 1]);
                            break;
                        }

                    case Opcode.EndBlock:
                        {
                            this.EndBlock();
                            break;
                        }

                    case Opcode.ProcedureBlock:
                        {
                            this.ProcedureBlock(
                                            this.interpreter.store[this.programRegister + 1]);
                            break;
                        }

                    case Opcode.ProcedureInvocation:
                        {
                            this.ProcedureInvocation(
                                            this.interpreter.store[this.programRegister + 1],
                                            this.interpreter.store[this.programRegister + 2]);
                            break;
                        }

                    case Opcode.EndProcedureBlock:
                        {
                            this.EndProcedureBlock(
                                            this.interpreter.store[this.programRegister + 1]);
                            break;
                        }

                    case Opcode.Index:
                        {
                            this.Index(this.interpreter.store[this.programRegister + 1]);
                            break;
                        }

                    case Opcode.Variable:
                        {
                            this.Variable(this.interpreter.store[this.programRegister + 1],
                                          this.interpreter.store[this.programRegister + 2]);
                            break;
                        }

                    case Opcode.ReferenceParameter:
                        {
                            this.ReferenceParameter(
                                          this.interpreter.store[this.programRegister + 1],
                                          this.interpreter.store[this.programRegister + 2]);
                            break;
                        }

                    case Opcode.Constant:
                        {
                            this.Constant(this.interpreter.store[this.programRegister + 1]);
                            break;
                        }

                    case Opcode.Value:
                        {
                            this.Value();
                            break;
                        }

                    case Opcode.Not:
                        {
                            this.Not();
                            break;
                        }

                    case Opcode.And:
                        {
                            this.And();
                            break;
                        }

                    case Opcode.Or:
                        {
                            this.Or();
                            break;
                        }

                    case Opcode.Multiply:
                        {
                            this.Multiply();
                            break;
                        }

                    case Opcode.Divide:
                        {
                            this.Divide();
                            break;
                        }

                    case Opcode.Modulo:
                        {
                            this.Modulo();
                            break;
                        }

                    case Opcode.Power:
                        {
                            this.Power();
                            break;
                        }

                    case Opcode.Less:
                        {
                            this.Less();
                            break;
                        }

                    case Opcode.LessOrEqual:
                        {
                            this.LessOrEqual();
                            break;
                        }

                    case Opcode.Equal:
                        {
                            this.Equal();
                            break;
                        }

                    case Opcode.NotEqual:
                        {
                            this.NotEqual();
                            break;
                        }

                    case Opcode.Greater:
                        {
                            this.Greater();
                            break;
                        }

                    case Opcode.GreaterOrEqual:
                        {
                            this.GreaterOrEqual();
                            break;
                        }

                    case Opcode.Add:
                        {
                            this.Add();
                            break;
                        }

                    case Opcode.Subtract:
                        {
                            this.Subtract();
                            break;
                        }

                    case Opcode.ReadBoolean:
                        {
                            this.ReadBoolean();
                            break;
                        }

                    case Opcode.ReadInteger:
                        {
                            this.ReadInteger();
                            break;
                        }

                    case Opcode.WriteBoolean:
                        {
                            this.WriteBoolean();
                            break;
                        }

                    case Opcode.WriteInteger:
                        {
                            this.WriteInteger();
                            break;
                        }

                    case Opcode.Assign:
                        {
                            this.Assign(this.interpreter.store[this.programRegister + 1]);
                            break;
                        }

                    case Opcode.Minus:
                        {
                            this.Minus();
                            break;
                        }

                    case Opcode.Do:
                        {
                            this.Do(this.interpreter.store[this.programRegister + 1]);
                            break;
                        }

                    case Opcode.Goto:
                        {
                            this.Goto(this.interpreter.store[this.programRegister + 1]);
                            break;
                        }

                    case Opcode.Open:
                        {
                            this.Open();
                            break;
                        }

                    case Opcode.Randomize:
                        {
                            this.Randomize();
                            break;
                        }

                    case Opcode.Send:
                        {
                            this.Send();
                            break;
                        }

                    case Opcode.Receive:
                        {
                            this.Receive();
                            break;
                        }

                    case Opcode.Parallel:
                        {
                            this.Parallel();
                            break;
                        }

                    default:
                        {
                            throw new Exception("Incorrect opcode.");
                        }
                }
            }
        }

        /// <summary>
        /// Allocates the specified number of locations for the stack. 
        /// </summary>
        /// <param name="words">Number of locations to allocate.</param>
        void Allocate(int words)
        {
            this.stackRegister = this.stackRegister + words;
            if (this.stackRegister >= Interpreter.Max)
            {
                throw new Exception("Stack overflow.");
            }
        }

        /// <summary>
        /// Processes a variable.
        /// </summary>
        /// <param name="level">Relative level of the variable.</param>
        /// <param name="displacement">Displacement of the variable.</param>
        void Variable(int level, int displacement)
        {
            this.Allocate(1);
            int x = this.baseRegister;
            while (level > 0)
            {
                x = this.stack[x];
                --level;
            }

            this.stack[this.stackRegister] = x + displacement;
            this.programRegister = this.programRegister + 3;
        }

        /// <summary>
        /// Processes a reference parameter.
        /// </summary>
        /// <param name="level">Relative level of the parameter.</param>
        /// <param name="displacement">Displacement of the parameter.</param>
        void ReferenceParameter(int level, int displacement)
        {
            this.Allocate(1);
            int x = this.baseRegister;
            while (level > 0)
            {
                x = this.stack[x];
                --level;
            }

            this.stack[this.stackRegister] = this.stack[x + displacement];
            this.programRegister = this.programRegister + 3;
        }

        /// <summary>
        /// Processes index.
        /// </summary>
        /// <param name="bound">Upper bound of the array.</param>
        void Index(int bound)
        {
            int i = this.stack[this.stackRegister];
            --(this.stackRegister);
            if ((i < 1) || (i > bound))
            {
                throw new Exception("Array index is out of bounds.");
            }
            else
            {
                this.stack[this.stackRegister] =
                  this.stack[this.stackRegister] + i - 1;
            }

            this.programRegister = this.programRegister + 2;
        }

        /// <summary>
        /// Processes a constant.
        /// </summary>
        /// <param name="value">Value of the constant.</param>
        void Constant(int value)
        {
            this.Allocate(1);
            this.stack[this.stackRegister] = value;
            this.programRegister = this.programRegister + 2;
        }

        /// <summary>
        /// Processes the value of an object.
        /// </summary>
        void Value()
        {
            this.stack[this.stackRegister] =
              this.stack[this.stack[this.stackRegister]];
            ++(this.programRegister);
        }

        /// <summary>
        /// Processes Not (!) operation.
        /// </summary>
        void Not()
        {
            this.stack[this.stackRegister] = 1 - this.stack[this.stackRegister];
            ++(this.programRegister);
        }

        /// <summary>
        /// Processes Multiplication (*) operation.
        /// </summary>
        void Multiply()
        {
            --(this.stackRegister);
            checked
            {
                this.stack[this.stackRegister] =
                  this.stack[this.stackRegister] * this.stack[this.stackRegister + 1];
            }

            ++(this.programRegister);
        }

        /// <summary>
        /// Processes Division (/) operation.
        /// </summary>
        void Divide()
        {
            --(this.stackRegister);
            checked
            {
                this.stack[this.stackRegister] =
                  this.stack[this.stackRegister] / this.stack[this.stackRegister + 1];
            }

            ++(this.programRegister);
        }

        /// <summary>
        /// Processes Modulo (%) operation.
        /// </summary>
        void Modulo()
        {
            --(this.stackRegister);
            checked
            {
                this.stack[this.stackRegister] =
                  this.stack[this.stackRegister] % this.stack[this.stackRegister + 1];
            }

            ++(this.programRegister);
        }

        /// <summary>
        /// Processes Power operation.
        /// </summary>
        void Power()
        {
            --(this.stackRegister);
            checked
            {
                this.stack[this.stackRegister] = Convert.ToInt32(Math.Pow(
                                                  this.stack[this.stackRegister],
                                                  this.stack[this.stackRegister + 1]));
            }

            ++(this.programRegister);
        }

        /// <summary>
        /// Processes Minus (-) operation.
        /// </summary>
        void Minus()
        {
            checked
            {
                this.stack[stackRegister] = -1 * this.stack[this.stackRegister];
            }

            ++(this.programRegister);
        }

        /// <summary>
        /// Processes Addition (+) operation.
        /// </summary>
        void Add()
        {
            --(this.stackRegister);
            checked
            {
                this.stack[this.stackRegister] =
                  this.stack[this.stackRegister] + this.stack[this.stackRegister + 1];
            }

            ++(this.programRegister);
        }

        /// <summary>
        /// Processes Subtraction (-) operation.
        /// </summary>
        void Subtract()
        {
            --(this.stackRegister);
            checked
            {
                this.stack[this.stackRegister] =
                  this.stack[this.stackRegister] - this.stack[this.stackRegister + 1];
            }

            ++(this.programRegister);
        }

        /// <summary>
        /// Processes Less (&lt;) operation.
        /// </summary>
        void Less()
        {
            --(this.stackRegister);
            this.stack[this.stackRegister] =
              (this.stack[this.stackRegister] < this.stack[this.stackRegister + 1])
                      ? 1 : 0;
            ++(this.programRegister);
        }

        /// <summary>
        /// Processes Less or Equal (&lt;=) operation.
        /// </summary>
        void LessOrEqual()
        {
            --(this.stackRegister);
            this.stack[this.stackRegister] =
              (this.stack[this.stackRegister] <= this.stack[this.stackRegister + 1])
                      ? 1 : 0;
            ++(this.programRegister);
        }

        /// <summary>
        /// Processes Equal (==) operation.
        /// </summary>
        void Equal()
        {
            --(this.stackRegister);
            this.stack[this.stackRegister] =
              (this.stack[this.stackRegister] == this.stack[this.stackRegister + 1])
                      ? 1 : 0;
            ++(this.programRegister);
        }

        /// <summary>
        /// Processes Not-equal (!=) operation.
        /// </summary>
        void NotEqual()
        {
            --(this.stackRegister);
            this.stack[this.stackRegister] =
              (this.stack[this.stackRegister] != this.stack[this.stackRegister + 1])
                      ? 1 : 0;
            ++(this.programRegister);
        }

        /// <summary>
        /// Processes Greater (>) operation.
        /// </summary>
        void Greater()
        {
            --(this.stackRegister);
            this.stack[this.stackRegister] =
              (this.stack[this.stackRegister] > this.stack[this.stackRegister + 1])
                      ? 1 : 0;
            ++(this.programRegister);
        }

        /// <summary>
        /// Processes Greater or Equal (>=) operation.
        /// </summary>
        void GreaterOrEqual()
        {
            --(this.stackRegister);
            this.stack[this.stackRegister] =
              (this.stack[this.stackRegister] >= this.stack[this.stackRegister + 1])
                      ? 1 : 0;
            ++(this.programRegister);
        }

        /// <summary>
        /// Processes And (&amp;) operation.
        /// </summary>
        void And()
        {
            --(this.stackRegister);
            if (this.stack[this.stackRegister] == 1)
            {
                this.stack[this.stackRegister] = this.stack[this.stackRegister + 1];
            }

            ++(this.programRegister);
        }

        /// <summary>
        /// Processes Or (|) operation.
        /// </summary>
        void Or()
        {
            --(this.stackRegister);
            if (this.stack[this.stackRegister] == 0)
            {
                this.stack[this.stackRegister] = this.stack[this.stackRegister + 1];
            }

            ++(this.programRegister);
        }

        /// <summary>
        /// Reads a boolean value.
        /// </summary>
        void ReadBoolean()
        {
            --(this.stackRegister);
            string input = Console.ReadLine();
            if (input.ToLower().Equals("true"))
            {
                this.stack[this.stack[this.stackRegister + 1]] = 1;
            }
            else if (input.ToLower().Equals("false"))
            {
                this.stack[this.stack[this.stackRegister + 1]] = 0;
            }
            else
            {
                throw new Exception("Boolean input was not in the correct format.");
            }

            ++(this.programRegister);
        }

        /// <summary>
        /// Reads an integer value.
        /// </summary>
        void ReadInteger()
        {
            --(this.stackRegister);
            checked
            {
                this.stack[this.stack[this.stackRegister + 1]] =
                                                  Convert.ToInt32(Console.ReadLine());
            }

            ++(this.programRegister);
        }

        /// <summary>
        /// Writes a boolean value.
        /// </summary>
        void WriteBoolean()
        {
            --(this.stackRegister);
            string output = (this.stack[this.stackRegister + 1] == 0)
                            ? "false" : "true";
            this.interpreter.output.WriteLine(output);
            ++(this.programRegister);
        }

        /// <summary>
        /// Writes an integer value.
        /// </summary>
        void WriteInteger()
        {
            --(this.stackRegister);
            this.interpreter.output.WriteLine(this.stack[this.stackRegister + 1]);
            ++(this.programRegister);
        }

        /// <summary>
        /// Assigns a value to an object. 
        /// </summary>
        /// <param name="total">Total number of assignments to make.</param>
        void Assign(int total)
        {
            this.stackRegister = this.stackRegister - (2 * total);
            int x = this.stackRegister;
            while (x < (this.stackRegister + total))
            {
                ++x;
                this.stack[this.stack[x]] = this.stack[x + total];
            }

            this.programRegister = this.programRegister + 2;
        }

        /// <summary>
        /// Conditionally executes a statement; 
        /// branches to the specified address otherwise.
        /// </summary>
        /// <param name="address">
        /// The address to branch to in case the conditional expression 
        /// evaluates to false.
        /// </param>
        void Do(int address)
        {
            if (this.stack[this.stackRegister] != 0)
            {
                this.programRegister = this.programRegister + 2;
            }
            else
            {
                this.programRegister = address;
            }

            --(this.stackRegister);
        }

        /// <summary>
        /// Unconditionally branches to the specified address.
        /// </summary>
        /// <param name="address">The address to branch to.</param>
        void Goto(int address)
        {
            this.programRegister = address;
        }

        /// <summary>
        /// Opens up a channel for use.
        /// </summary>
        void Open()
        {
            --(this.stackRegister);
            lock (this.interpreter.channelList)
            {
                this.stack[this.stack[this.stackRegister + 1]] =
                                                    this.interpreter.channelList.Count;
                this.interpreter.channelList.Add(new Channel<int>());
            }

            ++(this.programRegister);
        }

        /// <summary>
        /// Assigns a random value to an integer variable.
        /// </summary>
        void Randomize()
        {
            --(this.stackRegister);
            Random random = new Random(Environment.TickCount -
                                       Thread.CurrentThread.ManagedThreadId);
            this.stack[this.stack[this.stackRegister + 1]] = random.Next();
            ++(this.programRegister);
        }

        /// <summary>
        /// Sends an integer value through a channel.
        /// </summary>
        void Send()
        {
            --(this.stackRegister);
            int key = this.stack[this.stackRegister + 1];
            bool error = false;
            lock (this.interpreter.channelList)
            {
                if ((key < 1) || (key >= this.interpreter.channelList.Count))
                {
                    error = true;
                }
            }

            if (error)
            {
                throw new Exception("Attempt to send through an un-opened channel.");
            }
            else
            {
                this.interpreter.channelList[key].Send(this.stack[this.stackRegister]);
                ++(this.programRegister);
            }
        }

        /// <summary>
        /// Receives an integer value through a channel.
        /// </summary>
        void Receive()
        {
            --(this.stackRegister);
            int key = this.stack[this.stackRegister + 1];
            bool error = false;
            lock (this.interpreter.channelList)
            {
                if ((key < 1) || (key >= this.interpreter.channelList.Count))
                {
                    error = true;
                }
            }

            if (error)
            {
                throw new Exception("Attempt to receive through an un-opened channel.");
            }
            else
            {
                this.stack[this.stack[this.stackRegister]] =
                                          this.interpreter.channelList[key].Receive();
                ++(this.programRegister);
            }
        }

        /// <summary>
        /// Spawns a new node that runs in parallel.
        /// </summary>
        void Parallel()
        {
            int[] currentStack = new int[this.stackRegister + 1];
            Array.Copy(this.stack, currentStack, this.stackRegister + 1);
            Translator worker = new Translator(this.interpreter,
                                               currentStack,
                                               this.programRegister + 1,
                                               this.baseRegister,
                                               this.stackRegister);
            worker.Start();

            // Skip past the parallel statement, and the following 
            // procedure invocation as well.
            this.programRegister = this.programRegister + 4;
        }

        /// <summary>
        /// Processes start of a block.
        /// </summary>
        /// <param name="objectsLength">
        /// Total length of the objects in this block.
        /// </param>
        void Block(int objectsLength)
        {
            this.Allocate(1);
            this.stack[this.stackRegister] = this.baseRegister;
            this.baseRegister = this.stackRegister;
            this.programRegister = this.programRegister + 2;
            this.Allocate(objectsLength + 2);
        }

        /// <summary>
        /// Processes end of a block.
        /// </summary>
        void EndBlock()
        {
            this.stackRegister = this.baseRegister - 1;
            this.baseRegister = this.stack[this.baseRegister];
            ++(this.programRegister);
        }

        /// <summary>
        /// Processes start of a procedure block.
        /// </summary>
        /// <param name="objectsLength">
        /// Total length of the objects in this block.
        /// </param>
        void ProcedureBlock(int objectsLength)
        {
            this.Allocate(objectsLength);
            this.programRegister = this.programRegister + 2;
        }

        /// <summary>
        /// Processes end of a procedure block.
        /// </summary>
        /// <param name="parametersLength">Total length of parameters.</param>
        void EndProcedureBlock(int parametersLength)
        {
            this.stackRegister = this.baseRegister - parametersLength;
            if (this.baseRegister < (Interpreter.Max - 3))
            {
                this.stack[this.stackRegister] = this.stack[this.baseRegister + 3];
            }

            this.programRegister = this.stack[this.baseRegister + 2];
            this.baseRegister = this.stack[this.baseRegister + 1];
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
        void ProcedureInvocation(int level, int startAddress)
        {
            this.Allocate(3);
            int x = this.baseRegister;
            while (level > 0)
            {
                x = this.stack[x];
                level = level - 1;
            }

            this.stack[this.stackRegister - 2] = x;
            this.stack[this.stackRegister - 1] = this.baseRegister;
            if (this.mainProcedure)
            {
                this.stack[this.stackRegister] = this.interpreter.endOfProgram;
                this.mainProcedure = false;
            }
            else
            {
                this.stack[this.stackRegister] = this.programRegister + 3;
            }

            this.baseRegister = this.stackRegister - 2;
            this.programRegister = startAddress;
        }

        /// <summary>
        /// Processes start of program block.
        /// </summary>
        /// <param name="objectsLength">
        /// Total length of the objects in the block.
        /// </param>
        void Program(int objectsLength)
        {
            this.baseRegister = 0;
            this.stackRegister = this.baseRegister;
            this.Allocate(objectsLength + 2);
            this.programRegister = 2;
        }

        /// <summary>
        /// Processes end of program block.
        /// </summary>
        void EndProgram()
        {
            this.running = false;
        }
    }
}