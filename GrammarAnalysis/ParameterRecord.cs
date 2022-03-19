/******************************************************************************
 * Filename    = ParameterRecord.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = GrammarAnalysis
 *
 * Description = Defines the struct "ParameterRecord", a record for
 *               holding metadata of parameters.
 *****************************************************************************/

using LanguageConstructs;
using Type = LanguageConstructs.Type;

namespace GrammarAnalysis
{
    /// <summary>
    /// A record for holding metadata of parameters.
    /// </summary>
    public struct ParameterRecord
    {
        /// <summary>
        /// Gets or sets the type of the parameter.
        /// </summary>
        public Type ParameterType { get; set; }

        /// <summary>
        /// Gets or sets the kind of the parameter.
        /// </summary>
        public Kind ParameterKind { get; set; }
    }
}
