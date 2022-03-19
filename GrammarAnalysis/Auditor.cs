/******************************************************************************
 * Filename    = Auditor.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = GrammarAnalysis
 *
 * Description = Defines the class "Auditor", which maintains the object
 *               records and scope levels of the objects.
 *****************************************************************************/

using ErrorReporting;
using LanguageConstructs;
using System.Collections.Generic;

namespace GrammarAnalysis
{
    /// <summary>
    /// Maintains the object records and scope levels of the objects.
    /// </summary>
    public class Auditor
    {
        /// <summary>
        /// Dummy name for objects that don't require a unique name.
        /// Example: Return parameters for procedures of type void;
        ///           Variables accessed without being defined earlier.
        /// </summary>
        public const int NoName = -1;

        Annotator annotator;           // To print out any scope errors.
        List<ObjectRecord> blockTable; // List of references to the first object
                                       // record in each scope level. Please note
                                       // that the object record in turn is a 
                                       // linked list and can be used to traverse
                                       // to any other object record in the same
                                       // scope.

        /// <summary>
        /// Creates an instance of Auditor, which maintains the object
        /// records and scope levels of the objects.
        /// </summary>
        /// <param name="errorReporter">
        /// Annotator instance to print out scope errors, if any.
        /// </param>
        public Auditor(Annotator errorReporter)
        {
            this.BlockLevel = -1;
            this.blockTable = new List<ObjectRecord>();
            this.annotator = errorReporter;
        }

        /// <summary>
        /// Gets the current block level, i.e., the current scope level.
        /// </summary>
        public int BlockLevel { get; private set; }

        /// <summary>
        /// Gets the total length of objects defined in the current block.
        /// </summary>
        public int ObjectsLength
        {
            get
            {
                int count = 0;
                ObjectRecord record = this.blockTable[this.BlockLevel];
                while (record != null)
                {
                    if (record.MetaData.Kind == Kind.Array)
                    {
                        count = count + record.MetaData.UpperBound;
                    }
                    else if (record.MetaData.Kind != Kind.Procedure)
                    {
                        ++count;
                    }

                    record = record.Previous;
                }

                return count;
            }
        }

        /// <summary>
        /// Processes the starting of a new block.
        /// </summary>
        public void NewBlock()
        {
            ++(this.BlockLevel);
            this.blockTable.Add(null);
        }

        /// <summary>
        /// Processes the ending of a block.
        /// </summary>
        public void EndBlock()
        {
            this.blockTable[this.BlockLevel] = null;
            --(this.BlockLevel);
        }

        /// <summary>
        /// Gets the object record of an object by name.
        /// </summary>
        /// <param name="name">Name of the object.</param>
        /// <param name="objectRecord">Object record of the object.</param>
        public void Find(int name, ref ObjectRecord objectRecord)
        {
            bool found = false;
            int level = this.BlockLevel;
            while ((level >= 0) && !found)
            {
                found = this.Search(name, level, out objectRecord);
                --level;
            }

            if (!found)
            {
                this.annotator.ScopeError(ScopeErrorCategory.UndefinedName);
                Metadata metaData = new Metadata();
                metaData.Kind = Kind.Undefined;
                this.Define(name, metaData, ref objectRecord);
            }
        }

        /// <summary>
        /// Defines a new object.
        /// </summary>
        /// <param name="name">Name of the object.</param>
        /// <param name="metaData">Metadata of the object.</param>
        /// <param name="objectRecord">Object record of the object.</param>
        public void Define(int name,
                           Metadata metaData,
                           ref ObjectRecord objectRecord)
        {
            ObjectRecord other;
            bool found = false;
            if (name != Auditor.NoName)
            {
                int level = this.BlockLevel;
                while ((level >= 0) && !found)
                {
                    found = this.Search(name, level, out other);
                    --level;
                }
            }

            if (found)
            {
                this.annotator.ScopeError(ScopeErrorCategory.AmbiguousName);
            }
            else
            {
                bool isInputParameter = ((metaData.Kind == Kind.ReferenceParameter) ||
                                         (metaData.Kind == Kind.ValueParameter));
                bool isProcedure = (metaData.Kind == Kind.Procedure);

                objectRecord = new ObjectRecord();
                objectRecord.Name = name;
                objectRecord.Previous = this.blockTable[this.BlockLevel];
                metaData.Level = this.BlockLevel;
                ObjectRecord record = this.blockTable[this.BlockLevel];
                bool more = true;
                int displacement;  // Displacement of the first object in any scope.

                if (isInputParameter)
                {
                    // Starting displacement for input parameters.
                    displacement = -1;
                }
                else if (isProcedure)
                {
                    // No displacement necessary for procedure-names.
                    displacement = 0;
                }
                else
                {
                    // Starting displacement for 
                    //  variables, arrays, constants, return parameters.
                    displacement = 3;
                }

                while (more)
                {
                    if (record == null)
                    {
                        more = false;
                    }
                    else
                    {
                        if (isInputParameter)
                        {
                            if ((record.MetaData.Kind == Kind.ValueParameter) ||
                                (record.MetaData.Kind == Kind.ReferenceParameter))
                            {
                                --displacement;
                            }
                        }
                        else if (record.MetaData.Kind == Kind.Array)
                        {
                            displacement = displacement + record.MetaData.UpperBound;
                        }
                        else if (record.MetaData.Kind != Kind.Procedure)
                        {
                            ++displacement;
                        }

                        record = record.Previous;
                    }
                }

                metaData.Displacement = displacement;
                objectRecord.MetaData = metaData;
                this.blockTable[this.BlockLevel] = objectRecord;
            }
        }

        /// <summary>
        /// Searches for the given object in the specified scope level.
        /// </summary>
        /// <param name="name">Name of the object.</param>
        /// <param name="level">Scope level to search the object at.</param>
        /// <param name="objectRecord">Object record of the object.</param>
        /// <returns>
        /// A value indicating if the object is defined in the specified scope.
        /// </returns>
        bool Search(int name, int level, out ObjectRecord objectRecord)
        {
            bool more = true;
            bool found = false;
            objectRecord = blockTable[level];

            while (more)
            {
                if (objectRecord == null)
                {
                    more = false;
                }
                else if (objectRecord.Name == name)
                {
                    more = false;
                    found = true;
                }
                else
                {
                    objectRecord = objectRecord.Previous;
                }
            }

            return found;
        }
    }
}