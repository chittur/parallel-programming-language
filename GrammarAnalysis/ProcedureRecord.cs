﻿/******************************************************************************
 * Filename    = ProcedureRecord.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = GrammarAnalysis
 *
 * Description = Defines the class "ProcedureRecord", a record for holding 
 *               metadata of procedures.
 *****************************************************************************/

namespace GrammarAnalysis;

/// <summary>
/// A record for holding metadata of procedures.
/// </summary>
public class ProcedureRecord
{
    /// <summary>
    /// A constant value to indicate that the procedure does not access objects
    /// from any scope.
    /// </summary>
    public const int NoScope = -1;

    /// <summary>
    /// Creates an instance of ProcedureRecord, which is a record
    /// for holding metadata of procedures.
    /// </summary>
    public ProcedureRecord()
    {
        UsesIO = false;
        HighestScopeUsed = ProcedureRecord.NoScope;
        CallsParallelUnfriendly = false;
    }

    /// <summary>
    /// Gets or sets a value indicating if the procedure has I/O operations.
    /// </summary>
    public bool UsesIO { get; set; }

    /// <summary>
    /// Gets or sets a value for the highest scope of object accessed by the
    /// procedure.  0 is the highest possible scope.
    /// </summary>
    public int HighestScopeUsed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating if the procedure calls
    /// non parallel-friendly procedure(s).
    /// </summary>
    public bool CallsParallelUnfriendly { get; set; }
}
