/******************************************************************************
 * Filename    = Scanner.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = LexicalAnalysis
 *
 * Description = Defines the class "Scanner", which provides methods to scan a
 *               code file, and detect all the symbols in it.
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using LanguageConstructs;

namespace LexicalAnalysis;

/// <summary>
/// Provides methods to scan a code file, and detect all the symbols in it.
/// </summary>
public class Scanner
{
    private readonly List<WordRecord> _wordList; // List of variables + keywords.
    private readonly TextReader _reader;         // Text input to be scanned.

    /// <summary>
    /// Creates an instance of the Scanner, which provides methods to scan a
    /// code file, and detect all the symbols in it.
    /// </summary>
    /// <param name="reader">
    /// Text input to be scanned.
    /// </param>
    public Scanner(TextReader reader)
    {
        LineNumber = 1;
        IsLineCorrect = true;
        CurrentSymbol = Symbol.Unknown;
        Argument = -1;
        _wordList = [];
        _reader = reader;

        // Define the keywords in this language.
        DefineKeywords();
    }

    /// <summary>
    /// Gets a value indicating if the current line is without errors so far.
    /// </summary>
    public bool IsLineCorrect { get; private set; }

    /// <summary>
    /// Sets the value which indicates that the current line has error(s).
    /// </summary>
    public void SetLineIsIncorrect()
    {
        IsLineCorrect = false;
    }

    /// <summary>
    /// Gets the current symbol.
    /// </summary>
    public Symbol CurrentSymbol { get; private set; }

    /// <summary>
    /// Gets the current Argument.
    /// </summary>
    public int Argument { get; private set; }

    /// <summary>
    /// Gets the current line number.
    /// </summary>
    public int LineNumber { get; private set; }

    /// <summary>
    /// Finds the next symbol in the input stream being scanned.
    /// </summary>
    /// <returns>
    /// A value indicating if scanning may proceed or not.
    /// </returns>
    public bool NextSymbol()
    {
        bool next = true;
        char c = Peek();
        while (IsDelimiter(c)) // Delimiter?
        {
            if (c == '$')             // Comment
            {
                SkipLine();
                NewLine();
            }
            else
            {
                if (c == '\n')
                {
                    NewLine();
                }

                Read();
            }

            c = Peek();
        }

        if (char.IsLetter(c)) // Variable / Keyword?
        {
            StringBuilder text = new StringBuilder(c.ToString());
            Read();
            c = Peek();
            while (char.IsLetterOrDigit(c) || (c == '_'))
            {
                text.Append(c);
                Read();
                c = Peek();
            }

            Search(text.ToString());
        }
        else if (char.IsDigit(c)) // Number (integer)?
        {
            StringBuilder text = new StringBuilder(c.ToString());
            Read();
            c = Peek();
            while (char.IsDigit(c))
            {
                text.Append(c);
                Read();
                c = Peek();
            }

            bool result = int.TryParse(text.ToString(), out int value);
            if (result)
            {
                CurrentSymbol = Symbol.Numeral;
                Argument = value;
            }
            else
            {
                CurrentSymbol = Symbol.IntegerOutOfBounds;
            }
        }
        else
        {
            switch (c)
            {
                case '@':
                    {
                        Read();
                        CurrentSymbol = Symbol.Procedure;

                        break;
                    }

                case ';':           // Semicolon
                    {
                        Read();
                        CurrentSymbol = Symbol.SemiColon;

                        break;
                    }

                case ',':           // Comma
                    {
                        Read();
                        CurrentSymbol = Symbol.Comma;

                        break;
                    }

                case '&':           // And
                    {
                        Read();
                        CurrentSymbol = Symbol.And;

                        break;
                    }

                case '|':           // Or
                    {
                        Read();
                        CurrentSymbol = Symbol.Or;

                        break;
                    }

                case '!':           // Not or NotEqual
                    {
                        Read();
                        c = Peek();
                        if (c == '=')     // NotEqual
                        {
                            Read();
                            CurrentSymbol = Symbol.NotEqual;
                        }
                        else              // Not
                        {
                            CurrentSymbol = Symbol.Not;
                        }

                        break;
                    }

                case '=':           // Becomes or Equal
                    {
                        Read();
                        c = Peek();
                        if (c == '=')     // Equal
                        {
                            Read();
                            CurrentSymbol = Symbol.Equal;
                        }
                        else              // Becomes
                        {
                            CurrentSymbol = Symbol.Becomes;
                        }

                        break;
                    }

                case '>':           // GreaterOrEqual or Greater
                    {
                        Read();
                        c = Peek();
                        if (c == '=')     // GreaterOrEqual
                        {
                            Read();
                            CurrentSymbol = Symbol.GreaterOrEqual;
                        }
                        else              // Greater
                        {
                            CurrentSymbol = Symbol.Greater;
                        }

                        break;
                    }

                case '<':           // LessOrEqual or Less
                    {
                        Read();
                        c = Peek();
                        if (c == '=')     // LessOrEqual
                        {
                            Read();
                            CurrentSymbol = Symbol.LessOrEqual;
                        }
                        else              // Less
                        {
                            CurrentSymbol = Symbol.Less;
                        }

                        break;
                    }

                case '+':           // Plus
                    {
                        Read();
                        CurrentSymbol = Symbol.Plus;

                        break;
                    }

                case '-':           // Through or Minus
                    {
                        Read();
                        c = Peek();
                        if (c == '>')     // Through
                        {
                            Read();
                            CurrentSymbol = Symbol.Through;
                        }
                        else              // Minus
                        {
                            CurrentSymbol = Symbol.Minus;
                        }

                        break;
                    }

                case '*':           // Multiply
                    {
                        Read();
                        CurrentSymbol = Symbol.Multiply;

                        break;
                    }

                case '/':           // Divide
                    {
                        Read();
                        CurrentSymbol = Symbol.Divide;

                        break;
                    }

                case '%':           // Modulo
                    {
                        Read();
                        CurrentSymbol = Symbol.Modulo;

                        break;
                    }

                case '^':           // Power
                    {
                        Read();
                        CurrentSymbol = Symbol.Power;

                        break;
                    }

                case '{':           // Begin
                    {
                        Read();
                        CurrentSymbol = Symbol.Begin;

                        break;
                    }

                case '}':           // End
                    {
                        Read();
                        CurrentSymbol = Symbol.End;

                        break;
                    }

                case '[':           // LeftBracket
                    {
                        Read();
                        CurrentSymbol = Symbol.LeftBracket;

                        break;
                    }

                case ']':           // RightBracket
                    {
                        Read();
                        CurrentSymbol = Symbol.RightBracket;

                        break;
                    }

                case '(':           // LeftParanthesis
                    {
                        Read();
                        CurrentSymbol = Symbol.LeftParanthesis;

                        break;
                    }

                case ')':           // RightParanthesis
                    {
                        Read();
                        CurrentSymbol = Symbol.RightParanthesis;

                        break;
                    }

                case char.MaxValue:
                case char.MinValue: // EndOfText
                    {
                        CurrentSymbol = Symbol.EndOfText;
                        next = false;

                        break;
                    }

                default:            // Unknown
                    {
                        Read();
                        CurrentSymbol = Symbol.Unknown;
                        c = Peek();
                        while ((!IsDelimiter(c)) && (c != char.MinValue))
                        {
                            Read();
                            c = Peek();
                        }

                        break;
                    }
            }
        }

        Trace.WriteLine($"Scanner: Symbol = {CurrentSymbol}, Line = {LineNumber}.");
        return next;
    }

    /// <summary>
    /// Defines the keywords in the language.
    /// </summary>
    private void DefineKeywords()
    {
        DefineKeyword("boolean", Symbol.Boolean);
        DefineKeyword("channel", Symbol.Channel);
        DefineKeyword("constant", Symbol.Constant);
        DefineKeyword("else", Symbol.Else);
        DefineKeyword("false", Symbol.False);
        DefineKeyword("if", Symbol.If);
        DefineKeyword("integer", Symbol.Integer);
        DefineKeyword("open", Symbol.Open);
        DefineKeyword("parallel", Symbol.Parallel);
        DefineKeyword("randomize", Symbol.Randomize);
        DefineKeyword("read", Symbol.Read);
        DefineKeyword("receive", Symbol.Receive);
        DefineKeyword("reference", Symbol.Reference);
        DefineKeyword("send", Symbol.Send);
        DefineKeyword("true", Symbol.True);
        DefineKeyword("while", Symbol.While);
        DefineKeyword("write", Symbol.Write);
    }

    /// <summary>
    /// Defines a variable.
    /// </summary>
    /// <param name="spelling">
    /// Variable name.
    /// </param>
    private void DefineVariable(string spelling)
    {
        _wordList.Add(new WordRecord(spelling));
    }

    /// <summary>
    /// Defines a keyword.
    /// </summary>
    /// <param name="spelling">
    /// Keyword.
    /// </param>
    /// <param name="symbol">
    /// Symbol for the keyword.
    /// </param>
    private void DefineKeyword(string spelling, Symbol symbol)
    {
        _wordList.Add(new WordRecord(spelling, symbol));
    }

    /// <summary>
    /// Searches for the given word in the word list.
    /// If not found, defines the variable.
    /// Sets the current symbol and Argument.
    /// </summary>
    /// <param name="word">
    /// The word (variable / keyword) to be searched.
    /// </param>
    private void Search(string word)
    {
        int count = 0;
        int max = _wordList.Count;
        bool found = false;
        while ((count < max) && !found)
        {
            if (_wordList[count].Spelling == word)
            {
                found = true;
            }
            else
            {
                ++count;
            }
        }

        if (found)
        {
            if (_wordList[count].IsVariable)
            {
                CurrentSymbol = Symbol.Name;
                Argument = _wordList[count].Argument;
            }
            else
            {
                CurrentSymbol = _wordList[count].Symbol;
            }
        }
        else
        {
            CurrentSymbol = Symbol.Name;
            Argument = WordRecord.VariableCount;
            DefineVariable(word);
        }
    }

    /// <summary>
    /// Gets the next character in the stream.
    /// </summary>
    /// <param name="read">
    /// read = true:  reads the next character.
    ///      = false: peeks at the next character.
    /// </param>
    /// <returns>
    /// The next character in the stream.
    /// For EOF:         returns char.MinValue
    /// For null stream: returns char.MaxValue
    /// </returns>
    private char Next(bool read)
    {
        char c;
        if (_reader == null)
        {
            c = char.MaxValue;
        }
        else
        {
            int value;
            value = read ? _reader.Read() : _reader.Peek();
            c = (value == -1) ? char.MinValue : Convert.ToChar(value);
        }

        return c;
    }

    /// <summary>
    /// Peeks at the next character in the stream.
    /// </summary>
    /// <returns>
    /// The next character in the stream.
    /// For EOF:         returns char.MinValue
    /// For null stream: returns char.MaxValue
    /// </returns>
    private char Peek()
    {
        return Next(false);
    }

    /// <summary>
    /// Reads the next character in the stream.
    /// </summary>
    /// <returns>
    /// The next character in the stream.
    /// For EOF:         returns char.MinValue
    /// For null stream: returns char.MaxValue
    /// </returns>
    private char Read()
    {
        return Next(true);
    }

    /// <summary>
    /// Skips the current line.
    /// </summary>
    private void SkipLine()
    {
        _reader.ReadLine();
    }

    /// <summary>
    /// Processes start of a new line.
    /// </summary>
    private void NewLine()
    {
        if (!IsLineCorrect)
        {
            IsLineCorrect = true;
        }

        ++LineNumber;
    }

    /// <summary>
    /// Is the given character a delimiter character?
    /// </summary>
    /// <param name="c">The given character.</param>
    /// <returns>
    /// A value indicating if the current character is a delimiter or not.
    /// </returns>
    private static bool IsDelimiter(char c)
    {
        return (c == '$') ||
               (c == ' ') ||
               (c == '\t') ||
               (c == '\n') ||
                (c == '\r');
    }
}
