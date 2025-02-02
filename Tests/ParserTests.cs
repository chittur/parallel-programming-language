/******************************************************************************
 * Filename    = ParserTests.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = UnitTesting
 *
 * Description = Unit tests for the Parser class.
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Compilation;
using ErrorReporting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests;

/// <summary>
/// Unit tests for the Parser class.
/// </summary>
[TestClass]
public class ParserTests
{
    /// <summary>
    /// Tests scope errors and basic kind errors in compilation.
    /// </summary>
    [TestMethod]
    public void TestScopeAndBasicKindErrors()
    {
        // Code which should result in compilation errors.
        const string Code = @"
            {
                ;                       $ Error: GenericSyntaxError = 100
                integer i;
                boolean i;              $ Error: AmbiguousName = 202
                constant c = i;         $ Error: NonConstantInConstantDefinition = 301
                integer[0] array;       $ Error: NonPositiveIntegerIndexInArrayDeclaration = 304
                j = i;                  $ Error: UndefinedName = 201
                c = 10;                 $ Error: AssignmentModifiesConstant = 302   
                i, c = 5, 10;           $ Error: AssignmentModifiesConstant = 302
                i = array;              $ Error: ArrayVariableMissingIndexedSelector = 303
            }
            ";

        // Expected compilation errors for the code above.
        int[] expectedErrors =
            [
                (int)GenericErrorCategory.GenericSyntaxError,
                (int)ScopeErrorCategory.AmbiguousName,
                (int)KindErrorCategory.NonConstantInConstantDefinition,
                (int)KindErrorCategory.NonPositiveIntegerIndexInArrayDeclaration,
                (int)ScopeErrorCategory.UndefinedName,
                (int)KindErrorCategory.AssignmentModifiesConstant,
                (int)KindErrorCategory.AssignmentModifiesConstant,
                (int)KindErrorCategory.ArrayVariableMissingIndexedSelector,
            ];

        // Validate.
        ValidateErrors(Code, expectedErrors);
    }

    /// <summary>
    /// Tests various kind errors related to procedures.
    /// </summary>
    [TestMethod]
    public void TestProcedureKindErrors()
    {
        // Code which should result in compilation errors.
        const string Code = @"
            {
                @ Test(reference integer i, boolean j) { }
                @ Test2(integer i, reference boolean j) { }
                constant max = 1;
                constant signal = true;
                integer i;
                boolean b;

                read Test;                  $ Error: ProcedureAccessedAsObject = 305
                                            $ Error: InvalidTypeInReadStatement = 410
                Test(reference i);          $ Error: ArgumentCountMismatch = 306
                Test(reference i, true, i); $ Error: ArgumentCountMismatch = 306
                Test(3, true);              $ Error: ParameterKindMismatch = 307
                Test(reference max, true);  $ Error: ConstantPassedAsReferenceParameter = 308
                Test2(i, reference signal); $ Error: ConstantPassedAsReferenceParameter = 308
                i = 3, 4 * 5;               $ Error: AssignmentCountMismatch = 309
                b, i = true;                $ Error: AssignmentCountMismatch = 309
            }
            ";

        // Expected compilation errors for the code above.
        int[] expectedErrors =
            [
                (int)KindErrorCategory.ProcedureAccessedAsObject,
                (int)TypeErrorCategory.InvalidTypeInReadStatement,
                (int)KindErrorCategory.ArgumentCountMismatch,
                (int)KindErrorCategory.ArgumentCountMismatch,
                (int)KindErrorCategory.ParameterKindMismatch,
                (int)KindErrorCategory.ConstantPassedAsReferenceParameter,
                (int)KindErrorCategory.ConstantPassedAsReferenceParameter,
                (int)KindErrorCategory.AssignmentCountMismatch,
                (int)KindErrorCategory.AssignmentCountMismatch,
            ];

        // Validate.
        ValidateErrors(Code, expectedErrors);
    }

    /// <summary>
    /// Tests various kind errors related to constants.
    /// </summary>
    [TestMethod]
    public void TestConstantKindErrors()
    {
        // Code which should result in compilation errors.
        const string Code = @"
            {
                constant max = 10;
                integer i;
                boolean b;
                read max;               $ Error: ReadModifiesConstant = 310
                read b, max;            $ Error: ReadModifiesConstant = 310
                randomize max;          $ Error: RandomizeModifiesConstant = 311
                randomize i, max;       $ Error: RandomizeModifiesConstant = 311
            }
            ";

        // Expected compilation errors for the code above.
        int[] expectedErrors =
            [
                (int)KindErrorCategory.ReadModifiesConstant,
                (int)KindErrorCategory.ReadModifiesConstant,
                (int)KindErrorCategory.RandomizeModifiesConstant,
                (int)KindErrorCategory.RandomizeModifiesConstant,
            ];

        // Validate.
        ValidateErrors(Code, expectedErrors);
    }

    /// <summary>
    /// Tests various kind errors related to parallelism.
    /// </summary>
    [TestMethod]
    public void TestParallelKindErrors()
    {
        // Code which should result in compilation errors.
        const string Code = @"
            {
                integer i;
                constant max = 1;
                channel c;

                @ Level3()
                {
                    i = 0;  $ Uses non-local. So cannot be called in parallel.
                }

                @ Level2()
                {
                    Level3();
                }

                @ Level1()
                {
                    Level2();
                }

                @ [integer result] Test(reference integer a, boolean b)
                {                   $ Error: ParallelProcedureHasNonVoidReturn = 314
                                    $ Error: ParallelProcedureHasReferenceParameter = 315
                                    $ Error: ParallelProcedureHasNoChannelParameter = 316
                    read a;         $ Error: ParallelProcedureUsesIO = 317
                    i = 5;          $ Error: ParallelProcedureUsesNonLocals = 318
                    Level1();       $ Error: ParallelProcedureCallsUnfriendly = 319
                }

                parallel Test(reference i, true);
                receive max -> c;   $ Error: ReceiveModifiesConstant = 312
                parallel i();       $ Error: NonProcedureInParallelStatement = 313
            }
            ";

        // Expected compilation errors for the code above.
        int[] expectedErrors =
            [
                (int)KindErrorCategory.ParallelProcedureHasNonVoidReturn,
                (int)KindErrorCategory.ParallelProcedureHasReferenceParameter,
                (int)KindErrorCategory.ParallelProcedureHasNoChannelParameter,
                (int)KindErrorCategory.ParallelProcedureUsesIO,
                (int)KindErrorCategory.ParallelProcedureUsesNonLocals,
                (int)KindErrorCategory.ParallelProcedureCallsUnfriendly,
                (int)KindErrorCategory.ReceiveModifiesConstant,
                (int)KindErrorCategory.NonProcedureInParallelStatement,
            ];

        // Validate.
        ValidateErrors(Code, expectedErrors);
    }

    /// <summary>
    /// Tests various type errors.
    /// </summary>
    [TestMethod]
    public void TestTypeErrors()
    {
        // Code which should result in compilation errors.
        const string Code = @"
            {
                constant z = true;
                constant z2 = -z;   $ Error: MinusPrecedingNonIntegerInConstantDefinition = 407
                integer[z] array;   $ Error: NonIntegerIndexInArrayDeclaration = 401
                integer i;
                channel c;
                boolean b;
                @ Test(integer x) { }

                i = array[z];       $ Error: NonIntegerArrayIndex = 402
                i = i > 0;          $ Error: TypeMismatchInAssignment = 403
                if (i) { }          $ Error: NonBooleanInIfCondition = 404
                while (i) { }       $ Error: NonBooleanInWhileCondition = 405
                i = !i;             $ Error: NonBooleanToTheRightOfNotOperator = 406
                Test(z);            $ Error: ParameterTypeMismatch = 408
                write c;            $ Error: InvalidTypeInWriteStatement = 409
                write 5, c;         $ Error: InvalidTypeInWriteStatement = 409
                read c;             $ Error: InvalidTypeInReadStatement = 410
                read i, c;          $ Error: InvalidTypeInReadStatement = 410
                randomize b;        $ Error: NonIntegerInRandomizeStatement = 411
                randomize i, b;     $ Error: NonIntegerInRandomizeStatement = 411
                send b -> c;        $ Error: NonIntegerValueInSendStatement = 412
                send 0 -> z;        $ Error: NonChannelInSendStatement = 413
                receive b -> c;     $ Error: NonIntegerValueInReceiveStatement = 414
                receive i -> z;     $ Error: NonChannelInReceiveStatement = 415
                open b;             $ Error: NonChannelInOpenStatement = 416
                open c, b;          $ Error: NonChannelInOpenStatement = 416
            }
            ";

        // Expected compilation errors for the code above.
        int[] expectedErrors =
            [
                (int)TypeErrorCategory.MinusPrecedingNonIntegerInConstantDefinition,
                (int)TypeErrorCategory.NonIntegerIndexInArrayDeclaration,
                (int)TypeErrorCategory.NonIntegerArrayIndex,
                (int)TypeErrorCategory.TypeMismatchInAssignment,
                (int)TypeErrorCategory.NonBooleanInIfCondition,
                (int)TypeErrorCategory.NonBooleanInWhileCondition,
                (int)TypeErrorCategory.NonBooleanToTheRightOfNotOperator,
                (int)TypeErrorCategory.ParameterTypeMismatch,
                (int)TypeErrorCategory.InvalidTypeInWriteStatement,
                (int)TypeErrorCategory.InvalidTypeInWriteStatement,
                (int)TypeErrorCategory.InvalidTypeInReadStatement,
                (int)TypeErrorCategory.InvalidTypeInReadStatement,
                (int)TypeErrorCategory.NonIntegerInRandomizeStatement,
                (int)TypeErrorCategory.NonIntegerInRandomizeStatement,
                (int)TypeErrorCategory.NonIntegerValueInSendStatement,
                (int)TypeErrorCategory.NonChannelInSendStatement,
                (int)TypeErrorCategory.NonIntegerValueInReceiveStatement,
                (int)TypeErrorCategory.NonChannelInReceiveStatement,
                (int)TypeErrorCategory.NonChannelInOpenStatement,
                (int)TypeErrorCategory.NonChannelInOpenStatement,
            ];

        // Validate.
        ValidateErrors(Code, expectedErrors);
    }

    /// <summary>
    /// Tests various type errors for diadic operators.
    /// </summary>
    [TestMethod]
    public void TestDiadicOperatorTypeErrors()
    {
        // Code which should result in compilation errors.
        const string Code = @"
            {
                @ Test() { }
                integer i;
                boolean b;
                channel c;

                write b == i;       $ Error: TypeMismatchAcrossEqualityOperator = 451
                write i | b;        $ Error: NonBooleanLeftOfLogicalOperator = 452
                write b & i;        $ Error: NonBooleanRightOfLogicalOperator = 453
                write b < (2 + 3);  $ Error: NonIntegerLeftOfRelationalOperator = 454
                write i >= b;       $ Error: NonIntegerRightOfRelationalOperator = 455
                write b + i;        $ Error: NonIntegerLeftOfAdditionOperator = 456
                write i + (i <= 0); $ Error: NonIntegerRightOfAdditionOperator = 457
                write - (i < 0);    $ Error: NonIntegerRightOfAdditionOperator = 457
                write b * i;        $ Error: NonIntegerLeftOfMultiplicationOperator = 458
                write i ^ b;        $ Error: NonIntegerRightOfMultiplicationOperator = 459
                write Test() == b;  $ Error: TypeMismatchAcrossEqualityOperator = 451
                                    $ Error: InvalidTypeAcrossEqualityOperator = 460
            }
            ";

        // Expected compilation errors for the code above.
        int[] expectedErrors =
            [
                (int)DiadicTypeErrorCategory.TypeMismatchAcrossEqualityOperator,
                (int)DiadicTypeErrorCategory.NonBooleanLeftOfLogicalOperator,
                (int)DiadicTypeErrorCategory.NonBooleanRightOfLogicalOperator,
                (int)DiadicTypeErrorCategory.NonIntegerLeftOfRelationalOperator,
                (int)DiadicTypeErrorCategory.NonIntegerRightOfRelationalOperator,
                (int)DiadicTypeErrorCategory.NonIntegerLeftOfAdditionOperator,
                (int)DiadicTypeErrorCategory.NonIntegerRightOfAdditionOperator,
                (int)DiadicTypeErrorCategory.NonIntegerRightOfAdditionOperator,
                (int)DiadicTypeErrorCategory.NonIntegerLeftOfMultiplicationOperator,
                (int)DiadicTypeErrorCategory.NonIntegerRightOfMultiplicationOperator,
                (int)DiadicTypeErrorCategory.TypeMismatchAcrossEqualityOperator,
                (int)DiadicTypeErrorCategory.InvalidTypeAcrossEqualityOperator,
            ];

        // Validate.
        ValidateErrors(Code, expectedErrors);
    }

    /// <summary>
    /// Tests successful compilation.
    /// </summary>
    [TestMethod]
    public void TestSuccessfulCompilation()
    {
        // Code which should result in successful compilation.
        const string Code = @"
            {
              @ [boolean result] IsPrime(integer number)
              {
                if (number <= 0)
                {
                  result = false;
                }
                else 
                {
                  if (number <= 2)
                  {
                    result = true;
                  }
                  else
                  {
                    integer count;
    
                    result = true;
                    count = 2;
                    while ((count <= number / 2) & result)
                    {            
                      result = ((number % count) != 0);
                      count = count + 1;
                    }
                  }
                }
              }
 
              integer number;
  
              read number;
              write IsPrime(number);
            }
            ";

        string intermediateFilename = new Random().Next().ToString() + ".sachin";

        // Feed the code to the parser.
        Parser parser = new Parser();
        TextReader reader = new StringReader(Code);
        bool compiled = parser.Compile(reader, intermediateFilename);

        // Validate that the compilation succeeded.
        Assert.IsTrue(compiled);

        // Validate that the intermediate code file is not empty.
        string contents = File.ReadAllText(intermediateFilename);
        Assert.IsFalse(string.IsNullOrEmpty(contents));

        // Cleanup by deleting the intermediate code file.
        File.Delete(intermediateFilename);
    }

    /// <summary>
    /// Validates that the errors produced by the parser are as expected.
    /// </summary>
    /// <param name="code">The program being parsed</param>
    /// <param name="expectedErrors">Expected parsing errors</param>
    private static void ValidateErrors(string code, int[] expectedErrors)
    {
        // The list of actual parsing errors.
        List<int> actualErrors = [];

        // Feed the code to the parser.
        Parser parser = new Parser();
        TextReader reader = new StringReader(code);
        bool compiled = parser.Compile(
            reader,
            "Intermediate.sachin",
            delegate (string errorCategory, int errorCode, string errorMessage)
            {
                actualErrors.Add(errorCode);
                Trace.WriteLine($"Parser: {errorCategory} error {errorCode}, {errorMessage}");
            });

        // Validate that the compilation failed.
        Assert.IsFalse(compiled);

        // Validate that the compilation error codes appear as expected.
        int length = actualErrors.Count;
        Assert.AreEqual(expectedErrors.Length, length);
        if (expectedErrors.Length == length)
        {
            for (int i = 0; i < length; ++i)
            {
                Assert.AreEqual(actualErrors[i], expectedErrors[i]);
            }
        }
    }
}
