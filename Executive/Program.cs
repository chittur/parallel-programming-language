/******************************************************************************
 * Filename    = Program.cs
 *
 * Author      = Ramaswamy Krishnan-Chittur
 *
 * Product     = Compiler
 * 
 * Project     = Executive
 *
 * Description = Defines the executive class.
 *****************************************************************************/

using System;
using System.IO;
using Compilation;
using Runtime;

namespace Executive;

/// <summary>
/// The executive class.
/// </summary>
internal class Program
{
    /// <summary>
    /// Prints the help message.
    /// </summary>
    private static void PrintHelp()
    {
        Console.WriteLine("{0}{1}", "help".PadRight(35), ": Prints help message.\n");

        Console.WriteLine("{0}{1}{2}{3}{4}{5}",
                          "compile <filename>".PadRight(35),
                          ": Compiles the specified source file\n",
                          string.Empty.PadRight(35),
                          "  and generates an intermediate code file\n",
                          string.Empty.PadRight(35),
                          "  with name <filename>.sachin.\n");

        Console.WriteLine("{0}{1}",
                          "run <filename>".PadRight(35),
                          ": Runs the specified intermediate code file.\n");

        Console.WriteLine("{0}{1}{2}{3}{4}{5}",
                          "execute <filename>".PadRight(35),
                          ": Compiles the specified source file,\n",
                          string.Empty.PadRight(35),
                          "  and executes it provided there are no\n",
                          string.Empty.PadRight(35),
                          "  compilation errors.\n");
    }

    /// <summary>
    /// Prints the error message for invalid commands.
    /// </summary>
    private static void PrintInvalidCommand()
    {
        Console.WriteLine("The command line arguments provided are invalid.\n");
        Program.PrintHelp();
    }

    /// <summary>
    /// Runs the intermediate code.
    /// </summary>
    /// <param name="filename">The intermediate code file.</param>
    private static void Run(string filename)
    {
        Interpreter interpreter = new Interpreter(Console.In, Console.Out);
        interpreter.RunProgram(filename);
    }

    /// <summary>
    /// Compiles and optionally runs the program.
    /// </summary>
    /// <param name="filename">The source file.</param>
    /// <param name="run">Run the program?</param>
    private static void CompileAndOptionallyRun(string filename, bool run)
    {
        using FileStream stream = new FileStream(filename,
                                                  FileMode.Open,
                                                  FileAccess.Read);
        using (StreamReader reader = new StreamReader(stream))
        {
            Parser parser = new Parser();
            bool success = parser.Compile(reader, filename + ".sachin");

            if (success)
            {
                if (run)
                {
                    Program.Run(filename + ".sachin");
                }
            }
            else
            {
                Console.WriteLine("The compilation had failure(s).");
            }

            reader.Close();
        }

        stream.Close();
    }

    /// <summary>
    /// The entry point for this program.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    private static void Main(string[] args)
    {
        try
        {
            if (args.Length == 1)
            {
                if (args[0].Equals("/?") ||
                    args[0].Equals("-?") ||
                    args[0].Equals("?") ||
                    args[0].ToLower().Equals("help"))
                {
                    Program.PrintHelp();
                }
                else
                {
                    Program.PrintInvalidCommand();
                }
            }
            else if (args.Length == 2)
            {
                switch (args[0].ToLower())
                {
                    case "compile":
                        {
                            Program.CompileAndOptionallyRun(args[1], false);
                            break;
                        }

                    case "run":
                        {
                            Program.Run(args[1]);
                            break;
                        }

                    case "execute":
                        {
                            Program.CompileAndOptionallyRun(args[1], true);
                            break;
                        }

                    default:
                        {
                            Program.PrintInvalidCommand();
                            break;
                        }
                }
            }
            else
            {
                Program.PrintInvalidCommand();
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.Message);
        }
    }
}
