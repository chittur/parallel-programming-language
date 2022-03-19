/******************************************************************************
 * Filename    = Interpreter.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = Runtime
 *
 * Description = Defines the class "Interpreter", which loads the intermediate
 *               code into memory and invokes the master translator node.
 *****************************************************************************/

using Parallelism;
using System;
using System.Collections.Generic;
using System.IO;

namespace Runtime
{
    /// <summary>
    /// Loads the intermediate code into memory and invokes the master translator
    /// node.
    /// </summary>
    public class Interpreter
    {
        internal const int Max = 10002; // Available memory for the each node.
        internal int[] store;           // Memory.
        internal int endOfProgram;      // End address for the program code.
        internal List<Channel<int>> channelList; // List of channels.
        internal bool noGlobalErrors;   // No errors in any of the parallel nodes?
        internal TextWriter output;     // The output stream.

        /// <summary>
        /// Creates a new instance of Interpreter, which loads the intermediate
        /// code into memory and invokes the master translator node.
        /// </summary>
        /// <param name="output">The output stream.</param>
        public Interpreter(TextWriter output)
        {
            this.store = new int[Max];
            this.noGlobalErrors = true;
            this.channelList = new List<Channel<int>>();
            this.output = output;

            // We add a null to the first element of the channel list. This is
            // because the memory is initialized to 0 by default. We don't want
            // the first opened channel to get the value of 0 as this is 
            // "usually" the value of an un-opened channel; "usually" - because
            // an un-opened channel can possibly have any value that was left over
            // by a previous stack. In short, using an un-opened channel can produce
            // indeterminate results. Of course, we can clean up the stack space
            // every time we blow up a stack, but this is highly inefficient from
            // a performance perspective and hence we are not going to do that here.
            this.channelList.Add(null);
        }

        /// <summary>
        /// Loads and runs the intermediate code.
        /// </summary>
        /// <param name="filename">
        /// Path of the file containing the intermediate code.
        /// </param>
        public void RunProgram(string filename)
        {
            try
            {
                this.LoadProgram(filename); // Load the program.
                Translator master = new Translator(this, null, 0, 0, 0);
                master.Run();
            }
            catch (Exception exception)
            {
                this.HandleException(exception.Message);
            }
        }

        /// <summary>
        /// Handles exceptions from the nodes.
        /// </summary>
        /// <param name="message">Exception message</param>
        internal void HandleException(string message)
        {
            this.noGlobalErrors = false;
            lock (this)
            {
                output.WriteLine(message);
            }
        }

        /// <summary>
        /// Loads the intermediate code into memory.
        /// </summary>
        /// <param name="filename">
        /// Path of the file containing the intermediate code.
        /// </param>
        void LoadProgram(string filename)
        {
            string[] lines = File.ReadAllLines(filename);
            if (lines.Length >= Max)
            {
                throw new Exception("Program too big.");
            }

            int count = 0;
            foreach (string line in lines)
            {
                this.store[count] = Convert.ToInt32(line);
                ++count;
            }

            this.endOfProgram = count - 1;
        }
    }
}