/******************************************************************************
 * Filename    = State.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = Parallelism
 *
 * Description = Defines the enum "State", which eumerates the various states
 *               of a messaging channel.
 *****************************************************************************/

namespace Parallelism
{
    /// <summary>
    /// Enumerates the various states of a messaging channel.
    /// </summary>
    internal enum State
    {
        /// <summary>
        /// The channel is in idle state.
        /// </summary>
        Idle,

        /// <summary>
        /// Data has been sent through the channel.
        /// </summary>
        Sent,

        /// <summary>
        /// Data has been received through the channel.
        /// </summary>
        Received
    }
}