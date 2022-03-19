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

using LanguageConstructs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace LexicalAnalysis
{
    /// <summary>
    /// Provides methods to scan a code file, and detect all the symbols in it.
    /// </summary>
    public class Scanner
    {
        readonly List<WordRecord> wordList; // List of variables + keywords.
        readonly TextReader reader; // Text input to be scanned.

        /// <summary>
        /// Creates an instance of the Scanner, which provides methods to scan a
        /// code file, and detect all the symbols in it.
        /// </summary>
        /// <param name="reader">
        /// Text input to be scanned.
        /// </param>
        public Scanner(TextReader reader)
        {
            this.LineNumber = 1;
            this.IsLineCorrect = true;
            this.CurrentSymbol = Symbol.Unknown;
            this.Argument = -1;
            this.wordList = new List<WordRecord>();
            this.reader = reader;

            // Define the keywords in this language.
            this.DefineKeywords();
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
            this.IsLineCorrect = false;
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
            char c = this.Peek();
            while (this.IsDelimiter(c)) // Delimiter?
            {
                if (c == '$')             // Comment
                {
                    this.SkipLine();
                    this.NewLine();
                }
                else
                {
                    if (c == '\n')
                    {
                        this.NewLine();
                    }

                    this.Read();
                }

                c = this.Peek();
            }

            if (char.IsLetter(c)) // Variable / Keyword?
            {
                StringBuilder text = new StringBuilder(c.ToString());
                this.Read();
                c = this.Peek();
                while (char.IsLetterOrDigit(c) || (c == '_'))
                {
                    text.Append(c);
                    this.Read();
                    c = this.Peek();
                }

                Search(text.ToString());
            }
            else if (char.IsDigit(c)) // Number (integer)?
            {
                StringBuilder text = new StringBuilder(c.ToString());
                this.Read();
                c = this.Peek();
                while (char.IsDigit(c))
                {
                    text.Append(c);
                    this.Read();
                    c = this.Peek();
                }

                bool result = int.TryParse(text.ToString(), out int value);
                if (result)
                {
                    this.CurrentSymbol = Symbol.Numeral;
                    this.Argument = value;
                }
                else
                {
                    this.CurrentSymbol = Symbol.IntegerOutOfBounds;
                }
            }
            else
            {
                switch (c)
                {
                    case '@':
                        {
                            this.Read();
                            this.CurrentSymbol = Symbol.Procedure;

                            break;
                        }

                    case ';':           // Semicolon
                        {
                            this.Read();
                            this.CurrentSymbol = Symbol.SemiColon;

                            break;
                        }

                    case ',':           // Comma
                        {
                            this.Read();
                            this.CurrentSymbol = Symbol.Comma;

                            break;
                        }

                    case '&':           // And
                        {
                            this.Read();
                            this.CurrentSymbol = Symbol.And;

                            break;
                        }

                    case '|':           // Or
                        {
                            this.Read();
                            this.CurrentSymbol = Symbol.Or;

                            break;
                        }

                    case '!':           // Not or NotEqual
                        {
                            this.Read();
                            c = this.Peek();
                            if (c == '=')     // NotEqual
                            {
                                this.Read();
                                this.CurrentSymbol = Symbol.NotEqual;
                            }
                            else              // Not
                            {
                                this.CurrentSymbol = Symbol.Not;
                            }

                            break;
                        }

                    case '=':           // Becomes or Equal
                        {
                            this.Read();
                            c = this.Peek();
                            if (c == '=')     // Equal
                            {
                                this.Read();
                                this.CurrentSymbol = Symbol.Equal;
                            }
                            else              // Becomes
                            {
                                this.CurrentSymbol = Symbol.Becomes;
                            }

                            break;
                        }

                    case '>':           // GreaterOrEqual or Greater
                        {
                            this.Read();
                            c = this.Peek();
                            if (c == '=')     // GreaterOrEqual
                            {
                                this.Read();
                                this.CurrentSymbol = Symbol.GreaterOrEqual;
                            }
                            else              // Greater
                            {
                                this.CurrentSymbol = Symbol.Greater;
                            }

                            break;
                        }

                    case '<':           // LessOrEqual or Less
                        {
                            this.Read();
                            c = this.Peek();
                            if (c == '=')     // LessOrEqual
                            {
                                this.Read();
                                this.CurrentSymbol = Symbol.LessOrEqual;
                            }
                            else              // Less
                            {
                                this.CurrentSymbol = Symbol.Less;
                            }

                            break;
                        }

                    case '+':           // Plus
                        {
                            this.Read();
                            this.CurrentSymbol = Symbol.Plus;

                            break;
                        }

                    case '-':           // Through or Minus
                        {
                            this.Read();
                            c = this.Peek();
                            if (c == '>')     // Through
                            {
                                this.Read();
                                this.CurrentSymbol = Symbol.Through;
                            }
                            else              // Minus
                            {
                                this.CurrentSymbol = Symbol.Minus;
                            }

                            break;
                        }

                    case '*':           // Multiply
                        {
                            this.Read();
                            this.CurrentSymbol = Symbol.Multiply;

                            break;
                        }

                    case '/':           // Divide
                        {
                            this.Read();
                            this.CurrentSymbol = Symbol.Divide;

                            break;
                        }

                    case '%':           // Modulo
                        {
                            this.Read();
                            this.CurrentSymbol = Symbol.Modulo;

                            break;
                        }

                    case '^':           // Power
                        {
                            this.Read();
                            this.CurrentSymbol = Symbol.Power;

                            break;
                        }

                    case '{':           // Begin
                        {
                            this.Read();
                            this.CurrentSymbol = Symbol.Begin;

                            break;
                        }

                    case '}':           // End
                        {
                            this.Read();
                            this.CurrentSymbol = Symbol.End;

                            break;
                        }

                    case '[':           // LeftBracket
                        {
                            this.Read();
                            this.CurrentSymbol = Symbol.LeftBracket;

                            break;
                        }

                    case ']':           // RightBracket
                        {
                            this.Read();
                            this.CurrentSymbol = Symbol.RightBracket;

                            break;
                        }

                    case '(':           // LeftParanthesis
                        {
                            this.Read();
                            this.CurrentSymbol = Symbol.LeftParanthesis;

                            break;
                        }

                    case ')':           // RightParanthesis
                        {
                            this.Read();
                            this.CurrentSymbol = Symbol.RightParanthesis;

                            break;
                        }

                    case char.MaxValue:
                    case char.MinValue: // EndOfText
                        {
                            this.CurrentSymbol = Symbol.EndOfText;
                            next = false;

                            break;
                        }

                    default:            // Unknown
                        {
                            this.Read();
                            this.CurrentSymbol = Symbol.Unknown;
                            c = this.Peek();
                            while ((!(this.IsDelimiter(c))) && (c != char.MinValue))
                            {
                                this.Read();
                                c = this.Peek();
                            }

                            break;
                        }
                }
            }

            Trace.WriteLine($"Scanner: Symbol = {this.CurrentSymbol}, Line = {this.LineNumber}.");
            return next;
        }

        /// <summary>
        /// Defines the keywords in the language.
        /// </summary>
        void DefineKeywords()
        {
            this.DefineKeyword("boolean", Symbol.Boolean);
            this.DefineKeyword("channel", Symbol.Channel);
            this.DefineKeyword("constant", Symbol.Constant);
            this.DefineKeyword("else", Symbol.Else);
            this.DefineKeyword("false", Symbol.False);
            this.DefineKeyword("if", Symbol.If);
            this.DefineKeyword("integer", Symbol.Integer);
            this.DefineKeyword("open", Symbol.Open);
            this.DefineKeyword("parallel", Symbol.Parallel);
            this.DefineKeyword("randomize", Symbol.Randomize);
            this.DefineKeyword("read", Symbol.Read);
            this.DefineKeyword("receive", Symbol.Receive);
            this.DefineKeyword("reference", Symbol.Reference);
            this.DefineKeyword("send", Symbol.Send);
            this.DefineKeyword("true", Symbol.True);
            this.DefineKeyword("while", Symbol.While);
            this.DefineKeyword("write", Symbol.Write);
        }

        /// <summary>
        /// Defines a variable.
        /// </summary>
        /// <param name="spelling">
        /// Variable name.
        /// </param>
        void DefineVariable(string spelling)
        {
            this.wordList.Add(new WordRecord(spelling));
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
        void DefineKeyword(string spelling, Symbol symbol)
        {
            this.wordList.Add(new WordRecord(spelling, symbol));
        }

        /// <summary>
        /// Searches for the given word in the word list.
        /// If not found, defines the variable.
        /// Sets the current symbol and Argument.
        /// </summary>
        /// <param name="word">
        /// The word (variable / keyword) to be searched.
        /// </param>
        void Search(string word)
        {
            int count = 0;
            int max = this.wordList.Count;
            bool found = false;
            while ((count < max) && !found)
            {
                if (this.wordList[count].Spelling == word)
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
                if (this.wordList[count].IsVariable)
                {
                    this.CurrentSymbol = Symbol.Name;
                    this.Argument = this.wordList[count].Argument;
                }
                else
                {
                    this.CurrentSymbol = this.wordList[count].Symbol;
                }
            }
            else
            {
                this.CurrentSymbol = Symbol.Name;
                this.Argument = WordRecord.VariableCount;
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
        char Next(bool read)
        {
            char c;
            if (this.reader == null)
            {
                c = char.MaxValue;
            }
            else
            {
                int value;
                value = read ? this.reader.Read() : this.reader.Peek();
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
        char Peek()
        {
            return this.Next(false);
        }

        /// <summary>
        /// Reads the next character in the stream.
        /// </summary>
        /// <returns>
        /// The next character in the stream.
        /// For EOF:         returns char.MinValue
        /// For null stream: returns char.MaxValue
        /// </returns>
        char Read()
        {
            return this.Next(true);
        }

        /// <summary>
        /// Skips the current line.
        /// </summary>
        void SkipLine()
        {
            this.reader.ReadLine();
        }

        /// <summary>
        /// Processes start of a new line.
        /// </summary>
        void NewLine()
        {
            if (!(this.IsLineCorrect))
            {
                this.IsLineCorrect = true;
            }

            ++(this.LineNumber);
        }

        /// <summary>
        /// Is the given character a delimiter character?
        /// </summary>
        /// <param name="c">The given character.</param>
        /// <returns>
        /// A value indicating if the current character is a delimiter or not.
        /// </returns>
        bool IsDelimiter(char c)
        {
            return ((c == '$') ||
                    (c == ' ') ||
                    (c == '\t') ||
                    (c == '\n') ||
                    (c == '\r'));
        }
    }
}