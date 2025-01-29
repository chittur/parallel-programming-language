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

namespace Parallelism;

/// <summary>
/// Messaging channel between parallel nodes.
/// </summary>
/// <typeparam name="T">
/// The message type that would be passed through the channel.
/// </typeparam>
public class Channel<T> where T : struct
{
    State _state;  // Current state of the channel.
    T _content;    // Content of the channel.

    /// <summary>
    /// Creates an instance of Channel, messaging channel between parallel nodes.
    /// </summary>
    public Channel()
    {
        _state = State.Idle;
        _content = default;
    }

    /// <summary>
    /// Receives a message.
    /// </summary>
    /// <returns>The message.</returns>
    public T Receive()
    {
        int threadId = Thread.CurrentThread.ManagedThreadId; // For logging only.

        Trace.WriteLine($"Channel.Receive on thread {threadId}: Getting ready to receive.");
        while (_state != State.Sent)
        {
            Wait();
        }
        Trace.WriteLine($"Channel.Receive on thread {threadId}: Received.");

        _state = State.Received;
        NotifyAll();

        return _content;
    }

    /// <summary>
    /// Sends a message.
    /// </summary>
    /// <param name="value">The message.</param>
    public void Send(T value)
    {
        int threadId = Thread.CurrentThread.ManagedThreadId; // For logging only.

        Trace.WriteLine($"Channel.Send on thread {threadId}: Getting ready to send.");
        while (_state != State.Idle)
        {
            Wait();
        }
        Trace.WriteLine($"Channel.Send on thread {threadId}: Sent.");

        _content = value;
        _state = State.Sent;
        NotifyAll();

        Trace.WriteLine($"Channel.Send on thread {threadId}: Waiting for a receiver.");
        while (_state != State.Received)
        {
            Wait();
        }
        Trace.WriteLine($"Channel.Send on thread {threadId}: Receiver has received.");

        _state = State.Idle;
        NotifyAll();
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
