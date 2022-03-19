/******************************************************************************
 * Filename    = Channel.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = Parallelism
 *
 * Description = Defines the class "Channel", which is a messaging channel
 *               between parallel nodes.
 *****************************************************************************/

using System.Diagnostics;
using System.Threading;

namespace Parallelism
{
    /// <summary>
    /// Messaging channel between parallel nodes.
    /// </summary>
    /// <typeparam name="T">
    /// The message type that would be passed through the channel.
    /// </typeparam>
    public class Channel<T> where T : struct
    {
        State state;  // Current state of the channel.
        T content;    // Content of the channel.

        /// <summary>
        /// Creates an instance of Channel, messaging channel between parallel nodes.
        /// </summary>
        public Channel()
        {
            this.state = State.Idle;
            this.content = default;
        }

        /// <summary>
        /// Receives a message.
        /// </summary>
        /// <returns>The message.</returns>
        public T Receive()
        {
            int threadId = Thread.CurrentThread.ManagedThreadId; // For logging only.

            Trace.WriteLine($"Channel.Receive on thread {threadId}: Getting ready to receive.");
            while (this.state != State.Sent)
            {
                this.Wait();
            }
            Trace.WriteLine($"Channel.Receive on thread {threadId}: Received.");

            this.state = State.Received;
            this.NotifyAll();

            return this.content;
        }

        /// <summary>
        /// Sends a message.
        /// </summary>
        /// <param name="value">The message.</param>
        public void Send(T value)
        {
            int threadId = Thread.CurrentThread.ManagedThreadId; // For logging only.

            Trace.WriteLine($"Channel.Send on thread {threadId}: Getting ready to send.");
            while (this.state != State.Idle)
            {
                this.Wait();
            }
            Trace.WriteLine($"Channel.Send on thread {threadId}: Sent.");

            this.content = value;
            this.state = State.Sent;
            this.NotifyAll();

            Trace.WriteLine($"Channel.Send on thread {threadId}: Waiting for a receiver.");
            while (this.state != State.Received)
            {
                this.Wait();
            }
            Trace.WriteLine($"Channel.Send on thread {threadId}: Receiver has received.");

            this.state = State.Idle;
            this.NotifyAll();
        }

        /// <summary>
        /// Waits till notified by another thread.
        /// </summary>
        void Wait()
        {
            lock (this)
            {
                Monitor.Wait(this);
            }
        }

        /// <summary>
        /// Notifies all other waiting threads.
        /// </summary>
        void NotifyAll()
        {
            lock (this)
            {
                Monitor.PulseAll(this);
            }
        }
    }
}