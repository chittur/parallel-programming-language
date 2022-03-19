/******************************************************************************
 * Filename    = Node.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = Parallelism
 *
 * Description = Defines the abstract class "Node", which is an abstract
 *               class for parallel nodes.
 *****************************************************************************/

using System.Threading;

namespace Parallelism
{
    /// <summary>
    /// Abstract class for parallel nodes.
    /// </summary>
    public abstract class Node
    {
        /// <summary>
        /// The parallel node-specific function.
        /// </summary>
        public abstract void Run();

        /// <summary>
        /// Executes the parallel node function on a separate thread.
        /// </summary>
        protected virtual void Start()
        {
            Thread worker = new Thread(new ThreadStart(this.Run));
            worker.IsBackground = true;
            worker.Start();
        }
    }
}
