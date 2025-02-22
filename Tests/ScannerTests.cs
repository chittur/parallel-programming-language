/******************************************************************************
 * Filename    = ScannerTests.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = UnitTesting
 *
 * Description = Unit tests for the Scanner class.
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using LanguageConstructs;
using LexicalAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests;

/// <summary>
/// Unit tests for the Scanner class.
/// </summary>
[TestClass]
public class ScannerTests
{
    /// <summary>
    /// Tests all the symbols supported by the language.
    /// </summary>
    [TestMethod]
    public void TestSymbols()
    {
        // Code with the list of all symbols supported by the language.
        const string Code = @"& = { boolean channel , constant / else } == false > >= if 
                             integer [ ( < <= - % * hello ! != 234 open | + ^ parallel 
                             @ randomize read receive reference ] ) ; send
                             12345678901234567890 -> true # ##~~ while write";

        // Expected set of symbols fot the code above.
        Symbol[] expected =
            [
                Symbol.And,
                Symbol.Becomes,
                Symbol.Begin,
                Symbol.Boolean,
                Symbol.Channel,
                Symbol.Comma,
                Symbol.Constant,
                Symbol.Divide,
                Symbol.Else,
                Symbol.End,
                Symbol.Equal,
                Symbol.False,
                Symbol.Greater,
                Symbol.GreaterOrEqual,
                Symbol.If,
                Symbol.Integer,
                Symbol.LeftBracket,
                Symbol.LeftParanthesis,
                Symbol.Less,
                Symbol.LessOrEqual,
                Symbol.Minus,
                Symbol.Modulo,
                Symbol.Multiply,
                Symbol.Name,
                Symbol.Not,
                Symbol.NotEqual,
                Symbol.Numeral,
                Symbol.Open,
                Symbol.Or,
                Symbol.Plus,
                Symbol.Power,
                Symbol.Parallel,
                Symbol.Procedure,
                Symbol.Randomize,
                Symbol.Read,
                Symbol.Receive,
                Symbol.Reference,
                Symbol.RightBracket,
                Symbol.RightParanthesis,
                Symbol.SemiColon,
                Symbol.Send,
                Symbol.IntegerOutOfBounds,
                Symbol.Through,
                Symbol.True,
                Symbol.Unknown,
                Symbol.Unknown,
                Symbol.While,
                Symbol.Write,
                Symbol.EndOfText,
            ];

        // Feed the code to the scanner.
        TextReader reader = new StringReader(Code);
        Scanner scanner = new Scanner(reader);

        // Scan through till the end of the code, and get all symbols.
        List<Symbol> actual = [];
        while (scanner.NextSymbol())
        {
            actual.Add(scanner.CurrentSymbol);
        }
        actual.Add(scanner.CurrentSymbol);  // Scanner should set 'end of text' at the end.

        // Validate that the scanner has the correct line number in code.
        Assert.IsTrue(scanner.IsLineCorrect);
        Assert.AreEqual(scanner.LineNumber, 4 /* The number of lines in code above. */);

        // Validate that the count of symbols as well as their expected occurence matches.
        int length = actual.Count;
        Assert.AreEqual(expected.Length, length);
        if (expected.Length == length)
        {
            for (int i = 0; i < length; ++i)
            {
                Assert.AreEqual(actual[i], expected[i]);
            }
        }
    }

    /// <summary>
    /// Tests the argument when the symbol is a numeral or a name.
    /// </summary>
    [TestMethod]
    public void TestArguments()
    {
        // Code with numerals and names.
        const string Code = @"1234 hello new hello 9876";

        // Feed the code to the scanner.
        TextReader reader = new StringReader(Code);
        Scanner scanner = new Scanner(reader);

        // Expect a numeral and its value as the argument.
        bool next = scanner.NextSymbol();
        Assert.IsTrue(next);
        Assert.AreEqual(scanner.CurrentSymbol, Symbol.Numeral);
        Assert.AreEqual(scanner.Argument, 1234);

        // Expect a name and its occurence as the argument.
        next = scanner.NextSymbol();
        Assert.IsTrue(next);
        Assert.AreEqual(scanner.CurrentSymbol, Symbol.Name);
        int occurence = scanner.Argument;

        // Expect a new name and its occurence as the argument.
        next = scanner.NextSymbol();
        Assert.IsTrue(next);
        Assert.AreEqual(scanner.CurrentSymbol, Symbol.Name);
        Assert.AreNotEqual(scanner.Argument, occurence);

        // Expect the re-occurence of a name, and its original argument.
        next = scanner.NextSymbol();
        Assert.IsTrue(next);
        Assert.AreEqual(scanner.CurrentSymbol, Symbol.Name);
        Assert.AreEqual(scanner.Argument, occurence);

        // Expect another numeral and its value as the argument.
        next = scanner.NextSymbol();
        Assert.IsTrue(next);
        Assert.AreEqual(scanner.CurrentSymbol, Symbol.Numeral);
        Assert.AreEqual(scanner.Argument, 9876);

        // Expect end of scanning.
        next = scanner.NextSymbol();
        Assert.IsFalse(next);
        Assert.AreEqual(scanner.CurrentSymbol, Symbol.EndOfText);
    }

    /// <summary>
    /// Tests scanner when an empty reader is passed.
    /// </summary>
    [TestMethod]
    public void TestEmptyProgram()
    {
        // Feed an empty reader to the scanner.
        Scanner scanner = new Scanner(reader: null);

        bool next = scanner.NextSymbol();
        Assert.IsFalse(next);
    }

    /// <summary>
    /// Tests 'line is incorrect' functionality of the scanner.
    /// </summary>
    [TestMethod]
    public void TestLineIsIncorrect()
    {
        // Arrange

        // Set the line to be incorrect.
        Scanner scanner = new Scanner(new StringReader($"{Environment.NewLine}{{}}"));
        scanner.SetLineIsIncorrect();
        Assert.IsFalse(scanner.IsLineCorrect);

        // Act

        // The next symbol is a new line, so IsLineCorrect will be set to true after that.
        bool next = scanner.NextSymbol();

        // Assert
        Assert.IsTrue(next);
        Assert.IsTrue(scanner.IsLineCorrect);
    }
}
