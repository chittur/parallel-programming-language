/******************************************************************************
 * Filename    = ObjectRecord.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = GrammarAnalysis
 *
 * Description = Defines the class "ObjectRecord", a linked list of the
 *               objects under any one scope level.
 *****************************************************************************/

using System.Collections.Generic;

namespace GrammarAnalysis
{
    /// <summary>
    /// A linked list of the objects under any one scope level.
    /// </summary>
    public class ObjectRecord
    {
        /// <summary>
        /// Creates an instance of ObjectRecord, a linked list of the objects 
        /// under any one scope level.
        /// </summary>
        public ObjectRecord()
        {
            this.Name = 0;
            this.Previous = null;
            this.ParameterRecordList = new List<ParameterRecord>();
            this.ProcedureRecord = new ProcedureRecord();
        }

        /// <summary>
        /// Gets of sets the name of the object.
        /// </summary>
        public int Name { get; set; }

        /// <summary>
        /// Gets or sets the previous object in this scope level.
        /// </summary>
        public ObjectRecord Previous { get; set; }

        /// <summary>
        /// Gets or sets the metadata of the object.
        /// </summary>
        public Metadata MetaData { get; set; }

        /// <summary>
        /// Gets or sets the parameter record list.
        /// </summary>
        public List<ParameterRecord> ParameterRecordList { get; set; }

        /// <summary>
        /// Gets or sets the procedure record.
        /// </summary>
        public ProcedureRecord ProcedureRecord { get; set; }
    }
}