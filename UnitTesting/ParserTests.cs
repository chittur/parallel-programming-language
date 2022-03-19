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

using Compilation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace UnitTesting
{
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
            const string code = @"
            {
                ;                       $ Error: GenericSyntaxError = 100
                integer i;
                boolean i;              $ Error: AmbiguousName = 202
                constant c = i;         $ Error: NonConstantInConstantDefinition = 301
                integer[0] array;       $ Error: NonPositiveIntegerIndexInArrayDeclaration = 304
                j = i;                  $ Error: UndefinedName = 201
                c = 10;                 $ Error: AssignmentModifiesConstant = 302
                i = array;              $ Error: ArrayVariableMissingIndexedSelector = 303
            }
            ";

            // Expected compilation errors for the code above.
            int[] expectedErrors = new int[]
                {
                    100, // GenericSyntaxError
                    202, // AmbiguousName
                    301, // NonConstantInConstantDefinition
                    304, // NonPositiveIntegerIndexInArrayDeclaration
                    201, // UndefinedName
                    302, // AssignmentModifiesConstant
                    303, // ArrayVariableMissingIndexedSelector
                };

            // Validate.
            this.ValidateErrors(code, expectedErrors);
        }

        /// <summary>
        /// Tests various kind errors related to procedures.
        /// </summary>
        [TestMethod]
        public void TestProcedureKindErrors()
        {
            // Code which should result in compilation errors.
            const string code = @"
            {
                @ Test(reference integer i, boolean j) { }
                constant max = 1;
                integer i;

                read Test;                  $ Error: ProcedureAccessedAsObject = 305
                                            $        InvalidTypeInReadStatement = 410
                Test(reference i);          $ Error: ArgumentCountMismatch = 306
                Test(reference i, true, i); $ Error: ArgumentCountMismatch = 306
                Test(3, true);              $ Error: ParameterKindMismatch = 307
                Test(reference max, true);  $ Error: ConstantPassedAsReferenceParameter = 308
            }
            ";

            // Expected compilation errors for the code above.
            int[] expectedErrors = new int[]
                {
                    305, // ProcedureAccessedAsObject
                    410, // InvalidTypeInReadStatement
                    306, // ArgumentCountMismatch
                    306, // ArgumentCountMismatch
                    307, // ParameterKindMismatch
                    308, // ConstantPassedAsReferenceParameter
                };

            // Validate.
            this.ValidateErrors(code, expectedErrors);
        }

        /// <summary>
        /// Tests various kind errors related to constants.
        /// </summary>
        [TestMethod]
        public void TestConstantKindErrors()
        {
            // Code which should result in compilation errors.
            const string code = @"
            {
                constant max = 10;
                read max;               $ Error: ReadModifiesConstant = 310
                randomize max;          $ Error: RandomizeModifiesConstant = 311
            }
            ";

            // Expected compilation errors for the code above.
            int[] expectedErrors = new int[]
                {
                    310, // ReadModifiesConstant
                    311, // RandomizeModifiesConstant 
                };

            // Validate.
            this.ValidateErrors(code, expectedErrors);
        }

        /// <summary>
        /// Tests various kind errors related to parallelism.
        /// </summary>
        [TestMethod]
        public void TestParallelKindErrors()
        {
            // Code which should result in compilation errors.
            const string code = @"
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
            int[] expectedErrors = new int[]
                {
                    314, // ParallelProcedureHasNonVoidReturn
                    315, // ParallelProcedureHasReferenceParameter
                    316, // ParallelProcedureHasNoChannelParameter
                    317, // ParallelProcedureUsesIO
                    318, // ParallelProcedureUsesNonLocals
                    319, // ParallelProcedureCallsUnfriendly
                    312, // ReceiveModifiesConstant
                    313, // NonProcedureInParallelStatement
                };

            // Validate.
            this.ValidateErrors(code, expectedErrors);
        }

        /// <summary>
        /// Tests various type errors.
        /// </summary>
        [TestMethod]
        public void TestTypeErrors()
        {
            // Code which should result in compilation errors.
            const string code = @"
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
                read c;             $ Error: InvalidTypeInReadStatement = 410
                randomize b;        $ Error: NonIntegerInRandomizeStatement = 411
                send b -> c;        $ Error: NonIntegerValueInSendStatement = 412
                send 0 -> z;        $ Error: NonChannelInSendStatement = 413
                receive b -> c;     $ Error: NonIntegerValueInReceiveStatement = 414
                receive i -> z;     $ Error: NonChannelInReceiveStatement = 415
                open b;             $ Error: NonChannelInOpenStatement = 416
            }
            ";

            // Expected compilation errors for the code above.
            int[] expectedErrors = new int[]
                {
                    407,  // MinusPrecedingNonIntegerInConstantDefinition
                    401,  // NonIntegerIndexInArrayDeclaration
                    402,  // NonIntegerArrayIndex
                    403,  // TypeMismatchInAssignment
                    404,  // NonBooleanInIfCondition
                    405,  // NonBooleanInWhileCondition
                    406,  // NonBooleanToTheRightOfNotOperator
                    408,  // ParameterTypeMismatch
                    409,  // InvalidTypeInWriteStatement
                    410,  // InvalidTypeInReadStatement
                    411,  // NonIntegerInRandomizeStatement
                    412,  // NonIntegerValueInSendStatement
                    413,  // NonChannelInSendStatement
                    414,  // NonIntegerValueInReceiveStatement
                    415,  // NonChannelInReceiveStatement
                    416,  // NonChannelInOpenStatement
                };

            // Validate.
            this.ValidateErrors(code, expectedErrors);
        }

        /// <summary>
        /// Tests various type errors for diadic operators.
        /// </summary>
        [TestMethod]
        public void TestDiadicOperatorTypeErrors()
        {
            // Code which should result in compilation errors.
            const string code = @"
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
                write b * i;        $ Error: NonIntegerLeftOfMultiplicationOperator = 458
                write i ^ b;        $ Error: NonIntegerRightOfMultiplicationOperator = 459
                write Test() == b;  $ Error: TypeMismatchAcrossEqualityOperator = 451
                                    $ Error: InvalidTypeAcrossEqualityOperator = 460
            }
            ";

            // Expected compilation errors for the code above.
            int[] expectedErrors = new int[]
                {
                    451,  // TypeMismatchAcrossEqualityOperator
                    452,  // NonBooleanLeftOfLogicalOperator
                    453,  // NonBooleanRightOfLogicalOperator
                    454,  // NonIntegerLeftOfRelationalOperator
                    455,  // NonIntegerRightOfRelationalOperator
                    456,  // NonIntegerLeftOfAdditionOperator
                    457,  // NonIntegerRightOfAdditionOperator
                    458,  // NonIntegerLeftOfMultiplicationOperator
                    459,  // NonIntegerRightOfMultiplicationOperator
                    451,  // TypeMismatchAcrossEqualityOperator
                    460,  // InvalidTypeAcrossEqualityOperator
                };

            // Validate.
            this.ValidateErrors(code, expectedErrors);
        }

        /// <summary>
        /// Tests successful compilation.
        /// </summary>
        [TestMethod]
        public void TestSuccessfulCompilation()
        {
            // Code which should result in successful compilation.
            const string code = @"
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
            TextReader reader = new StringReader(code);
            bool compiled = parser.Compile(reader, intermediateFilename);

            // Validate that the compilation succeeded.
            Assert.IsTrue(compiled);

            // Validate that the intermediate code file is not empty.
            string contents = File.ReadAllText(intermediateFilename);
            Assert.IsFalse(string.IsNullOrEmpty(contents));

            // Cleanup by deleting the intermediate code file.
            File.Delete(intermediateFilename);
        }

        // Validates that the errors produced by the parser are as expected.
        void ValidateErrors(string code, int[] expectedErrors)
        {
            // The list of actual parsing errors.
            List<int> actualErrors = new List<int>();

            // Feed the code to the parser.
            Parser parser = new Parser();
            TextReader reader = new StringReader(code);
            bool compiled = parser.Compile(
                reader,
                "Intermediate.sachin", // Intermediate code file.
                                       // But should not be written to as compilation should fail.
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
}
