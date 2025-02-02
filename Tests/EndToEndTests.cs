﻿/******************************************************************************
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
        const string Expected = "165\r\n";

        // Validate that the program produces the expected output.
        Validate(Code, string.Empty, Expected);
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

              @ Process()
              {
                integer i;
                boolean b;

                read i, b;
                i = i ^ 2;
                i = i + 1 - 1;
                i = i * 1;
                i = i / 1;

                b = b & true;
                b = b | false;
                b = Invert(b);

                write i, b;
              }

              Process();
            }
            ";

        // Input to the program.
        const string Input = "15\r\ntrue\r\n";

        // Expected output of the program.
        const string Expected = "225\r\nfalse\r\n";

        // Validate that the program produces the expected output.
        Validate(Code, Input, Expected);
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
