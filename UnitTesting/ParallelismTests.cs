/******************************************************************************
 * Filename    = ParallelismTests.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = UnitTesting
 *
 * Description = Unit tests for the Parallelism module.
 *****************************************************************************/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parallelism;
using System;

namespace UnitTesting
{
    /// <summary>
    /// Unit tests for the Parallelism module.
    /// </summary>
    [TestClass]
    public class ParallelismTests
    {
        /// <summary>
        /// A sample 'Node'.
        /// </summary>
        class SampleNode : Node
        {
            /// <summary>
            /// Creates the sample node class.
            /// </summary>
            /// <param name="c">A channel to pass integers</param>
            public SampleNode(Channel<int> c)
            {
                this.channel = c;
            }

            /// <summary>
            /// Executes the node.
            /// </summary>
            public override void Run()
            {
                // Receive an integer through the channel. Doubles it, and send that back.
                int number = this.channel.Receive();
                this.channel.Send(number * 2);
            }

            /// <summary>
            /// Spins off this sample node function in another thread.
            /// </summary>
            public void Execute()
            {
                base.Start();
            }

            /// <summary>
            /// Channel to send and receive integers.
            /// </summary>
            readonly Channel<int> channel;
        }

        /// <summary>
        /// Runs a basic test for the Node and the Channel classes.
        /// </summary>
        [TestMethod]
        public void TestBasicNodeAndChannel()
        {
            // Create a channel.
            Channel<int> channel = new Channel<int>();

            // Create a sample node and start it.
            SampleNode node = new SampleNode(channel);
            node.Execute();

            // The sample node is running in parallel. It is expecting to
            // receive an integer. It will then double it and send it back.
            int input = new Random().Next(1000); // Send a random number.
            channel.Send(input);
            int number = channel.Receive();

            // Validate that the value received from the sample node is double the input.
            Assert.AreEqual(number, input * 2);
        }
    }
}
