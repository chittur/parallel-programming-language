/******************************************************************************
 * Filename    = Set.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = GrammarAnalysis
 *
 * Description = Defines the class "Set", a set for holding a list of symbols.
 *****************************************************************************/

using System.Collections.Generic;
using LanguageConstructs;

namespace GrammarAnalysis;

/// <summary>
/// A set for holding a list of symbols.
/// </summary>
public class Set
{
    private readonly List<Symbol> _symbolList;

    /// <summary>
    /// Creates a new instance of Set, a set for holding a list of symbols.
    /// </summary>
    /// <param name="sets">
    /// Sets containing the symbols to be added to the list.
    /// </param>
    public Set(params Set[] sets)
    {
        _symbolList = [];
        foreach (Set set in sets)
        {
            _symbolList.AddRange(set._symbolList);
        }
    }

    /// <summary>
    /// Creates a new instance of Set, a set for holding a list of symbols.
    /// </summary>
    /// <param name="set">
    /// Set containing the symbols to be added to the list.
    /// </param>
    /// <param name="symbols">
    /// Symbols to be added to the list.
    /// </param>
    public Set(Set set, params Symbol[] symbols)
    {
        _symbolList = new List<Symbol>(set._symbolList);
        foreach (Symbol symbol in symbols)
        {
            _symbolList.Add(symbol);
        }
    }

    /// <summary>
    /// Creates a new instance of Set, a set for holding a list of symbols.
    /// </summary>
    /// <param name="symbols">Symbols to be added to the list.</param>
    public Set(params Symbol[] symbols)
    {
        _symbolList = [.. symbols];
    }

    /// <summary>
    /// Does the set contain the given symbol?
    /// </summary>
    /// <param name="symbol">
    /// The symbol to be searched for existence in the set.
    /// </param>
    /// <returns>
    /// A value indicating if the set contains the specified symbol.
    /// </returns>
    public bool Contains(Symbol symbol)
    {
        return _symbolList.Contains(symbol);
    }
}
