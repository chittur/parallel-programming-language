/******************************************************************************
 * Filename    = MetaData.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = GrammarAnalysis
 *
 * Description = Defines the struct "MetaData", a record for holding the
 *               metadata of objects.
 *****************************************************************************/

using LanguageConstructs;

namespace GrammarAnalysis;

/// <summary>
/// A record for holding the metadata of objects.
/// </summary>
public struct Metadata
{
    /// <summary>
    /// Gets or sets the kind of the object: 
    /// Constant, Variable, Array, Undefined?
    /// </summary>
    public Kind Kind { get; set; }

    /// <summary>
    /// Gets or sets the type of the object: Integer, Boolean, Universal?
    /// </summary>
    public Type Type { get; set; }

    /// <summary>
    /// Gets or sets the value of a constant or variable.
    /// For objects of type boolean, true = 1 and false = 0.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Gets or sets the Upper bound, applicable for arrays. Lower bound is 1.
    /// </summary>
    public int UpperBound { get; set; }

    /// <summary>
    /// Gets or sets the scope level. 
    /// The scope level starts at 0, and goes up by 1 for each nesting.
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Gets or sets the displacement within the current scope.
    /// </summary>
    public int Displacement { get; set; }

    /// <summary>
    /// Gets or sets the procedure label, the starting label for procedures.
    /// </summary>
    public int ProcedureLabel { get; set; }
}
