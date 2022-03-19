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

using CodeGeneration;
using ErrorReporting;
using GrammarAnalysis;
using LanguageConstructs;
using LexicalAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Type = LanguageConstructs.Type;

namespace Compilation
{
    /// <summary>
    /// Provides methods to perform compilation.
    /// </summary>
    public class Parser
    {
        Stack<int> procedureNest; // Current scope of nested procedures.
        Scanner scanner;          // Lexical analysis.
        Annotator annotator;      // Error reporting.
        Auditor auditor;          // Scope analysis.
        Assembler assembler;      // Code generation.

        /// <summary>
        /// Category of the operation for object access / expression.
        /// </summary>
        enum OperationCategory
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
            return this.Compile(reader, intermediateCodeFilename, null);
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
            this.procedureNest = new Stack<int>();
            this.scanner = new Scanner(reader);
            this.annotator = new Annotator(onPrintErrorReport ?? this.PrintCompilationError);
            this.auditor = new Auditor(this.annotator);
            this.assembler = new Assembler(this.annotator);

            // Define stop symbols.
            Set stopSymbols = new Set(Symbol.EndOfText);

            // Parse.
            bool proceed = this.scanner.NextSymbol();
            if (proceed)
            {
                this.Program(stopSymbols);
                if (!(this.IsCurrentSymbol(Symbol.EndOfText)))
                {
                    this.ReportSyntaxErrorAndRecover(stopSymbols);
                }

                proceed = this.annotator.ErrorFree;
                if (proceed)
                {
                    this.assembler.GenerateExecutable(intermediateCodeFilename);
                }
            }
            else
            {
                this.ReportSyntaxErrorAndRecover(stopSymbols);
            }

            return proceed;
        }

        /// <summary>
        /// Gets the current argument.
        /// </summary>
        int Argument
        {
            get
            {
                return (this.scanner.Argument);
            }
        }

        /// <summary>
        /// Gets the current symbol.
        /// </summary>
        Symbol CurrentSymbol
        {
            get
            {
                return (this.scanner.CurrentSymbol);
            }
        }

        /// <summary>
        /// Gets a value indicating if the given symbol matches the current symbol.
        /// </summary>
        /// <param name="symbol">The symbol of interest.</param>
        /// <returns>
        /// A value indicating if the given symbol matches the current symbol.
        /// </returns>
        bool IsCurrentSymbol(Symbol symbol)
        {
            return (this.CurrentSymbol == symbol);
        }

        /// <summary>
        /// Checks if the current symbol matches the expected symbol.
        /// </summary>
        /// <param name="expectedSymbol">The expected symbol.</param>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void Expect(Symbol expectedSymbol, Set stopSymbols)
        {
            if (IsCurrentSymbol(expectedSymbol))
            {
                this.scanner.NextSymbol();
            }
            else
            {
                this.ReportSyntaxErrorAndRecover(stopSymbols);
            }

            this.SyntaxCheck(stopSymbols);
        }

        /// <summary>
        /// Checks if the current symbol is a name.
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery</param>
        /// <returns>The argument of the name.</returns>
        int ExpectName(Set stopSymbols)
        {
            int name;
            if (this.CurrentSymbol == Symbol.Name)
            {
                name = this.Argument;
                this.scanner.NextSymbol();
            }
            else
            {
                name = Auditor.NoName;
                this.ReportSyntaxErrorAndRecover(stopSymbols);
            }

            this.SyntaxCheck(stopSymbols);

            return name;
        }

        /// <summary>
        /// Prints out compilation errors.
        /// </summary>
        /// <param name="errorCategory">Error category.</param>
        /// <param name="errorCode">Error code.</param>
        /// <param name="errorMessage">Error message.</param>
        void PrintCompilationError(string errorCategory,
                                   int errorCode,
                                   string errorMessage)
        {
            if (this.scanner.IsLineCorrect)
            {
                this.scanner.SetLineIsIncorrect();
                Console.WriteLine($"{errorCategory} error in line {this.scanner.LineNumber}. " +
                                  $"Error code = {errorCode}.\n{errorMessage}\n");
                Trace.WriteLine($"{errorCategory} error in line {this.scanner.LineNumber}. " +
                                $"Error code = {errorCode}. {errorMessage}");
            }
        }

        /// <summary>
        /// Reports syntax error and performs error recovery.
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void ReportSyntaxErrorAndRecover(Set stopSymbols)
        {
            this.annotator.SyntaxError();
            if (this.scanner.IsLineCorrect)
            {
                bool proceed = true;
                while ((!(stopSymbols.Contains(this.scanner.CurrentSymbol))) &&
                       proceed)
                {
                    proceed = this.scanner.NextSymbol();
                }
            }
        }

        /// <summary>
        /// Performs syntax check.
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void SyntaxCheck(Set stopSymbols)
        {
            if (!(stopSymbols.Contains(this.scanner.CurrentSymbol)))
            {
                this.ReportSyntaxErrorAndRecover(stopSymbols);
            }
        }

        /// <summary>
        /// Assembles code based on the arithmetical/logical operation.
        /// </summary>
        /// <param name="symbol">
        /// Symbol representing the arithmetical/logical operation.
        /// </param>
        void Operation(Symbol symbol)
        {
            switch (symbol)
            {
                case Symbol.Plus:
                    {
                        this.assembler.Add();
                        break;
                    }

                case Symbol.Minus:
                    {
                        this.assembler.Subtract();
                        break;
                    }

                case Symbol.Less:
                    {
                        this.assembler.Less();
                        break;
                    }

                case Symbol.LessOrEqual:
                    {
                        this.assembler.LessOrEqual();
                        break;
                    }

                case Symbol.Equal:
                    {
                        this.assembler.Equal();
                        break;
                    }

                case Symbol.NotEqual:
                    {
                        this.assembler.NotEqual();
                        break;
                    }

                case Symbol.Greater:
                    {
                        this.assembler.Greater();
                        break;
                    }

                case Symbol.GreaterOrEqual:
                    {
                        this.assembler.GreaterOrEqual();
                        break;
                    }

                case Symbol.And:
                    {
                        this.assembler.And();
                        break;
                    }

                case Symbol.Or:
                    {
                        this.assembler.Or();
                        break;
                    }

                case Symbol.Multiply:
                    {
                        this.assembler.Multiply();
                        break;
                    }

                case Symbol.Divide:
                    {
                        this.assembler.Divide();
                        break;
                    }

                case Symbol.Modulo:
                    {
                        this.assembler.Modulo();
                        break;
                    }

                case Symbol.Power:
                    {
                        this.assembler.Power();
                        break;
                    }

                default:
                    {
                        this.annotator.InternalError(
                          InternalErrorCategory.InvalidOperationSymbol);
                        break;
                    }
            }
        }

        /// <summary>
        /// Program = Block
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void Program(Set stopSymbols)
        {
            // Assemble code for Program.
            int objectsLengthLabel = this.assembler.CurrentAddress + 1;
            this.assembler.Program(0);

            // Define start of a new block.
            this.auditor.NewBlock();

            // Process Block.
            this.Block(false, stopSymbols);

            // Resolve the label for total objects length in this block.
            this.assembler.ResolveArgument(objectsLengthLabel,
                                           this.auditor.ObjectsLength);

            // Define end of the block.
            this.auditor.EndBlock();

            // Assemble code for "end-of-program".
            this.assembler.EndProgram();
        }

        /// <summary>
        /// Block = "{" DefinitionPart StatementPart "}" 
        /// </summary>
        /// <param name="newBlock">Should the auditor process a new block?</param>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void Block(bool newBlock, Set stopSymbols)
        {
            // Label for total objects length in this block.
            int objectsLengthLabel = this.assembler.CurrentAddress + 1;

            if (newBlock)
            {
                // Start a new block.
                this.auditor.NewBlock();

                // Assemble code for Block.
                this.assembler.Block(0);
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
            this.Expect(Symbol.Begin, new Set(beginStopSymbols,
                                              definitionPartStopSymbols,
                                              statementPartStopSymbols,
                                              endStopSymbols,
                                              stopSymbols));
            this.DefinitionPart(new Set(definitionPartStopSymbols,
                                        statementPartStopSymbols,
                                        endStopSymbols,
                                        stopSymbols));
            this.StatementPart(new Set(statementPartStopSymbols,
                                       endStopSymbols,
                                       stopSymbols));
            this.Expect(Symbol.End, new Set(endStopSymbols, stopSymbols));

            if (newBlock)
            {
                // Resolve the label for total objects length in this block.
                this.assembler.ResolveArgument(objectsLengthLabel,
                                               this.auditor.ObjectsLength);

                // Assemble code for "end-of-block".
                this.assembler.EndBlock();

                // End a block.
                this.auditor.EndBlock();
            }
        }

        /// <summary>
        /// DefinitionPart = { ProcedureDefinition    |
        ///                    ConstantDefinition ";" | 
        ///                    VariableDefinition ";"   } 
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void DefinitionPart(Set stopSymbols)
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
            while (definitionSymbols.Contains(this.CurrentSymbol))
            {
                bool expectSemiColon = this.Definition(
                                                  new Set(definitionPartStopSymbols,
                                                          stopSymbols));
                if (expectSemiColon)
                {
                    this.Expect(Symbol.SemiColon, new Set(semiColonStopSymbols,
                                                          definitionPartStopSymbols,
                                                          stopSymbols));
                }
            }
        }

        /// <summary>
        /// StatementPart = { IfStatement | WhileStatement | Statement ";" }
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void StatementPart(Set stopSymbols)
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
            while (statementSymbols.Contains(this.CurrentSymbol))
            {
                if (this.IsCurrentSymbol(Symbol.If) ||
                    this.IsCurrentSymbol(Symbol.While))
                {
                    // Define stop symbols.
                    Set statementStopSymbols = new Set(statementSymbols);

                    // Analyze.
                    this.Statement(new Set(statementStopSymbols, stopSymbols));
                }
                else
                {
                    // Define stop symbols.
                    Set statementStopSymbols = new Set(Symbol.SemiColon);
                    Set semiColonStopSymbols = new Set(statementSymbols);

                    // Analyze.
                    this.Statement(new Set(statementStopSymbols, stopSymbols));
                    this.Expect(Symbol.SemiColon, new Set(semiColonStopSymbols,
                                                          statementStopSymbols,
                                                          stopSymbols));
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
        bool Definition(Set stopSymbols)
        {
            bool expectSemicolon = true;

            switch (this.CurrentSymbol)
            {
                case Symbol.Constant:
                    {
                        this.ConstantDefinition(stopSymbols);
                        break;
                    }

                // Type symbols. 
                case Symbol.Integer:
                case Symbol.Boolean:
                case Symbol.Channel:
                    {
                        this.VariableDefinition(stopSymbols);
                        break;
                    }

                case Symbol.Procedure:
                    {
                        this.ProcedureDefinition(stopSymbols);
                        expectSemicolon = false;
                        break;
                    }

                default:
                    {
                        this.ReportSyntaxErrorAndRecover(stopSymbols);
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
        void ConstantDefinition(Set stopSymbols)
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
            this.Expect(Symbol.Constant, new Set(constantStopSymbols,
                                                 constantNameStopSymbols,
                                                 becomesStopSymbols,
                                                 minusStopSymbols,
                                                 stopSymbols));

            // Expect constant object's name.
            int name = this.ExpectName(new Set(constantNameStopSymbols,
                                               becomesStopSymbols,
                                               minusStopSymbols,
                                               stopSymbols));

            // Expect "=".
            this.Expect(Symbol.Becomes, new Set(becomesStopSymbols,
                                                minusStopSymbols,
                                                stopSymbols));

            // "-" follows?
            bool minusPrecedes = false;
            if (IsCurrentSymbol(Symbol.Minus))
            {
                // Expect "-".
                this.Expect(Symbol.Minus, new Set(minusStopSymbols, stopSymbols));
                minusPrecedes = true;
            }

            // Expect Constant.
            Metadata metadata = new Metadata
            {
                Kind = Kind.Constant
            };
            this.Constant(ref metadata, stopSymbols);

            // If "-" precedes the integer constant on right hand side, process.
            if (minusPrecedes)
            {
                if (metadata.Type != Type.Integer)
                {
                    this.annotator.TypeError(
                        metadata.Type,
                        TypeErrorCategory.MinusPrecedingNonIntegerInConstantDefinition);
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
            this.auditor.Define(name, metadata, ref objectRecord);
        }

        /// <summary>
        /// Constant = Numeral | BooleanSymbol | ConstantName
        /// </summary>
        /// <param name="metadata">Metadata of the object.</param>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void Constant(ref Metadata metadata, Set stopSymbols)
        {
            switch (this.CurrentSymbol)
            {
                case Symbol.Numeral:
                    {
                        metadata.Type = Type.Integer;
                        this.Expect(Symbol.Numeral, stopSymbols);
                        metadata.Value = this.Argument;
                        break;
                    }

                case Symbol.Name:
                    {
                        // Find the metadata of the object at the right hand side (rhs)
                        // of the constant definition.
                        ObjectRecord rhsObjectRecord = new ObjectRecord();
                        this.auditor.Find(this.Argument, ref rhsObjectRecord);
                        Metadata rhsMetadata = rhsObjectRecord.MetaData;
                        if (rhsMetadata.Kind == Kind.Constant)
                        {
                            metadata.Type = rhsMetadata.Type;
                            metadata.Value = rhsMetadata.Value;
                        }
                        else
                        {
                            this.annotator.KindError(
                                    rhsMetadata.Kind,
                                    KindErrorCategory.NonConstantInConstantDefinition);
                            metadata.Type = Type.Universal;
                        }

                        this.Expect(Symbol.Name, stopSymbols);
                        break;
                    }

                case Symbol.True:
                    {
                        metadata.Type = Type.Boolean;
                        metadata.Value = 1;
                        this.BooleanSymbol(stopSymbols);
                        break;
                    }

                case Symbol.False:
                    {
                        metadata.Type = Type.Boolean;
                        metadata.Value = 0;
                        this.BooleanSymbol(stopSymbols);
                        break;
                    }

                default:
                    {
                        metadata.Type = Type.Universal;
                        this.ReportSyntaxErrorAndRecover(stopSymbols);
                        break;
                    }
            }
        }

        /// <summary>
        /// VariableDefinition = TypeSymbol VariableList | 
        ///                      TypeSymbol ArrayDeclaration
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void VariableDefinition(Set stopSymbols)
        {
            // Define stop symbols.
            Set typeSymbolstopSymbols = new Set(Symbol.Name,
                                                Symbol.LeftBracket);

            // Analyze.
            Type type = this.TypeSymbol(typeSymbolstopSymbols);
            if (IsCurrentSymbol(Symbol.LeftBracket))
            {
                this.ArrayDeclaration(type, stopSymbols);
            }
            else
            {
                Metadata metaData = new Metadata
                {
                    Kind = Kind.Variable,
                    Type = type
                };
                this.VariableList(metaData, stopSymbols);
            }
        }

        /// <summary>
        ///  ProcedureDefinition = "@" [ "[" TypeSymbol Name "]" ] Name
        ///                         "("  [ ParameterDefinition ] ")" Block
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void ProcedureDefinition(Set stopSymbols)
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
            this.Expect(Symbol.Procedure, new Set(procedureStopSymbols,
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
            if (this.IsCurrentSymbol(Symbol.LeftBracket))
            {
                // Expect "[".
                this.Expect(Symbol.LeftBracket, new Set(leftBracketStopSymbols,
                                                        typeSymbolStopSymbols,
                                                        returnVariableStopSymbols,
                                                        rightBracketStopSymbols,
                                                        procedureNameStopSymbols,
                                                        leftParanthesisStopSymbols,
                                                        parameterDefinitionStopSymbols,
                                                        rightParanthesisStopSymbols,
                                                        stopSymbols));

                // Expect TypeSymbol.
                type = this.TypeSymbol(new Set(typeSymbolStopSymbols,
                                               returnVariableStopSymbols,
                                               rightBracketStopSymbols,
                                               procedureNameStopSymbols,
                                               leftParanthesisStopSymbols,
                                               parameterDefinitionStopSymbols,
                                               rightParanthesisStopSymbols,
                                               stopSymbols));

                // Expect return variable name.
                returnVariable = this.ExpectName(
                                              new Set(returnVariableStopSymbols,
                                                      rightBracketStopSymbols,
                                                      procedureNameStopSymbols,
                                                      leftParanthesisStopSymbols,
                                                      parameterDefinitionStopSymbols,
                                                      rightParanthesisStopSymbols,
                                                      stopSymbols));

                // Expect "]".
                this.Expect(Symbol.RightBracket,
                                              new Set(rightBracketStopSymbols,
                                                      procedureNameStopSymbols,
                                                      leftParanthesisStopSymbols,
                                                      parameterDefinitionStopSymbols,
                                                      rightParanthesisStopSymbols,
                                                      stopSymbols));
            }

            // Expect procedure name.
            int procedureName = this.ExpectName(
                                            new Set(procedureNameStopSymbols,
                                                    leftParanthesisStopSymbols,
                                                    parameterDefinitionStopSymbols,
                                                    rightParanthesisStopSymbols,
                                                    stopSymbols));

            // Define the procedure name.
            Metadata metaData = new Metadata();
            metaData.Kind = Kind.Procedure;
            metaData.Type = type;
            metaData.ProcedureLabel = this.assembler.CurrentAddress + 2;
            ObjectRecord objectRecord = new ObjectRecord();
            this.auditor.Define(procedureName, metaData, ref objectRecord);

            // Start of new block.
            this.auditor.NewBlock();

            // Define the return variable.
            if (returnVariable != Auditor.NoName)
            {
                Metadata returnVariableMetadata = new Metadata
                {
                    Kind = Kind.ReturnParameter,
                    Type = type
                };
                ObjectRecord returnVariableObjectRecord = new ObjectRecord();
                this.auditor.Define(returnVariable,
                                    returnVariableMetadata,
                                    ref returnVariableObjectRecord);
            }

            // Expect "(".
            this.Expect(Symbol.LeftParanthesis,
                        new Set(leftParanthesisStopSymbols,
                                parameterDefinitionStopSymbols,
                                rightParanthesisStopSymbols,
                                stopSymbols));

            // Expect ParameterDefinition.
            List<ParameterRecord> parameterRecordList = new List<ParameterRecord>();
            if (this.IsCurrentSymbol(Symbol.Reference) ||
                this.IsCurrentSymbol(Symbol.Integer) ||
                this.IsCurrentSymbol(Symbol.Boolean) ||
                this.IsCurrentSymbol(Symbol.Channel))
            {
                this.ParameterDefinition(ref parameterRecordList,
                                         new Set(parameterDefinitionStopSymbols,
                                                 rightParanthesisStopSymbols,
                                                 stopSymbols));
            }

            // Set the parameter record list.
            objectRecord.ParameterRecordList = parameterRecordList;

            // Expect ")".
            this.Expect(Symbol.RightParanthesis, new Set(rightParanthesisStopSymbols,
                                                         stopSymbols));

            // Define "end-of-procedure" label.
            int endOfProcedureLabel = this.assembler.CurrentAddress + 1;

            // Assemble code.
            this.assembler.Goto(endOfProcedureLabel);

            // Define "total-variables-in-this-scope" label.
            int variablesLengthLabel = this.assembler.CurrentAddress + 1;

            // Assemble code for procedure activation.
            this.assembler.ProcedureBlock(variablesLengthLabel);

            // Push the procedure name onto the stack.
            this.procedureNest.Push(procedureName);

            // Expect Block.
            this.Block(false, stopSymbols);
            int totalBlockVariables = this.auditor.ObjectsLength;

            // If the procedure uses parallel recursion, make sure it is 
            // not accessing any non-local variables or using IO statements
            // or calling parallel unfriendly procedures after the recursive 
            // call was made.
            if (objectRecord.ProcedureRecord.ExamineParallelRecursion)
            {
                this.ExamineRecursiveParallelFriendliness(objectRecord);
            }

            // End of block.
            this.auditor.EndBlock();

            // Pop the procedure name from the stack.
            this.procedureNest.Pop();

            // Assemble code for end of procedure activation.
            this.assembler.EndProcedureBlock(parameterRecordList.Count);

            // Resolve "total-variables-in-this-scope" and "end-of-procedure" labels.
            this.assembler.ResolveArgument(variablesLengthLabel,
                                           totalBlockVariables);
            this.assembler.ResolveAddress(endOfProcedureLabel);
        }

        /// <summary>
        /// ["reference"] TypeSymbol Name { "," ["reference"] TypeSymbol Name }
        /// </summary>
        /// <param name="parameterRecordList">List of parameter records.</param>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void ParameterDefinition(ref List<ParameterRecord> parameterRecordList,
                                 Set stopSymbols)
        {
            ParameterRecord parameterRecord = new ParameterRecord();
            parameterRecordList = new List<ParameterRecord>();
            List<int> parameterList = new List<int>();

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
            if (this.IsCurrentSymbol(Symbol.Reference))
            {
                this.Expect(Symbol.Reference, new Set(referenceStopSymbols,
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
            parameterRecord.ParameterType = this.TypeSymbol(
                                                  new Set(typeSymbolStopSymbols,
                                                          commaStopSymbols,
                                                          referenceStopSymbols,
                                                          parameterNameStopSymbols,
                                                          stopSymbols));

            // Add the parameter record to the parameter record list.
            parameterRecordList.Add(parameterRecord);

            // Expect parameter name.
            int parameter = this.ExpectName(new Set(parameterNameStopSymbols,
                                                    stopSymbols));
            parameterList.Add(parameter);

            // "," follows?
            while (IsCurrentSymbol(Symbol.Comma))
            {
                parameterRecord = new ParameterRecord();

                // Expect ",".
                this.Expect(Symbol.Comma, new Set(referenceStopSymbols,
                                                  typeSymbolStopSymbols,
                                                  parameterNameStopSymbols,
                                                  commaStopSymbols,
                                                  stopSymbols));

                // Reference parameter?
                if (this.IsCurrentSymbol(Symbol.Reference))
                {
                    this.Expect(Symbol.Reference, new Set(referenceStopSymbols,
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
                parameterRecord.ParameterType = this.TypeSymbol(
                                                    new Set(typeSymbolStopSymbols,
                                                            commaStopSymbols,
                                                            referenceStopSymbols,
                                                            parameterNameStopSymbols,
                                                            stopSymbols));

                // Add the parameter record to the parameter record list.
                parameterRecordList.Add(parameterRecord);

                // Expect parameter name.
                parameter = this.ExpectName(new Set(parameterNameStopSymbols,
                                                    stopSymbols));
                parameterList.Add(parameter);
            }

            int max = parameterRecordList.Count;
            if (parameterList.Count != max)
            {
                this.annotator.InternalError(
                                      InternalErrorCategory.InternalProcessingError);
            }
            else
            {
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
                    this.auditor.Define(parameterList[count],
                                        metaData,
                                        ref newObjectRecord);
                }
            }
        }

        /// <summary>
        /// TypeSymbol = "integer" | "boolean" | "channel"
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        /// <returns>Returns the Type.</returns>
        Type TypeSymbol(Set stopSymbols)
        {
            Type type = Type.Universal;

            switch (this.CurrentSymbol)
            {
                case Symbol.Integer:
                    {
                        this.Expect(Symbol.Integer, stopSymbols);
                        type = Type.Integer;
                        break;
                    }

                case Symbol.Boolean:
                    {
                        this.Expect(Symbol.Boolean, stopSymbols);
                        type = Type.Boolean;
                        break;
                    }

                case Symbol.Channel:
                    {
                        this.Expect(Symbol.Channel, stopSymbols);
                        type = Type.Channel;
                        break;
                    }

                default:
                    {
                        this.ReportSyntaxErrorAndRecover(stopSymbols);
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
        void ArrayDeclaration(Type type, Set stopSymbols)
        {
            // Define stop symbols.
            Set rightBracketStopSymbols = new Set(Symbol.Name);
            Set constantStopSymbols = new Set(Symbol.RightBracket);
            Set leftBracketStopSymbols = new Set(Symbol.Numeral,
                                                 Symbol.Name);

            // Expect "[".
            this.Expect(Symbol.LeftBracket, new Set(leftBracketStopSymbols,
                                                    constantStopSymbols,
                                                    rightBracketStopSymbols,
                                                    stopSymbols));

            // Find the metadata of the constant that defines the bound of the array.
            Metadata arrayBoundMetadata = new Metadata();
            this.Constant(ref arrayBoundMetadata, new Set(constantStopSymbols,
                                                          rightBracketStopSymbols,
                                                          stopSymbols));

            // The bound of the array has to be of type integer.
            if (arrayBoundMetadata.Type != Type.Integer)
            {
                this.annotator.TypeError(
                                  arrayBoundMetadata.Type,
                                  TypeErrorCategory.NonIntegerIndexInArrayDeclaration);
            }

            // Expect "]".
            this.Expect(Symbol.RightBracket, new Set(rightBracketStopSymbols,
                                                     stopSymbols));

            // Set the metadata of the array object.
            Metadata metadata = new Metadata();
            metadata.Kind = Kind.Array;
            metadata.Type = type;
            metadata.UpperBound = arrayBoundMetadata.Value;

            // The bound of the array has to be a positive integer.
            if ((metadata.UpperBound <= 0) &&
                (metadata.Type == Type.Integer)) /* if not an integer, it would be 
                                        caught in the type checking above. */
            {
                this.annotator.KindError(
                          Kind.Variable,
                          KindErrorCategory.NonPositiveIntegerIndexInArrayDeclaration);
            }

            // Expect VariableList.
            this.VariableList(metadata, stopSymbols);
        }

        /// <summary>
        /// VariableList = VariableName { "," VariableName } 
        /// </summary>
        /// <param name="metadata">Metadata of the object(s).</param>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void VariableList(Metadata metadata, Set stopSymbols)
        {
            // Define stop symbols.
            Set variableNameStopSymbols = new Set(Symbol.Comma);
            Set commaStopSymbols = new Set(Symbol.Name);

            // Read the object name, and define it.
            int name = this.ExpectName(new Set(variableNameStopSymbols,
                                               commaStopSymbols,
                                               stopSymbols));
            ObjectRecord objectRecord = new ObjectRecord();
            this.auditor.Define(name, metadata, ref objectRecord);

            // "," follows?
            while (IsCurrentSymbol(Symbol.Comma))
            {
                // Expect ",".
                this.Expect(Symbol.Comma, new Set(commaStopSymbols,
                                                  variableNameStopSymbols,
                                                  stopSymbols));

                // Read the object name, and define it.
                name = this.ExpectName(new Set(variableNameStopSymbols,
                                               commaStopSymbols,
                                               stopSymbols));
                ObjectRecord newObjectRecord = new ObjectRecord();
                this.auditor.Define(name, metadata, ref newObjectRecord);
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
        void ObjectAccessList(OperationCategory operation, Set stopSymbols)
        {
            // Define stop symbols.
            Set objectAccessStopSymbols = new Set(Symbol.Comma);
            Set commaStopSymbols = new Set(Symbol.Name);

            // Expect ObjectAccess.
            Metadata metadata =
              this.ObjectAccess(new Set(objectAccessStopSymbols, stopSymbols));

            if (operation == OperationCategory.Read)
            {
                // Read statement should not be applied on a constant.
                if (metadata.Kind == Kind.Constant)
                {
                    this.annotator.KindError(Kind.Constant,
                                             KindErrorCategory.ReadModifiesConstant);
                }

                // Assemble code.
                if (metadata.Type == Type.Boolean)
                {
                    this.assembler.ReadBoolean();
                }
                else if (metadata.Type == Type.Integer)
                {
                    this.assembler.ReadInteger();
                }
                else
                {
                    this.annotator.TypeError(
                                       metadata.Type,
                                       TypeErrorCategory.InvalidTypeInReadStatement);
                }
            }
            else if (operation == OperationCategory.Randomize)
            {
                if (metadata.Type != Type.Integer)
                {
                    this.annotator.TypeError(
                                      metadata.Type,
                                      TypeErrorCategory.NonIntegerInRandomizeStatement);
                }

                if (metadata.Kind == Kind.Constant)
                {
                    this.annotator.KindError(
                                       Kind.Constant,
                                       KindErrorCategory.RandomizeModifiesConstant);
                }

                // Assemble code.
                this.assembler.Randomize();
            }
            else if (operation == OperationCategory.Open)
            {
                if (metadata.Type != Type.Channel)
                {
                    this.annotator.TypeError(
                                        metadata.Type,
                                        TypeErrorCategory.NonChannelInOpenStatement);
                }

                // Assemble code.
                this.assembler.Open();
            }

            // "," follows?
            while (this.IsCurrentSymbol(Symbol.Comma))
            {
                // Expect ",".
                this.Expect(Symbol.Comma, new Set(commaStopSymbols,
                                                  objectAccessStopSymbols,
                                                  stopSymbols));

                // Expect ObjectAccess.
                metadata =
                  this.ObjectAccess(new Set(objectAccessStopSymbols, stopSymbols));

                if (operation == OperationCategory.Read)
                {
                    // Read statement should not be applied on a constant.
                    if (metadata.Kind == Kind.Constant)
                    {
                        this.annotator.KindError(Kind.Constant,
                                                 KindErrorCategory.ReadModifiesConstant);
                    }

                    // Assemble code.
                    if (metadata.Type == Type.Boolean)
                    {
                        this.assembler.ReadBoolean();
                    }
                    else if (metadata.Type == Type.Integer)
                    {
                        this.assembler.ReadInteger();
                    }
                    else
                    {
                        this.annotator.TypeError(
                                           metadata.Type,
                                           TypeErrorCategory.InvalidTypeInReadStatement);
                    }
                }
                else if (operation == OperationCategory.Randomize)
                {
                    if (metadata.Type != Type.Integer)
                    {
                        this.annotator.TypeError(
                                        metadata.Type,
                                        TypeErrorCategory.NonIntegerInRandomizeStatement);
                    }

                    if (metadata.Kind == Kind.Constant)
                    {
                        this.annotator.KindError(
                                         Kind.Constant,
                                         KindErrorCategory.RandomizeModifiesConstant);
                    }

                    // Assemble code.
                    this.assembler.Randomize();
                }
                else if (operation == OperationCategory.Open)
                {
                    if (metadata.Type != Type.Channel)
                    {
                        this.annotator.TypeError(
                                            metadata.Type,
                                            TypeErrorCategory.NonChannelInOpenStatement);
                    }

                    // Assemble code.
                    this.assembler.Open();
                }
            }
        }

        /// <summary>
        /// ObjectAccess = ObjectName [ IndexedSelector ]
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        /// <returns>Returns the metadata of the accessed object.</returns>
        Metadata ObjectAccess(Set stopSymbols)
        {
            // Define stop symbols.
            Set objectNameStopSymbols = new Set(Symbol.LeftBracket);

            // Access the object.
            ObjectRecord objectRecord = new ObjectRecord();
            this.auditor.Find(this.Argument, ref objectRecord);
            this.Expect(Symbol.Name, new Set(objectNameStopSymbols, stopSymbols));

            // Is the statement in a procedure block?
            if (((objectRecord.MetaData.Kind == Kind.Variable) ||
                 (objectRecord.MetaData.Kind == Kind.Array)) &&
                (this.procedureNest.Count != 0))
            {
                int procedureName = this.procedureNest.Peek();
                ObjectRecord procedureRecord = new ObjectRecord();
                this.auditor.Find(procedureName, ref procedureRecord);
                if ((procedureRecord.ProcedureRecord.HighestScopeUsed ==
                                                          ProcedureRecord.NoScope) ||
                    (procedureRecord.ProcedureRecord.HighestScopeUsed >
                                                          objectRecord.MetaData.Level))
                {
                    procedureRecord.ProcedureRecord.HighestScopeUsed =
                                                            objectRecord.MetaData.Level;
                }
            }

            if (objectRecord.MetaData.Kind == Kind.Procedure)
            {
                this.annotator.KindError(
                                Kind.Procedure,
                                KindErrorCategory.ProcedureAccessedAsObject);
            }
            else if (objectRecord.MetaData.Kind == Kind.Array)
            {
                if (!(IsCurrentSymbol(Symbol.LeftBracket)))
                {
                    this.annotator.KindError(
                                  Kind.Array,
                                  KindErrorCategory.ArrayVariableMissingIndexedSelector);
                }
                else
                {
                    // Assemble code for the array object.
                    int displacement = objectRecord.MetaData.Displacement;
                    int level = this.auditor.BlockLevel - objectRecord.MetaData.Level;
                    this.assembler.Variable(level, displacement);

                    // Assemble code for the indexed selector.
                    this.IndexedSelector(stopSymbols);
                    this.assembler.Index(objectRecord.MetaData.UpperBound);
                }
            }
            else
            {
                // Assemble code for the object.
                int displacement = objectRecord.MetaData.Displacement;
                int level = this.auditor.BlockLevel - objectRecord.MetaData.Level;
                if (objectRecord.MetaData.Kind == Kind.ReferenceParameter)
                {
                    this.assembler.ReferenceParameter(level, displacement);
                }
                else
                {
                    this.assembler.Variable(level, displacement);
                }
            }

            return objectRecord.MetaData;
        }

        /// <summary>
        /// IndexedSelector = "[" Expression "]" 
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void IndexedSelector(Set stopSymbols)
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
            this.Expect(Symbol.LeftBracket, new Set(leftBracketStopSymbols,
                                                    expressionStopSymbols,
                                                    stopSymbols));

            // Expect Expression.
            Type type = this.Expression(new Set(expressionStopSymbols, stopSymbols));
            if (type != Type.Integer)
            {
                this.annotator.TypeError(type, TypeErrorCategory.NonIntegerArrayIndex);
            }

            // Expect "]".
            this.Expect(Symbol.RightBracket, stopSymbols);
        }

        /// <summary>
        /// BooleanSymbol = "false" | "true" 
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void BooleanSymbol(Set stopSymbols)
        {
            switch (this.CurrentSymbol)
            {
                case Symbol.False:
                    {
                        this.Expect(Symbol.False, stopSymbols);
                        break;
                    }

                case Symbol.True:
                    {
                        this.Expect(Symbol.True, stopSymbols);
                        break;
                    }

                default:
                    {
                        this.ReportSyntaxErrorAndRecover(stopSymbols);
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
        void Statement(Set stopSymbols)
        {
            switch (this.CurrentSymbol)
            {
                case Symbol.Read:
                    {
                        this.ReadStatement(stopSymbols);
                        break;
                    }

                case Symbol.Write:
                    {
                        this.WriteStatement(stopSymbols);
                        break;
                    }

                case Symbol.Name:
                    {
                        ObjectRecord objectRecord = new ObjectRecord();
                        this.auditor.Find(this.Argument, ref objectRecord);
                        if (objectRecord.MetaData.Kind == Kind.Procedure)
                        {
                            this.ProcedureInvocation(false, stopSymbols);
                        }
                        else
                        {
                            this.AssignmentStatement(stopSymbols);
                        }

                        break;
                    }

                case Symbol.If:
                    {
                        this.IfStatement(stopSymbols);
                        break;
                    }

                case Symbol.While:
                    {
                        this.WhileStatement(stopSymbols);
                        break;
                    }

                case Symbol.Randomize:
                    {
                        this.RandomizeStatement(stopSymbols);
                        break;
                    }

                case Symbol.Open:
                    {
                        this.OpenStatement(stopSymbols);
                        break;
                    }

                case Symbol.Send:
                    {
                        this.SendStatement(stopSymbols);
                        break;
                    }

                case Symbol.Receive:
                    {
                        this.ReceiveStatement(stopSymbols);
                        break;
                    }

                case Symbol.Parallel:
                    {
                        this.ParallelStatement(stopSymbols);
                        break;
                    }

                default:
                    {
                        this.ReportSyntaxErrorAndRecover(stopSymbols);
                        break;
                    }
            }
        }

        /// <summary>
        /// ReadStatement = "read" ObjectAccessList
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void ReadStatement(Set stopSymbols)
        {
            // Define stop symbols.
            Set readStopSymbols = new Set(Symbol.Name);

            // Expect "read".
            this.Expect(Symbol.Read, new Set(readStopSymbols, stopSymbols));

            // Is the statement in a procedure block?
            if (this.procedureNest.Count != 0)
            {
                int procedureName = this.procedureNest.Peek();
                ObjectRecord procedureRecord = new ObjectRecord();
                this.auditor.Find(procedureName, ref procedureRecord);
                procedureRecord.ProcedureRecord.UsesIO = true;
            }

            // Expect ObjectAccessList.
            this.ObjectAccessList(OperationCategory.Read, stopSymbols);
        }

        /// <summary>
        /// WriteStatement = "write" ExpressionList 
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void WriteStatement(Set stopSymbols)
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
            this.Expect(Symbol.Write, new Set(writeStopSymbols, stopSymbols));

            // Is the statement in a procedure block?
            if (this.procedureNest.Count != 0)
            {
                int procedureName = this.procedureNest.Peek();
                ObjectRecord procedureRecord = new ObjectRecord();
                this.auditor.Find(procedureName, ref procedureRecord);
                procedureRecord.ProcedureRecord.UsesIO = true;
            }

            // Expect ExpressList.
            this.ExpressionList(OperationCategory.Write, stopSymbols);
        }

        /// <summary>
        /// OpenStatement = "open" ObjectAccessList
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void OpenStatement(Set stopSymbols)
        {
            // Define stop symbols.
            Set openStopSymbols = new Set(Symbol.Name);

            // Expect "open".
            this.Expect(Symbol.Open, new Set(openStopSymbols, stopSymbols));

            // Expect ObjectAccessList.
            this.ObjectAccessList(OperationCategory.Open, stopSymbols);
        }

        /// <summary>
        /// RandomizeStatement = "random" ObjectAccessList
        /// </summary>
        /// <param name="stopSymbols"></param>
        void RandomizeStatement(Set stopSymbols)
        {
            // Define stop symbols.
            Set randomizeStopSymbols = new Set(Symbol.Name);

            // Expect "random".
            this.Expect(Symbol.Randomize, new Set(randomizeStopSymbols, stopSymbols));

            // Expect ObjectAccessList.
            this.ObjectAccessList(OperationCategory.Randomize, stopSymbols);
        }

        /// <summary>
        /// SendStatement = "send" Expression "->" Expression
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void SendStatement(Set stopSymbols)
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
            this.Expect(Symbol.Send, new Set(sendStopSymbols,
                                             expressionStopSymbols,
                                             throughStopSymbols,
                                             stopSymbols));

            // Expect Expression.
            Type integerExpressionType = this.Expression(new Set(
                                                              expressionStopSymbols,
                                                              throughStopSymbols,
                                                              stopSymbols));

            // Expression should be of type integer.
            if (integerExpressionType != Type.Integer)
            {
                this.annotator.TypeError(
                                    integerExpressionType,
                                    TypeErrorCategory.NonIntegerValueInSendStatement);
            }

            // Expect "->".
            this.Expect(Symbol.Through, new Set(throughStopSymbols, stopSymbols));

            // Expect Expression.
            Type channelType = this.Expression(stopSymbols);

            // Object should be of type channel.
            if (channelType != Type.Channel)
            {
                this.annotator.TypeError(channelType,
                                         TypeErrorCategory.NonChannelInSendStatement);
            }

            // Assemble code for send.
            this.assembler.Send();
        }

        /// <summary>
        /// ReceiveStatement = "receive" ObjectAccess "->" Expression
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void ReceiveStatement(Set stopSymbols)
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
            this.Expect(Symbol.Receive, new Set(receiveStopSymbols,
                                                integerVariableStopSymbols,
                                                throughStopSymbols,
                                                stopSymbols));

            // Expect ObjectAccess.
            Metadata integerVariableMetadata = this.ObjectAccess(
                                            new Set(integerVariableStopSymbols,
                                                    throughStopSymbols,
                                                    stopSymbols));

            // Object should be of type integer.
            if (integerVariableMetadata.Type != Type.Integer)
            {
                this.annotator.TypeError(
                                 integerVariableMetadata.Type,
                                 TypeErrorCategory.NonIntegerValueInReceiveStatement);
            }

            // Object should be not of kind constant.
            if (integerVariableMetadata.Kind == Kind.Constant)
            {
                this.annotator.KindError(Kind.Constant,
                                         KindErrorCategory.ReceiveModifiesConstant);
            }

            // Expect "->".
            this.Expect(Symbol.Through, new Set(throughStopSymbols, stopSymbols));

            // Expect ObjectAccess.
            Type channelType = this.Expression(stopSymbols);

            // Object should be of type channel.
            if (channelType != Type.Channel)
            {
                this.annotator.TypeError(
                                      channelType,
                                      TypeErrorCategory.NonChannelInReceiveStatement);
            }

            // Assemble code for receive.
            this.assembler.Receive();
        }

        /// <summary>
        /// Examines whether a procedure that employs parallel recursion
        /// is parallel friendly.
        /// </summary>
        /// <param name="objectRecord">Object record of the procedure.</param>
        void ExamineRecursiveParallelFriendliness(ObjectRecord objectRecord)
        {
            // Uses I/O statements?
            if (objectRecord.ProcedureRecord.UsesIO)
            {
                this.annotator.KindError(Kind.Procedure,
                                         KindErrorCategory.ParallelRecursionUsesIO);
            }

            // Uses non local variables?
            if ((objectRecord.ProcedureRecord.HighestScopeUsed !=
                                                      ProcedureRecord.NoScope) &&
                (objectRecord.ProcedureRecord.HighestScopeUsed <=
                                                      objectRecord.MetaData.Level))
            {
                this.annotator.KindError(
                                    Kind.Procedure,
                                    KindErrorCategory.ParallelRecursionUsesNonLocals);
            }

            // Calls parallel unfriendly procedures?
            if (objectRecord.ProcedureRecord.CallsParallelUnfriendly)
            {
                this.annotator.KindError(
                                Kind.Procedure,
                                KindErrorCategory.ParallelRecursionCallsUnfriendly);
            }
        }

        /// <summary>
        /// ParallelStatement = "parallel" ProcedureInvocation
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void ParallelStatement(Set stopSymbols)
        {
            // Define stop symbols.
            Set parallelStopSymbols = new Set(Symbol.Name);

            // Expect "parallel".
            this.Expect(Symbol.Parallel, new Set(parallelStopSymbols, stopSymbols));

            // Expect the prallel procedure name.
            ObjectRecord objectRecord = new ObjectRecord();
            this.auditor.Find(this.Argument, ref objectRecord);
            if (objectRecord.MetaData.Kind == Kind.Procedure)
            {
                // Has non-void return?
                if (objectRecord.MetaData.Type != Type.Void)
                {
                    this.annotator.KindError(
                                  Kind.Procedure,
                                  KindErrorCategory.ParallelProcedureHasNonVoidReturn);
                }

                List<ParameterRecord> parameterRecordList =
                                                    objectRecord.ParameterRecordList;
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
                    this.annotator.KindError(
                              Kind.Procedure,
                              KindErrorCategory.ParallelProcedureHasReferenceParameter);
                }

                // Has no channel parameter?
                if (!(hasChannelParameter))
                {
                    this.annotator.KindError(
                              Kind.Procedure,
                              KindErrorCategory.ParallelProcedureHasNoChannelParameter);
                }

                // Uses I/O statements?
                if (objectRecord.ProcedureRecord.UsesIO)
                {
                    this.annotator.KindError(Kind.Procedure,
                                             KindErrorCategory.ParallelProcedureUsesIO);
                }

                // Uses non local variables?
                if ((objectRecord.ProcedureRecord.HighestScopeUsed !=
                                                        ProcedureRecord.NoScope) &&
                    (objectRecord.ProcedureRecord.HighestScopeUsed <=
                                                        objectRecord.MetaData.Level))
                {
                    this.annotator.KindError(
                                    Kind.Procedure,
                                    KindErrorCategory.ParallelProcedureUsesNonLocals);
                }

                // Calls parallel unfriendly procedures?
                if (objectRecord.ProcedureRecord.CallsParallelUnfriendly)
                {
                    this.annotator.KindError(
                                    Kind.Procedure,
                                    KindErrorCategory.ParallelProcedureCallsUnfriendly);
                }
            }
            else
            {
                // "parallel" keyword is not followed by procedure invocation.
                this.annotator.KindError(
                          objectRecord.MetaData.Kind,
                          KindErrorCategory.NonProcedureInParallelStatement);
            }

            // Procedure invocation.
            this.ProcedureInvocation(true, stopSymbols);
        }

        /// <summary>
        /// AssignmentStatement = ObjectAccess [, ObjectAccess] "=" 
        ///                        Expression [, Expression]
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void AssignmentStatement(Set stopSymbols)
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

            List<Type> leftHandSideTypes = new List<Type>();

            // Expect ObjectAccess.
            Metadata metadata = this.ObjectAccess(
                                            new Set(objectAccessStopSymbols,
                                                    becomesStopSymbols,
                                                    expressionStopSymbols,
                                                    expressionCommaStopSymbols,
                                                    stopSymbols));
            leftHandSideTypes.Add(metadata.Type);

            // Left hand side of the assignment statement should not be a constant.
            if (metadata.Kind == Kind.Constant)
            {
                this.annotator.KindError(
                                metadata.Kind,
                                KindErrorCategory.AssignmentModifiesConstant);
            }

            // "," follows?
            while (this.IsCurrentSymbol(Symbol.Comma))
            {
                // Expect ",".
                this.Expect(Symbol.Comma, new Set(objectAccessCommaStopSymbols,
                                                  objectAccessStopSymbols,
                                                  becomesStopSymbols,
                                                  expressionStopSymbols,
                                                  expressionCommaStopSymbols,
                                                  stopSymbols));

                // Expect ObjectAccess.
                metadata = this.ObjectAccess(new Set(objectAccessStopSymbols,
                                                     becomesStopSymbols,
                                                     expressionStopSymbols,
                                                     expressionCommaStopSymbols,
                                                     stopSymbols));
                leftHandSideTypes.Add(metadata.Type);

                // Left hand side of the assignment statement should not be a constant.
                if (metadata.Kind == Kind.Constant)
                {
                    this.annotator.KindError(
                                    metadata.Kind,
                                    KindErrorCategory.AssignmentModifiesConstant);
                }
            }

            // Expect "="
            this.Expect(Symbol.Becomes, new Set(becomesStopSymbols,
                                                expressionStopSymbols,
                                                expressionCommaStopSymbols,
                                                stopSymbols));

            List<Type> rightHandSideTypes = new List<Type>();

            // Expect Expression.
            Type rightHandSideType = this.Expression(new Set(expressionStopSymbols,
                                                             stopSymbols));
            rightHandSideTypes.Add(rightHandSideType);

            // "," follows?
            while (this.IsCurrentSymbol(Symbol.Comma))
            {
                // Expect ",".
                this.Expect(Symbol.Comma, new Set(expressionCommaStopSymbols,
                                                  expressionStopSymbols,
                                                  stopSymbols));

                // Expect Expression.
                rightHandSideType = this.Expression(new Set(expressionStopSymbols,
                                                            stopSymbols));
                rightHandSideTypes.Add(rightHandSideType);
            }

            int total = leftHandSideTypes.Count;
            if (total != rightHandSideTypes.Count)
            {
                this.annotator.KindError(Kind.Variable,
                                         KindErrorCategory.AssignmentCountMismatch);
            }
            else
            {
                for (int count = 0; count < total; ++count)
                {
                    // Make sure the types on either side of the assignment match.
                    if ((leftHandSideTypes[count] != rightHandSideTypes[count]) &&
                        (rightHandSideTypes[count] != Type.Universal))
                    {
                        this.annotator.TypeError(
                                            leftHandSideTypes[count],
                                            TypeErrorCategory.TypeMismatchInAssignment);
                    }
                }
            }

            // Assemble code.
            assembler.Assign(total);
        }

        /// <summary>
        /// IfStatement = "if" "(" Expression ")" Block [ "else" Block ]
        /// NOTE: 'Expression' in the above rule has to be of type boolean.
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void IfStatement(Set stopSymbols)
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
            this.Expect(Symbol.If, new Set(ifStopSymbols,
                                           leftParanthesisStopSymbols,
                                           expressionStopSymbols,
                                           rightParanthesisStopSymbols,
                                           blockStopSymbols,
                                           stopSymbols));

            // Expect "(".
            this.Expect(Symbol.LeftParanthesis, new Set(leftParanthesisStopSymbols,
                                                        expressionStopSymbols,
                                                        rightParanthesisStopSymbols,
                                                        blockStopSymbols,
                                                        stopSymbols));

            // Expect Expression.
            Type type = this.Expression(new Set(expressionStopSymbols,
                                                rightParanthesisStopSymbols,
                                                blockStopSymbols,
                                                stopSymbols));

            // Make sure the expression would evaluate to a boolean value.
            if (type != Type.Boolean)
            {
                this.annotator.TypeError(type,
                                         TypeErrorCategory.NonBooleanInIfCondition);
            }

            // Expect ")".
            this.Expect(Symbol.RightParanthesis, new Set(rightParanthesisStopSymbols,
                                                         blockStopSymbols,
                                                         stopSymbols));

            // Assemble code.
            int ifBlockEndLabel = assembler.CurrentAddress + 1;
            this.assembler.Do(ifBlockEndLabel);

            // Expect Block.
            this.Block(true, new Set(blockStopSymbols, stopSymbols));

            // "else" follows?
            if (IsCurrentSymbol(Symbol.Else))
            {
                // Define stop symbols.
                Set elseStopSymbols = new Set(Symbol.Begin);

                // Expect "else".
                this.Expect(Symbol.Else, new Set(elseStopSymbols,
                                                 stopSymbols));

                // Assemble code.
                int elseBlockEndLabel = assembler.CurrentAddress + 1;
                this.assembler.Goto(elseBlockEndLabel);

                // Resolve "if-block-ends-here" label.
                this.assembler.ResolveAddress(ifBlockEndLabel);

                // Expect Block.
                this.Block(true, stopSymbols);

                // Resolve "else-block-ends-here" label.
                this.assembler.ResolveAddress(elseBlockEndLabel);
            }
            else
            {
                // Resolve "if-block-ends-here" label.
                this.assembler.ResolveAddress(ifBlockEndLabel);
            }
        }

        /// <summary>
        /// WhileStatement = "while" "(" Expression ")" Block
        /// NOTE: 'Expression' in the above rule has to be of type boolean.
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void WhileStatement(Set stopSymbols)
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
            int whileStatementStartLabel = assembler.CurrentAddress;

            // Expect "while"
            this.Expect(Symbol.While, new Set(whileStopSymbols,
                                              leftParanthesisStopSymbols,
                                              expressionStopSymbols,
                                              rightParanthesisStopSymbols,
                                              stopSymbols));

            // Expect "(".
            this.Expect(Symbol.LeftParanthesis, new Set(leftParanthesisStopSymbols,
                                                        expressionStopSymbols,
                                                        rightParanthesisStopSymbols,
                                                        stopSymbols));

            // Expect Expression.
            Type type = this.Expression(new Set(expressionStopSymbols,
                                                rightParanthesisStopSymbols,
                                                stopSymbols));

            // Make sure the expression would evaluate to a boolean value.
            if (type != Type.Boolean)
            {
                this.annotator.TypeError(type,
                                         TypeErrorCategory.NonBooleanInWhileCondition);
            }

            // Expect ")".
            this.Expect(Symbol.RightParanthesis, new Set(rightParanthesisStopSymbols,
                                                         stopSymbols));

            // Assemble code.
            int whileBlockEndLabel = assembler.CurrentAddress + 1;
            this.assembler.Do(whileBlockEndLabel);

            // Expect Block.
            this.Block(true, stopSymbols);

            // End of the while block, go back and evaluate the while condition.
            this.assembler.Goto(whileStatementStartLabel);

            // Resolve "while-block-ends-here" label.
            this.assembler.ResolveAddress(whileBlockEndLabel);
        }

        /// <summary>
        /// AddingOperator = "+" | "-"
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void AddingOperator(Set stopSymbols)
        {
            switch (this.CurrentSymbol)
            {
                case Symbol.Plus:
                    {
                        this.Expect(Symbol.Plus, stopSymbols);
                        break;
                    }

                case Symbol.Minus:
                    {
                        this.Expect(Symbol.Minus, stopSymbols);
                        break;
                    }

                default:
                    {
                        this.ReportSyntaxErrorAndRecover(stopSymbols);
                        break;
                    }
            }
        }

        /// <summary>
        /// RelationalOperator = "&lt;" | "&lt;=" | "==" | "!=" | "&gt;" | "&gt;=" 
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void RelationalOperator(Set stopSymbols)
        {
            switch (this.CurrentSymbol)
            {
                case Symbol.Less:
                    {
                        this.Expect(Symbol.Less, stopSymbols);
                        break;
                    }

                case Symbol.LessOrEqual:
                    {
                        this.Expect(Symbol.LessOrEqual, stopSymbols);
                        break;
                    }

                case Symbol.Equal:
                    {
                        this.Expect(Symbol.Equal, stopSymbols);
                        break;
                    }

                case Symbol.NotEqual:
                    {
                        this.Expect(Symbol.NotEqual, stopSymbols);
                        break;
                    }

                case Symbol.Greater:
                    {
                        this.Expect(Symbol.Greater, stopSymbols);
                        break;
                    }

                case Symbol.GreaterOrEqual:
                    {
                        this.Expect(Symbol.GreaterOrEqual, stopSymbols);
                        break;
                    }

                default:
                    {
                        this.ReportSyntaxErrorAndRecover(stopSymbols);
                        break;
                    }
            }
        }

        /// <summary>
        /// PrimaryOperator = "&amp;" | "|"
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void PrimaryOperator(Set stopSymbols)
        {
            switch (this.CurrentSymbol)
            {
                case Symbol.And:
                    {
                        this.Expect(Symbol.And, stopSymbols);
                        break;
                    }

                case Symbol.Or:
                    {
                        this.Expect(Symbol.Or, stopSymbols);
                        break;
                    }

                default:
                    {
                        this.ReportSyntaxErrorAndRecover(stopSymbols);
                        break;
                    }
            }
        }

        /// <summary>
        /// MultiplyingOperator = "*" | "/" | "%" | "^" 
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void MultiplyingOperator(Set stopSymbols)
        {
            switch (this.CurrentSymbol)
            {
                case Symbol.Multiply:
                    {
                        this.Expect(Symbol.Multiply, stopSymbols);
                        break;
                    }

                case Symbol.Divide:
                    {
                        this.Expect(Symbol.Divide, stopSymbols);
                        break;
                    }

                case Symbol.Modulo:
                    {
                        this.Expect(Symbol.Modulo, stopSymbols);
                        break;
                    }

                case Symbol.Power:
                    {
                        this.Expect(Symbol.Power, stopSymbols);
                        break;
                    }

                default:
                    {
                        this.ReportSyntaxErrorAndRecover(stopSymbols);
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
        Type ProcedureInvocation(bool isParallel, Set stopSymbols)
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
            Set leftParanthesisStopSymbols = new Set(commaStopSymbols,
                                                     Symbol.RightParanthesis);
            Set procedureNameStopSymbols = new Set(Symbol.LeftParanthesis);

            // Expect procedure name.
            int name = this.ExpectName(new Set(procedureNameStopSymbols,
                                               leftParanthesisStopSymbols,
                                               referenceStopSymbols,
                                               commaStopSymbols,
                                               stopSymbols));

            // Find the procedure name.
            ObjectRecord objectRecord = new ObjectRecord();
            this.auditor.Find(name, ref objectRecord);

            // Is the statement in a procedure block?
            if (this.procedureNest.Count != 0)
            {
                int procedureName = this.procedureNest.Peek();
                ObjectRecord procedureRecord = new ObjectRecord();
                this.auditor.Find(procedureName, ref procedureRecord);
                if (!(procedureRecord.ProcedureRecord.CallsParallelUnfriendly))
                {
                    procedureRecord.ProcedureRecord.CallsParallelUnfriendly =
                      ((objectRecord.ProcedureRecord.HighestScopeUsed !=
                                    ProcedureRecord.NoScope) &&
                       (objectRecord.ProcedureRecord.HighestScopeUsed <=
                                    procedureRecord.MetaData.Level)) ||
                              objectRecord.ProcedureRecord.UsesIO ||
                              objectRecord.ProcedureRecord.CallsParallelUnfriendly;
                }

                if (isParallel && (procedureRecord.Name == objectRecord.Name))
                {
                    if ((procedureRecord.ProcedureRecord.CallsParallelUnfriendly) ||
                        (procedureRecord.ProcedureRecord.UsesIO) ||
                        ((objectRecord.ProcedureRecord.HighestScopeUsed !=
                                                  ProcedureRecord.NoScope) &&
                         (objectRecord.ProcedureRecord.HighestScopeUsed <=
                                                  objectRecord.MetaData.Level)))
                    {
                        procedureRecord.ProcedureRecord.ExamineParallelRecursion = false;
                    }
                    else
                    {
                        procedureRecord.ProcedureRecord.ExamineParallelRecursion = true;
                    }
                }
            }

            // Create a list for the argument records.
            List<ParameterRecord> argumentRecordList = new List<ParameterRecord>();

            // Expect "(".
            this.Expect(Symbol.LeftParanthesis, new Set(leftParanthesisStopSymbols,
                                                        referenceStopSymbols,
                                                        stopSymbols));

            if (!(this.IsCurrentSymbol(Symbol.RightParanthesis)))
            {
                ParameterRecord argumentRecord = new ParameterRecord();

                // Define stop symbols.
                Set expressionStopSymbols = new Set(Symbol.Comma,
                                                    Symbol.RightParanthesis);
                Set objectAccessStopSymbols = new Set(Symbol.Comma,
                                                      Symbol.RightParanthesis);

                // "reference" follows?
                if (this.IsCurrentSymbol(Symbol.Reference))
                {
                    // Expect "reference".
                    this.Expect(Symbol.Reference, new Set(referenceStopSymbols,
                                                          objectAccessStopSymbols,
                                                          commaStopSymbols,
                                                          stopSymbols));

                    // Expect ObjectAccess.
                    Metadata metadata = this.ObjectAccess(
                                                      new Set(objectAccessStopSymbols,
                                                              stopSymbols));

                    // Make sure a constant is not passed in as a reference parameter.
                    if (metadata.Kind == Kind.Constant)
                    {
                        this.annotator.KindError(
                              Kind.Constant,
                              KindErrorCategory.ConstantPassedAsReferenceParameter);
                    }

                    argumentRecord.ParameterType = metadata.Type;
                    argumentRecord.ParameterKind = Kind.ReferenceParameter;
                }
                else
                {
                    // Expect Expression.
                    argumentRecord.ParameterType = this.Expression(
                                                          new Set(expressionStopSymbols,
                                                                  stopSymbols));
                    argumentRecord.ParameterKind = Kind.ValueParameter;
                }

                // Add the argument record to the argument record list.
                argumentRecordList.Add(argumentRecord);

                // "," follows?
                while (this.IsCurrentSymbol(Symbol.Comma))
                {
                    // Expect ",".
                    this.Expect(Symbol.Comma, new Set(commaStopSymbols,
                                                      expressionStopSymbols,
                                                      stopSymbols));

                    argumentRecord = new ParameterRecord();

                    // "reference" follows?
                    if (this.IsCurrentSymbol(Symbol.Reference))
                    {
                        // Expect "reference".
                        this.Expect(Symbol.Reference, new Set(referenceStopSymbols,
                                                              objectAccessStopSymbols,
                                                              commaStopSymbols,
                                                              stopSymbols));

                        // Expect ObjectAccess.
                        Metadata metadata = this.ObjectAccess(
                                                          new Set(objectAccessStopSymbols,
                                                                  stopSymbols));

                        // Make sure a constant is not passed in as a reference parameter.
                        if (metadata.Kind == Kind.Constant)
                        {
                            this.annotator.KindError(
                                Kind.Constant,
                                KindErrorCategory.ConstantPassedAsReferenceParameter);
                        }

                        argumentRecord.ParameterType = metadata.Type;
                        argumentRecord.ParameterKind = Kind.ReferenceParameter;
                    }
                    else
                    {
                        // Expect Expression.
                        argumentRecord.ParameterType = this.Expression(
                                                            new Set(expressionStopSymbols,
                                                                    stopSymbols));
                        argumentRecord.ParameterKind = Kind.ValueParameter;
                    }

                    // Add the argument record to the argument record list.
                    argumentRecordList.Add(argumentRecord);
                }
            }

            // Expect ")".
            this.Expect(Symbol.RightParanthesis, stopSymbols);

            // Check if the procedure invocation statement matches its signature.
            List<ParameterRecord> parameterRecordList =
                                                    objectRecord.ParameterRecordList;
            int totalParameters = parameterRecordList.Count;
            if (argumentRecordList.Count != totalParameters)
            {
                this.annotator.KindError(Kind.Procedure,
                                         KindErrorCategory.ArgumentCountMismatch);
            }
            else
            {
                for (int count = 0; count < totalParameters; ++count)
                {
                    if (parameterRecordList[count].ParameterKind !=
                        argumentRecordList[count].ParameterKind)
                    {
                        this.annotator.KindError(argumentRecordList[count].ParameterKind,
                                                 KindErrorCategory.ParameterKindMismatch);
                    }

                    if (parameterRecordList[count].ParameterType !=
                        argumentRecordList[count].ParameterType)
                    {
                        this.annotator.TypeError(argumentRecordList[count].ParameterType,
                                                 TypeErrorCategory.ParameterTypeMismatch);
                    }
                }
            }

            // Assemble code.
            if (isParallel)
            {
                this.assembler.Parallel();
            }

            this.assembler.ProcedureInvocation(
                              this.auditor.BlockLevel - objectRecord.MetaData.Level,
                              objectRecord.MetaData.ProcedureLabel);

            return (objectRecord.MetaData.Type);
        }

        /// <summary>
        /// ExpressionList = Expression { "," Expression } 
        /// </summary>
        /// <param name="operation">Category of operation.</param>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        void ExpressionList(OperationCategory operation, Set stopSymbols)
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
            Type type = this.Expression(new Set(expressionStopSymbols, stopSymbols));

            if (operation == OperationCategory.Write)
            {
                // Assemble code.
                if (type == Type.Boolean)
                {
                    this.assembler.WriteBoolean();
                }
                else if (type == Type.Integer)
                {
                    this.assembler.WriteInteger();
                }
                else
                {
                    this.annotator.TypeError(
                                      type,
                                      TypeErrorCategory.InvalidTypeInWriteStatement);
                }
            }

            // "," follows?
            while (this.IsCurrentSymbol(Symbol.Comma))
            {
                // Expect ",".
                this.Expect(Symbol.Comma, new Set(commaStopSymbols,
                                                  expressionStopSymbols,
                                                  stopSymbols));

                // Expect Expression.
                type = this.Expression(new Set(expressionStopSymbols, stopSymbols));

                if (operation == OperationCategory.Write)
                {
                    // Assemble code.
                    if (type == Type.Boolean)
                    {
                        this.assembler.WriteBoolean();
                    }
                    else if (type == Type.Integer)
                    {
                        this.assembler.WriteInteger();
                    }
                    else
                    {
                        this.annotator.TypeError(
                                          type,
                                          TypeErrorCategory.InvalidTypeInWriteStatement);
                    }
                }
            }
        }

        /// <summary>
        /// Expression = PrimaryExpression { PrimaryOperator PrimaryExpression } 
        /// </summary>
        /// <param name="stopSymbols">Stop symbols for error recovery.</param>
        /// <returns>The type that the expression would evaluate to.</returns>
        Type Expression(Set stopSymbols)
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
            Type type = this.PrimaryExpression(new Set(primaryExpressionStopSymbols,
                                                       stopSymbols));

            // Primary operator follows?
            if (primaryOperatorSymbols.Contains(this.CurrentSymbol))
            {
                // Make sure the PrimaryExpression evaluates to type Boolean.
                if (type != Type.Boolean)
                {
                    this.annotator.TypeError(
                              type,
                              DiadicTypeErrorCategory.NonBooleanLeftOfLogicalOperator,
                              this.CurrentSymbol);
                }
            }

            while (primaryOperatorSymbols.Contains(this.CurrentSymbol))
            {
                // Preserve the primary operator symbol.
                Symbol lastPrimaryOperator = this.CurrentSymbol;

                // Expect PrimaryOperator.
                this.PrimaryOperator(new Set(primaryOperatorStopSymbols,
                                             primaryExpressionStopSymbols,
                                             stopSymbols));

                // Expect PrimaryExpression.
                type = this.PrimaryExpression(new Set(primaryExpressionStopSymbols,
                                                      stopSymbols));

                // Make sure the PrimaryExpression evaluates to type Boolean.
                if (type != Type.Boolean)
                {
                    this.annotator.TypeError(
                              type,
                              DiadicTypeErrorCategory.NonBooleanRightOfLogicalOperator,
                              this.CurrentSymbol);
                }

                // Assemble code.
                this.Operation(lastPrimaryOperator);
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
        Type PrimaryExpression(Set stopSymbols)
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
            Type type = this.SimpleExpression(new Set(simpleExpressionStopSymbols,
                                                      relationalOperatorStopSymbols,
                                                      stopSymbols));

            // Relational operator follows?
            if (relationalOperatorSymbols.Contains(this.CurrentSymbol))
            {
                // Preserve the relational operator symbol.
                Symbol lastRelationalOperator = this.CurrentSymbol;

                // Equality operation?
                if ((this.CurrentSymbol == Symbol.Equal) ||
                    (this.CurrentSymbol == Symbol.NotEqual))
                {
                    // Expect RelationalOperator.
                    this.RelationalOperator(new Set(relationalOperatorStopSymbols,
                                                    stopSymbols));

                    // Expect SimpleExpression.
                    Type typeOfRightHandElement = this.SimpleExpression(stopSymbols);

                    // Types on either side of an equality operator should match.
                    if ((type != typeOfRightHandElement) &&
                        (typeOfRightHandElement != Type.Universal))
                    {
                        this.annotator.TypeError(
                                type,
                                DiadicTypeErrorCategory.TypeMismatchAcrossEqualityOperator,
                                lastRelationalOperator);
                    }

                    // Types across equality operator should not be void.
                    if (type == Type.Void)
                    {
                        this.annotator.TypeError(
                                type,
                                DiadicTypeErrorCategory.InvalidTypeAcrossEqualityOperator,
                                lastRelationalOperator);
                    }
                }
                else  // Comparison operation
                {
                    // Expect RelationalOperator.
                    this.RelationalOperator(new Set(relationalOperatorStopSymbols,
                                                    stopSymbols));

                    // Expect SimpleExpression.
                    Type typeOfRightHandElement = this.SimpleExpression(stopSymbols);

                    // Types on either side of a comparison operator should be integers.
                    if (type != Type.Integer)
                    {
                        this.annotator.TypeError(
                              type,
                              DiadicTypeErrorCategory.NonIntegerLeftOfRelationalOperator,
                              lastRelationalOperator);
                    }
                    else if (typeOfRightHandElement != Type.Integer)
                    {
                        this.annotator.TypeError(
                            typeOfRightHandElement,
                            DiadicTypeErrorCategory.NonIntegerRightOfRelationalOperator,
                            lastRelationalOperator);
                    }
                }

                // Assemble code.
                this.Operation(lastRelationalOperator);

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
        Type SimpleExpression(Set stopSymbols)
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
                this.Expect(Symbol.Minus, new Set(minusStopSymbols,
                                                  termStopSymbols,
                                                  stopSymbols));
                minusPrecedes = true;
            }

            // Expect Term.
            Type type = this.Term(new Set(termStopSymbols, stopSymbols));

            if (minusPrecedes)
            {
                // "-" should be followed by an integer Term.
                if (type != Type.Integer)
                {
                    this.annotator.TypeError(
                              Type.Integer,
                              DiadicTypeErrorCategory.NonIntegerRightOfAdditionOperator,
                              Symbol.Minus);
                }

                // Assemble code.
                this.assembler.Minus();
            }

            // Adding operator follows?
            Symbol lastAddingOperator = Symbol.Unknown;
            bool addingOperation = false;
            if (addingOperatorSymbols.Contains(this.CurrentSymbol))
            {
                addingOperation = true;
            }

            while (addingOperatorSymbols.Contains(this.CurrentSymbol))
            {
                // Preserve the last adding operator.
                lastAddingOperator = this.CurrentSymbol;

                // Left hand side of adding operator has to be of type Integer.
                if (type != Type.Integer)
                {
                    this.annotator.TypeError(
                              type,
                              DiadicTypeErrorCategory.NonIntegerLeftOfAdditionOperator,
                              lastAddingOperator);
                }

                // Expect AddingOperator.
                this.AddingOperator(new Set(addingOperatorStopSymbols,
                                            termStopSymbols,
                                            stopSymbols));

                // Expect Term.
                type = this.Term(new Set(termStopSymbols, stopSymbols));

                // Assemble code.
                this.Operation(lastAddingOperator);
            }

            if (addingOperation)
            {
                // Right hand side of adding operator has to be of type Integer.
                if (type != Type.Integer)
                {
                    this.annotator.TypeError(
                            type,
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
        Type Term(Set stopSymbols)
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
            Type type = this.Factor(new Set(factorStopSymbols, stopSymbols));

            // MultiplyingOperator follows?
            Symbol lastMultiplyingOperator = Symbol.Unknown;
            bool multiplyingOperation = false;
            if (multiplyingOperatorSymbols.Contains(this.CurrentSymbol))
            {
                multiplyingOperation = true;
            }

            while (multiplyingOperatorSymbols.Contains(this.CurrentSymbol))
            {
                // Preserve the last multiplying operator.
                lastMultiplyingOperator = this.CurrentSymbol;

                // Left hand side of multiplying operator has to be of type Integer.
                if (type != Type.Integer)
                {
                    this.annotator.TypeError(
                        type,
                        DiadicTypeErrorCategory.NonIntegerLeftOfMultiplicationOperator,
                        lastMultiplyingOperator);
                }

                // Expect MultiplyingOperator.
                this.MultiplyingOperator(new Set(multiplyingOperatorStopSymbols,
                                                 factorStopSymbols,
                                                 stopSymbols));

                // Expect Factor.
                type = this.Factor(new Set(factorStopSymbols, stopSymbols));

                // Assemble code.
                this.Operation(lastMultiplyingOperator);
            }

            if (multiplyingOperation)
            {
                // Right hand side of multiplying operator has to be of type Integer.
                if (type != Type.Integer)
                {
                    this.annotator.TypeError(type,
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
        Type Factor(Set stopSymbols)
        {
            Type type = Type.Universal;

            switch (this.CurrentSymbol)
            {
                case Symbol.Numeral:
                case Symbol.True:
                case Symbol.False:
                    {
                        Metadata metadata = new Metadata();
                        this.Constant(ref metadata, stopSymbols); // Expect Constant.
                        type = metadata.Type;
                        this.assembler.Constant(metadata.Value);  // Assemble code.
                        break;
                    }

                case Symbol.Name:
                    {
                        ObjectRecord objectRecord = new ObjectRecord();
                        this.auditor.Find(this.Argument, ref objectRecord);

                        switch (objectRecord.MetaData.Kind)
                        {
                            case Kind.Constant:
                                {
                                    Metadata metaData = new Metadata();
                                    this.Constant(ref metaData, stopSymbols); // Expect Constant.
                                    type = metaData.Type;
                                    this.assembler.Constant(metaData.Value);  // Assemble code.
                                    break;
                                }

                            case Kind.Variable:
                            case Kind.ValueParameter:
                            case Kind.ReferenceParameter:
                            case Kind.ReturnParameter:
                            case Kind.Array:
                                {
                                    Metadata metadata = this.ObjectAccess(stopSymbols);
                                    type = metadata.Type;
                                    this.assembler.Value();                   // Assemble code.
                                    break;
                                }

                            case Kind.Procedure:
                                {
                                    type = this.ProcedureInvocation(false, stopSymbols);
                                    break;
                                }

                            default:
                                {
                                    this.Expect(Symbol.Name, stopSymbols);
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
                        this.Expect(Symbol.LeftParanthesis,
                                                  new Set(leftParanthesisStopSymbols,
                                                          expressionStopSymbols,
                                                          stopSymbols));

                        // Expect Expression.
                        type = this.Expression(new Set(expressionStopSymbols,
                                                       stopSymbols));

                        // Expect ")".
                        this.Expect(Symbol.RightParanthesis, stopSymbols);
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
                        this.Expect(Symbol.Not, new Set(notStopSymbols,
                                                        stopSymbols));

                        // Expect Factor.
                        type = this.Factor(stopSymbols);

                        // Make sure the expression is of type Boolean.
                        if (type != Type.Boolean)
                        {
                            this.annotator.TypeError(
                                        type,
                                        TypeErrorCategory.NonBooleanToTheRightOfNotOperator);
                        }

                        // Assemble code.
                        this.assembler.Not();
                        break;
                    }

                default:
                    {
                        this.ReportSyntaxErrorAndRecover(stopSymbols);
                        type = Type.Universal;
                        break;
                    }

            }

            return type;
        }
    }
}