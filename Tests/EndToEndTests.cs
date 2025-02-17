/******************************************************************************
 * Filename    = EndToEndTests.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = UnitTesting
 *
 * Description = End-to-end tests for this project.
 *****************************************************************************/

using System;
using System.IO;
using System.Text;
using Compilation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Runtime;

namespace Tests;

/// <summary>
/// End-to-end tests for this project.
/// </summary>
[TestClass]
public class EndToEndTests
{
    /// <summary>
    /// Tests 'array out of bounds' runtime error for overflow.
    /// </summary>
    [TestMethod]
    public void TestOverflowArrayOutOfBoundsRuntimeError()
    {
        const string Code = @"
            $ - Sample program that tests 'array out of bounds' runtime error.

            {
              integer[5] numbers;
              numbers[6] = 1;
            }
            ";

        // Validate that the program produces the expected runtime error.
        Validate(Code, string.Empty, $"{Translator.ArrayIndexOutOfBoundsMessage}{Environment.NewLine}");
    }

    /// <summary>
    /// Tests 'array out of bounds' runtime error for underflow.
    /// </summary>
    [TestMethod]
    public void TestUnderflowArrayOutOfBoundsRuntimeError()
    {
        const string Code = @"
            $ - Sample program that tests 'array out of bounds' runtime error.

            {
              integer[5] numbers;
              numbers[0] = 1;
            }
            ";

        // Validate that the program produces the expected runtime error.
        Validate(Code, string.Empty, $"{Translator.ArrayIndexOutOfBoundsMessage}{Environment.NewLine}");
    }

    /// <summary>
    /// Tests 'boolean input incorrect format' runtime error.
    /// </summary>
    [TestMethod]
    public void TestBooleanInputIncorrectFormatRuntimeError()
    {
        const string Code = @"
            $ - Sample program that tests 'boolean input incorrect format' runtime error.

            {
              boolean var;
              read var;
            }
            ";

        // Validate that the program produces the expected runtime error.
        Validate(Code, $"0{Environment.NewLine}", $"{Translator.BooleanInputIncorrectFormatMessage}{Environment.NewLine}");
    }

    /// <summary>
    /// Tests 'send through unopened channel' runtime error.
    /// </summary>
    [TestMethod]
    public void TestSendThroughUnopenedChannelRuntimeError()
    {
        const string Code = @"
            $ - Sample program that tests 'send through unopened channel' runtime error.

            {
              channel c;
              send 1 -> c;
            }
            ";

        // Validate that the program produces the expected runtime error.
        Validate(Code, string.Empty, $"{Translator.SendThroughUnopenedChannelMessage}{Environment.NewLine}");
    }

    /// <summary>
    /// Tests 'receive through unopened channel' runtime error.
    /// </summary>
    [TestMethod]
    public void TestReceiveThroughUnopenedChannelRuntimeError()
    {
        const string Code = @"
            $ - Sample program that tests 'receive through unopened channel' runtime error.

            {
              channel c;
              integer i;
              receive i -> c;
            }
            ";

        // Validate that the program produces the expected runtime error.
        Validate(Code, string.Empty, $"{Translator.ReceiveThroughUnopenedChannelMessage}{Environment.NewLine}");
    }

    /// <summary>
    /// Tests basic end-to-end flow, multiple assignment, and parallel recursion.
    /// </summary>
    [TestMethod]
    public void TestBasicAndParallelEndToEnd()
    {
        // Sample program that exercises various language features.
        const string Code = @"
            $ - Program to sort an array of numbers using the Selection Sort algorithm.
            $ - Then feed the number to a parallel recursive function to find the sum
            $   of the squares of its digits.
            $ - Demonstrates the usage of arrays, parallel assignment, parallel recursion.

            {
              @ Sort(boolean descending, reference integer a, reference integer b)
              {
                boolean condition;
    
                if (descending)
                {
                  condition = a < b;
                }
                else
                {
                  condition = a > b;
                }

                if (condition)
                {
                  a, b = b, a; $ Swap a with b.
                }
              }

              @ Node(integer number, channel bottom)
              {
                if (number >= 10)
                {
                  integer digit, result;
                  channel top;
            
                  open top;
                  digit = number % 10;
                  parallel Node(number / 10, top);
                  result = digit * digit;
                  receive number -> top;
                  send (number + result) -> bottom;
                }
                else
                {
                  send (number * number) -> bottom;
                }
              }

              constant max = 5;
              constant descending = true; $ Sort in descending order
              integer[max] numbers;
              integer loop, count, largest, result;
              channel link;

              $ Input numbers to sort.
              numbers[1] = 12;
              numbers[2] = 13597;
              numbers[3] = 12;
              numbers[4] = -18;
              numbers[5] = 0;

              $ Sort in descending order.
              loop = 0;
              while (loop < max - 1)
              {
                count = 1;
                while (count < max)
                {
                  Sort(descending, 
                       reference numbers[count], 
                       reference numbers[count + 1]);
                  count = count + 1;
                }

                loop = loop + 1;
              }

              $ Largest number after sorting should be the first one in the array.
              largest = numbers[1]; $ Should be equal to 13597 at this point.
  
              $ Feed the largest number to the parallel function.
              open link;
              parallel Node(largest, link);
              receive result -> link;

              write result;         $ Result should be the sum of squares of 13597.
            }
            ";

        // Expected output of the program: Sum of sqaures of the digits of 13597.
        string expected = $"165{Environment.NewLine}";

        // Validate that the program produces the expected output.
        Validate(Code, string.Empty, expected);
    }

    /// <summary>
    /// Tests procedures and operators end-to-end.
    /// </summary>
    [TestMethod]
    public void TestProceduresAndOperatorsEndToEnd()
    {
        // Sample program that exercises a few language constructs like procedures and operators.
        const string Code = @"
            {
              $ - Program to exercise a few language constructs like procedures and operators.

              @ [boolean result] Invert(boolean value)
              {
                result = !value;
              }

              @ [integer result] Double(integer value)
              {
                result = value * 2;
              }

              @ Process()
              {
                integer i, j;
                boolean b, c;
                constant max = 5;
                constant min = -max;

                read i, b, c;
                randomize j;
                i = i ^ 2;
                i = i + 1 - 1;
                i = i * (3 % 2);
                i = i / 1;

                b = b & true;
                b = b | false;
                b = (j != j) | (b & (3 < 4) & (3 > 2) & (3 == 3) & (3 != 4) & (3 <= 4) & (3 >= 2));
                b = Invert(b);

                write i, b, Invert(c), Double(i);
              }

              Process();
            }
            ";

        // Input to the program.
        string input = $"15{Environment.NewLine}true{Environment.NewLine}false{Environment.NewLine}";

        // Expected output of the program.
        string expected = $"225{Environment.NewLine}false{Environment.NewLine}true{Environment.NewLine}450{Environment.NewLine}";

        // Validate that the program produces the expected output.
        Validate(Code, input, expected);
    }

    /// <summary>
    /// Validates that the given code produces the expected output.
    /// </summary>
    /// <param name="code">The program being tested</param>
    /// <param name="input">The input for the program</param>
    /// <param name="expected">The expected output</param>
    private static void Validate(string code, string input, string expected)
    {
        // Intermediate code file.
        string intermediateFilename = new Random().Next().ToString() + ".sachin";

        // Feed the code to the parser.
        Parser parser = new Parser();
        TextReader codeStream = new StringReader(code);
        bool compiled = parser.Compile(codeStream, intermediateFilename);

        // Validate that the compilation succeeded.
        Assert.IsTrue(compiled);

        if (compiled)
        {
            // Feed the intermediate code file into the Runtime.
            using (TextReader reader = new StringReader(input))
            {
                StringBuilder stringBuilder = new StringBuilder();
                using TextWriter writer = new StringWriter(stringBuilder);
                Interpreter interpreter = new Interpreter(reader, writer);
                interpreter.RunProgram(intermediateFilename);
                string result = stringBuilder.ToString();

                // Validate that the output of the program is as expected.
                Assert.AreEqual(result, expected);
            }

            // Cleanup by deleting the intermediate code file.
            File.Delete(intermediateFilename);
        }
    }
}
