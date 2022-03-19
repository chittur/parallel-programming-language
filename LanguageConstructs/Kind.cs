/******************************************************************************
 * Filename    = Kind.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = LanguageConstructs
 *
 * Description = Defines the enum "Kind", which enumerates all the kinds
 *               of objects supported in the language.
 *****************************************************************************/

namespace LanguageConstructs
{
    /// <summary>
    /// Enumerates all the kinds of objects supported in the language.
    /// </summary>
    public enum Kind
    {
        /// <summary>
        /// An undefined kind.
        /// </summary>
        Undefined,

        /// <summary>
        /// Objects of this kind always carry a constant value. 
        /// </summary>
        Constant,

        /// <summary>
        /// Objects of this kind may have their values changed.
        /// </summary>
        Variable,

        /// <summary>
        /// Objects of this kind represent an array of variables.
        /// </summary>
        Array,

        /// <summary>
        /// Objects of this kind are value parameters to a procedure.
        /// </summary>
        ValueParameter,

        /// <summary>
        /// Objects of this kind are reference parameters to a procedure.
        /// </summary>
        ReferenceParameter,

        /// <summary>
        /// An object of this kind is the return parameter to a procedure.
        /// </summary>
        ReturnParameter,

        /// <summary>
        /// Names of this kind represent procedures.
        /// </summary>
        Procedure
    }
}