/******************************************************************************
 * Filename    = Parser.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = Compilation
 *
 * Description = Defines the class "Parser", which utilizes the lexical 
 *               analysis, grammar analysis, and code generation libraries
 *               to perform compilation.
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CodeGeneration;
using ErrorReporting;
using GrammarAnalysis;
using LanguageConstructs;
using LexicalAnalysis;
using Type = LanguageConstructs.Type;

namespace Compilation;

/// <summary>
/// Provides methods to perform compilation.
/// </summary>
public class Parser
{
    private Stack<int> _procedureNest; // Current scope of nested procedures.
    private Scanner _scanner;          // Lexical analysis.
    private Annotator _annotator;      // Error reporting.
    private Auditor _auditor;          // Scope analysis.
    private Assembler _assembler;      // Code generation.

    /// <summary>
    /// Category of the operation for object access / expression.
    /// </summary>
    private enum OperationCategory
    {
        None,
        Read,
        Write,
        Open,
        Randomize
    }

    /// <summary>
    /// Creates an instance of Parser, which provides methods 
    /// to perform compilation.
    /// </summary>
    public Parser() { }

    /// <summary>
    /// Compiles the source code and generates a file with the intermediate
    /// code if the compilation is successful.
    /// </summary>
    /// <param name="reader">Stream pointing to the source code.</param>
    /// <param name="intermediateCodeFilename">
    /// Filename of the intermediate code file to be generated if the
    /// compilation is successful.
    /// </param>
    /// <returns>
    /// A value indicating if the compilation is successful.
    /// </returns>
    public bool Compile(TextReader reader, string intermediateCodeFilename)
    {
        return Compile(reader, intermediateCodeFilename, null);
    }

    /// <summary>
    /// Compiles the source code and generates a file with the intermediate
    /// code if the compilation is successful.
    /// </summary>
    /// <param name="reader">Stream pointing to the source code.</param>
    /// <param name="intermediateCodeFilename">
    /// Filename of the intermediate code file to be generated if the
    /// compilation is successful.
    /// </param>
    /// <param name="onPrintErrorReport">
    /// Optional delegate to print compilation errors.
    /// </param>
    /// <returns>
    /// A value indicating if the compilation is successful.
    /// </returns>
    public bool Compile(TextReader reader,
                        string intermediateCodeFilename,
                        Annotator.OnPrintErrorReport onPrintErrorReport)
    {
        _procedureNest = new Stack<int>();
        _scanner = new Scanner(reader);
        _annotator = new Annotator(onPrintErrorReport ?? PrintCompilationError);
        _auditor = new Auditor(_annotator);
        _assembler = new Assembler(_annotator);

        // Define stop symbols.
        Set stopSymbols = new Set(Symbol.EndOfText);

        // Parse.
        bool proceed = _scanner.NextSymbol();
        if (proceed)
        {
            Program(stopSymbols);
            if (!IsCurrentSymbol(Symbol.EndOfText))
            {
                ReportSyntaxErrorAndRecover(stopSymbols);
            }

            proceed = _annotator.ErrorFree;
            if (proceed)
            {
                _assembler.GenerateExecutable(intermediateCodeFilename);
            }
        }
        else
        {
            ReportSyntaxErrorAndRecover(stopSymbols);
        }

        return proceed;
    }

    /// <summary>
    /// Gets the current argument.
    /// </summary>
    private int Argument => _scanner.Argument;

    /// <summary>
    /// Gets the current symbol.
    /// </summary>
    private Symbol CurrentSymbol => _scanner.CurrentSymbol;

    /// <summary>
    /// Gets a value indicating if the given symbol matches the current symbol.
    /// </summary>
    /// <param name="symbol">The symbol of interest.</param>
    /// <returns>
    /// A value indicating if the given symbol matches the current symbol.
    /// </returns>
    private bool IsCurrentSymbol(Symbol symbol)
    {
        return CurrentSymbol == symbol;
    }

    /// <summary>
    /// Checks if the current symbol matches the expected symbol.
    /// </summary>
    /// <param name="expectedSymbol">The expected symbol.</param>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void Expect(Symbol expectedSymbol, Set stopSymbols)
    {
        if (IsCurrentSymbol(expectedSymbol))
        {
            _scanner.NextSymbol();
        }
        else
        {
            ReportSyntaxErrorAndRecover(stopSymbols);
        }

        SyntaxCheck(stopSymbols);
    }

    /// <summary>
    /// Checks if the current symbol is a name.
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery</param>
    /// <returns>The argument of the name.</returns>
    private int ExpectName(Set stopSymbols)
    {
        int name;
        if (CurrentSymbol == Symbol.Name)
        {
            name = Argument;
            _scanner.NextSymbol();
        }
        else
        {
            name = Auditor.NoName;
            ReportSyntaxErrorAndRecover(stopSymbols);
        }

        SyntaxCheck(stopSymbols);

        return name;
    }

    /// <summary>
    /// Prints out compilation errors.
    /// </summary>
    /// <param name="errorCategory">Error category.</param>
    /// <param name="errorCode">Error code.</param>
    /// <param name="errorMessage">Error message.</param>
    private void PrintCompilationError(string errorCategory,
                               int errorCode,
                               string errorMessage)
    {
        if (_scanner.IsLineCorrect)
        {
            _scanner.SetLineIsIncorrect();
            Console.WriteLine($"{errorCategory} error in line {_scanner.LineNumber}. " +
                              $"Error code = {errorCode}.\n{errorMessage}\n");
            Trace.WriteLine($"{errorCategory} error in line {_scanner.LineNumber}. " +
                            $"Error code = {errorCode}. {errorMessage}");
        }
    }

    /// <summary>
    /// Reports syntax error and performs error recovery.
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void ReportSyntaxErrorAndRecover(Set stopSymbols)
    {
        _annotator.SyntaxError();
        if (_scanner.IsLineCorrect)
        {
            bool proceed = true;
            while ((!stopSymbols.Contains(_scanner.CurrentSymbol)) && proceed)
            {
                proceed = _scanner.NextSymbol();
            }
        }
    }

    /// <summary>
    /// Performs syntax check.
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void SyntaxCheck(Set stopSymbols)
    {
        if (!stopSymbols.Contains(_scanner.CurrentSymbol))
        {
            ReportSyntaxErrorAndRecover(stopSymbols);
        }
    }

    /// <summary>
    /// Assembles code based on the arithmetical/logical operation.
    /// </summary>
    /// <param name="symbol">
    /// Symbol representing the arithmetical/logical operation.
    /// </param>
    private void Operation(Symbol symbol)
    {
        switch (symbol)
        {
            case Symbol.Plus:
                {
                    _assembler.Add();
                    break;
                }

            case Symbol.Minus:
                {
                    _assembler.Subtract();
                    break;
                }

            case Symbol.Less:
                {
                    _assembler.Less();
                    break;
                }

            case Symbol.LessOrEqual:
                {
                    _assembler.LessOrEqual();
                    break;
                }

            case Symbol.Equal:
                {
                    _assembler.Equal();
                    break;
                }

            case Symbol.NotEqual:
                {
                    _assembler.NotEqual();
                    break;
                }

            case Symbol.Greater:
                {
                    _assembler.Greater();
                    break;
                }

            case Symbol.GreaterOrEqual:
                {
                    _assembler.GreaterOrEqual();
                    break;
                }

            case Symbol.And:
                {
                    _assembler.And();
                    break;
                }

            case Symbol.Or:
                {
                    _assembler.Or();
                    break;
                }

            case Symbol.Multiply:
                {
                    _assembler.Multiply();
                    break;
                }

            case Symbol.Divide:
                {
                    _assembler.Divide();
                    break;
                }

            case Symbol.Modulo:
                {
                    _assembler.Modulo();
                    break;
                }

            case Symbol.Power:
                {
                    _assembler.Power();
                    break;
                }

            default:
                {
                    Debug.Assert(false, $"Invalid operation symbol {symbol}.");
                    break;
                }
        }
    }

    /// <summary>
    /// Program = Block
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void Program(Set stopSymbols)
    {
        // Assemble code for Program.
        int objectsLengthLabel = _assembler.CurrentAddress + 1;
        _assembler.Program(0);

        // Define start of a new block.
        _auditor.NewBlock();

        // Process Block.
        Block(false, stopSymbols);

        // Resolve the label for total objects length in this block.
        _assembler.ResolveArgument(objectsLengthLabel, _auditor.ObjectsLength);

        // Define end of the block.
        _auditor.EndBlock();

        // Assemble code for "end-of-program".
        _assembler.EndProgram();
    }

    /// <summary>
    /// Block = "{" DefinitionPart StatementPart "}" 
    /// </summary>
    /// <param name="newBlock">Should the auditor process a new block?</param>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void Block(bool newBlock, Set stopSymbols)
    {
        // Label for total objects length in this block.
        int objectsLengthLabel = _assembler.CurrentAddress + 1;

        if (newBlock)
        {
            // Start a new block.
            _auditor.NewBlock();

            // Assemble code for Block.
            _assembler.Block(0);
        }

        // Define stop symbols.
        Set definitionPartStopSymbols = new Set(Symbol.Name,
                                                Symbol.If,
                                                Symbol.While,
                                                Symbol.Read,
                                                Symbol.Write,
                                                Symbol.Open,
                                                Symbol.Randomize,
                                                Symbol.Send,
                                                Symbol.Receive,
                                                Symbol.Parallel);
        Set statementPartStopSymbols = new Set(definitionPartStopSymbols,
                                               Symbol.End);
        Set endStopSymbols = new Set(statementPartStopSymbols,
                                     Symbol.Constant,
                                     Symbol.Integer,
                                     Symbol.Boolean,
                                     Symbol.Channel,
                                     Symbol.Procedure);
        Set beginStopSymbols = new Set(endStopSymbols);

        // Analyze.
        Expect(Symbol.Begin, new Set(beginStopSymbols,
                                     definitionPartStopSymbols,
                                     statementPartStopSymbols,
                                     endStopSymbols,
                                     stopSymbols));
        DefinitionPart(new Set(definitionPartStopSymbols,
                               statementPartStopSymbols,
                               endStopSymbols,
                               stopSymbols));
        StatementPart(new Set(statementPartStopSymbols,
                              endStopSymbols,
                              stopSymbols));
        Expect(Symbol.End, new Set(endStopSymbols, stopSymbols));

        if (newBlock)
        {
            // Resolve the label for total objects length in this block.
            _assembler.ResolveArgument(objectsLengthLabel, _auditor.ObjectsLength);

            // Assemble code for "end-of-block".
            _assembler.EndBlock();

            // End a block.
            _auditor.EndBlock();
        }
    }

    /// <summary>
    /// DefinitionPart = { ProcedureDefinition    |
    ///                    ConstantDefinition ";" | 
    ///                    VariableDefinition ";"   } 
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void DefinitionPart(Set stopSymbols)
    {
        // Define definition symbols.
        Set definitionSymbols = new Set(Symbol.Constant,
                                        Symbol.Integer,
                                        Symbol.Boolean,
                                        Symbol.Channel,
                                        Symbol.Procedure);

        // Define stop symbols.
        Set definitionPartStopSymbols = new Set(Symbol.SemiColon);
        Set semiColonStopSymbols = new Set(definitionSymbols);

        // Analyze. "if" and "while" statements are not followed by semicolon.
        while (definitionSymbols.Contains(CurrentSymbol))
        {
            bool expectSemiColon = Definition(new Set(definitionPartStopSymbols, stopSymbols));
            if (expectSemiColon)
            {
                Expect(Symbol.SemiColon, new Set(semiColonStopSymbols, definitionPartStopSymbols, stopSymbols));
            }
        }
    }

    /// <summary>
    /// StatementPart = { IfStatement | WhileStatement | Statement ";" }
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void StatementPart(Set stopSymbols)
    {
        // Define statement symbols.
        Set statementSymbols = new Set(Symbol.Name,
                                       Symbol.If,
                                       Symbol.While,
                                       Symbol.Read,
                                       Symbol.Write,
                                       Symbol.Open,
                                       Symbol.Randomize,
                                       Symbol.Send,
                                       Symbol.Receive,
                                       Symbol.Parallel);
        while (statementSymbols.Contains(CurrentSymbol))
        {
            if (IsCurrentSymbol(Symbol.If) || IsCurrentSymbol(Symbol.While))
            {
                // Define stop symbols.
                Set statementStopSymbols = new Set(statementSymbols);

                // Analyze.
                Statement(new Set(statementStopSymbols, stopSymbols));
            }
            else
            {
                // Define stop symbols.
                Set statementStopSymbols = new Set(Symbol.SemiColon);
                Set semiColonStopSymbols = new Set(statementSymbols);

                // Analyze.
                Statement(new Set(statementStopSymbols, stopSymbols));
                Expect(Symbol.SemiColon, new Set(semiColonStopSymbols, statementStopSymbols, stopSymbols));
            }
        }
    }

    /// <summary>
    /// Definition = ConstantDefinition | 
    ///              VariableDefinition | 
    ///              ProcedureDefinition
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    /// <returns>A value indicating whether to expect a semicolon.</returns>
    private bool Definition(Set stopSymbols)
    {
        Debug.Assert(CurrentSymbol == Symbol.Constant ||
                     CurrentSymbol == Symbol.Integer ||
                     CurrentSymbol == Symbol.Boolean ||
                     CurrentSymbol == Symbol.Channel ||
                     CurrentSymbol == Symbol.Procedure,
                     $"Definition: Invalid definition symbol {CurrentSymbol}.");
        bool expectSemicolon = true;
        switch (CurrentSymbol)
        {
            case Symbol.Constant:
                {
                    ConstantDefinition(stopSymbols);
                    break;
                }

            // Type symbols. 
            case Symbol.Integer:
            case Symbol.Boolean:
            case Symbol.Channel:
                {
                    VariableDefinition(stopSymbols);
                    break;
                }

            case Symbol.Procedure:
                {
                    ProcedureDefinition(stopSymbols);
                    expectSemicolon = false;
                    break;
                }
        }

        return expectSemicolon;
    }

    /// <summary>
    /// ConstantDefinition = "constant" ConstantName "=" [ "-" ] Constant.
    /// NOTE: "-" should be followed by an integer constant.
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void ConstantDefinition(Set stopSymbols)
    {
        // Define stop symbols.
        Set minusStopSymbols = new Set(Symbol.Numeral,
                                       Symbol.Name,
                                       Symbol.True,
                                       Symbol.False);
        Set becomesStopSymbols = new Set(minusStopSymbols, Symbol.Minus);
        Set constantNameStopSymbols = new Set(Symbol.Becomes);
        Set constantStopSymbols = new Set(Symbol.Name);

        // Expect "constant".
        Expect(Symbol.Constant, new Set(constantStopSymbols,
                                        constantNameStopSymbols,
                                        becomesStopSymbols,
                                        minusStopSymbols,
                                        stopSymbols));

        // Expect constant object's name.
        int name = ExpectName(new Set(constantNameStopSymbols,
                                      becomesStopSymbols,
                                      minusStopSymbols,
                                      stopSymbols));

        // Expect "=".
        Expect(Symbol.Becomes, new Set(becomesStopSymbols, minusStopSymbols, stopSymbols));

        // "-" follows?
        bool minusPrecedes = false;
        if (IsCurrentSymbol(Symbol.Minus))
        {
            // Expect "-".
            Expect(Symbol.Minus, new Set(minusStopSymbols, stopSymbols));
            minusPrecedes = true;
        }

        // Expect Constant.
        Metadata metadata = new Metadata
        {
            Kind = Kind.Constant
        };
        Constant(ref metadata, stopSymbols);

        // If "-" precedes the integer constant on right hand side, process.
        if (minusPrecedes)
        {
            if (metadata.Type != Type.Integer)
            {
                _annotator.TypeError(metadata.Type, TypeErrorCategory.MinusPrecedingNonIntegerInConstantDefinition);
                metadata.Kind = Kind.Undefined;
                metadata.Type = Type.Universal;
            }
            else
            {
                metadata.Value = -1 * metadata.Value;
            }
        }

        // Define the name only at the end to eliminate conditions like
        //    constant integer myConstant = myConstant
        ObjectRecord objectRecord = new ObjectRecord();
        _auditor.Define(name, metadata, ref objectRecord);
    }

    /// <summary>
    /// Constant = Numeral | BooleanSymbol | ConstantName
    /// </summary>
    /// <param name="metadata">Metadata of the object.</param>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void Constant(ref Metadata metadata, Set stopSymbols)
    {
        switch (CurrentSymbol)
        {
            case Symbol.Numeral:
                {
                    metadata.Type = Type.Integer;
                    Expect(Symbol.Numeral, stopSymbols);
                    metadata.Value = Argument;
                    break;
                }

            case Symbol.Name:
                {
                    // Find the metadata of the object at the right hand side (rhs)
                    // of the constant definition.
                    ObjectRecord rhsObjectRecord = new ObjectRecord();
                    _auditor.Find(Argument, ref rhsObjectRecord);
                    Metadata rhsMetadata = rhsObjectRecord.MetaData;
                    if (rhsMetadata.Kind == Kind.Constant)
                    {
                        metadata.Type = rhsMetadata.Type;
                        metadata.Value = rhsMetadata.Value;
                    }
                    else
                    {
                        _annotator.KindError(
                                rhsMetadata.Kind,
                                KindErrorCategory.NonConstantInConstantDefinition);
                        metadata.Type = Type.Universal;
                    }

                    Expect(Symbol.Name, stopSymbols);
                    break;
                }

            case Symbol.True:
                {
                    metadata.Type = Type.Boolean;
                    metadata.Value = 1;
                    BooleanSymbol(stopSymbols);
                    break;
                }

            case Symbol.False:
                {
                    metadata.Type = Type.Boolean;
                    metadata.Value = 0;
                    BooleanSymbol(stopSymbols);
                    break;
                }

            default:
                {
                    metadata.Type = Type.Universal;
                    ReportSyntaxErrorAndRecover(stopSymbols);
                    break;
                }
        }
    }

    /// <summary>
    /// VariableDefinition = TypeSymbol VariableList | 
    ///                      TypeSymbol ArrayDeclaration
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void VariableDefinition(Set stopSymbols)
    {
        // Define stop symbols.
        Set typeSymbolstopSymbols = new Set(Symbol.Name, Symbol.LeftBracket);

        // Analyze.
        Type type = TypeSymbol(typeSymbolstopSymbols);
        if (IsCurrentSymbol(Symbol.LeftBracket))
        {
            ArrayDeclaration(type, stopSymbols);
        }
        else
        {
            Metadata metaData = new Metadata
            {
                Kind = Kind.Variable,
                Type = type
            };
            VariableList(metaData, stopSymbols);
        }
    }

    /// <summary>
    ///  ProcedureDefinition = "@" [ "[" TypeSymbol Name "]" ] Name
    ///                         "("  [ ParameterDefinition ] ")" Block
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void ProcedureDefinition(Set stopSymbols)
    {
        // Define stop symbols.
        Set rightParanthesisStopSymbols = new Set(Symbol.Begin);
        Set parameterDefinitionStopSymbols = new Set(Symbol.RightParanthesis);
        Set leftParanthesisStopSymbols = new Set(Symbol.Integer,
                                                 Symbol.Boolean,
                                                 Symbol.Channel,
                                                 Symbol.Reference,
                                                 Symbol.RightParanthesis);
        Set procedureNameStopSymbols = new Set(Symbol.LeftParanthesis);
        Set rightBracketStopSymbols = new Set(Symbol.Name);
        Set returnVariableStopSymbols = new Set(Symbol.RightBracket);
        Set typeSymbolStopSymbols = new Set(Symbol.Name);
        Set leftBracketStopSymbols = new Set(Symbol.Integer,
                                             Symbol.Boolean,
                                             Symbol.Channel);
        Set procedureStopSymbols = new Set(Symbol.LeftBracket, Symbol.Name);

        // Expect "procedure".
        Expect(Symbol.Procedure, new Set(procedureStopSymbols,
                                         leftBracketStopSymbols,
                                         typeSymbolStopSymbols,
                                         returnVariableStopSymbols,
                                         rightBracketStopSymbols,
                                         procedureNameStopSymbols,
                                         leftParanthesisStopSymbols,
                                         parameterDefinitionStopSymbols,
                                         rightParanthesisStopSymbols,
                                         stopSymbols));

        // A return type is specified.
        Type type = Type.Void;
        int returnVariable = Auditor.NoName;
        if (IsCurrentSymbol(Symbol.LeftBracket))
        {
            // Expect "[".
            Expect(Symbol.LeftBracket, new Set(leftBracketStopSymbols,
                                               typeSymbolStopSymbols,
                                               returnVariableStopSymbols,
                                               rightBracketStopSymbols,
                                               procedureNameStopSymbols,
                                               leftParanthesisStopSymbols,
                                               parameterDefinitionStopSymbols,
                                               rightParanthesisStopSymbols,
                                               stopSymbols));

            // Expect TypeSymbol.
            type = TypeSymbol(new Set(typeSymbolStopSymbols,
                                      returnVariableStopSymbols,
                                      rightBracketStopSymbols,
                                      procedureNameStopSymbols,
                                      leftParanthesisStopSymbols,
                                      parameterDefinitionStopSymbols,
                                      rightParanthesisStopSymbols,
                                      stopSymbols));

            // Expect return variable name.
            returnVariable = ExpectName(new Set(returnVariableStopSymbols,
                                                rightBracketStopSymbols,
                                                procedureNameStopSymbols,
                                                leftParanthesisStopSymbols,
                                                parameterDefinitionStopSymbols,
                                                rightParanthesisStopSymbols,
                                                stopSymbols));

            // Expect "]".
            Expect(Symbol.RightBracket, new Set(rightBracketStopSymbols,
                                                procedureNameStopSymbols,
                                                leftParanthesisStopSymbols,
                                                parameterDefinitionStopSymbols,
                                                rightParanthesisStopSymbols,
                                                stopSymbols));
        }

        // Expect procedure name.
        int procedureName = ExpectName(new Set(procedureNameStopSymbols,
                                               leftParanthesisStopSymbols,
                                               parameterDefinitionStopSymbols,
                                               rightParanthesisStopSymbols,
                                               stopSymbols));

        // Define the procedure name.
        Metadata metaData = new Metadata
        {
            Kind = Kind.Procedure,
            Type = type,
            ProcedureLabel = _assembler.CurrentAddress + 2
        };
        ObjectRecord objectRecord = new ObjectRecord();
        _auditor.Define(procedureName, metaData, ref objectRecord);

        // Start of new block.
        _auditor.NewBlock();

        // Define the return variable.
        if (returnVariable != Auditor.NoName)
        {
            Metadata returnVariableMetadata = new Metadata
            {
                Kind = Kind.ReturnParameter,
                Type = type
            };
            ObjectRecord returnVariableObjectRecord = new ObjectRecord();
            _auditor.Define(returnVariable, returnVariableMetadata, ref returnVariableObjectRecord);
        }

        // Expect "(".
        Expect(Symbol.LeftParanthesis,
                    new Set(leftParanthesisStopSymbols,
                            parameterDefinitionStopSymbols,
                            rightParanthesisStopSymbols,
                            stopSymbols));

        // Expect ParameterDefinition.
        List<ParameterRecord> parameterRecordList = [];
        if (IsCurrentSymbol(Symbol.Reference) ||
            IsCurrentSymbol(Symbol.Integer) ||
            IsCurrentSymbol(Symbol.Boolean) ||
            IsCurrentSymbol(Symbol.Channel))
        {
            ParameterDefinition(ref parameterRecordList,
                                     new Set(parameterDefinitionStopSymbols,
                                             rightParanthesisStopSymbols,
                                             stopSymbols));
        }

        // Set the parameter record list.
        objectRecord.ParameterRecordList = parameterRecordList;

        // Expect ")".
        Expect(Symbol.RightParanthesis, new Set(rightParanthesisStopSymbols, stopSymbols));

        // Define "end-of-procedure" label.
        int endOfProcedureLabel = _assembler.CurrentAddress + 1;

        // Assemble code.
        _assembler.Goto(endOfProcedureLabel);

        // Define "total-variables-in-this-scope" label.
        int variablesLengthLabel = _assembler.CurrentAddress + 1;

        // Assemble code for procedure activation.
        _assembler.ProcedureBlock(variablesLengthLabel);

        // Push the procedure name onto the stack.
        _procedureNest.Push(procedureName);

        // Expect Block.
        Block(false, stopSymbols);
        int totalBlockVariables = _auditor.ObjectsLength;

        // End of block.
        _auditor.EndBlock();

        // Pop the procedure name from the stack.
        _procedureNest.Pop();

        // Assemble code for end of procedure activation.
        _assembler.EndProcedureBlock(parameterRecordList.Count);

        // Resolve "total-variables-in-this-scope" and "end-of-procedure" labels.
        _assembler.ResolveArgument(variablesLengthLabel, totalBlockVariables);
        _assembler.ResolveAddress(endOfProcedureLabel);
    }

    /// <summary>
    /// ["reference"] TypeSymbol Name { "," ["reference"] TypeSymbol Name }
    /// </summary>
    /// <param name="parameterRecordList">List of parameter records.</param>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void ParameterDefinition(ref List<ParameterRecord> parameterRecordList, Set stopSymbols)
    {
        ParameterRecord parameterRecord = new ParameterRecord();
        parameterRecordList = [];
        List<int> parameterList = [];

        // Define stop symbols.
        Set commaStopSymbols = new Set(Symbol.Integer,
                                       Symbol.Boolean,
                                       Symbol.Channel,
                                       Symbol.Reference);
        Set typeSymbolStopSymbols = new Set(Symbol.Name);
        Set parameterNameStopSymbols = new Set(Symbol.Comma);
        Set referenceStopSymbols = new Set(Symbol.Integer,
                                           Symbol.Boolean,
                                           Symbol.Channel);

        // Reference parameter?
        if (IsCurrentSymbol(Symbol.Reference))
        {
            Expect(Symbol.Reference, new Set(referenceStopSymbols,
                                             parameterNameStopSymbols,
                                             typeSymbolStopSymbols,
                                             commaStopSymbols,
                                             stopSymbols));
            parameterRecord.ParameterKind = Kind.ReferenceParameter;
        }
        else
        {
            parameterRecord.ParameterKind = Kind.ValueParameter;
        }

        // Expect TypeSymbol.
        parameterRecord.ParameterType = TypeSymbol(new Set(typeSymbolStopSymbols,
                                                           commaStopSymbols,
                                                           referenceStopSymbols,
                                                           parameterNameStopSymbols,
                                                           stopSymbols));

        // Add the parameter record to the parameter record list.
        parameterRecordList.Add(parameterRecord);

        // Expect parameter name.
        int parameter = ExpectName(new Set(parameterNameStopSymbols, stopSymbols));
        parameterList.Add(parameter);

        // "," follows?
        while (IsCurrentSymbol(Symbol.Comma))
        {
            parameterRecord = new ParameterRecord();

            // Expect ",".
            Expect(Symbol.Comma, new Set(referenceStopSymbols,
                                         typeSymbolStopSymbols,
                                         parameterNameStopSymbols,
                                         commaStopSymbols,
                                         stopSymbols));

            // Reference parameter?
            if (IsCurrentSymbol(Symbol.Reference))
            {
                Expect(Symbol.Reference, new Set(referenceStopSymbols,
                                                 parameterNameStopSymbols,
                                                 typeSymbolStopSymbols,
                                                 commaStopSymbols,
                                                 stopSymbols));
                parameterRecord.ParameterKind = Kind.ReferenceParameter;
            }
            else
            {
                parameterRecord.ParameterKind = Kind.ValueParameter;
            }

            // Expect TypeSymbol.
            parameterRecord.ParameterType = TypeSymbol(new Set(typeSymbolStopSymbols,
                                                               commaStopSymbols,
                                                               referenceStopSymbols,
                                                               parameterNameStopSymbols,
                                                               stopSymbols));

            // Add the parameter record to the parameter record list.
            parameterRecordList.Add(parameterRecord);

            // Expect parameter name.
            parameter = ExpectName(new Set(parameterNameStopSymbols, stopSymbols));
            parameterList.Add(parameter);
        }

        int max = parameterRecordList.Count;

        Debug.Assert((parameterList.Count == max), "Parameter list count does not match the parameter record list count.");

        // The first parameter needs to have a relative displacement of -1, the 
        // second parameter needs to have a relative displacement of -2 etc.
        // Hence define the arguments in the reverse order.
        for (int count = max - 1; count >= 0; --count)
        {
            Metadata metaData = new Metadata
            {
                Type = parameterRecordList[count].ParameterType,
                Kind = parameterRecordList[count].ParameterKind
            };

            ObjectRecord newObjectRecord = new ObjectRecord();
            _auditor.Define(parameterList[count], metaData, ref newObjectRecord);
        }
    }

    /// <summary>
    /// TypeSymbol = "integer" | "boolean" | "channel"
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    /// <returns>Returns the Type.</returns>
    private Type TypeSymbol(Set stopSymbols)
    {
        Type type = Type.Universal;

        switch (CurrentSymbol)
        {
            case Symbol.Integer:
                {
                    Expect(Symbol.Integer, stopSymbols);
                    type = Type.Integer;
                    break;
                }

            case Symbol.Boolean:
                {
                    Expect(Symbol.Boolean, stopSymbols);
                    type = Type.Boolean;
                    break;
                }

            case Symbol.Channel:
                {
                    Expect(Symbol.Channel, stopSymbols);
                    type = Type.Channel;
                    break;
                }

            default:
                {
                    ReportSyntaxErrorAndRecover(stopSymbols);
                    break;
                }
        }

        return type;
    }

    /// <summary>
    /// ArrayDeclaration = "[" Constant "]" VariableList 
    /// NOTE: The Constant in the above rule has to be of type integer.
    /// </summary>
    /// <param name="type">Type of the array.</param>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void ArrayDeclaration(Type type, Set stopSymbols)
    {
        // Define stop symbols.
        Set rightBracketStopSymbols = new Set(Symbol.Name);
        Set constantStopSymbols = new Set(Symbol.RightBracket);
        Set leftBracketStopSymbols = new Set(Symbol.Numeral,
                                             Symbol.Name);

        // Expect "[".
        Expect(Symbol.LeftBracket, new Set(leftBracketStopSymbols,
                                           constantStopSymbols,
                                           rightBracketStopSymbols,
                                           stopSymbols));

        // Find the metadata of the constant that defines the bound of the array.
        Metadata arrayBoundMetadata = new Metadata();
        Constant(ref arrayBoundMetadata, new Set(constantStopSymbols,
                                                 rightBracketStopSymbols,
                                                 stopSymbols));

        // The bound of the array has to be of type integer.
        if (arrayBoundMetadata.Type != Type.Integer)
        {
            _annotator.TypeError(arrayBoundMetadata.Type,
                                 TypeErrorCategory.NonIntegerIndexInArrayDeclaration);
        }

        // Expect "]".
        Expect(Symbol.RightBracket, new Set(rightBracketStopSymbols, stopSymbols));

        // Set the metadata of the array object.
        Metadata metadata = new Metadata
        {
            Kind = Kind.Array,
            Type = type,
            UpperBound = arrayBoundMetadata.Value
        };

        // The bound of the array has to be a positive integer.
        // if not an integer, it would be caught in the type checking above.
        if ((metadata.UpperBound <= 0) && (metadata.Type == Type.Integer))
        {
            _annotator.KindError(Kind.Variable, KindErrorCategory.NonPositiveIntegerIndexInArrayDeclaration);
        }

        // Expect VariableList.
        VariableList(metadata, stopSymbols);
    }

    /// <summary>
    /// VariableList = VariableName { "," VariableName } 
    /// </summary>
    /// <param name="metadata">Metadata of the object(s).</param>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void VariableList(Metadata metadata, Set stopSymbols)
    {
        // Define stop symbols.
        Set variableNameStopSymbols = new Set(Symbol.Comma);
        Set commaStopSymbols = new Set(Symbol.Name);

        // Read the object name, and define it.
        int name = ExpectName(new Set(variableNameStopSymbols, commaStopSymbols, stopSymbols));
        ObjectRecord objectRecord = new ObjectRecord();
        _auditor.Define(name, metadata, ref objectRecord);

        // "," follows?
        while (IsCurrentSymbol(Symbol.Comma))
        {
            // Expect ",".
            Expect(Symbol.Comma, new Set(commaStopSymbols, variableNameStopSymbols, stopSymbols));

            // Read the object name, and define it.
            name = ExpectName(new Set(variableNameStopSymbols, commaStopSymbols, stopSymbols));
            ObjectRecord newObjectRecord = new ObjectRecord();
            _auditor.Define(name, metadata, ref newObjectRecord);
        }
    }

    /// <summary>
    /// ObjectAccessList = ObjectAccess { "," ObjectAccess }
    /// </summary>
    /// <param name="operation">
    /// Category of the operation.
    /// </param>
    /// <param name="stopSymbols">
    /// Stop symbols for error recovery.
    /// </param>
    private void ObjectAccessList(OperationCategory operation, Set stopSymbols)
    {
        // Define stop symbols.
        Set objectAccessStopSymbols = new Set(Symbol.Comma);
        Set commaStopSymbols = new Set(Symbol.Name);

        // Expect ObjectAccess.
        Metadata metadata = ObjectAccess(new Set(objectAccessStopSymbols, stopSymbols));

        if (operation == OperationCategory.Read)
        {
            // Read statement should not be applied on a constant.
            if (metadata.Kind == Kind.Constant)
            {
                _annotator.KindError(Kind.Constant, KindErrorCategory.ReadModifiesConstant);
            }

            // Assemble code.
            if (metadata.Type == Type.Boolean)
            {
                _assembler.ReadBoolean();
            }
            else if (metadata.Type == Type.Integer)
            {
                _assembler.ReadInteger();
            }
            else
            {
                _annotator.TypeError(metadata.Type, TypeErrorCategory.InvalidTypeInReadStatement);
            }
        }
        else if (operation == OperationCategory.Randomize)
        {
            if (metadata.Type != Type.Integer)
            {
                _annotator.TypeError(metadata.Type, TypeErrorCategory.NonIntegerInRandomizeStatement);
            }

            if (metadata.Kind == Kind.Constant)
            {
                _annotator.KindError(Kind.Constant, KindErrorCategory.RandomizeModifiesConstant);
            }

            // Assemble code.
            _assembler.Randomize();
        }
        else if (operation == OperationCategory.Open)
        {
            if (metadata.Type != Type.Channel)
            {
                _annotator.TypeError(metadata.Type, TypeErrorCategory.NonChannelInOpenStatement);
            }

            // Assemble code.
            _assembler.Open();
        }

        // "," follows?
        while (IsCurrentSymbol(Symbol.Comma))
        {
            // Expect ",".
            Expect(Symbol.Comma, new Set(commaStopSymbols, objectAccessStopSymbols, stopSymbols));

            // Expect ObjectAccess.
            metadata = ObjectAccess(new Set(objectAccessStopSymbols, stopSymbols));

            if (operation == OperationCategory.Read)
            {
                // Read statement should not be applied on a constant.
                if (metadata.Kind == Kind.Constant)
                {
                    _annotator.KindError(Kind.Constant, KindErrorCategory.ReadModifiesConstant);
                }

                // Assemble code.
                if (metadata.Type == Type.Boolean)
                {
                    _assembler.ReadBoolean();
                }
                else if (metadata.Type == Type.Integer)
                {
                    _assembler.ReadInteger();
                }
                else
                {
                    _annotator.TypeError(metadata.Type, TypeErrorCategory.InvalidTypeInReadStatement);
                }
            }
            else if (operation == OperationCategory.Randomize)
            {
                if (metadata.Type != Type.Integer)
                {
                    _annotator.TypeError(metadata.Type, TypeErrorCategory.NonIntegerInRandomizeStatement);
                }

                if (metadata.Kind == Kind.Constant)
                {
                    _annotator.KindError(Kind.Constant, KindErrorCategory.RandomizeModifiesConstant);
                }

                // Assemble code.
                _assembler.Randomize();
            }
            else if (operation == OperationCategory.Open)
            {
                if (metadata.Type != Type.Channel)
                {
                    _annotator.TypeError(metadata.Type, TypeErrorCategory.NonChannelInOpenStatement);
                }

                // Assemble code.
                _assembler.Open();
            }
        }
    }

    /// <summary>
    /// ObjectAccess = ObjectName [ IndexedSelector ]
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    /// <returns>Returns the metadata of the accessed object.</returns>
    private Metadata ObjectAccess(Set stopSymbols)
    {
        // Define stop symbols.
        Set objectNameStopSymbols = new Set(Symbol.LeftBracket);

        // Access the object.
        ObjectRecord objectRecord = new ObjectRecord();
        _auditor.Find(Argument, ref objectRecord);
        Expect(Symbol.Name, new Set(objectNameStopSymbols, stopSymbols));

        // Is the statement in a procedure block?
        if (((objectRecord.MetaData.Kind == Kind.Variable) ||
             (objectRecord.MetaData.Kind == Kind.Array)) &&
            (_procedureNest.Count != 0))
        {
            int procedureName = _procedureNest.Peek();
            ObjectRecord procedureRecord = new ObjectRecord();
            _auditor.Find(procedureName, ref procedureRecord);
            if ((procedureRecord.ProcedureRecord.HighestScopeUsed == ProcedureRecord.NoScope) ||
                (procedureRecord.ProcedureRecord.HighestScopeUsed > objectRecord.MetaData.Level))
            {
                procedureRecord.ProcedureRecord.HighestScopeUsed = objectRecord.MetaData.Level;
            }
        }

        if (objectRecord.MetaData.Kind == Kind.Procedure)
        {
            _annotator.KindError(Kind.Procedure, KindErrorCategory.ProcedureAccessedAsObject);
        }
        else if (objectRecord.MetaData.Kind == Kind.Array)
        {
            if (!IsCurrentSymbol(Symbol.LeftBracket))
            {
                _annotator.KindError(Kind.Array, KindErrorCategory.ArrayVariableMissingIndexedSelector);
            }
            else
            {
                // Assemble code for the array object.
                int displacement = objectRecord.MetaData.Displacement;
                int level = _auditor.BlockLevel - objectRecord.MetaData.Level;
                _assembler.Variable(level, displacement);

                // Assemble code for the indexed selector.
                IndexedSelector(stopSymbols);
                _assembler.Index(objectRecord.MetaData.UpperBound);
            }
        }
        else
        {
            // Assemble code for the object.
            int displacement = objectRecord.MetaData.Displacement;
            int level = _auditor.BlockLevel - objectRecord.MetaData.Level;
            if (objectRecord.MetaData.Kind == Kind.ReferenceParameter)
            {
                _assembler.ReferenceParameter(level, displacement);
            }
            else
            {
                _assembler.Variable(level, displacement);
            }
        }

        return objectRecord.MetaData;
    }

    /// <summary>
    /// IndexedSelector = "[" Expression "]" 
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void IndexedSelector(Set stopSymbols)
    {
        // Define stop symbols.
        Set leftBracketStopSymbols = new Set(Symbol.Minus,
                                             Symbol.Name,
                                             Symbol.Numeral,
                                             Symbol.False,
                                             Symbol.True,
                                             Symbol.Not,
                                             Symbol.LeftParanthesis);
        Set expressionStopSymbols = new Set(Symbol.RightBracket);

        // Expect "[".
        Expect(Symbol.LeftBracket, new Set(leftBracketStopSymbols, expressionStopSymbols, stopSymbols));

        // Expect Expression.
        Type type = Expression(new Set(expressionStopSymbols, stopSymbols));
        if (type != Type.Integer)
        {
            _annotator.TypeError(type, TypeErrorCategory.NonIntegerArrayIndex);
        }

        // Expect "]".
        Expect(Symbol.RightBracket, stopSymbols);
    }

    /// <summary>
    /// BooleanSymbol = "false" | "true" 
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void BooleanSymbol(Set stopSymbols)
    {
        Debug.Assert(CurrentSymbol == Symbol.False || CurrentSymbol == Symbol.True, $"BooleanSymbol: Invalid symbol {CurrentSymbol}.");
        switch (CurrentSymbol)
        {
            case Symbol.False:
                {
                    Expect(Symbol.False, stopSymbols);
                    break;
                }

            case Symbol.True:
                {
                    Expect(Symbol.True, stopSymbols);
                    break;
                }
        }
    }

    /// <summary>
    /// Statement = ReadStatement       |
    ///             WriteStatement      |
    ///             AssignmentStatement | 
    ///             IfStatement         |
    ///             WhileStatement      |
    ///             ProcedureInvocation |
    ///             RandomizeStatement  |
    ///             OpenStatement       |
    ///             SendStatement       |
    ///             ReceiveStatement    |
    ///             ParallelStatement
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void Statement(Set stopSymbols)
    {
        Debug.Assert(CurrentSymbol == Symbol.Read ||
                     CurrentSymbol == Symbol.Write ||
                     CurrentSymbol == Symbol.Name ||
                     CurrentSymbol == Symbol.If ||
                     CurrentSymbol == Symbol.While ||
                     CurrentSymbol == Symbol.Randomize ||
                     CurrentSymbol == Symbol.Open ||
                     CurrentSymbol == Symbol.Send ||
                     CurrentSymbol == Symbol.Receive ||
                     CurrentSymbol == Symbol.Parallel,
                     $"Statement: Invalid symbol {CurrentSymbol}.");
        switch (CurrentSymbol)
        {
            case Symbol.Read:
                {
                    ReadStatement(stopSymbols);
                    break;
                }

            case Symbol.Write:
                {
                    WriteStatement(stopSymbols);
                    break;
                }

            case Symbol.Name:
                {
                    ObjectRecord objectRecord = new ObjectRecord();
                    _auditor.Find(Argument, ref objectRecord);
                    if (objectRecord.MetaData.Kind == Kind.Procedure)
                    {
                        ProcedureInvocation(false, stopSymbols);
                    }
                    else
                    {
                        AssignmentStatement(stopSymbols);
                    }

                    break;
                }

            case Symbol.If:
                {
                    IfStatement(stopSymbols);
                    break;
                }

            case Symbol.While:
                {
                    WhileStatement(stopSymbols);
                    break;
                }

            case Symbol.Randomize:
                {
                    RandomizeStatement(stopSymbols);
                    break;
                }

            case Symbol.Open:
                {
                    OpenStatement(stopSymbols);
                    break;
                }

            case Symbol.Send:
                {
                    SendStatement(stopSymbols);
                    break;
                }

            case Symbol.Receive:
                {
                    ReceiveStatement(stopSymbols);
                    break;
                }

            case Symbol.Parallel:
                {
                    ParallelStatement(stopSymbols);
                    break;
                }
        }
    }

    /// <summary>
    /// ReadStatement = "read" ObjectAccessList
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void ReadStatement(Set stopSymbols)
    {
        // Define stop symbols.
        Set readStopSymbols = new Set(Symbol.Name);

        // Expect "read".
        Expect(Symbol.Read, new Set(readStopSymbols, stopSymbols));

        // Is the statement in a procedure block?
        if (_procedureNest.Count != 0)
        {
            int procedureName = _procedureNest.Peek();
            ObjectRecord procedureRecord = new ObjectRecord();
            _auditor.Find(procedureName, ref procedureRecord);
            procedureRecord.ProcedureRecord.UsesIO = true;
        }

        // Expect ObjectAccessList.
        ObjectAccessList(OperationCategory.Read, stopSymbols);
    }

    /// <summary>
    /// WriteStatement = "write" ExpressionList 
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void WriteStatement(Set stopSymbols)
    {
        // Define stop symbols.
        Set writeStopSymbols = new Set(Symbol.Minus,
                                       Symbol.Name,
                                       Symbol.Numeral,
                                       Symbol.False,
                                       Symbol.True,
                                       Symbol.Not,
                                       Symbol.LeftParanthesis);

        // Expect "write".
        Expect(Symbol.Write, new Set(writeStopSymbols, stopSymbols));

        // Is the statement in a procedure block?
        if (_procedureNest.Count != 0)
        {
            int procedureName = _procedureNest.Peek();
            ObjectRecord procedureRecord = new ObjectRecord();
            _auditor.Find(procedureName, ref procedureRecord);
            procedureRecord.ProcedureRecord.UsesIO = true;
        }

        // Expect ExpressList.
        ExpressionList(OperationCategory.Write, stopSymbols);
    }

    /// <summary>
    /// OpenStatement = "open" ObjectAccessList
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void OpenStatement(Set stopSymbols)
    {
        // Define stop symbols.
        Set openStopSymbols = new Set(Symbol.Name);

        // Expect "open".
        Expect(Symbol.Open, new Set(openStopSymbols, stopSymbols));

        // Expect ObjectAccessList.
        ObjectAccessList(OperationCategory.Open, stopSymbols);
    }

    /// <summary>
    /// RandomizeStatement = "random" ObjectAccessList
    /// </summary>
    /// <param name="stopSymbols"></param>
    private void RandomizeStatement(Set stopSymbols)
    {
        // Define stop symbols.
        Set randomizeStopSymbols = new Set(Symbol.Name);

        // Expect "random".
        Expect(Symbol.Randomize, new Set(randomizeStopSymbols, stopSymbols));

        // Expect ObjectAccessList.
        ObjectAccessList(OperationCategory.Randomize, stopSymbols);
    }

    /// <summary>
    /// SendStatement = "send" Expression "->" Expression
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void SendStatement(Set stopSymbols)
    {
        // Define stop symbols.
        Set throughStopSymbols = new Set(Symbol.Minus,
                                         Symbol.Name,
                                         Symbol.Numeral,
                                         Symbol.False,
                                         Symbol.True,
                                         Symbol.Not,
                                         Symbol.LeftParanthesis);
        Set expressionStopSymbols = new Set(Symbol.Through);
        Set sendStopSymbols = new Set(Symbol.Minus,
                                      Symbol.Name,
                                      Symbol.Numeral,
                                      Symbol.False,
                                      Symbol.True,
                                      Symbol.Not,
                                      Symbol.LeftParanthesis);

        // Expect "send".
        Expect(Symbol.Send, new Set(sendStopSymbols, expressionStopSymbols, throughStopSymbols, stopSymbols));

        // Expect Expression.
        Type integerExpressionType = Expression(new Set(expressionStopSymbols, throughStopSymbols, stopSymbols));

        // Expression should be of type integer.
        if (integerExpressionType != Type.Integer)
        {
            _annotator.TypeError(integerExpressionType, TypeErrorCategory.NonIntegerValueInSendStatement);
        }

        // Expect "->".
        Expect(Symbol.Through, new Set(throughStopSymbols, stopSymbols));

        // Expect Expression.
        Type channelType = Expression(stopSymbols);

        // Object should be of type channel.
        if (channelType != Type.Channel)
        {
            _annotator.TypeError(channelType, TypeErrorCategory.NonChannelInSendStatement);
        }

        // Assemble code for send.
        _assembler.Send();
    }

    /// <summary>
    /// ReceiveStatement = "receive" ObjectAccess "->" Expression
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void ReceiveStatement(Set stopSymbols)
    {
        // Define stop symbols.
        Set throughStopSymbols = new Set(Symbol.Minus,
                                         Symbol.Name,
                                         Symbol.Numeral,
                                         Symbol.False,
                                         Symbol.True,
                                         Symbol.Not,
                                         Symbol.LeftParanthesis);
        Set integerVariableStopSymbols = new Set(Symbol.Through);
        Set receiveStopSymbols = new Set(Symbol.Name);

        // Expect "receive".
        Expect(Symbol.Receive, new Set(receiveStopSymbols,
                                       integerVariableStopSymbols,
                                       throughStopSymbols,
                                       stopSymbols));

        // Expect ObjectAccess.
        Metadata integerVariableMetadata = ObjectAccess(new Set(integerVariableStopSymbols,
                                                                throughStopSymbols,
                                                                stopSymbols));

        // Object should be of type integer.
        if (integerVariableMetadata.Type != Type.Integer)
        {
            _annotator.TypeError(integerVariableMetadata.Type, TypeErrorCategory.NonIntegerValueInReceiveStatement);
        }

        // Object should be not of kind constant.
        if (integerVariableMetadata.Kind == Kind.Constant)
        {
            _annotator.KindError(Kind.Constant, KindErrorCategory.ReceiveModifiesConstant);
        }

        // Expect "->".
        Expect(Symbol.Through, new Set(throughStopSymbols, stopSymbols));

        // Expect ObjectAccess.
        Type channelType = Expression(stopSymbols);

        // Object should be of type channel.
        if (channelType != Type.Channel)
        {
            _annotator.TypeError(channelType, TypeErrorCategory.NonChannelInReceiveStatement);
        }

        // Assemble code for receive.
        _assembler.Receive();
    }

    /// <summary>
    /// ParallelStatement = "parallel" ProcedureInvocation
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void ParallelStatement(Set stopSymbols)
    {
        // Define stop symbols.
        Set parallelStopSymbols = new Set(Symbol.Name);

        // Expect "parallel".
        Expect(Symbol.Parallel, new Set(parallelStopSymbols, stopSymbols));

        // Expect the prallel procedure name.
        ObjectRecord objectRecord = new ObjectRecord();
        _auditor.Find(Argument, ref objectRecord);
        if (objectRecord.MetaData.Kind == Kind.Procedure)
        {
            // Has non-void return?
            if (objectRecord.MetaData.Type != Type.Void)
            {
                _annotator.KindError(Kind.Procedure, KindErrorCategory.ParallelProcedureHasNonVoidReturn);
            }

            List<ParameterRecord> parameterRecordList = objectRecord.ParameterRecordList;
            bool hasReferenceParameter = false;
            bool hasChannelParameter = false;
            foreach (ParameterRecord parameterRecord in parameterRecordList)
            {
                if (parameterRecord.ParameterKind == Kind.ReferenceParameter)
                {
                    hasReferenceParameter = true;
                }

                if (parameterRecord.ParameterType == Type.Channel)
                {
                    hasChannelParameter = true;
                }
            }

            // Has reference parameter?
            if (hasReferenceParameter)
            {
                _annotator.KindError(Kind.Procedure, KindErrorCategory.ParallelProcedureHasReferenceParameter);
            }

            // Has no channel parameter?
            if (!hasChannelParameter)
            {
                _annotator.KindError(Kind.Procedure, KindErrorCategory.ParallelProcedureHasNoChannelParameter);
            }

            // Uses I/O statements?
            if (objectRecord.ProcedureRecord.UsesIO)
            {
                _annotator.KindError(Kind.Procedure, KindErrorCategory.ParallelProcedureUsesIO);
            }

            // Uses non local variables?
            if ((objectRecord.ProcedureRecord.HighestScopeUsed != ProcedureRecord.NoScope) &&
                (objectRecord.ProcedureRecord.HighestScopeUsed <= objectRecord.MetaData.Level))
            {
                _annotator.KindError(Kind.Procedure, KindErrorCategory.ParallelProcedureUsesNonLocals);
            }

            // Calls parallel unfriendly procedures?
            if (objectRecord.ProcedureRecord.CallsParallelUnfriendly)
            {
                _annotator.KindError(Kind.Procedure, KindErrorCategory.ParallelProcedureCallsUnfriendly);
            }
        }
        else
        {
            // "parallel" keyword is not followed by procedure invocation.
            _annotator.KindError(objectRecord.MetaData.Kind, KindErrorCategory.NonProcedureInParallelStatement);
        }

        // Procedure invocation.
        ProcedureInvocation(true, stopSymbols);
    }

    /// <summary>
    /// AssignmentStatement = ObjectAccess [, ObjectAccess] "=" Expression [, Expression]
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void AssignmentStatement(Set stopSymbols)
    {
        // Define stop symbols.
        Set expressionStopSymbols = new Set(Symbol.Comma);
        Set expressionCommaStopSymbols = new Set(Symbol.Minus,
                                                 Symbol.Name,
                                                 Symbol.Numeral,
                                                 Symbol.False,
                                                 Symbol.True,
                                                 Symbol.Not,
                                                 Symbol.LeftParanthesis);
        Set becomesStopSymbols = new Set(expressionCommaStopSymbols);
        Set objectAccessStopSymbols = new Set(Symbol.Becomes, Symbol.Comma);
        Set objectAccessCommaStopSymbols = new Set(Symbol.Name);

        List<Type> leftHandSideTypes = [];

        // Expect ObjectAccess.
        Metadata metadata = ObjectAccess(new Set(objectAccessStopSymbols,
                                                 becomesStopSymbols,
                                                 expressionStopSymbols,
                                                 expressionCommaStopSymbols,
                                                 stopSymbols));
        leftHandSideTypes.Add(metadata.Type);

        // Left hand side of the assignment statement should not be a constant.
        if (metadata.Kind == Kind.Constant)
        {
            _annotator.KindError(metadata.Kind, KindErrorCategory.AssignmentModifiesConstant);
        }

        // "," follows?
        while (IsCurrentSymbol(Symbol.Comma))
        {
            // Expect ",".
            Expect(Symbol.Comma, new Set(objectAccessCommaStopSymbols,
                                         objectAccessStopSymbols,
                                         becomesStopSymbols,
                                         expressionStopSymbols,
                                         expressionCommaStopSymbols,
                                         stopSymbols));

            // Expect ObjectAccess.
            metadata = ObjectAccess(new Set(objectAccessStopSymbols,
                                            becomesStopSymbols,
                                            expressionStopSymbols,
                                            expressionCommaStopSymbols,
                                            stopSymbols));
            leftHandSideTypes.Add(metadata.Type);

            // Left hand side of the assignment statement should not be a constant.
            if (metadata.Kind == Kind.Constant)
            {
                _annotator.KindError(metadata.Kind, KindErrorCategory.AssignmentModifiesConstant);
            }
        }

        // Expect "="
        Expect(Symbol.Becomes, new Set(becomesStopSymbols,
                                       expressionStopSymbols,
                                       expressionCommaStopSymbols,
                                       stopSymbols));

        List<Type> rightHandSideTypes = [];

        // Expect Expression.
        Type rightHandSideType = Expression(new Set(expressionStopSymbols, stopSymbols));
        rightHandSideTypes.Add(rightHandSideType);

        // "," follows?
        while (IsCurrentSymbol(Symbol.Comma))
        {
            // Expect ",".
            Expect(Symbol.Comma, new Set(expressionCommaStopSymbols,
                                         expressionStopSymbols,
                                         stopSymbols));

            // Expect Expression.
            rightHandSideType = Expression(new Set(expressionStopSymbols, stopSymbols));
            rightHandSideTypes.Add(rightHandSideType);
        }

        int total = leftHandSideTypes.Count;
        if (total != rightHandSideTypes.Count)
        {
            _annotator.KindError(Kind.Variable, KindErrorCategory.AssignmentCountMismatch);
        }
        else
        {
            for (int count = 0; count < total; ++count)
            {
                // Make sure the types on either side of the assignment match.
                if ((leftHandSideTypes[count] != rightHandSideTypes[count]) &&
                    (rightHandSideTypes[count] != Type.Universal))
                {
                    _annotator.TypeError(leftHandSideTypes[count], TypeErrorCategory.TypeMismatchInAssignment);
                }
            }
        }

        // Assemble code.
        _assembler.Assign(total);
    }

    /// <summary>
    /// IfStatement = "if" "(" Expression ")" Block [ "else" Block ]
    /// NOTE: 'Expression' in the above rule has to be of type boolean.
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void IfStatement(Set stopSymbols)
    {
        // Define stop symbols.
        Set blockStopSymbols = new Set(Symbol.Else);
        Set rightParanthesisStopSymbols = new Set(Symbol.Begin);
        Set expressionStopSymbols = new Set(Symbol.RightParanthesis);
        Set leftParanthesisStopSymbols = new Set(Symbol.Minus,
                                                 Symbol.Name,
                                                 Symbol.Numeral,
                                                 Symbol.False,
                                                 Symbol.True,
                                                 Symbol.Not,
                                                 Symbol.LeftParanthesis);
        Set ifStopSymbols = new Set(Symbol.LeftParanthesis);

        // Expect "if".
        Expect(Symbol.If, new Set(ifStopSymbols,
                                  leftParanthesisStopSymbols,
                                  expressionStopSymbols,
                                  rightParanthesisStopSymbols,
                                  blockStopSymbols,
                                  stopSymbols));

        // Expect "(".
        Expect(Symbol.LeftParanthesis, new Set(leftParanthesisStopSymbols,
                                               expressionStopSymbols,
                                               rightParanthesisStopSymbols,
                                               blockStopSymbols,
                                               stopSymbols));

        // Expect Expression.
        Type type = Expression(new Set(expressionStopSymbols,
                                       rightParanthesisStopSymbols,
                                       blockStopSymbols,
                                       stopSymbols));

        // Make sure the expression would evaluate to a boolean value.
        if (type != Type.Boolean)
        {
            _annotator.TypeError(type, TypeErrorCategory.NonBooleanInIfCondition);
        }

        // Expect ")".
        Expect(Symbol.RightParanthesis, new Set(rightParanthesisStopSymbols,
                                                blockStopSymbols,
                                                stopSymbols));

        // Assemble code.
        int ifBlockEndLabel = _assembler.CurrentAddress + 1;
        _assembler.Do(ifBlockEndLabel);

        // Expect Block.
        Block(true, new Set(blockStopSymbols, stopSymbols));

        // "else" follows?
        if (IsCurrentSymbol(Symbol.Else))
        {
            // Define stop symbols.
            Set elseStopSymbols = new Set(Symbol.Begin);

            // Expect "else".
            Expect(Symbol.Else, new Set(elseStopSymbols, stopSymbols));

            // Assemble code.
            int elseBlockEndLabel = _assembler.CurrentAddress + 1;
            _assembler.Goto(elseBlockEndLabel);

            // Resolve "if-block-ends-here" label.
            _assembler.ResolveAddress(ifBlockEndLabel);

            // Expect Block.
            Block(true, stopSymbols);

            // Resolve "else-block-ends-here" label.
            _assembler.ResolveAddress(elseBlockEndLabel);
        }
        else
        {
            // Resolve "if-block-ends-here" label.
            _assembler.ResolveAddress(ifBlockEndLabel);
        }
    }

    /// <summary>
    /// WhileStatement = "while" "(" Expression ")" Block
    /// NOTE: 'Expression' in the above rule has to be of type boolean.
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void WhileStatement(Set stopSymbols)
    {
        // Define stop symbols.
        Set rightParanthesisStopSymbols = new Set(Symbol.Begin);
        Set expressionStopSymbols = new Set(Symbol.RightParanthesis);
        Set leftParanthesisStopSymbols = new Set(Symbol.Minus,
                                                 Symbol.Name,
                                                 Symbol.Numeral,
                                                 Symbol.False,
                                                 Symbol.True,
                                                 Symbol.Not,
                                                 Symbol.LeftParanthesis);
        Set whileStopSymbols = new Set(Symbol.LeftParanthesis);

        // Define "while-block-starts-here" label.
        int whileStatementStartLabel = _assembler.CurrentAddress;

        // Expect "while"
        Expect(Symbol.While, new Set(whileStopSymbols,
                                     leftParanthesisStopSymbols,
                                     expressionStopSymbols,
                                     rightParanthesisStopSymbols,
                                     stopSymbols));

        // Expect "(".
        Expect(Symbol.LeftParanthesis, new Set(leftParanthesisStopSymbols,
                                               expressionStopSymbols,
                                               rightParanthesisStopSymbols,
                                               stopSymbols));

        // Expect Expression.
        Type type = Expression(new Set(expressionStopSymbols,
                                       rightParanthesisStopSymbols,
                                       stopSymbols));

        // Make sure the expression would evaluate to a boolean value.
        if (type != Type.Boolean)
        {
            _annotator.TypeError(type, TypeErrorCategory.NonBooleanInWhileCondition);
        }

        // Expect ")".
        Expect(Symbol.RightParanthesis, new Set(rightParanthesisStopSymbols, stopSymbols));

        // Assemble code.
        int whileBlockEndLabel = _assembler.CurrentAddress + 1;
        _assembler.Do(whileBlockEndLabel);

        // Expect Block.
        Block(true, stopSymbols);

        // End of the while block, go back and evaluate the while condition.
        _assembler.Goto(whileStatementStartLabel);

        // Resolve "while-block-ends-here" label.
        _assembler.ResolveAddress(whileBlockEndLabel);
    }

    /// <summary>
    /// AddingOperator = "+" | "-"
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void AddingOperator(Set stopSymbols)
    {
        Debug.Assert(CurrentSymbol == Symbol.Plus || CurrentSymbol == Symbol.Minus, $"AddingOperator: Invalid symbol {CurrentSymbol}.");
        switch (CurrentSymbol)
        {
            case Symbol.Plus:
                {
                    Expect(Symbol.Plus, stopSymbols);
                    break;
                }

            case Symbol.Minus:
                {
                    Expect(Symbol.Minus, stopSymbols);
                    break;
                }
        }
    }

    /// <summary>
    /// RelationalOperator = "&lt;" | "&lt;=" | "==" | "!=" | "&gt;" | "&gt;=" 
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void RelationalOperator(Set stopSymbols)
    {
        Debug.Assert(CurrentSymbol == Symbol.Less ||
                     CurrentSymbol == Symbol.LessOrEqual ||
                     CurrentSymbol == Symbol.Equal ||
                     CurrentSymbol == Symbol.NotEqual ||
                     CurrentSymbol == Symbol.Greater ||
                     CurrentSymbol == Symbol.GreaterOrEqual,
                     $"RelationalOperator: Invalid symbol {CurrentSymbol}.");
        switch (CurrentSymbol)
        {
            case Symbol.Less:
                {
                    Expect(Symbol.Less, stopSymbols);
                    break;
                }

            case Symbol.LessOrEqual:
                {
                    Expect(Symbol.LessOrEqual, stopSymbols);
                    break;
                }

            case Symbol.Equal:
                {
                    Expect(Symbol.Equal, stopSymbols);
                    break;
                }

            case Symbol.NotEqual:
                {
                    Expect(Symbol.NotEqual, stopSymbols);
                    break;
                }

            case Symbol.Greater:
                {
                    Expect(Symbol.Greater, stopSymbols);
                    break;
                }

            case Symbol.GreaterOrEqual:
                {
                    Expect(Symbol.GreaterOrEqual, stopSymbols);
                    break;
                }
        }
    }

    /// <summary>
    /// PrimaryOperator = "&amp;" | "|"
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void PrimaryOperator(Set stopSymbols)
    {
        Debug.Assert(CurrentSymbol == Symbol.And || CurrentSymbol == Symbol.Or, $"PrimaryOperator: Invalid symbol {CurrentSymbol}.");
        switch (CurrentSymbol)
        {
            case Symbol.And:
                {
                    Expect(Symbol.And, stopSymbols);
                    break;
                }

            case Symbol.Or:
                {
                    Expect(Symbol.Or, stopSymbols);
                    break;
                }
        }
    }

    /// <summary>
    /// MultiplyingOperator = "*" | "/" | "%" | "^" 
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void MultiplyingOperator(Set stopSymbols)
    {
        Debug.Assert(CurrentSymbol == Symbol.Multiply ||
                     CurrentSymbol == Symbol.Divide ||
                     CurrentSymbol == Symbol.Modulo ||
                     CurrentSymbol == Symbol.Power,
                     $"MultiplyingOperator: Invalid symbol {CurrentSymbol}.");
        switch (CurrentSymbol)
        {
            case Symbol.Multiply:
                {
                    Expect(Symbol.Multiply, stopSymbols);
                    break;
                }

            case Symbol.Divide:
                {
                    Expect(Symbol.Divide, stopSymbols);
                    break;
                }

            case Symbol.Modulo:
                {
                    Expect(Symbol.Modulo, stopSymbols);
                    break;
                }

            case Symbol.Power:
                {
                    Expect(Symbol.Power, stopSymbols);
                    break;
                }
        }
    }

    /// <summary>
    /// ProcedureInvocation = 
    ///                 Name "(" { Expression | "reference" ObjectAccess } ")"
    /// </summary>
    /// <param name="isParallel">
    /// Is the procedure invoked in a parallel statement?
    /// </param>
    /// <param name="stopSymbols">
    /// Stop symbols for error recovery.
    /// </param>
    /// <returns>
    /// Return type of the method.
    /// </returns>
    private Type ProcedureInvocation(bool isParallel, Set stopSymbols)
    {
        // Define stop symbols.
        Set commaStopSymbols = new Set(Symbol.Minus,
                                       Symbol.Name,
                                       Symbol.Numeral,
                                       Symbol.False,
                                       Symbol.True,
                                       Symbol.Not,
                                       Symbol.LeftParanthesis,
                                       Symbol.Reference);
        Set referenceStopSymbols = new Set(Symbol.Name);
        Set leftParanthesisStopSymbols = new Set(commaStopSymbols, Symbol.RightParanthesis);
        Set procedureNameStopSymbols = new Set(Symbol.LeftParanthesis);

        // Expect procedure name.
        int name = ExpectName(new Set(procedureNameStopSymbols,
                                      leftParanthesisStopSymbols,
                                      referenceStopSymbols,
                                      commaStopSymbols,
                                      stopSymbols));

        // Find the procedure name.
        ObjectRecord objectRecord = new ObjectRecord();
        _auditor.Find(name, ref objectRecord);

        // Is the statement in a procedure block?
        if (_procedureNest.Count != 0)
        {
            int procedureName = _procedureNest.Peek();
            ObjectRecord procedureRecord = new ObjectRecord();
            _auditor.Find(procedureName, ref procedureRecord);
            if (!procedureRecord.ProcedureRecord.CallsParallelUnfriendly)
            {
                procedureRecord.ProcedureRecord.CallsParallelUnfriendly =
                  ((objectRecord.ProcedureRecord.HighestScopeUsed != ProcedureRecord.NoScope) &&
                   (objectRecord.ProcedureRecord.HighestScopeUsed <= procedureRecord.MetaData.Level)) ||
                   objectRecord.ProcedureRecord.UsesIO ||
                   objectRecord.ProcedureRecord.CallsParallelUnfriendly;
            }
        }

        // Create a list for the argument records.
        List<ParameterRecord> argumentRecordList = [];

        // Expect "(".
        Expect(Symbol.LeftParanthesis, new Set(leftParanthesisStopSymbols,
                                               referenceStopSymbols,
                                               stopSymbols));

        if (!IsCurrentSymbol(Symbol.RightParanthesis))
        {
            ParameterRecord argumentRecord = new ParameterRecord();

            // Define stop symbols.
            Set expressionStopSymbols = new Set(Symbol.Comma, Symbol.RightParanthesis);
            Set objectAccessStopSymbols = new Set(Symbol.Comma, Symbol.RightParanthesis);

            // "reference" follows?
            if (IsCurrentSymbol(Symbol.Reference))
            {
                // Expect "reference".
                Expect(Symbol.Reference, new Set(referenceStopSymbols,
                                                 objectAccessStopSymbols,
                                                 commaStopSymbols,
                                                 stopSymbols));

                // Expect ObjectAccess.
                Metadata metadata = ObjectAccess(new Set(objectAccessStopSymbols, stopSymbols));

                // Make sure a constant is not passed in as a reference parameter.
                if (metadata.Kind == Kind.Constant)
                {
                    _annotator.KindError(Kind.Constant, KindErrorCategory.ConstantPassedAsReferenceParameter);
                }

                argumentRecord.ParameterType = metadata.Type;
                argumentRecord.ParameterKind = Kind.ReferenceParameter;
            }
            else
            {
                // Expect Expression.
                argumentRecord.ParameterType = Expression(new Set(expressionStopSymbols, stopSymbols));
                argumentRecord.ParameterKind = Kind.ValueParameter;
            }

            // Add the argument record to the argument record list.
            argumentRecordList.Add(argumentRecord);

            // "," follows?
            while (IsCurrentSymbol(Symbol.Comma))
            {
                // Expect ",".
                Expect(Symbol.Comma, new Set(commaStopSymbols, expressionStopSymbols, stopSymbols));

                argumentRecord = new ParameterRecord();

                // "reference" follows?
                if (IsCurrentSymbol(Symbol.Reference))
                {
                    // Expect "reference".
                    Expect(Symbol.Reference, new Set(referenceStopSymbols,
                                                     objectAccessStopSymbols,
                                                     commaStopSymbols,
                                                     stopSymbols));

                    // Expect ObjectAccess.
                    Metadata metadata = ObjectAccess(new Set(objectAccessStopSymbols, stopSymbols));

                    // Make sure a constant is not passed in as a reference parameter.
                    if (metadata.Kind == Kind.Constant)
                    {
                        _annotator.KindError(Kind.Constant, KindErrorCategory.ConstantPassedAsReferenceParameter);
                    }

                    argumentRecord.ParameterType = metadata.Type;
                    argumentRecord.ParameterKind = Kind.ReferenceParameter;
                }
                else
                {
                    // Expect Expression.
                    argumentRecord.ParameterType = Expression(new Set(expressionStopSymbols, stopSymbols));
                    argumentRecord.ParameterKind = Kind.ValueParameter;
                }

                // Add the argument record to the argument record list.
                argumentRecordList.Add(argumentRecord);
            }
        }

        // Expect ")".
        Expect(Symbol.RightParanthesis, stopSymbols);

        // Check if the procedure invocation statement matches its signature.
        List<ParameterRecord> parameterRecordList = objectRecord.ParameterRecordList;
        int totalParameters = parameterRecordList.Count;
        if (argumentRecordList.Count != totalParameters)
        {
            _annotator.KindError(Kind.Procedure, KindErrorCategory.ArgumentCountMismatch);
        }
        else
        {
            for (int count = 0; count < totalParameters; ++count)
            {
                if (parameterRecordList[count].ParameterKind != argumentRecordList[count].ParameterKind)
                {
                    _annotator.KindError(argumentRecordList[count].ParameterKind, KindErrorCategory.ParameterKindMismatch);
                }

                if (parameterRecordList[count].ParameterType != argumentRecordList[count].ParameterType)
                {
                    _annotator.TypeError(argumentRecordList[count].ParameterType, TypeErrorCategory.ParameterTypeMismatch);
                }
            }
        }

        // Assemble code.
        if (isParallel)
        {
            _assembler.Parallel();
        }

        _assembler.ProcedureInvocation(_auditor.BlockLevel - objectRecord.MetaData.Level,
                                       objectRecord.MetaData.ProcedureLabel);

        return objectRecord.MetaData.Type;
    }

    /// <summary>
    /// ExpressionList = Expression { "," Expression } 
    /// </summary>
    /// <param name="operation">Category of operation.</param>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    private void ExpressionList(OperationCategory operation, Set stopSymbols)
    {
        // Define stop symbols.
        Set expressionStopSymbols = new Set(Symbol.Comma);
        Set commaStopSymbols = new Set(Symbol.Minus,
                                       Symbol.Name,
                                       Symbol.Numeral,
                                       Symbol.False,
                                       Symbol.True,
                                       Symbol.Not,
                                       Symbol.LeftParanthesis);

        // Expect Expression.
        Type type = Expression(new Set(expressionStopSymbols, stopSymbols));

        if (operation == OperationCategory.Write)
        {
            // Assemble code.
            if (type == Type.Boolean)
            {
                _assembler.WriteBoolean();
            }
            else if (type == Type.Integer)
            {
                _assembler.WriteInteger();
            }
            else
            {
                _annotator.TypeError(type, TypeErrorCategory.InvalidTypeInWriteStatement);
            }
        }

        // "," follows?
        while (IsCurrentSymbol(Symbol.Comma))
        {
            // Expect ",".
            Expect(Symbol.Comma, new Set(commaStopSymbols, expressionStopSymbols, stopSymbols));

            // Expect Expression.
            type = Expression(new Set(expressionStopSymbols, stopSymbols));

            if (operation == OperationCategory.Write)
            {
                // Assemble code.
                if (type == Type.Boolean)
                {
                    _assembler.WriteBoolean();
                }
                else if (type == Type.Integer)
                {
                    _assembler.WriteInteger();
                }
                else
                {
                    _annotator.TypeError(type, TypeErrorCategory.InvalidTypeInWriteStatement);
                }
            }
        }
    }

    /// <summary>
    /// Expression = PrimaryExpression { PrimaryOperator PrimaryExpression } 
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    /// <returns>The type that the expression would evaluate to.</returns>
    private Type Expression(Set stopSymbols)
    {
        // Define primary operator symbols.
        Set primaryOperatorSymbols = new Set(Symbol.And, Symbol.Or);

        // Define stop symbols.
        Set primaryExpressionStopSymbols = new Set(primaryOperatorSymbols);
        Set primaryOperatorStopSymbols = new Set(Symbol.Minus,
                                                 Symbol.Name,
                                                 Symbol.Numeral,
                                                 Symbol.False,
                                                 Symbol.True,
                                                 Symbol.Not,
                                                 Symbol.LeftParanthesis);

        // Expect PrimaryExpression.
        Type type = PrimaryExpression(new Set(primaryExpressionStopSymbols, stopSymbols));

        // Primary operator follows?
        if (primaryOperatorSymbols.Contains(CurrentSymbol))
        {
            // Make sure the PrimaryExpression evaluates to type Boolean.
            if (type != Type.Boolean)
            {
                _annotator.TypeError(type, DiadicTypeErrorCategory.NonBooleanLeftOfLogicalOperator, CurrentSymbol);
            }
        }

        while (primaryOperatorSymbols.Contains(CurrentSymbol))
        {
            // Preserve the primary operator symbol.
            Symbol lastPrimaryOperator = CurrentSymbol;

            // Expect PrimaryOperator.
            PrimaryOperator(new Set(primaryOperatorStopSymbols, primaryExpressionStopSymbols, stopSymbols));

            // Expect PrimaryExpression.
            type = PrimaryExpression(new Set(primaryExpressionStopSymbols, stopSymbols));

            // Make sure the PrimaryExpression evaluates to type Boolean.
            if (type != Type.Boolean)
            {
                _annotator.TypeError(type,
                                     DiadicTypeErrorCategory.NonBooleanRightOfLogicalOperator,
                                     CurrentSymbol);
            }

            // Assemble code.
            Operation(lastPrimaryOperator);
        }

        return type;
    }

    /// <summary>
    /// PrimaryExpression = 
    /// SimpleExpression [ RelationalOperator SimpleExpression ]
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    /// <returns>
    /// The type that the primary expression would evaluate to.
    /// </returns>
    private Type PrimaryExpression(Set stopSymbols)
    {
        // Define relational operator symbols.
        Set relationalOperatorSymbols = new Set(Symbol.Less,
                                                Symbol.LessOrEqual,
                                                Symbol.Equal,
                                                Symbol.NotEqual,
                                                Symbol.Greater,
                                                Symbol.GreaterOrEqual);

        // Define stop symbols.
        Set relationalOperatorStopSymbols = new Set(Symbol.Minus,
                                                    Symbol.Name,
                                                    Symbol.Numeral,
                                                    Symbol.False,
                                                    Symbol.True,
                                                    Symbol.Not,
                                                    Symbol.LeftParanthesis);
        Set simpleExpressionStopSymbols = new Set(relationalOperatorSymbols);

        // Expect SimpleExpression.
        Type type = SimpleExpression(new Set(simpleExpressionStopSymbols,
                                             relationalOperatorStopSymbols,
                                             stopSymbols));

        // Relational operator follows?
        if (relationalOperatorSymbols.Contains(CurrentSymbol))
        {
            // Preserve the relational operator symbol.
            Symbol lastRelationalOperator = CurrentSymbol;

            // Equality operation?
            if ((CurrentSymbol == Symbol.Equal) || (CurrentSymbol == Symbol.NotEqual))
            {
                // Expect RelationalOperator.
                RelationalOperator(new Set(relationalOperatorStopSymbols, stopSymbols));

                // Expect SimpleExpression.
                Type typeOfRightHandElement = SimpleExpression(stopSymbols);

                // Types on either side of an equality operator should match.
                if ((type != typeOfRightHandElement) &&
                    (typeOfRightHandElement != Type.Universal))
                {
                    _annotator.TypeError(type, DiadicTypeErrorCategory.TypeMismatchAcrossEqualityOperator, lastRelationalOperator);
                }

                // Types across equality operator should not be void.
                if (type == Type.Void)
                {
                    _annotator.TypeError(type,
                                         DiadicTypeErrorCategory.InvalidTypeAcrossEqualityOperator,
                                         lastRelationalOperator);
                }
            }
            else  // Comparison operation
            {
                // Expect RelationalOperator.
                RelationalOperator(new Set(relationalOperatorStopSymbols, stopSymbols));

                // Expect SimpleExpression.
                Type typeOfRightHandElement = SimpleExpression(stopSymbols);

                // Types on either side of a comparison operator should be integers.
                if (type != Type.Integer)
                {
                    _annotator.TypeError(type,
                                         DiadicTypeErrorCategory.NonIntegerLeftOfRelationalOperator,
                                         lastRelationalOperator);
                }
                else if (typeOfRightHandElement != Type.Integer)
                {
                    _annotator.TypeError(typeOfRightHandElement,
                                         DiadicTypeErrorCategory.NonIntegerRightOfRelationalOperator,
                                         lastRelationalOperator);
                }
            }

            // Assemble code.
            Operation(lastRelationalOperator);

            // Relational operation should evalute to type Boolean.
            type = Type.Boolean;
        }

        return type;
    }

    /// <summary>
    /// SimpleExpression = [ "-" ] Term { AddingOperator Term } 
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    /// <returns>
    /// The type that the simple expression would evaluate to.
    /// </returns>
    private Type SimpleExpression(Set stopSymbols)
    {
        // Define adding operator symbols.
        Set addingOperatorSymbols = new Set(Symbol.Plus, Symbol.Minus);

        // Define stop symbols.
        Set termStopSymbols = new Set(addingOperatorSymbols);
        Set addingOperatorStopSymbols = new Set(Symbol.Minus,
                                                Symbol.Name,
                                                Symbol.Numeral,
                                                Symbol.False,
                                                Symbol.True,
                                                Symbol.Not,
                                                Symbol.LeftParanthesis);
        Set minusStopSymbols = new Set(addingOperatorStopSymbols);

        // Is the Term preceeded by a "-" ?
        bool minusPrecedes = false;
        if (IsCurrentSymbol(Symbol.Minus))
        {
            Expect(Symbol.Minus, new Set(minusStopSymbols, termStopSymbols, stopSymbols));
            minusPrecedes = true;
        }

        // Expect Term.
        Type type = Term(new Set(termStopSymbols, stopSymbols));

        if (minusPrecedes)
        {
            // "-" should be followed by an integer Term.
            if (type != Type.Integer)
            {
                _annotator.TypeError(Type.Integer,
                                     DiadicTypeErrorCategory.NonIntegerRightOfAdditionOperator,
                                     Symbol.Minus);
            }

            // Assemble code.
            _assembler.Minus();
        }

        // Adding operator follows?
        Symbol lastAddingOperator = Symbol.Unknown;
        bool addingOperation = false;
        if (addingOperatorSymbols.Contains(CurrentSymbol))
        {
            addingOperation = true;
        }

        while (addingOperatorSymbols.Contains(CurrentSymbol))
        {
            // Preserve the last adding operator.
            lastAddingOperator = CurrentSymbol;

            // Left hand side of adding operator has to be of type Integer.
            if (type != Type.Integer)
            {
                _annotator.TypeError(type,
                                     DiadicTypeErrorCategory.NonIntegerLeftOfAdditionOperator,
                                     lastAddingOperator);
            }

            // Expect AddingOperator.
            AddingOperator(new Set(addingOperatorStopSymbols, termStopSymbols, stopSymbols));

            // Expect Term.
            type = Term(new Set(termStopSymbols, stopSymbols));

            // Assemble code.
            Operation(lastAddingOperator);
        }

        if (addingOperation)
        {
            // Right hand side of adding operator has to be of type Integer.
            if (type != Type.Integer)
            {
                _annotator.TypeError(type,
                                     DiadicTypeErrorCategory.NonIntegerRightOfAdditionOperator,
                                     lastAddingOperator);
                type = Type.Integer;
            }
        }

        return type;
    }

    /// <summary>
    /// Term = Factor { MultiplyingOperator Factor } 
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    /// <returns>The type that the Term would evaluate to.</returns>
    private Type Term(Set stopSymbols)
    {
        // Define multiplying operator symbols.
        Set multiplyingOperatorSymbols = new Set(Symbol.Multiply,
                                                 Symbol.Divide,
                                                 Symbol.Modulo,
                                                 Symbol.Power);

        // Define stop symbols.
        Set factorStopSymbols = new Set(multiplyingOperatorSymbols);
        Set multiplyingOperatorStopSymbols = new Set(Symbol.Name,
                                                     Symbol.Numeral,
                                                     Symbol.False,
                                                     Symbol.True,
                                                     Symbol.Not,
                                                     Symbol.LeftParanthesis);

        // Expect Factor.
        Type type = Factor(new Set(factorStopSymbols, stopSymbols));

        // MultiplyingOperator follows?
        Symbol lastMultiplyingOperator = Symbol.Unknown;
        bool multiplyingOperation = false;
        if (multiplyingOperatorSymbols.Contains(CurrentSymbol))
        {
            multiplyingOperation = true;
        }

        while (multiplyingOperatorSymbols.Contains(CurrentSymbol))
        {
            // Preserve the last multiplying operator.
            lastMultiplyingOperator = CurrentSymbol;

            // Left hand side of multiplying operator has to be of type Integer.
            if (type != Type.Integer)
            {
                _annotator.TypeError(type,
                                     DiadicTypeErrorCategory.NonIntegerLeftOfMultiplicationOperator,
                                     lastMultiplyingOperator);
            }

            // Expect MultiplyingOperator.
            MultiplyingOperator(new Set(multiplyingOperatorStopSymbols, factorStopSymbols, stopSymbols));

            // Expect Factor.
            type = Factor(new Set(factorStopSymbols, stopSymbols));

            // Assemble code.
            Operation(lastMultiplyingOperator);
        }

        if (multiplyingOperation)
        {
            // Right hand side of multiplying operator has to be of type Integer.
            if (type != Type.Integer)
            {
                _annotator.TypeError(type,
                                     DiadicTypeErrorCategory.NonIntegerRightOfMultiplicationOperator,
                                     lastMultiplyingOperator);
                type = Type.Integer;
            }
        }

        return type;
    }

    /// <summary>
    /// Factor = Constant | ObjectAccess | "(" Expression ")" | "!" Factor 
    /// </summary>
    /// <param name="stopSymbols">Stop symbols for error recovery.</param>
    /// <returns>The type that the Factor evaluates to.</returns>
    private Type Factor(Set stopSymbols)
    {
        Type type;
        switch (CurrentSymbol)
        {
            case Symbol.Numeral:
            case Symbol.True:
            case Symbol.False:
                {
                    Metadata metadata = new Metadata();
                    Constant(ref metadata, stopSymbols); // Expect Constant.
                    type = metadata.Type;
                    _assembler.Constant(metadata.Value);  // Assemble code.
                    break;
                }

            case Symbol.Name:
                {
                    ObjectRecord objectRecord = new ObjectRecord();
                    _auditor.Find(Argument, ref objectRecord);

                    switch (objectRecord.MetaData.Kind)
                    {
                        case Kind.Constant:
                            {
                                Metadata metaData = new Metadata();
                                Constant(ref metaData, stopSymbols); // Expect Constant.
                                type = metaData.Type;
                                _assembler.Constant(metaData.Value);  // Assemble code.
                                break;
                            }

                        case Kind.Variable:
                        case Kind.ValueParameter:
                        case Kind.ReferenceParameter:
                        case Kind.ReturnParameter:
                        case Kind.Array:
                            {
                                Metadata metadata = ObjectAccess(stopSymbols);
                                type = metadata.Type;
                                _assembler.Value();                   // Assemble code.
                                break;
                            }

                        case Kind.Procedure:
                            {
                                type = ProcedureInvocation(false, stopSymbols);
                                break;
                            }

                        default:
                            {
                                Expect(Symbol.Name, stopSymbols);
                                type = Type.Universal;
                                break;
                            }
                    }

                    break;
                }

            case Symbol.LeftParanthesis:
                {
                    // Define stop symbols.
                    Set leftParanthesisStopSymbols = new Set(Symbol.Minus,
                                                             Symbol.Name,
                                                             Symbol.Numeral,
                                                             Symbol.False,
                                                             Symbol.True,
                                                             Symbol.Not,
                                                             Symbol.LeftParanthesis);
                    Set expressionStopSymbols = new Set(Symbol.RightParanthesis);

                    // Expect "(".
                    Expect(Symbol.LeftParanthesis, new Set(leftParanthesisStopSymbols,
                                                           expressionStopSymbols,
                                                           stopSymbols));

                    // Expect Expression.
                    type = Expression(new Set(expressionStopSymbols, stopSymbols));

                    // Expect ")".
                    Expect(Symbol.RightParanthesis, stopSymbols);
                    break;
                }

            case Symbol.Not:
                {
                    // Define stop symbols.
                    Set notStopSymbols = new Set(Symbol.Name,
                                                 Symbol.Numeral,
                                                 Symbol.False,
                                                 Symbol.True,
                                                 Symbol.Not,
                                                 Symbol.LeftParanthesis);

                    // Expect "!"
                    Expect(Symbol.Not, new Set(notStopSymbols, stopSymbols));

                    // Expect Factor.
                    type = Factor(stopSymbols);

                    // Make sure the expression is of type Boolean.
                    if (type != Type.Boolean)
                    {
                        _annotator.TypeError(type, TypeErrorCategory.NonBooleanToTheRightOfNotOperator);
                    }

                    // Assemble code.
                    _assembler.Not();
                    break;
                }

            default:
                {
                    ReportSyntaxErrorAndRecover(stopSymbols);
                    type = Type.Universal;
                    break;
                }

        }

        return type;
    }
}
