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
    /// Runs an end-to-end sanity test.
    /// </summary>
    [TestMethod]
    public void TestBasicEndToEnd()
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

        // Intermediate code file.
        string intermediateFilename = new Random().Next().ToString() + ".sachin";

        // Feed the code to the parser.
        Parser parser = new Parser();
        TextReader reader = new StringReader(Code);
        bool compiled = parser.Compile(reader, intermediateFilename);

        // Validate that the compilation succeeded.
        Assert.IsTrue(compiled);

        if (compiled)
        {
            // Feed the intermediate code file into the Runtime.
            StringBuilder stringBuilder = new StringBuilder();
            using (TextWriter writer = new StringWriter(stringBuilder))
            {
                Interpreter interpreter = new Interpreter(writer);
                interpreter.RunProgram(intermediateFilename);
                string result = stringBuilder.ToString();

                // Validate that the output of the program is as expected.
                const string Expected = "165\r\n"; // Sum of sqaures of the digits of 13597.
                Assert.AreEqual(result, Expected);
            }

            // Cleanup by deleting the intermediate code file.
            File.Delete(intermediateFilename);
        }
    }
}
