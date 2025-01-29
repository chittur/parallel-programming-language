/******************************************************************************
 * Filename    = Type.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = LanguageConstructs
 *
 * Description = Defines the enum "Type", which enumerates all the types
 *               of objects supported in the language.
 *****************************************************************************/

namespace LanguageConstructs;

/// <summary>
/// Enumerates all the types of objects supported in the language.
/// </summary>
public enum Type
{
    /// <summary>
    /// A universal type; objects of this type do not have any specific type
    /// details as such.
    /// </summary>
    Universal,

    /// <summary>
    /// A Boolean type; objects of this type can hold the values true or false.
    /// </summary>
    Boolean,

    /// <summary>
    /// An integer type; objects of this type can hold a valid integer which
    /// ranges from 0 to a certain maximum value.
    /// </summary>
    Integer,

    /// <summary>
    /// A channel type; objects of this type represent a channel that acts as
    /// the medium of communication between two nodes that run in parallel.
    /// </summary>
    Channel,

    /// <summary>
    /// A void type; procedures can have return type of void.
    /// </summary>
    Void
}
