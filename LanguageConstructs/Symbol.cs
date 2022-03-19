/******************************************************************************
 * Filename    = Symbol.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = LanguageConstructs
 *
 * Description = Defines the enum "Symbol", which enumerates all the symbols
 *               in the language.
 *****************************************************************************/

namespace LanguageConstructs
{
    /// <summary>
    /// Enumeration of all the symbols in the language.
    /// </summary>
    public enum Symbol
    {
        /// <summary>
        /// &amp;
        /// </summary>
        And,

        /// <summary>
        /// =
        /// </summary>
        Becomes,

        /// <summary>
        /// {
        /// </summary>
        Begin,

        /// <summary>
        /// boolean
        /// </summary>
        Boolean,

        /// <summary>
        /// channel
        /// </summary>
        Channel,

        /// <summary>
        /// ,
        /// </summary>
        Comma,

        /// <summary>
        /// constant
        /// </summary>
        Constant,

        /// <summary>
        /// /
        /// </summary>
        Divide,

        /// <summary>
        /// else
        /// </summary>
        Else,

        /// <summary>
        /// }
        /// </summary>
        End,

        /// <summary>
        /// [End of text]
        /// </summary>
        EndOfText,

        /// <summary>
        /// ==
        /// </summary>
        Equal,

        /// <summary>
        /// false
        /// </summary>
        False,

        /// <summary>
        /// >
        /// </summary>
        Greater,

        /// <summary>
        /// >=
        /// </summary>
        GreaterOrEqual,

        /// <summary>
        /// if
        /// </summary>
        If,

        /// <summary>
        /// integer
        /// </summary>
        Integer,

        /// <summary>
        /// [
        /// </summary>
        LeftBracket,

        /// <summary>
        /// (
        /// </summary>
        LeftParanthesis,

        /// <summary>
        /// &lt;
        /// </summary>
        Less,

        /// <summary>
        /// &lt;=
        /// </summary>
        LessOrEqual,

        /// <summary>
        /// -
        /// </summary>
        Minus,

        /// <summary>
        /// %
        /// </summary>
        Modulo,

        /// <summary>
        /// *
        /// </summary>
        Multiply,

        /// <summary>
        /// [Variable / Keyword]
        /// </summary>
        Name,

        /// <summary>
        /// !
        /// </summary>
        Not,

        /// <summary>
        /// !=
        /// </summary>
        NotEqual,

        /// <summary>
        /// [Integer]
        /// </summary>
        Numeral,

        /// <summary>
        /// open
        /// </summary>
        Open,

        /// <summary>
        /// |
        /// </summary>
        Or,

        /// <summary>
        /// +
        /// </summary>
        Plus,

        /// <summary>
        /// ^
        /// </summary>
        Power,

        /// <summary>
        /// parallel
        /// </summary>
        Parallel,

        /// <summary>
        /// @
        /// </summary>
        Procedure,

        /// <summary>
        /// auto-inserted at the beginning of the code
        /// </summary>
        Program,

        /// <summary>
        /// randomize
        /// </summary>
        Randomize,

        /// <summary>
        /// read
        /// </summary>
        Read,

        /// <summary>
        /// receive
        /// </summary>
        Receive,

        /// <summary>
        /// reference
        /// </summary>
        Reference,

        /// <summary>
        /// ]
        /// </summary>
        RightBracket,

        /// <summary>
        /// )
        /// </summary>
        RightParanthesis,

        /// <summary>
        /// ;
        /// </summary>
        SemiColon,

        /// <summary>
        /// send
        /// </summary>
        Send,

        /// <summary>
        /// [Integer out of bounds / Invalid integer]
        /// </summary>
        IntegerOutOfBounds,

        /// <summary>
        /// ->
        /// </summary>
        Through,

        /// <summary>
        /// true
        /// </summary>
        True,

        /// <summary>
        /// [Unknown symbol]
        /// </summary>
        Unknown,

        /// <summary>
        /// while
        /// </summary>
        While,

        /// <summary>
        /// write
        /// </summary>
        Write
    }
}