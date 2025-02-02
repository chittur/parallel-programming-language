/******************************************************************************
 * Filename    = WordRecord.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = LexicalAnalysis
 *
 * Description = Defines the class "WordRecord", a record for variables and 
 *               keywords.
 *****************************************************************************/

using System.Diagnostics;
using LanguageConstructs;

namespace LexicalAnalysis;

/// <summary>
/// Record for variables and keywords.
/// </summary>
internal class WordRecord
{
    /// <summary>
    /// Is the word a variable? if not, it is a keyword.
    /// </summary>
    public readonly bool IsVariable;

    /// <summary>
    /// Spelling of the word.
    /// </summary>
    public readonly string Spelling;

    /// <summary>
    /// Symbol of the word.
    /// </summary>
    public readonly Symbol Symbol;

    /// <summary>
    /// Argument of the word.
    /// </summary>
    public readonly int Argument;

    // Static constructor. Initializes the static variable.
    static WordRecord()
    {
        WordRecord.VariableCount = 0;
    }

    /// <summary>
    /// Defines a new word record for a variable.
    /// </summary>
    /// <param name="spelling">
    /// Variable name.
    /// </param>
    public WordRecord(string spelling)
    {
        IsVariable = true;
        Spelling = spelling;
        Symbol = Symbol.Name;
        Argument = WordRecord.VariableCount;

        ++WordRecord.VariableCount;
    }

    /// <summary>
    /// Defines a new word record for a keyword.
    /// </summary>
    /// <param name="spelling">
    /// Keyword.
    /// </param>
    /// <param name="symbol">
    /// Symbol of the keyword.
    /// </param>
    public WordRecord(string spelling, Symbol symbol)
    {
        Debug.Assert(symbol != Symbol.Name);
        Spelling = spelling;
        Symbol = symbol;
        IsVariable = false;
        Argument = -1;
    }

    /// <summary>
    /// Gets the total number of defined variables.
    /// </summary>
    public static int VariableCount { get; private set; }
}
